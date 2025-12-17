using UnityEngine;

[ExecuteInEditMode]
public class ZoneVisuals : MonoBehaviour
{
    // Array to hold multiple particle systems (Border, Fill, etc.)
    private ParticleSystem[] _allSystems;
    private Collider2D _col;
    private ZoneShapeGenerator _generator;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        // Grab ALL particle systems in children
        _allSystems = GetComponentsInChildren<ParticleSystem>();
        
        _col = GetComponent<Collider2D>();
        _generator = GetComponent<ZoneShapeGenerator>();
    }

    public void UpdateParticlesToCollider()
    {
        Initialize(); 
        
        if (_allSystems == null || _allSystems.Length == 0) return;

        float radius = 1f;
        float innerRadius = 0f;

        // 1. Get Radius Data (Prioritize Generator)
        if (_generator != null)
        {
             radius = _generator.outerRadius;
             innerRadius = _generator.innerRadius; 
        }
        else if (_col is CircleCollider2D circle)
        {
            radius = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        }
        else if (_col is PolygonCollider2D poly)
        {
            radius = poly.bounds.extents.x;
        }

        // 2. Loop through ALL systems and apply the radius
        foreach(var ps in _allSystems)
        {
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            
            // Adjust "Radius Thickness" to support donuts
            if (radius > 0)
            {
                // If you want the "Fill" to be solid but the "Border" to be thin,
                // you might need a naming convention check here.
                // For now, this scales the donut hole for everyone:
                float thickness = 1f - (innerRadius / radius);
                shape.radiusThickness = Mathf.Clamp01(thickness);
            }
        }
    }
}