using Photon.Pun;
using UnityEngine;

public class PlayerWeaponHitAbility : PlayerAbility
{
    private void OnTriggerEnter(Collider other)
    {
        if (!_owner.PhotonView.IsMine) return;
        if (_owner.IsDead) return;

        if (other.transform == _owner.transform) return;

        if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            // 상대방의 PhotonView를 찾는다 (Player든 Monster든 공통)
            PhotonView targetView = other.GetComponentInParent<PhotonView>();
            if (targetView == null) return;

            // 플레이어인 경우 죽은 상태 체크
            PlayerController otherPlayer = other.GetComponentInParent<PlayerController>();
            if (otherPlayer != null && otherPlayer.IsDead) return;

            // 몬스터인 경우 죽은 상태 체크
            MonsterController monster = other.GetComponentInParent<MonsterController>();
            if (monster != null && monster.IsDead) return;

            // 상대방의 TakeDamage를 RPC로 호출한다 (Player, Monster 모두 동작)
            targetView.RPC(nameof(damageable.TakeDamage), RpcTarget.All,
                _owner.Stat.Damage, PhotonNetwork.LocalPlayer.ActorNumber);

            _owner.GetAbility<PlayerWeaponColliderAbility>().DisableWeaponCollider();
        }
    }
}
