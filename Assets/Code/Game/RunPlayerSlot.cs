using UnityEngine;
using TMPro;
using Photon.Realtime;
using UnityEngine.UI;

public class RunPlayerSlot : BasePlayerSlot
{
    [Header("Run UI Components")]
    public TextMeshProUGUI scoreText;
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

        // 자신의 정보는 노란색, 다른 플레이어의 정보는 흰색으로 표시합니다.
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

        // 같은 점수 UI를 달리기와 배틀로얄 모드에서 함께 사용할 수 있도록 승리 점수를 구분합니다.
        int targetScore = gameManager switch
        {
            RunGameManager runGameManager => runGameManager.winScore,
            BattleRoyalGameManager battleRoyalGameManager => battleRoyalGameManager.winScore,
            _ => 0
        };

        scoreText.text = targetScore > 0
            ? $"{score} / {targetScore}"
            : score.ToString();
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
