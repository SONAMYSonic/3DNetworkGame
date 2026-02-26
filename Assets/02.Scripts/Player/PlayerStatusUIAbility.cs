using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUIAbility : PlayerAbility
{
    [SerializeField] private Image _healthGauge;
    [SerializeField] private Image _staminaGauge;
    
    private void Update()
    {
        _healthGauge.fillAmount = _owner.Stat.CurrentHealth / _owner.Stat.MaxHealth;
        _staminaGauge.fillAmount = _owner.Stat.CurrentStamina / _owner.Stat.MaxStamina;
    }
}
