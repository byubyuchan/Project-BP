using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class DebugSceneManager : MonoBehaviourPunCallbacks
{
    [Header("테스트할 캐릭터 프리팹 이름 (Resources 폴더 기준)")]
    public string playerPrefabName = "Player_EarthQuake";

    [Header("테스트할 캐릭터 배열")]
    public GameObject[] playerPrefabs;

    [Header("스폰 위치")]
    public Transform spawnPoint;

    [Header("캐릭터 이름 입력 UI")]
    public TextMeshProUGUI txt;

    public GameObject currentPlayer;

    private bool isEnteringPracticeRoom;

    private void Start()
    {
        // 이전 씬에서 멈춘 Photon 콜백을 연습장 진입 시 다시 활성화합니다.
        PhotonNetwork.IsMessageQueueRunning = true;

        // 현재 연결 상태에 맞춰 서버 연결, 연습방 생성 또는 캐릭터 생성을 진행합니다.
        EnterPracticeRoom();
    }

    // 메인 로비에서 이미 서버에 연결된 상태도 처리하는 연습방 입장 함수입니다.
    private void EnterPracticeRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            SpawnPlayer();
            return;
        }

        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("<color=yellow>[연습장]</color> Photon 서버 연결을 시도합니다.");

            if (string.IsNullOrWhiteSpace(PhotonNetwork.NickName))
            {
                PhotonNetwork.NickName = "Tester_" + Random.Range(100, 999);
            }

            PhotonNetwork.ConnectUsingSettings();
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady &&
            PhotonNetwork.Server == ServerConnection.MasterServer)
        {
            CreatePracticeRoom();
        }
    }

    // 다른 플레이어가 검색하거나 입장하지 못하는 1인 연습방을 생성합니다.
    private void CreatePracticeRoom()
    {
        if (isEnteringPracticeRoom || PhotonNetwork.InRoom)
        {
            return;
        }

        isEnteringPracticeRoom = true;

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 1,
            IsOpen = false,
            IsVisible = false
        };

        Debug.Log("<color=yellow>[연습장]</color> 1인 연습방을 생성합니다.");
        PhotonNetwork.CreateRoom(null, roomOptions, TypedLobby.Default);
    }

    public override void OnConnectedToMaster()
    {
        // 새로 연결된 경우와 메인 로비에서 이미 연결된 경우가 같은 입장 로직을 사용합니다.
        EnterPracticeRoom();
    }

    public override void OnJoinedRoom()
    {
        // 방 입장이 끝났으므로 중복 요청 방지 상태를 해제합니다.
        isEnteringPracticeRoom = false;
        Debug.Log("<color=yellow>[연습장]</color> 연습방 입장이 완료되어 캐릭터를 생성합니다.");

        if (currentPlayer == null)
        {
            SpawnPlayer();
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        // 생성 실패 시 다시 시도할 수 있도록 요청 상태를 해제하고 원인을 출력합니다.
        isEnteringPracticeRoom = false;
        Debug.LogError($"<color=red>[연습장]</color> 연습방 생성 실패: {message} ({returnCode})");
    }

    public void SpawnPlayer()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[연습장] 방에 입장하지 않아 캐릭터를 생성할 수 없습니다.");
            return;
        }

        if (playerPrefabs == null || playerPrefabs.Length == 0)
        {
            Debug.LogWarning("[연습장] 랜덤 캐릭터 프리팹이 등록되지 않았습니다.");
            return;
        }

        // 기존 캐릭터가 있으면 제거하고, 최초 생성이라면 아무 일도 하지 않습니다.
        DestroyPlayer();
        SpawnRandomPlayer();
    }

    // 함수 이름만 보고 역할을 알 수 있도록 랜덤 생성 함수임을 명시합니다.
    private void SpawnRandomPlayer()
    {
        Vector3 position =
            spawnPoint != null ? spawnPoint.position : Vector3.zero;

        Quaternion rotation =
            spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        int randomIndex = Random.Range(0, playerPrefabs.Length);
        GameObject selectedPrefab = playerPrefabs[randomIndex];

        if (selectedPrefab == null)
        {
            Debug.LogWarning("[연습장] 선택된 랜덤 캐릭터 프리팹이 비어 있습니다.");
            return;
        }

        currentPlayer = PhotonNetwork.Instantiate(
            selectedPrefab.name,
            position,
            rotation
        );
    }

    // Lagacy
    public void SpawnPlayerToName()
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        string targetPrefab = playerPrefabName;

        if (txt != null)
        {
            string cleanedText = txt.text.Trim('\u200B', ' ', '\n', '\r');

            if (cleanedText.Length > 0)
            {
                targetPrefab = cleanedText;
            }
        }

        if (Resources.Load<GameObject>(targetPrefab) == null)
        {
            Debug.LogWarning($"<color=orange>[경고]</color> '{targetPrefab}' 프리팹을 찾을 수 없습니다. 이름을 확인해주세요.");
            return;
        }

        Debug.Log($"<color=green>[생성]</color> 캐릭터 생성 시도: [{targetPrefab}]");

        DestroyPlayer();
        currentPlayer = PhotonNetwork.Instantiate(targetPrefab, pos, rot);

        MoveByKeys moveByKeys = currentPlayer.GetComponent<MoveByKeys>();
        if (moveByKeys != null)
        {
            moveByKeys.isInvincible = false;
        }
    }


    public void DestroyPlayer()
    {
        if (currentPlayer == null)
        {
            return;
        }

        PhotonView photonView = currentPlayer.GetComponent<PhotonView>();

        // (변경) 자신이 소유한 네트워크 캐릭터만 제거합니다.
        if (photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(currentPlayer);
        }

        currentPlayer = null;
    }
}
