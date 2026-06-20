using UnityEngine;
using TMPro;
using Photon.Chat;
using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class LobbySocialManager : MonoBehaviour, IChatClientListener
{
    [Header("Chat UI")]
    public TMP_InputField chatInput;
    public GameObject chatTextPrefab;
    public Transform chatContent;
    public ScrollRect chatScrollRect;

    [Header("User List UI")]
    public Transform userContent;
    public GameObject userSlotPrefab;

    [Header("UI Controller Reference")]
    public RightPanelController rightPanelController;

    private ChatClient chatClient;
    private string lobbyChannelName = "Global_Lobby";
    private string myChatUserId;

    private void Start()
    {
        if (!PhotonNetwork.NickName.Contains("#"))
        {
            string uniquePUNId = PhotonNetwork.LocalPlayer?.UserId;

            int hash = 0;

            if (!string.IsNullOrEmpty(uniquePUNId))
            {
                hash = Mathf.Abs(uniquePUNId.GetHashCode());
            }
            else
            {
                hash = Random.Range(0, 10000);
            }

            string tagId = (hash % 10000).ToString("D4");

            PhotonNetwork.NickName = PhotonNetwork.NickName + "#" + tagId;
        }

        myChatUserId = PhotonNetwork.NickName;

        if (rightPanelController != null)
        {
            rightPanelController.UpdateDisplayName(FormatNameWithColor(myChatUserId));
        }

        chatClient = new ChatClient(this);
        chatClient.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, "1.0", new AuthenticationValues(myChatUserId));

        if (chatInput != null)
        {
            chatInput.onSubmit.AddListener(OnSubmitChat);
        }
    }

    private string FormatNameWithColor(string rawName)
    {
        int hashIndex = rawName.IndexOf('#');
        if (hashIndex >= 0)
        {
            string namePart = rawName.Substring(0, hashIndex);
            string tagPart = rawName.Substring(hashIndex);

            return $"{namePart}<color=#CCCCCC>{tagPart}</color>";
        }
        return rawName;
    }

    private void Update()
    {
        if (chatClient != null)
        {
            chatClient.Service();
        }
    }

    private void OnSubmitChat(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            StartCoroutine(RefocusInputField());
            return;
        }

        chatClient.PublishMessage(lobbyChannelName, text);
        chatInput.text = "";
        StartCoroutine(RefocusInputField());
    }

    private IEnumerator RefocusInputField()
    {
        yield return null;
        chatInput.ActivateInputField();
    }

    public void OnConnected()
    {
        ChannelCreationOptions options = new ChannelCreationOptions { PublishSubscribers = true };
        chatClient.Subscribe(lobbyChannelName, 0, -1, options);
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        if (chatClient.TryGetChannel(lobbyChannelName, out ChatChannel channel))
        {
            RefreshUserList(channel.Subscribers);
        }
    }

    private void RefreshUserList(HashSet<string> subscribers)
    {
        foreach (Transform child in userContent) Destroy(child.gameObject);
        foreach (string user in subscribers) AddUserSlot(user);
    }

    private void AddUserSlot(string userName)
    {
        GameObject slot = Instantiate(userSlotPrefab, userContent);
        slot.name = userName;

        TMP_Text text = slot.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = FormatNameWithColor(userName);
        }
    }

    private void RemoveUserSlot(string userName)
    {
        foreach (Transform child in userContent)
        {
            if (child.name == userName)
            {
                Destroy(child.gameObject);
                break;
            }
        }
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            GameObject textObj = Instantiate(chatTextPrefab, chatContent);
            TMP_Text chatText = textObj.GetComponent<TMP_Text>();

            chatText.text = $"{FormatNameWithColor(senders[i])}: {messages[i]}";

            chatText.ForceMeshUpdate();
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return null;
        if (chatScrollRect != null) chatScrollRect.verticalNormalizedPosition = 0f;
    }

    public void OnUserSubscribed(string channel, string user)
    {
        if (channel == lobbyChannelName) AddUserSlot(user);
    }

    public void OnUserUnsubscribed(string channel, string user)
    {
        if (channel == lobbyChannelName) RemoveUserSlot(user);
    }

    public void DebugReturn(DebugLevel level, string message) { }
    public void OnDisconnected() { }
    public void OnChatStateChange(ChatState state) { }
    public void OnUnsubscribed(string[] channels) { }
    public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }
    public void OnPrivateMessage(string sender, object message, string channelName) { }
}