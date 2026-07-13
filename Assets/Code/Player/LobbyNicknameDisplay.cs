using Photon.Pun;
using TMPro;
using UnityEngine;

public class LobbyNicknameDisplay : MonoBehaviour
{
    [Header("닉네임 표시")]
    [SerializeField] private TMP_Text nicknameText;

    private void Start()
    {
        RefreshNickname();
    }

    // Photon에 저장된 현재 플레이어 닉네임만 화면에 표시한다.
    public void RefreshNickname()
    {
        if (nicknameText == null)
        {
            Debug.LogWarning("[LobbyNicknameDisplay] 닉네임 텍스트가 연결되지 않았습니다.");
            return;
        }

        string nickname = PhotonNetwork.NickName;

        // 닉네임을 아직 받지 못한 경우 빈 문자열 대신 기본 문구를 표시한다.
        nicknameText.text = string.IsNullOrWhiteSpace(nickname)
            ? "게스트"
            : nickname;
    }
}