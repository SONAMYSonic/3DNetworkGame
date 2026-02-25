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
            // damageable.TakeDamage(_owner.Stat.Damage);
            
            // RPC로 대미지를 적용해야지 모든 클라이언트에서 대미지가 적용됨
            // _owner.PhotonView.RPC(nameof(damageable.TakeDamage), RpcTarget.All, _owner.Stat.Damage);
            // ㄴ _owner은 내 플레이어이므로 상대방 플레이어의 PhotonView가 아니다. 따라서 상대방 플레이어의 PhotonView를 찾아야 한다.
            
            // 상대방의 TakeDamage를 RPC로 호출한다.
            PlayerController otherPlayer = other.GetComponentInParent<PlayerController>();
            
            // 상대가 죽은 상태면 대미지를 적용하지 않는다.
            if (otherPlayer != null && otherPlayer.IsDead) return;
            
            otherPlayer.PhotonView.RPC(nameof(damageable.TakeDamage), RpcTarget.All, _owner.Stat.Damage);
            
            _owner.GetAbility<PlayerWeaponColliderAbility>().DisableWeaponCollider();
            
        }
    }
}
