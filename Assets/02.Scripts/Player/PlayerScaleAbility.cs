using UnityEngine;

/// <summary>
/// 점수 1,000점마다 플레이어의 모델 scale과 히트박스(DamageCollider) 크기를 0.1씩 증가시킨다.
/// 점수는 Photon 커스텀 프로퍼티로 이미 동기화되어 있으므로,
/// 모든 클라이언트가 ScoreManager의 데이터를 기반으로 독립적으로 scale을 계산한다.
/// </summary>
public class PlayerScaleAbility : PlayerAbility
{
    // 점수 1,000점마다 scale 0.1 증가
    private const int SCORE_PER_LEVEL = 1000;
    private const float SCALE_INCREMENT = 0.1f;

    private Vector3 _baseScale;

    private void Start()
    {
        _baseScale = transform.localScale;

        ScoreManager.OnDataChanged += UpdateScale;
        UpdateScale();
    }

    private void OnDestroy()
    {
        ScoreManager.OnDataChanged -= UpdateScale;
    }

    private void UpdateScale()
    {
        if (ScoreManager.Instance == null) return;
        if (_owner == null || _owner.PhotonView == null) return;
        if (_owner.PhotonView.Owner == null) return;

        int actorNumber = _owner.PhotonView.Owner.ActorNumber;

        if (!ScoreManager.Instance.Scores.TryGetValue(actorNumber, out ScoreData data))
            return;

        // 1,000점당 1레벨 (정수 나눗셈으로 내림)
        int level = data.Score / SCORE_PER_LEVEL;
        float scaleMultiplier = 1f + level * SCALE_INCREMENT;

        // root transform을 스케일하면 모델, DamageCollider, CharacterController 모두 비례 증가
        transform.localScale = _baseScale * scaleMultiplier;
    }
}
