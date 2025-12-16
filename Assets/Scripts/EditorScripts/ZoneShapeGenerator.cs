using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PolygonCollider2D))]
[ExecuteInEditMode] // Updates shape in Editor without playing
public class ZoneShapeGenerator : MonoBehaviour
{
    public enum ShapeType { Circle, HollowCone, Box }
    
    public ShapeType shapeType;
    
    [Header("Settings")]
    public float outerRadius = 3f;
    public float innerRadius = 1f; // For Hollow Cone
    [Range(10, 360)] public float angle = 90f; // For Cone
    public int segments = 32; // Smoothness

    private void OnValidate()
    {
        UpdateCollider();
    }

    public void UpdateCollider()
    {
        PolygonCollider2D poly = GetComponent<PolygonCollider2D>();
        poly.points = GeneratePoints();
    }

    private Vector2[] GeneratePoints()
    {
        List<Vector2> points = new List<Vector2>();

        if (shapeType == ShapeType.Circle)
        {
            float angleStep = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float rad = Mathf.Deg2Rad * (i * angleStep);
                points.Add(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * outerRadius);
            }
        }
        else if (shapeType == ShapeType.HollowCone)
        {
            // 1. Generate Outer Arc
            float halfAngle = angle / 2f;
            float currentAngle = -halfAngle + 90; // +90 to start facing "Up"
            float angleStep = angle / segments;

            // Outer Arc
            for (int i = 0; i <= segments; i++)
            {
                float rad = Mathf.Deg2Rad * currentAngle;
                points.Add(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * outerRadius);
                currentAngle += angleStep;
            }

            // 2. Generate Inner Arc (Reverse order to close loop)
            currentAngle -= angleStep; // Step back once
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