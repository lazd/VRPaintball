using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{
    public int damage = 10;
    public float rate = 20;
    public bool hasHit = false;

    public float speed = 50f;
    public float time = 2f;

    public float destroyTime = 0f;

    void OnCollisionEnter(Collision collision)
    {
        // The actual object that got hit
        var hitObject = collision.collider.gameObject;
        
        // Shields block hits
        if (hitObject.CompareTag("Shield"))
        {
            return;
        }

        // The parent object
        var hit = collision.gameObject;

        var health = hit.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        if (!hasHit)
        {
            if (hitObject.CompareTag("Bullet"))
            {
                // Destroy immediately if bullets hit
                Destroy(hitObject);
                Destroy(gameObject);
            }
            else
            {
                // Schedule destroy
                Destroy(gameObject, destroyTime);
            }
        }

        hasHit = true;
    }
}