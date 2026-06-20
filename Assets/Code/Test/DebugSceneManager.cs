using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class DebugSceneManager : MonoBehaviourPunCallbacks
{
    [Header("테스트할 캐릭터 프리팹 이름 (Resources 폴더 기준)")]
    public string playerPrefabName = "Player_EarthQuake";

    [Header("스폰 위치")]
    public Transform spawnPoint;

    [Header("Text")]
    public TextMeshProUGUI txt;

    public GameObject currentPlayer;

    private void Start()
    {
        // 1. 이미 연결되어 방 안에 있다면 (로비에서 넘어온 경우) 바로 스폰!
        if (PhotonNetwork.InRoom)
        {
            SpawnPlayer();
        }
        // 2. 연결 자체가 안 되어 있다면 (에디터에서 테스트 씬을 바로 재생한 경우)
        else if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("<color=yellow>[Debug]</color> 포톤 서버에 다이렉트 연결을 시도합니다...");
            PhotonNetwork.LocalPlayer.NickName = "Tester_" + Random.Range(100, 999);
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // 3. 서버 접속 완료 시 
    public override void OnConnectedToMaster()
    {
        Debug.Log("<color=yellow>[Debug]</color> 마스터 서버 접속 완료! 디버그 룸에 입장합니다.");
        
        // JoinOrCreateRoom: 방이 없으면 만들고, 있으면 그냥 들어가는 개꿀 함수
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 4 };
        PhotonNetwork.JoinOrCreateRoom("Debug_Test_Room", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("<color=yellow>[Debug]</color> 디버그 룸 입장 완료! 초기 캐릭터를 자동 스폰합니다.");

        // 방에 들어오자마자 딱 한 번 깔끔하게 소환!
        if (currentPlayer == null)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
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
            Debug.LogWarning($"<color=orange>[Warning]</color> '{targetPrefab}' 프리팹을 찾을 수 없습니다! 오타를 확인해주세요.");
            return;
        }

        Debug.Log($"<color=green>[Spawn]</color> 소환 시도: [{targetPrefab}]");

        DestroyPlayer();
        currentPlayer = PhotonNetwork.Instantiate(targetPrefab, pos, rot);
    }

    public void DestroyPlayer()
    {
        if (currentPlayer != null)
        {
            // 포톤 네트워크 상에서 완벽하게 파괴
            PhotonNetwork.Destroy(currentPlayer);
            currentPlayer = null;
            Debug.Log("<color=red>[Destroy]</color> 기존 캐릭터 파괴 완료!");
        }
    }
}