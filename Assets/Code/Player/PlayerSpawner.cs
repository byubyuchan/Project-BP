using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    public static PlayerSpawner instance;

    [Header("Player Prefabs (Resources 폴더 내의 이름과 일치해야 함)")]
    [SerializeField] private GameObject[] playerPrefabs;

    protected GameObject player;

    [SerializeField]
    private GameObject canvas;

    [SerializeField]
    private Transform[] spawnZones;

    [Header("Camera Settings")]
    [SerializeField]
    private GameObject ghostCamera;
    private GameObject currentCamera;

    [Header("Dead Effect Settings")]
    [SerializeField] private int effectIndex;

    [SerializeField] private float offset = 30f;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        StartCoroutine(SpawnWhenReady());
    }

    IEnumerator SpawnWhenReady()
    {
        float timeout = 5f;
        while (!PhotonNetwork.InRoom && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (PhotonNetwork.InRoom)
        {
            SpawnPlayer();
            if (canvas != null) canvas.SetActive(false);
        }
        else
        {
            Debug.LogError("방 입장 대기 시간 초과! 네트워크 연결을 확인하세요.");
        }
    }
    public void RequestRespawn(GameObject player, float delay)
    {
        StartCoroutine(RespawnRoutine(player, delay));
    }

    private IEnumerator RespawnRoutine(GameObject player, float delay)
    {
        if (ghostCamera != null && currentCamera == null)
        {
            Vector3 spawnPosition = player.transform.position + (player.transform.forward * 40f) + (Vector3.up * 40f);
            Vector3 lookDirection = player.transform.position - spawnPosition;
            Quaternion spawnRotation = Quaternion.LookRotation(lookDirection);

            currentCamera = Instantiate(ghostCamera, spawnPosition, spawnRotation);
        }

        EffectManager.Instance.RequestExplosion(effectIndex, player.transform.position);
        PhotonNetwork.Destroy(player);

        ColorGrading colorGrading = null;

        if (currentCamera != null)
        {
            PostProcessVolume volume = currentCamera.GetComponent<PostProcessVolume>();
            if (volume != null && volume.profile != null)
            {
                volume.profile.TryGetSettings(out colorGrading);
            }
        }

        float elapsedTime = 0f;

        BaseGameManager manager = Object.FindFirstObjectByType<BaseGameManager>();

        while (elapsedTime < delay)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / delay;

            if (colorGrading != null)
            {
                colorGrading.saturation.value = Mathf.Lerp(-100f, -25f, t);
            }

            int remainTime = Mathf.CeilToInt(delay - elapsedTime);

            // 사망 카운트다운 UI가 없는 배틀로얄 씬에서도 부활 루틴이 중단되지 않게 합니다.
            if (manager != null && manager.messageText != null)
            {
                manager.ShowMessageAnother($"{remainTime}");
            }

            yield return null;
        }

        // 메시지 UI가 연결된 씬에서만 카운트다운을 숨깁니다.
        if (manager != null && manager.messageText != null)
        {
            manager.messageText.gameObject.SetActive(false);
        }

        if (playerPrefabs == null || playerPrefabs.Length == 0)
        {
            Debug.LogError("[PlayerSpawner] 리스폰 실패! Player Prefabs 배열이 비어있습니다. 인스펙터를 확인하세요.");
            yield break;
        }

        if (currentCamera != null)
        {
            Destroy(currentCamera);
        }

        ReSpawn();
    }

    public void SpawnPlayer()
    {
        if (playerPrefabs == null || playerPrefabs.Length == 0) return;

        if (PhotonNetwork.InRoom)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject p in players)
            {
                PhotonView pv = p.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    PhotonNetwork.Destroy(p);
                    Spawn();
                    return;
                }
            }
            Spawn();
        }
    }

    public void Spawn()
    {
        if (playerPrefabs == null || playerPrefabs.Length == 0) return;

        int randomPrefabIndex = Random.Range(0, playerPrefabs.Length);
        GameObject selectedPrefab = playerPrefabs[randomPrefabIndex];

        Vector3 spawnPos = transform.position;
        Quaternion spawnRot = transform.rotation;

        BattleRoyalGameManager battleRoyalGameManager = BattleRoyalGameManager.Instance;

        if (TryGetRandomSpawnZone(out Vector3 randomSpawnPosition, out Quaternion randomSpawnRotation))
        {
            spawnPos = randomSpawnPosition;
            spawnRot = randomSpawnRotation;
        }

        if (BattleRoyalGameManager.Instance == null)
        {
            AddRandomSpawnOffset(ref spawnPos);
        }

        player = PhotonNetwork.Instantiate(selectedPrefab.name, spawnPos, spawnRot);

        if (battleRoyalGameManager != null)
        {
            ApplyBattleRoyalSpawnProtection(player, battleRoyalGameManager.respawnInvincibleDuration);
        }
    }

    public void ReSpawn()
    {
        if (playerPrefabs == null || playerPrefabs.Length == 0)
        {
            Debug.LogError("[PlayerSpawner] 리스폰 실패! Player Prefabs 배열이 비어있습니다. 인스펙터를 확인하세요.");
            return;
        }

        int randomPrefabIndex = Random.Range(0, playerPrefabs.Length);
        Vector3 spawnPos = transform.position;
        Quaternion spawnRot = transform.rotation;

        BattleRoyalGameManager battleRoyalGameManager = BattleRoyalGameManager.Instance;

        // 배틀로얄은 체크포인트를 사용하지 않고 등록된 스폰 지점 중 하나를 무작위로 선택합니다.
        if (battleRoyalGameManager != null)
        {
            if (TryGetRandomSpawnZone(out Vector3 battleSpawnPosition, out Quaternion battleSpawnRotation))
            {
                spawnPos = battleSpawnPosition;
                spawnRot = battleSpawnRotation;
            }
            else
            {
                Debug.LogWarning("[PlayerSpawner] 배틀로얄 스폰 지점이 없어 PlayerSpawner 위치에서 부활합니다.");
            }
        }
        else
        {
            // 달리기 모드는 기존 체크포인트 부활 규칙을 그대로 유지합니다.
            BaseGameManager gameManager = Object.FindFirstObjectByType<BaseGameManager>();
            if (gameManager != null &&
                gameManager.GetBestRespawnPoint(out Vector3 targetPos, out Quaternion targetRot))
            {
                spawnPos = targetPos;
                spawnRot = targetRot;
            }
            else if (TryGetRandomSpawnZone(out Vector3 randomSpawnPosition, out Quaternion randomSpawnRotation))
            {
                spawnPos = randomSpawnPosition;
                spawnRot = randomSpawnRotation;
            }
        }

        // 배틀로얄 부활 위치에도 랜덤 오프셋을 더하지 않고 안전한 스폰 지점을 그대로 사용합니다.
        if (battleRoyalGameManager == null)
        {
            AddRandomSpawnOffset(ref spawnPos);
        }

        player = PhotonNetwork.Instantiate(playerPrefabs[randomPrefabIndex].name, spawnPos, spawnRot);

        // 배틀로얄에서 부활한 캐릭터에게 설정된 시간만큼 무적을 적용합니다.
        if (battleRoyalGameManager != null)
        {
            ApplyBattleRoyalSpawnProtection(player,battleRoyalGameManager.respawnInvincibleDuration);
        }
    }

    // 인스펙터에 등록된 스폰 지점 중 하나의 위치와 회전을 무작위로 반환합니다.
    private bool TryGetRandomSpawnZone(out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (spawnZones == null || spawnZones.Length == 0) return false;

        int index = Random.Range(0, spawnZones.Length);
        Transform spawnZone = spawnZones[index];
        if (spawnZone == null) return false;

        spawnPosition = spawnZone.position;
        spawnRotation = spawnZone.rotation;
        return true;
    }

    // 기존 모드에서 사용하던 스폰 위치 분산값을 한곳에서 적용합니다.
    private void AddRandomSpawnOffset(ref Vector3 spawnPosition)
    {
        spawnPosition.z += Random.Range(-offset, offset);
        spawnPosition.x += Random.Range(-offset, offset);
    }

    // 부활한 캐릭터에 런타임 무적 컴포넌트를 연결하여 프리팹 수정을 최소화합니다.
    private void ApplyBattleRoyalSpawnProtection(GameObject spawnedPlayer, float duration)
    {
        if (spawnedPlayer == null) return;

        BRController protection =
            spawnedPlayer.GetComponent<BRController>();

        if (protection == null)
        {
            protection = spawnedPlayer.AddComponent<BRController>();
        }

        protection.Activate(duration);
    }

    // 회전 방향을 직접 받아옴.
    public void InstantReSpawn(Vector3 targetPos, Quaternion targetRot)
    {
        if (playerPrefabs == null || playerPrefabs.Length == 0) return;
        EffectManager.Instance.RequestExplosion(effectIndex, player.transform.position);
        PhotonNetwork.Destroy(player);

        Vector3 spawnPos = targetPos;
        spawnPos.z += Random.Range(-offset, offset);
        spawnPos.x += Random.Range(-offset, offset);

        int randomPrefabIndex = Random.Range(0, playerPrefabs.Length);
        player = PhotonNetwork.Instantiate(playerPrefabs[randomPrefabIndex].name, spawnPos, targetRot);
    }

    #region PunCallbacks 옵션
    public override void OnLeftRoom() { Debug.Log("방을 떠났습니다."); }
    public override void OnCreateRoomFailed(short returnCode, string message) { Debug.LogError($"방 생성 실패: {message}"); }
    public override void OnJoinRoomFailed(short returnCode, string message) { Debug.LogError($"방 참가 실패: {message}"); }
    #endregion
}
