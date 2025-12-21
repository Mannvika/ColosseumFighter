using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteInEditMode]
public class ZoneVisuals : MonoBehaviour
{
    [SerializeField] private ZoneShapeGenerator shapeGenerator;
    [Tooltip("Controls the color/alpha of the zone directly.")]
    public Color zoneColor = new Color(1, 0, 0, 0.5f); // Default Red/Transparent
    
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        if (shapeGenerator == null) shapeGenerator = GetComponent<ZoneShapeGenerator>();
    }

    private void OnEnable()
    {
        UpdateVisuals();
    }

    private void OnValidate()
    {
        // Allows color updates in real-time in the editor
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (shapeGenerator == null) shapeGenerator = GetComponent<ZoneShapeGenerator>();
        if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
        if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();

        if (shapeGenerator == null || _meshFilter == null) return;

        if (_meshRenderer.sharedMaterial == null)
        {
            _meshRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        if (_mesh == null)
        {
            // Try to pick up the existing mesh from the filter first
            if (_meshFilter.sharedMesh != null)
            {
                _mesh = _meshFilter.sharedMesh;
            }
            else
            {
                // Only create a new one if absolutely necessary
                _mesh = new Mesh();
                _mesh.name = "ProceduralZone";
                _meshFilter.mesh = _mesh;
            }
        }

        // Generate the geometry
        if (shapeGenerator.shapeType == ZoneShapeGenerator.ShapeType.Box)
            GenerateBoxMesh();
        else
            GenerateRadialMesh();
            
        // --- FIX 2: Apply Color ---
        ApplyColors();
    }

    private void ApplyColors()
    {
        if (_mesh == null) return;
        
        // We need one color per vertex. 
        Vector3[] verts = _mesh.vertices;
        Color[] colors = new Color[verts.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = zoneColor;
        }
        _mesh.colors = colors;
    }

    private void GenerateBoxMesh()
    {
        _mesh.Clear();
        
        float outer = shapeGenerator.outerRadius;
        float inner = shapeGenerator.innerRadius;

        Vector3[] vertices = new Vector3[8];
        Vector2[] uvs = new Vector2[8];

        Vector3[] corners = new Vector3[]
        {
            new Vector3(1, 1, 0), new Vector3(1, -1, 0),
            new Vector3(-1, -1, 0), new Vector3(-1, 1, 0)
        };

        for (int i = 0; i < 4; i++)
        {
            vertices[i] = corners[i] * inner;
            uvs[i] = new Vector2(0, 0); 
            vertices[i + 4] = corners[i] * outer;
            uvs[i + 4] = new Vector2(1, 1); 
        }

        int[] triangles = new int[]
        {
            0, 4, 5,  0, 5, 1,
            1, 5, 6,  1, 6, 2,
            2, 6, 7,  2, 7, 3,
            3, 7, 4,  3, 4, 0
        };

        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.uv = uvs;
        _mesh.RecalculateNormals();
    }

    private void GenerateRadialMesh()
    {
        _mesh.Clear();

        float angle = shapeGenerator.angle;
        float outerRad = shapeGenerator.outerRadius;
        float innerRad = shapeGenerator.innerRadius;
        int segments = shapeGenerator.segments;
        
        if (shapeGenerator.shapeType == ZoneShapeGenerator.ShapeType.Circle)
        {
            angle = 360f;
        }

        int vertexCount = (segments + 1) * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 6];
        Vector2[] uvs = new Vector2[vertexCount];

        float currentAngle = (shapeGenerator.shapeType == ZoneShapeGenerator.ShapeType.HollowCone) 
            ? -angle / 2f + 90 
            : 0f;
            
        float angleStep = angle / segments;

        int vertIndex = 0;
        int triIndex = 0;

        for (int i = 0; i <= segments; i++)
        {
            float rad = Mathf.Deg2Rad * currentAngle;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);

            vertices[vertIndex] = dir * innerRad;
            uvs[vertIndex] = new Vector2(0, (float)i / segments); 

            vertices[vertIndex + 1] = dir * outerRad;
            uvs[vertIndex + 1] = new Vector2(1, (float)i / segments); 

            if (i < segments)
            {
                triangles[triIndex] = vertIndex;
                triangles[triIndex + 1] = vertIndex + 2;
                triangles[triIndex + 2] = vertIndex + 1;

                triangles[triIndex + 3] = vertIndex + 1;
                triangles[triIndex + 4] = vertIndex + 2;
                triangles[triIndex + 5] = vertIndex + 3;

                triIndex += 6;
            }

            vertIndex += 2;
            currentAngle += angleStep;
        }

        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.uv = uvs;
        _mesh.RecalculateNormals();
    }
}