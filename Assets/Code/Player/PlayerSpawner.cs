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

            manager.ShowMessageAnother($"{remainTime}");

            yield return null;
        }

        manager.messageText.gameObject.SetActive(false);

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

        if (spawnZones.Length > 0)
        {
            int index = Random.Range(0, spawnZones.Length);
            spawnPos = spawnZones[index].position;
            spawnRot = spawnZones[index].rotation;
        }

        spawnPos.z += Random.Range(-offset, offset);
        spawnPos.x += Random.Range(-offset, offset);
        player = PhotonNetwork.Instantiate(selectedPrefab.name, spawnPos, spawnRot);
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

        // 체크포인트가 있는지 확인
        BaseGameManager gameManager = Object.FindFirstObjectByType<BaseGameManager>();
        if (gameManager != null && gameManager.GetBestRespawnPoint(out Vector3 targetPos, out Quaternion targetRot))
        {
            spawnPos = targetPos;
            spawnRot = targetRot;
        }
        // 부활장소가 랜덤인 모드인지 확인
        else if(spawnZones.Length > 0)
        {
            int index = Random.Range(0, spawnZones.Length);
            spawnPos = spawnZones[index].position;
            spawnRot = spawnZones[index].rotation;
        }

        spawnPos.z += Random.Range(-offset, offset);
        spawnPos.x += Random.Range(-offset, offset);

        player = PhotonNetwork.Instantiate(playerPrefabs[randomPrefabIndex].name, spawnPos, spawnRot);
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