using UnityEngine;
using Unity.Netcode;
using System.Collections;

[CreateAssetMenu(fileName = "SignatureAbility", menuName = "Scriptable Objects/Abilities/SignatureAbility/ColliderSignature")]
public class ColliderSignatureAbility : SignatureAbility
{
    public GameObject colliderPrefab;

    public override IEnumerator SignatureRoutine(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.UsingSignatureAbility;
        SpawnCollider(parent);
        OnEnd(parent, isServer);
        yield return null;
    }

    private void SpawnCollider(PlayerController parent)
    {
        Vector2 spawnPosition = (Vector2)parent.transform.position + ((Vector2)parent.transform.up * 1f);
        GameObject collider = Instantiate(colliderPrefab, spawnPosition, parent.transform.rotation);
        collider.GetComponent<NetworkObject>().Spawn();
    }
    
}
