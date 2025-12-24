using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

public class PlayerVisuals : NetworkBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private SpriteRenderer sprite;

    public Transform visualSpawnPoint;

    [HideInInspector]
    public Color InitialColor;
    public Color blockingColor;

    public enum VisualSlot {Melee, Primary, Signature, Projectile, Blocking, Dashing};

    private void Awake()
    {
        if(_playerController == null)
        {
            _playerController = GetComponent<PlayerController>();
        }
        if(sprite == null)
        {
            sprite = GetComponent<SpriteRenderer>();
        }
        InitialColor = sprite.color;
    }

    public void TriggerAbilityVisual(VisualSlot slot, GameObject prefab, Vector2 direction, float cooldown, Vector2 visualSize)
    {
        if (_playerController.isRollingBack) return;

        if(IsOwner && _playerController.isPredicting)
        {
            SpawnVisualLocal(prefab, direction, cooldown, visualSize);
        }

        if (IsServer)
        {
            TriggerVisualClientRpc(slot, direction, cooldown, visualSize);
        }
    }

    [ClientRpc]
    private void TriggerVisualClientRpc(VisualSlot slot, Vector2 direction, float cooldown, Vector2 visualSize)
    {
        if (IsOwner) return;

        GameObject prefab = GetPrefabFromSlot(slot);
        if(prefab != null)
        {
            SpawnVisualLocal(prefab, direction, cooldown, visualSize);
        }
    }

    private void SpawnVisualLocal(GameObject prefab, Vector2 direction, float cooldown, Vector2 visualSize)
    {
        if (prefab == null) return;

        var visual = Instantiate(prefab, visualSpawnPoint.position, Quaternion.identity);
        float angle = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f;        
        visual.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var transient = visual.GetComponent<TransientVisual>();
        if (transient != null) 
        { 
            visual.transform.SetParent(visualSpawnPoint);
            transient.StartScale = visualSize; 
            transient.EndScale = visualSize * 0.75f;
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

    public void ChangeColor(Color newColor)
    {
        sprite.color = newColor;
    }
}
