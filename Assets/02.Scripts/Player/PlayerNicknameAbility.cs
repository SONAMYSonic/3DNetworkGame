using System;
using TMPro;
using UnityEngine;

public class PlayerNicknameAbility : PlayerAbility
{
    [SerializeField] private TextMeshProUGUI _playerNickname;

    private void Start()
    {
        _playerNickname.text = _owner.PhotonView.Owner.NickName;

        if (_owner.PhotonView.IsMine)
        {
            _playerNickname.color = Color.green;
        }
        else
        {
            _playerNickname.color = Color.red;
        }
    }
}
