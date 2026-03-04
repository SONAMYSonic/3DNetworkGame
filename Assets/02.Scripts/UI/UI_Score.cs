using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UI_Score : MonoBehaviour
{
    private List<UI_ScoreItem> _items;

    private void Start()
    {
        _items = GetComponentsInChildren<UI_ScoreItem>(true).ToList();

        ScoreManager.OnDataChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        ScoreManager.OnDataChanged -= Refresh;
    }

    private void Refresh()
    {
        if (ScoreManager.Instance == null) return;

        // Linq를 이용한 점수 정렬 (1등 ~ 3등)
        // OrderByDescending: 점수가 높은 순서대로 내림차순 정렬
        // ThenBy: 동점일 경우 닉네임 오름차순으로 2차 정렬 (안정 정렬)
        // Take: UI 아이템 수만큼만 가져오기 (최대 표시 인원 제한)
        // ToList: IEnumerable → List 변환 (인덱스 접근 가능)
        List<ScoreData> sortedScores = ScoreManager.Instance.Scores.Values
            .OrderByDescending(data => data.Score)
            .ThenBy(data => data.Nickname)
            .Take(_items.Count)
            .ToList();

        for (int i = 0; i < _items.Count; i++)
        {
            if (i < sortedScores.Count)
            {
                _items[i].gameObject.SetActive(true);
                _items[i].Set(i + 1, sortedScores[i].Nickname, sortedScores[i].Score);
            }
            else
            {
                // 플레이어 수보다 UI 아이템이 많으면 비활성화 + 초기화
                _items[i].Clear();
                _items[i].gameObject.SetActive(false);
            }
        }
    }
}
