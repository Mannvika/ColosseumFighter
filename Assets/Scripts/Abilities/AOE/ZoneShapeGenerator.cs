using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PolygonCollider2D))]
[ExecuteInEditMode]
public class ZoneShapeGenerator : MonoBehaviour
{
    public enum ShapeType { Circle, HollowCone, Box }
    
    public ShapeType shapeType;
    
    [Header("Settings")]
    public float outerRadius = 3f;
    public float innerRadius = 1f; 
    [Range(10, 360)] public float angle = 90f; 
    public int segments = 32; 

    private void OnValidate()
    {
        UpdateCollider();
    }

    public void UpdateCollider()
    {
        PolygonCollider2D poly = GetComponent<PolygonCollider2D>();

        // SPECIAL CASE: Circle with Inner Radius > 0 creates a "Donut"
        // This requires TWO separate paths: one for the outside, one for the hole.
        if (shapeType == ShapeType.Circle && innerRadius > 0)
        {
            poly.pathCount = 2;
            poly.SetPath(0, GenerateCirclePath(outerRadius));
            poly.SetPath(1, GenerateCirclePath(innerRadius));
        }
        else
        {
            // Standard behavior (1 path)
            poly.pathCount = 1;
            poly.SetPath(0, GenerateStandardPoints());
        }

        if (TryGetComponent<ZoneVisuals>(out var visuals))
        {
            visuals.UpdateVisuals();
        }
    }

    // Helper for simple circles (used for both the outer ring and the hole)
    private Vector2[] GenerateCirclePath(float radius)
    {
        List<Vector2> points = new List<Vector2>();
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float rad = Mathf.Deg2Rad * (i * angleStep);
            points.Add(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius);
        }
        return points.ToArray();
    }

    // Your original logic for other shapes
    private Vector2[] GenerateStandardPoints()
    {
        List<Vector2> points = new List<Vector2>();

        if (shapeType == ShapeType.Circle)
        {
            // If we are here, innerRadius is likely 0, so just draw a solid circle
            return GenerateCirclePath(outerRadius);
        }
        else if (shapeType == ShapeType.HollowCone)
        {
            float halfAngle = angle / 2f;
            float currentAngle = -halfAngle + 90; 
            float angleStep = angle / segments;

            // Outer Arc
            for (int i = 0; i <= segments; i++)
            {
                float rad = Mathf.Deg2Rad * currentAngle;
                points.Add(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * outerRadius);
                currentAngle += angleStep;
            }

            // Inner Arc (Reverse)
            currentAngle -= angleStep; 
            for (int i = 0; i <= segments; i++)
            {
                float rad = Mathf.Deg2Rad * currentAngle;
                points.Add(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * innerRadius);
                currentAngle -= angleStep;
            }
        }
        
        return points.ToArray();
    }
}