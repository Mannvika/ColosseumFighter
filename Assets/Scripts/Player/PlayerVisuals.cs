using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

public class PlayerVisuals : NetworkBehaviour
{
    [SerializeField] private PlayerController _playerController;

    public Transform visualSpawnPoint;

    public enum VisualSlot {Melee, Primary, Signature, Projectile, Blocking, Dashing};

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    public void TriggerAbilityVisual(VisualSlot slot, GameObject prefab, Vector2 direction, float cooldown)
    {
        if (_playerController.isRollingBack) return;

        if(IsOwner && _playerController.isPredicting)
        {
            SpawnVisualLocal(prefab, direction, cooldown);
        }

        if (IsServer)
        {
            TriggerVisualClientRpc(slot, direction, cooldown);
        }
    }

    [ClientRpc]
    private void TriggerVisualClientRpc(VisualSlot slot, Vector2 direction, float cooldown)
    {
        if (IsOwner) return;

        GameObject prefab = GetPrefabFromSlot(slot);
        if(prefab != null)
        {
            SpawnVisualLocal(prefab, direction, cooldown);
        }
    }

    private void SpawnVisualLocal(GameObject prefab, Vector2 direction, float cooldown)
    {
        if (prefab == null) return;

        var visual = Instantiate(prefab, visualSpawnPoint.position, Quaternion.identity);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        visual.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var transient = visual.GetComponent<TransientVisual>();
        if (transient != null) 
        { 
            visual.transform.SetParent(visualSpawnPoint);
            transient.Play(cooldown); 
        }
        else
        { 
            Destroy(visual, 0.5f); 
        }
    }

    private GameObject GetPrefabFromSlot(VisualSlot slot)
    {
        var data = _playerController.championData;
        switch (slot)
        {
            case VisualSlot.Melee: return data.meleeAttack.visualsPrefab;
            case VisualSlot.Primary: return data.primaryAbility.visualsPrefab;
            case VisualSlot.Signature: return data.signatureAbility.visualsPrefab;
            case VisualSlot.Projectile: return data.projectileAbility.visualsPrefab;
            case VisualSlot.Blocking: return data.blockAbility.visualsPrefab;
            case VisualSlot.Dashing: return data.dashAbility.visualsPrefab;
        }
        return null;
    }
}
