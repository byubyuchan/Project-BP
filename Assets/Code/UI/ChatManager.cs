using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChatManager : MonoBehaviourPunCallbacks
{
    public static ChatManager Instance;

    [Header("Chat UI")]
    public TMP_InputField chatInput;
    public Transform content;
    public GameObject chatPrefab;
    public ScrollRect chatScroolRect;

    //[Header("Chat Backgrounds")]
    //public Image chatPanelImage;
    //public Image scrollViewImage;

    [Header("Chat Window")]
    public RectTransform chatWindowRect;

    [Header("Preview Chat UI")]
    public GameObject previewPanel;
    public Transform previewContent;
    public ScrollRect previewScrollRect;
    public float previewShowTime = 3f;
    public float previewFadeTime = 1f;
    public int maxPreviewCount = 4;

    private bool isChatActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SetChatUIActive(false);
        AddSystemMessage("You have joined the room.");
    }

    public void ToggleChat()
    {
        if (!isChatActive)
        {
            SetChatUIActive(true);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(chatInput.text))
            {
                SendChatMessage();
            }
            SetChatUIActive(false);

            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void SetChatUIActive(bool isActive)
    {
        isChatActive = isActive;

        if (chatWindowRect != null)
        {
            chatWindowRect.gameObject.SetActive(isActive);
        }
        //else
        //{
        //    chatInput.gameObject.SetActive(isActive);
        //    chatScroolRect.gameObject.SetActive(isActive);
        //    if (chatPanelImage != null) chatPanelImage.enabled = isActive;
        //    if (scrollViewImage != null) scrollViewImage.enabled = isActive;
        //}

        if (previewPanel != null) previewPanel.SetActive(!isActive);

        if (isActive)
        {
            chatInput.text = "";
            chatInput.ActivateInputField();
        }
        else
        {
            chatInput.text = "";
        }
    }

    public void SendChatMessage()
    {
        // 완성된 메시지에 현재 입력 중인 조합 문자열을 포함하여 보냅니다
        string message = chatInput.text + Input.compositionString;

        if (!string.IsNullOrWhiteSpace(message))
        {
            string senderName = PhotonNetwork.NickName;
            photonView.RPC("ReceiveMessage", RpcTarget.All, senderName, message);
        }

        chatInput.text = "";
        chatInput.ActivateInputField();
    }

    [PunRPC]
    public void ReceiveMessage(string senderName, string message)
    {
        GameObject newChat = Instantiate(chatPrefab, content);
        TextMeshProUGUI chatText = newChat.GetComponent<TextMeshProUGUI>();
        chatText.text = $"{senderName}: {message}";
        StartCoroutine(ScrollToBottom());
        SpawnPreviewMessage($"{senderName}: {message}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        AddSystemMessage($"{newPlayer.NickName} has joined the room.");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        AddSystemMessage($"{otherPlayer.NickName} has left the room.");
    }

    private void AddSystemMessage(string message)
    {
        GameObject newChat = Instantiate(chatPrefab, content);
        TextMeshProUGUI chatText = newChat.GetComponent<TextMeshProUGUI>();
        chatText.text = $"<color=yellow>{message}</color>";
        StartCoroutine(ScrollToBottom());
        SpawnPreviewMessage($"<color=yellow>{message}</color>");
    }

    private void SpawnPreviewMessage(string message)
    {
        if (previewContent == null) return;

        if (previewContent.childCount >= maxPreviewCount)
        {
            Destroy(previewContent.GetChild(0).gameObject);
        }

        GameObject newPreview = Instantiate(chatPrefab, previewContent);
        TextMeshProUGUI previewText = newPreview.GetComponent<TextMeshProUGUI>();
        previewText.text = message;

        StartCoroutine(FadeOutAndDestroy(previewText));

        if (previewScrollRect != null)
        {
            StartCoroutine(ScrollPreviewToBottom());
        }
    }

    private IEnumerator FadeOutAndDestroy(TextMeshProUGUI textComponent)
    {
        yield return new WaitForSeconds(previewShowTime);

        float timer = 0f;
        Color originalColor = textComponent.color;

        while (timer < previewFadeTime)
        {
            if (textComponent == null) yield break;

            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / previewFadeTime);
            textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        if (textComponent != null)
        {
            Destroy(textComponent.gameObject);
        }
    }

    IEnumerator ScrollToBottom()
    {
        yield return null; // Wait for the end of the frame to text to calculate its size
        yield return null; // Wait another frame to content size to expand its size

        LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());

        chatScroolRect.verticalNormalizedPosition = 0f;
    }

    public IEnumerator ScrollPreviewToBottom()
    {
        yield return null;
        yield return null;

        LayoutRebuilder.ForceRebuildLayoutImmediate(previewContent.GetComponent<RectTransform>());

        previewScrollRect.verticalNormalizedPosition = 0f;
    }
}