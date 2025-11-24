using UnityEngine;

[CreateAssetMenu(fileName = "MeleeAttack", menuName = "Scriptable Objects/MeleeAttack")]
public class MeleeAttack : AbilityBase
{
    public float damage;
    public float offsetDistance;
    public Vector2 hitboxSize;
    public LayerMask hitLayers;
    public override void Activate(PlayerController parent)
    {
        parent.currentState = PlayerState.Attacking;
        Vector2 origin = (Vector2)parent.transform.position + (Vector2)(parent.transform.forward * offsetDistance);
        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, hitboxSize, parent.transform.eulerAngles.z, hitLayers);

        foreach (var hit in hits)
        {
            if (hit.gameObject == parent.gameObject) continue; // Skip self
            Debug.Log("Hit " + hit.gameObject.name + " for " + damage + " damage.");
        }

        DebugExtensions.DrawBox(origin, hitboxSize, parent.transform.eulerAngles.z, Color.red, 0.5f);
        EndAbility(parent);
    }

    public override void EndAbility(PlayerController parent)
    {
        parent.currentState = PlayerState.Normal;
        Debug.Log("Melee attack ended.");
    }
}

public static class DebugExtensions
{
    public static void DrawBox(Vector2 center, Vector2 size, float angle, Color color, float duration)
    {
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, angle), Vector3.one);
        Vector3 p1 = rotationMatrix.MultiplyPoint(new Vector3(-size.x / 2, -size.y / 2));
        Vector3 p2 = rotationMatrix.MultiplyPoint(new Vector3(size.x / 2, -size.y / 2));
        Vector3 p3 = rotationMatrix.MultiplyPoint(new Vector3(size.x / 2, size.y / 2));
        Vector3 p4 = rotationMatrix.MultiplyPoint(new Vector3(-size.x / 2, size.y / 2));

        Debug.DrawLine(p1, p2, color, duration);
        Debug.DrawLine(p2, p3, color, duration);
        Debug.DrawLine(p3, p4, color, duration);
        Debug.DrawLine(p4, p1, color, duration);
    }
}