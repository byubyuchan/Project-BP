using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;

public class RoomSlot : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI modeText;
    public TextMeshProUGUI playerText;
    public GameObject lockIcon;

    public void SetInfo(RoomInfo info)
    {
        roomNameText.text = info.CustomProperties["roomName"].ToString();

        if (info.CustomProperties.ContainsKey("mode"))
            modeText.text = (string)info.CustomProperties["mode"];
        else
            modeText.text = "Default";

        playerText.text = $"{info.PlayerCount} / {info.MaxPlayers}";

        bool isPrivate = false;
        if (info.CustomProperties.ContainsKey("isPrivate"))
            isPrivate = (bool)info.CustomProperties["isPrivate"];

        lockIcon.SetActive(isPrivate);

        GetComponent<Button>().interactable = true;
    }

    public void ClearSlot()
    {
        roomNameText.text = "";
        modeText.text = "";
        playerText.text = "";
        lockIcon.SetActive(false);
        GetComponent<Button>().interactable = false;
    }
}