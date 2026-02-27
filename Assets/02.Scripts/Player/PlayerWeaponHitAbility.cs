using Photon.Pun;
using UnityEngine;

public class PlayerWeaponHitAbility : PlayerAbility
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Hit: {other.name}, damageable: {other.GetComponentInParent<IDamageable>()}");

        if (!_owner.PhotonView.IsMine) return;
        if (_owner.IsDead) return;

        // 자기 자신의 모든 콜라이더 무시 (자식 오브젝트 포함)
        if (other.transform.root == transform.root) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        PhotonView targetView = other.GetComponentInParent<PhotonView>();
        if (targetView == null) return;

        // 죽은 대상은 무시
        PlayerController otherPlayer = other.GetComponentInParent<PlayerController>();
        if (otherPlayer != null && otherPlayer.IsDead) return;

        MonsterController monster = other.GetComponentInParent<MonsterController>();
        if (monster != null && monster.IsDead) return;

        targetView.RPC(nameof(damageable.TakeDamage), RpcTarget.All,
            _owner.Stat.Damage, PhotonNetwork.LocalPlayer.ActorNumber);

        _owner.GetAbility<PlayerWeaponColliderAbility>().DisableWeaponCollider();
    }
}
