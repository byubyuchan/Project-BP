using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;
using Photon.Pun;

public class WarmupPlayerSlot : BasePlayerSlot
{
    [Header("Warmup UI Components")]
    public GameObject crownIcon;
    public Button optionButton;

    public override void SetEmpty()
    {
        base.SetEmpty();
        if (backgroundImage != null) backgroundImage.enabled = false;
        if (crownIcon != null) crownIcon.SetActive(false);
        if (optionButton != null) optionButton.gameObject.SetActive(false);
    }

    public override void Setup(Player player)
    {
        base.Setup(player);

        if (backgroundImage != null) backgroundImage.enabled = true;

        if (player.IsMasterClient)
        {
            if (crownIcon != null) crownIcon.SetActive(true);
            if (optionButton != null) optionButton.gameObject.SetActive(false);
        }
        else
        {
            if (crownIcon != null) crownIcon.SetActive(false);
            if (optionButton != null) optionButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }

        if (optionButton != null)
        {
            optionButton.onClick.RemoveAllListeners();
            optionButton.onClick.AddListener(OnOptionButtonClicked);
        }
    }

    private void OnOptionButtonClicked()
    {
        WarmupManager wm = gameManager as WarmupManager;
        if (wm != null && TargetPlayer != null)
        {
            wm.OpenHostOptionPanel(TargetPlayer, Input.mousePosition);
        }
    }
}
