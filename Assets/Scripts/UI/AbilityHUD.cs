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
        _localPlayer.OnChampionDataLoaded += SetUpLocalPlayerUI;
        if (_localPlayer.championData != null) SetUpLocalPlayerUI();

        Debug.Log("[AbilityHUD] Local Player Setup Complete. Hunting for Enemy...");

        while(_enemyPlayer == null)
        {
            var allPlayers = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            Debug.Log($"[AbilityHUD] Searching... Found {allPlayers.Length} total PlayerControllers in scene.");

            foreach (var p in allPlayers)
            {
                if(p != _localPlayer)
                {
                    Debug.Log($"[AbilityHUD] FOUND CANDIDATE: {p.name} (NetId: {p.NetworkObjectId})");
                    
                    if (p.championData == null) 
                        Debug.LogWarning($"[AbilityHUD] ... but its championData is NULL.");
                    else 
                        Debug.Log($"[AbilityHUD] ... and it has data: {p.championData.name}");

                    _enemyPlayer = p;
                    break;
                }
            }

            if (_enemyPlayer == null) 
            {
                Debug.LogWarning("[AbilityHUD] Enemy not found yet. Retrying in 0.5s...");
                yield return new WaitForSeconds(0.5f);
            }
        }

        Debug.Log($"[AbilityHUD] Enemy Player Locked: {_enemyPlayer.name}. Hooking events.");
        
        _enemyPlayer.OnChampionDataLoaded += SetupEnemyPlayerUI;

        if (_enemyPlayer.championData != null) 
        {
            Debug.Log("[AbilityHUD] Data was waiting for us. Setting up UI immediately.");
            SetupEnemyPlayerUI();
        }
        else
        {
            Debug.Log("[AbilityHUD] Waiting for Enemy OnChampionDataLoaded event...");
        }
    }

    private void SetUpLocalPlayerUI()
    {
        if(_localPlayer == null) return;

        _localPlayer.AbilitySystem.OnCooldownStarted -= HandleCooldown;
        _localPlayer.Resources.BlockCharge.OnValueChanged -= UpdateBlockUI;
        _localPlayer.Resources.SignatureCharge.OnValueChanged -= UpdateSignatureUI;
        var h = _localPlayer.GetComponent<Health>();
        if(h != null) h.currentHealth.OnValueChanged -= UpdateLocalHealthUI;

        abilitySlots.Clear();
        abilitySlots.Add(new AbilitySlot { abilityRef = _localPlayer.championData.dashAbility, barRect = dashBar });
        abilitySlots.Add(new AbilitySlot { abilityRef = _localPlayer.championData.primaryAbility, barRect = primaryBar });

        _localPlayer.AbilitySystem.OnCooldownStarted += HandleCooldown;
        _localPlayer.Resources.BlockCharge.OnValueChanged += UpdateBlockUI;
        _localPlayer.Resources.SignatureCharge.OnValueChanged += UpdateSignatureUI;

        UpdateBlockUI(0, _localPlayer.Resources.BlockCharge.Value);
        UpdateSignatureUI(0, _localPlayer.Resources.SignatureCharge.Value);

        if (h != null)
        {
            h.currentHealth.OnValueChanged += UpdateLocalHealthUI;
            UpdateLocalHealthUI(0, h.currentHealth.Value);
        }
    }

    private void SetupEnemyPlayerUI()
    {
        Debug.Log("Looking For enemy player");
        if (_enemyPlayer == null) return;

        var enemyHealth = _enemyPlayer.GetComponent<Health>();
        if (enemyHealth == null)
        {
            Debug.LogWarning("SetupEnemyPlayerUI failed: Enemy has no Health script");
            return;
        }

        enemyHealth.currentHealth.OnValueChanged -= UpdateEnemyHealthUI;
        enemyHealth.currentHealth.OnValueChanged += UpdateEnemyHealthUI;
        UpdateEnemyHealthUI(0, enemyHealth.currentHealth.Value);
    }

    private void OnDestroy()
    {
        if (_localPlayer != null)
        {
            _localPlayer.OnChampionDataLoaded -= SetUpLocalPlayerUI;
            _localPlayer.AbilitySystem.OnCooldownStarted -= HandleCooldown;
            _localPlayer.Resources.BlockCharge.OnValueChanged -= UpdateBlockUI;
            _localPlayer.Resources.SignatureCharge.OnValueChanged -= UpdateSignatureUI;
            
            var h = _localPlayer.GetComponent<Health>();
            if(h != null) h.currentHealth.OnValueChanged -= UpdateLocalHealthUI;
        }

        if (_enemyPlayer != null)
        {
            _enemyPlayer.OnChampionDataLoaded -= SetupEnemyPlayerUI;

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
            float fill = Mathf.Clamp01(current / max);
            barImage.fillAmount = fill;
        }
    }

    private void HandleCooldown(AbilityBase ability, float duration)
    {
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
        if (_localPlayer.championData == null) return;
        UpdateBarScale(localHealthBar, current, _localPlayer.championData.maxHealth);
    }

    private void UpdateEnemyHealthUI(float previous, float current)
    {
        Debug.Log("Looking For enemy player");
        if (_enemyPlayer == null) return;
        Debug.Log("Found Enemy Player, looking for champ data");

        if (_enemyPlayer.championData == null)
        {
            Debug.LogWarning($"[AbilityHUD] Update received, but ChampionData is NULL for {_enemyPlayer.name}");
            return; 
        }
        Debug.Log($"[AbilityHUD] Enemy Health Logic Running: {current}/{_enemyPlayer.championData.maxHealth}");
        UpdateBarScale(enemyHealthBar, current, _enemyPlayer.championData.maxHealth);
    }

    private void UpdateBlockUI(float previous, float current)
    {
        if (blockBar == null || _localPlayer.championData == null) return;
        UpdateBarScale(blockBar, current, _localPlayer.championData.blockAbility.maxCharge);
    }

    private void UpdateSignatureUI(float previous, float current)
    {
        if (signatureBar == null || _localPlayer.championData == null) return;         
        UpdateBarScale(signatureBar, current, _localPlayer.championData.signatureAbility.maxCharge);
    }
}