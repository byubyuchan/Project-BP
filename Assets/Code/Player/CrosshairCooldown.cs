using Photon.Pun;
using Photon.Pun.UtilityScripts;
using UnityEngine;
using UnityEngine.UI;

// 플레이어의 공격 쿨다운을 크로스헤어 UI로 표시하는 스크립트
public class CrosshairCooldown : MonoBehaviour
{
    [Header("Dependencies")]
    public MoveByKeys playerMovement;
    public Image cooldownImage;
    private Image[] images;

    private Color customBaseColor = Color.white;
    private float maxOpacity = 1.0f;
    private CrosshairController crosshairController;

    // 플레이어가 풀링으로 재사용되기 때문에 이 스크립트 또한 재활성화 될 때마다 초기화 필요
    void OnEnable()
    {
        crosshairController = GetComponent<CrosshairController>();
        if (crosshairController == null || !crosshairController.IsPreview) FindMyPlayer();

        images = GetComponentsInChildren<Image>();

        string savedHex = PlayerPrefs.GetString("CrosshairColorHex", "#000000");
        if (ColorUtility.TryParseHtmlString(savedHex, out Color savedColor))
        {
            customBaseColor = savedColor;
        }

        maxOpacity = PlayerPrefs.GetFloat("CrosshairOpacity", 1.0f);

        BaseOptionManager.OnCrosshairColorChanged += UpdateCustomColor;
        BaseOptionManager.OnCrosshairOpacityChanged += UpdateMaxOpacity;
    }

    void OnDisable()
    {
        BaseOptionManager.OnCrosshairColorChanged -= UpdateCustomColor;
        BaseOptionManager.OnCrosshairOpacityChanged -= UpdateMaxOpacity;
    }

    private void UpdateCustomColor(Color newColor)
    {
        customBaseColor = newColor;
    }

    private void UpdateMaxOpacity(float newOpacity)
    {
        maxOpacity = newOpacity;
    }

    // IsMine으로 내 플레이어를 찾아 참조
    private void FindMyPlayer()
    {
        // 풀에 반환된 이전 플레이어 참조를 먼저 제거한다.
        playerMovement = null;

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

    private bool IsValidLocalPlayer()
    {
        if (playerMovement == null || !playerMovement.gameObject.activeInHierarchy)
        {
            return false;
        }

        PhotonView pv = playerMovement.GetComponent<PhotonView>();
        return pv != null && pv.IsMine;
    }

    void Update()
    {
        // 설정 프리뷰는 플레이어의 공격 쿨다운/사망 상태와 무관하게
        // 저장된 외형을 그대로 보여줘야 한다.
        if (crosshairController != null && crosshairController.IsPreview) return;

        // 풀에 반환된 이전 캐릭터이거나 소유권이 바뀐 경우 새 로컬 플레이어를 찾는다.
        if (!IsValidLocalPlayer())
        {
            FindMyPlayer();
            if (!IsValidLocalPlayer()) return;
        }

        float timePassed = Time.time - playerMovement.lastAttackTime;
        float progress = Mathf.Clamp01(timePassed / playerMovement.attackCooldown);

        // 유저가 설정한 최대 투명도(maxOpacity)를 기준으로 계산합니다
        // 쿨타임 중일 때는 설정된 투명도의 30%만 보여주고, 쿨타임이 다 차면 설정된 투명도(100%)로 보여줍니다
        float currentAlpha = (progress < 1f) ? (maxOpacity * 0.3f) : maxOpacity;

        Color appliedColor = customBaseColor;
        appliedColor.a = currentAlpha;

        if (cooldownImage != null && cooldownImage.gameObject.activeSelf)
        {
            cooldownImage.fillAmount = progress;
            cooldownImage.color = appliedColor;
        }
        else
        {
            if (images != null)
            {
                foreach (var img in images)
                {
                    if (img != null && img.gameObject.activeInHierarchy) img.color = appliedColor;
                }
            }
        }
    }
}
