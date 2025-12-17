using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(fileName = "ZoneAbility", menuName = "Scriptable Objects/Abilities/SpawnZone")]
public class SpawnZoneAbility : AbilityBase
{
    [Header("Zone Configuration")]
    public GameObject zonePrefab;
    public float damagePerTick;
    public float duration;
    public float tickRate = 0.5f;

    [Header("Placement")]
    public bool spawnAtMouse; 
    public float maxRange = 10f;

    public override void Activate(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Normal;

        if(parent.isPredicting) return;

        if (isServer)
        {
            SpawnZone(parent);
        }
    }

    private void SpawnZone(PlayerController parent)
    {
        Vector2 spawnPos;

        if (spawnAtMouse)
        {
            Vector2 inputPos = parent.GetComponent<PlayerInputHandler>().CurrentInput.MousePosition;
            Vector2 dir = (inputPos - (Vector2)parent.transform.position);
            if(dir.magnitude > maxRange) dir = dir.normalized * maxRange;
            
            spawnPos = (Vector2)parent.transform.position + dir;
        }
        else
        {
            spawnPos = parent.transform.position;
        }

        GameObject zoneObj = Instantiate(zonePrefab, spawnPos, Quaternion.identity);
        
        if (spawnAtMouse)
        {
            Vector2 dir = spawnPos - (Vector2)parent.transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; 
            zoneObj.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        DamageZone zoneScript = zoneObj.GetComponent<DamageZone>();
        zoneScript.CasterID = parent.OwnerClientId;
        
        zoneScript.DamagePerTick = damagePerTick;
        zoneScript.Duration = duration;
        zoneScript.TickRate = tickRate;

        zoneObj.GetComponent<NetworkObject>().Spawn();
    }

    public override void OnEnd(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Normal;
        base.OnEnd(parent, isServer);
    }
}