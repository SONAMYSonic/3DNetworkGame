using System;
using UnityEngine;

public class PlayerWeaponColliderAbility : PlayerAbility
{
    [SerializeField] private Collider _playerWeaponCollider;
    

    private void Start()
    {
        DisableWeaponCollider();
    }
    
    public void EnableWeaponCollider()
    {
        _playerWeaponCollider.enabled = true;
    }
    
    public void DisableWeaponCollider()
    {
        _playerWeaponCollider.enabled = false;
    }
}
