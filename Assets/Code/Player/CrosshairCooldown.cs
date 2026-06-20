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

    // 플레이어가 풀링으로 재사용되기 때문에 이 스크립트 또한 재활성화 될 때마다 초기화 필요
    void OnEnable()
    {
        FindMyPlayer();

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
        // 만약 플레이어를 잃어버렸다면(캐릭터 파괴/재생성 시) 다시 찾기 시도
        if (playerMovement == null)
        {
            FindMyPlayer();
            if (playerMovement == null) return;
        }

        //if (cooldownImage.gameObject.activeSelf)
        //{
        //    float timePassed = Time.time - playerMovement.lastAttackTime;
        //    float progress = Mathf.Clamp01(timePassed / playerMovement.attackCooldown);
        //    cooldownImage.fillAmount = progress;

        //    Color appliedColor = customBaseColor;
        //    appliedColor.a = (progress < 1f) ? 0.3f : 1.0f;

        //    cooldownImage.color = appliedColor;
        //}
        //else
        //{
        //    float timePassed = Time.time - playerMovement.lastAttackTime;
        //    float progress = Mathf.Clamp01(timePassed / playerMovement.attackCooldown);
        //    Color appliedColor = customBaseColor;
        //    appliedColor.a = (progress < 1f) ? 0.3f : 1.0f;
        //    foreach (var img in images)
        //    {
        //        if (img != null && img.gameObject.activeInHierarchy) img.color = appliedColor;
        //    }
        //}

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