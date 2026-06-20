using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;

public abstract class BasePlayerSlot : MonoBehaviour
{
    [Header("Base UI Components")]
    public Image backgroundImage;
    public TextMeshProUGUI nameText;

    public Player TargetPlayer { get; protected set; }
    public bool IsEmpty { get; protected set; } = true;

    protected BaseGameManager gameManager;

    public virtual void Init(BaseGameManager manager)
    { 
        gameManager = manager;
    }

    public virtual void SetEmpty()
    {
        TargetPlayer = null;
        IsEmpty = true;
        if (nameText != null) nameText.text = "";
    }

    public virtual void Setup(Player player)
    {
        TargetPlayer = player;
        IsEmpty = false;
        if (nameText != null) nameText.text = player.NickName;
    }
}
