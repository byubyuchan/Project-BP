using Photon.Pun;
using Photon.Pun.UtilityScripts;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CrosshairVisualSync))]
public class CrosshairCooldown : MonoBehaviour
{
    [Header("Dependencies")]
    public MoveByKeys playerMovement;

    [Tooltip("여기에 Circle 오브젝트의 Image 컴포넌트를 할당해주세요!")]
    public Image cooldownImage;

    private CrosshairVisualSync visualSync;

    void Awake()
    {
        visualSync = GetComponent<CrosshairVisualSync>();
    }

    void OnEnable()
    {
        FindMyPlayer();
    }

    private void FindMyPlayer()
    {
        var allPlayers = FindObjectsByType<MoveByKeys>(FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                playerMovement = p;
                break;
            }
        }
    }

    void Update()
    {
        if (playerMovement == null)
        {
            FindMyPlayer();
        }

        float progress = 1f;

        if (playerMovement != null)
        {
            float timePassed = Time.time - playerMovement.lastAttackTime;
            progress = Mathf.Clamp01(timePassed / playerMovement.attackCooldown);
        }

        if (cooldownImage != null && cooldownImage.gameObject.activeInHierarchy)
        {
            cooldownImage.fillAmount = progress;
        }

        if (visualSync != null)
        {
            float alphaMultiplier = (progress < 1f) ? 0.3f : 1.0f;
            visualSync.ApplyCooldownAlphaMode(alphaMultiplier);
        }
    }
}