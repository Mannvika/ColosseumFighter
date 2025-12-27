using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AbilityHUD : MonoBehaviour
{
    [System.Serializable]
    public struct AbilitySlot
    {
        public AbilityBase abilityRef;
        public RectTransform barRect;   
    }

    public List<AbilitySlot> abilitySlots = new List<AbilitySlot>();


    [Header("Cooldowns")]
    public RectTransform dashBar;
    public RectTransform primaryBar;

    [Header("Charge Bars")]
    public RectTransform blockBar;    
    public RectTransform signatureBar;  

    [Header("Health Bars")]
    public RectTransform localHealthBar;
    public RectTransform enemyHealthBar;

    private PlayerController _localPlayer;
    private PlayerController _enemyPlayer;

    private void Start()
    {
        StartCoroutine(FindPlayerRoutine());
    }

    private IEnumerator FindPlayerRoutine()
    {
        while (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            yield return null;
        }
        _localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
        SetUpLocalPlayerUI();

        while(_enemyPlayer == null)
        {
            var allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

            foreach (var p in allPlayers)
            {
                if(p != _localPlayer)
                {
                    _enemyPlayer = p;
                    SetupEnemyPlayerUI();
                    break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void SetUpLocalPlayerUI()
    {
        abilitySlots.Clear();

        abilitySlots.Add(new AbilitySlot { abilityRef = _localPlayer.championData.dashAbility, barRect = dashBar });
        abilitySlots.Add(new AbilitySlot { abilityRef = _localPlayer.championData.primaryAbility, barRect = primaryBar });

        _localPlayer.AbilitySystem.OnCooldownStarted += HandleCooldown;
        _localPlayer.Resources.BlockCharge.OnValueChanged += UpdateBlockUI;
        _localPlayer.Resources.SignatureCharge.OnValueChanged += UpdateSignatureUI;

        UpdateBlockUI(0, _localPlayer.Resources.BlockCharge.Value);
        UpdateSignatureUI(0, _localPlayer.Resources.SignatureCharge.Value);

        var health = _localPlayer.GetComponent<Health>();
        health.currentHealth.OnValueChanged += UpdateLocalHealthUI;
        UpdateLocalHealthUI(0, health.currentHealth.Value);
    }

    private void SetupEnemyPlayerUI()
    {
        var enemyHealth = _enemyPlayer.GetComponent<Health>();
        enemyHealth.currentHealth.OnValueChanged += UpdateEnemyHealthUI;
        UpdateEnemyHealthUI(0, enemyHealth.currentHealth.Value);
    }

    private void OnDestroy()
    {
        if (_localPlayer != null)
        {
           _localPlayer.AbilitySystem.OnCooldownStarted -= HandleCooldown;
           _localPlayer.Resources.BlockCharge.OnValueChanged -= UpdateBlockUI;
           _localPlayer.Resources.SignatureCharge.OnValueChanged -= UpdateSignatureUI;
           
           var h = _localPlayer.GetComponent<Health>();
           if(h != null) h.currentHealth.OnValueChanged -= UpdateLocalHealthUI;
        }

        if (_enemyPlayer != null)
        {
            var h = _enemyPlayer.GetComponent<Health>();
            if(h != null) h.currentHealth.OnValueChanged -= UpdateEnemyHealthUI;
        }
    }
    private void UpdateBarScale(RectTransform barRect, float current, float max)
    {
        if (barRect == null || max <= 0) return;
        
        UnityEngine.UI.Image barImage = barRect.GetComponent<UnityEngine.UI.Image>();
        
        if (barImage != null)
        {
            barImage.fillAmount = Mathf.Clamp01(current / max);
        }
    }

    private void HandleCooldown(AbilityBase ability, float duration)
    {
        string cleanName = ability.name.Replace("(Clone)", "").Trim();
        foreach (var slot in abilitySlots) {
            if (slot.abilityRef == ability) {
                StartCoroutine(AnimateScale(slot.barRect, duration));
                return;
            }
        }
    }

    private IEnumerator AnimateScale(RectTransform target, float duration)
    {
        if (target == null) yield break;

        float timer = 0f;
        
        target.localScale = new Vector3(1, 0, 1);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            target.localScale = new Vector3(1, progress, 1);

            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private void UpdateLocalHealthUI(float previous, float current)
    {
        float maxHealth = _localPlayer.championData.maxHealth; 
        UpdateBarScale(localHealthBar, current, maxHealth);
    }

    private void UpdateEnemyHealthUI(float previous, float current)
    {
        float maxHealth = _enemyPlayer.championData.maxHealth; 
        UpdateBarScale(enemyHealthBar, current, maxHealth);
    }

    private void UpdateBlockUI(float previous, float current)
    {
        if (blockBar == null) return;
        float max = _localPlayer.championData.blockAbility.maxCharge;
        UpdateBarScale(blockBar, current, max);
    }

    private void UpdateSignatureUI(float previous, float current)
    {
         if (signatureBar == null) return;
         float max = _localPlayer.championData.signatureAbility.maxCharge;
         UpdateBarScale(signatureBar, current, max);
    }
}