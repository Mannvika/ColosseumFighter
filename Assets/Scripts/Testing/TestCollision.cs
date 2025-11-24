using UnityEngine;

public class TestCollision : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Log the name of the object that entered the trigger
        Debug.Log(gameObject.name + " triggered by " + other.gameObject.name);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Log the name of the object this object collided with
        Debug.Log(gameObject.name + " collided with " + collision.gameObject.name);
    }
}
