using UnityEngine;
using TMPro;
using Photon.Realtime;
using UnityEngine.UI;

public class RunPlayerSlot : BasePlayerSlot
{
    [Header("Run UI Components")]
    public TextMeshProUGUI scoreText; // 점수나 진행도 표시
    public TextMeshProUGUI rankText;

    public override void SetEmpty()
    {
        base.SetEmpty();
        if (backgroundImage != null) backgroundImage.enabled = false;
        if (scoreText != null) scoreText.text = "";
        if (rankText != null) rankText.text = "";
    }

    public override void Setup(Player player)
    {
        base.Setup(player);
        if (backgroundImage != null) backgroundImage.enabled = true;

        // 자기 자신은 노란색, 다른 플레이어는 흰색으로 표시
        Color textColor = player.IsLocal ? Color.yellow : Color.white;

        if (nameText != null) nameText.color = textColor;
        if (scoreText != null) scoreText.color = textColor;
        if (rankText != null) rankText.color = textColor;

        UpdateScore(0);
        UpdateRank(0);
    }

    public void UpdateScore(int score)
    {
        if (IsEmpty) return;
        scoreText.text = $"{score} / 3"; // 예: 달린 거리
    }

    public void UpdateRank(int rank)
    {
        if (rank <= 0)
        {
            rankText.text = "-";
            return;
        }

        string suffix = "th";
        int lastDigit = rank % 10;

        if (lastDigit == 1)
        {
            suffix = "st";
        }
        else if (lastDigit == 2)
        {
            suffix = "nd";
        }
        else if (lastDigit == 3)
        {
            suffix = "rd";
        }

        if (rankText != null) rankText.text = $"{rank}{suffix}";
    }
}