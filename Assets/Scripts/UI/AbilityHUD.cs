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

    [Header("Cooldowns")]
    public List<AbilitySlot> abilitySlots = new List<AbilitySlot>();

    [Header("Charge Bars")]
    public RectTransform blockBar;    
    public RectTransform signatureBar;  

    private PlayerController _localPlayer;

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

        _localPlayer.OnAbilityCooldownStarted += HandleCooldown;

        _localPlayer.currentBlockCharge.OnValueChanged += UpdateBlockUI;
        _localPlayer.currentSignatureCharge.OnValueChanged += UpdateSignatureUI;

        UpdateBlockUI(0, _localPlayer.currentBlockCharge.Value);
        UpdateSignatureUI(0, _localPlayer.currentSignatureCharge.Value);
    }

    private void OnDestroy()
    {
        if (_localPlayer != null)
        {
            _localPlayer.OnAbilityCooldownStarted -= HandleCooldown;
            _localPlayer.currentBlockCharge.OnValueChanged -= UpdateBlockUI;
            _localPlayer.currentSignatureCharge.OnValueChanged -= UpdateSignatureUI;
        }
    }

    private void HandleCooldown(AbilityBase ability, float duration)
    {
        string cleanName = ability.name.Replace("(Clone)", "").Trim();

        foreach (var slot in abilitySlots)
        {
            if (slot.abilityRef.name == cleanName)
            {
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

    private void UpdateBlockUI(float previous, float current)
    {
        if (blockBar == null) return;
        if (_localPlayer.championData.blockAbility.maxCharge <= 0) return;

        float max = _localPlayer.championData.blockAbility.maxCharge;
        float percent = Mathf.Clamp01(current / max);

        blockBar.localScale = new Vector3(1, percent, 1);
    }

    private void UpdateSignatureUI(float previous, float current)
    {
        if (signatureBar == null) return;
        if (_localPlayer.championData.signatureAbility.maxCharge <= 0) return;

        float max = _localPlayer.championData.signatureAbility.maxCharge;
        float percent = Mathf.Clamp01(current / max);

        signatureBar.localScale = new Vector3(1, percent, 1);
    }
}