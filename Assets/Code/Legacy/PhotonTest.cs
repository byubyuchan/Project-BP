using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhotonTest : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_InputField m_InputField;
    [SerializeField] private TextMeshProUGUI m_textConnectLog;
    [SerializeField] private TextMeshProUGUI m_textPlayerList;

    void Start()
    {
        Screen.SetResolution(1920, 1080, false);
        m_textConnectLog.text = "Log\n";
    }

    public override void OnConnectedToMaster()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 5;

        PhotonNetwork.LocalPlayer.NickName = m_InputField.text;
        PhotonNetwork.JoinOrCreateRoom("Room1", options, null);

    }
    public override void OnJoinedRoom()
    {
        updatePlayer();
        m_textConnectLog.text += m_InputField.text;
        m_textConnectLog.text += " 님이 방에 참가하였습니다.\n";
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        updatePlayer();
        m_textConnectLog.text += newPlayer.NickName;
        m_textConnectLog.text += " 님이 입장하였습니다.\n";
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        updatePlayer();
        m_textConnectLog.text += otherPlayer.NickName;
        m_textConnectLog.text += " 님이 퇴장하였습니다.\n";
    }

    public void Connect()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    void updatePlayer()
    {
        m_textPlayerList.text = "접속자";
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            m_textPlayerList.text += "\n";
            m_textPlayerList.text += PhotonNetwork.PlayerList[i].NickName;
        }
    }


}
