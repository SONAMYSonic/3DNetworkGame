using TMPro;
using UnityEngine;

public class UI_ScoreItem : MonoBehaviour
{
    public TMP_Text RankTextUI;
    public TMP_Text NicknameTextUI;
    public TMP_Text ScoreTextUI;

    public void Set(int rank, string nickname, int score)
    {
        RankTextUI.text = rank.ToString();
        NicknameTextUI.text = nickname;
        ScoreTextUI.text = $"{score:N0}";
    }

    public void Clear()
    {
        RankTextUI.text = "";
        NicknameTextUI.text = "";
        ScoreTextUI.text = "";
    }
}
