using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.UI;

public class RightPanelController : MonoBehaviour
{
    [Header("Top Info")]
    public TextMeshProUGUI playerNameText;

    [Header("Tab Buttons")]
    public Button chatButton;
    public Button friendButton;
    public Button userButton;

    [Header("Tab Panels")]
    public GameObject chatPanel;
    public GameObject friendPanel;
    public GameObject userPanel;

    private void Start()
    {
        OnChatButtonClicked();
    }

    public void UpdateDisplayName(string newName)
    {
        if (playerNameText != null)
        {
            playerNameText.text = newName;
        }
    }

    public void OnChatButtonClicked()
    {
        if (chatButton != null) chatButton.interactable = false;
        if (friendButton != null) friendButton.interactable = true;
        if (userButton != null) userButton.interactable = true;

        if (chatPanel != null) chatPanel.SetActive(true);
        if (friendPanel != null) friendPanel.SetActive(false);
        if (userPanel != null) userPanel.SetActive(false);
    }

    public void OnFriendButtonClicked()
    {
        if (chatButton != null) chatButton.interactable = true;
        if (friendButton != null) friendButton.interactable = false;
        if (userButton != null) userButton.interactable = true;

        if (chatPanel != null) chatPanel.SetActive(false);
        if (friendPanel != null) friendPanel.SetActive(true);
        if (userPanel != null) userPanel.SetActive(false);
    }

    public void OnUserButtonClicked()
    {
        if (chatButton != null) chatButton.interactable = true;
        if (friendButton != null) friendButton.interactable = true;
        if (userButton != null) userButton.interactable = false;

        if (chatPanel != null) chatPanel.SetActive(false);
        if (friendPanel != null) friendPanel.SetActive(false);
        if (userPanel != null) userPanel.SetActive(true);
    }
}