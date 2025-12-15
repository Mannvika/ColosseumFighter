using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DynamicCamera : MonoBehaviour
{
    [Header("Targets")]
    // We will fill this automatically
    public List<Transform> targets = new List<Transform>();

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 0, -10f); // Default Z offset for 2D
    public float smoothTime = 0.5f; // How "lazy" the camera is (0 is instant, 0.5 is smooth)
    public float minZoom = 5f;      // Closest the camera can get
    public float maxZoom = 20f;     // Furthest the camera can get
    public float zoomLimiter = 50f; // Higher number = less zoom sensitivity

    private Vector3 velocity; // For SmoothDamp reference
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    // LateUpdate is CRITICAL. It runs after PlayerController has moved the players.
    // If you use Update(), the camera will jitter because it fights the player movement.
    void LateUpdate()
    {
        // 1. Logic to find players if we don't have them yet (Netcode spawning)
        if (targets.Count <= 1)
        {
            FindPlayers();
            return;
        }
        
        // 2. Remove null targets (if a player disconnects)
        targets.RemoveAll(item => item == null);

        if (targets.Count == 0) return;

        // 3. Move and Zoom
        Move();
        Zoom();
    }

    void Move()
    {
        Vector3 centerPoint = GetCenterPoint();
        Vector3 newPosition = centerPoint + offset;

        // Smoothly move from current position to new position
        transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref velocity, smoothTime);
    }

    void Zoom()
    {
        // Calculate the new zoom based on the width of the bounding box
        float newZoom = Mathf.Lerp(minZoom, maxZoom, GetGreatestDistance() / zoomLimiter);

        // Smoothly apply the zoom
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, newZoom, Time.deltaTime);
    }

    float GetGreatestDistance()
    {
        // Creates a bounding box around all players
        var bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            bounds.Encapsulate(targets[i].position);
        }

        // Return width or height, whichever is larger
        return Mathf.Max(bounds.size.x, bounds.size.y);
    }

    Vector3 GetCenterPoint()
    {
        if (targets.Count == 1)
        {
            return targets[0].position;
        }

        var bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            bounds.Encapsulate(targets[i].position);
        }

        return bounds.center;
    }

    // Simple helper to find players by Tag
    void FindPlayers()
    {
        // Make sure your Player Prefab has the tag "Player"
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        foreach(GameObject p in players)
        {
            if(!targets.Contains(p.transform))
            {
                targets.Add(p.transform);
            }
        }
    }
}