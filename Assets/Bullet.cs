using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Bullet : MonoBehaviour
{
    public int damage = 10;
    public float rate = 20;
    public bool hasHit = false;

    public float speed = 50f;
    public float time = 2f;

    public float destroyTime = 0f;

    public float splatScale = 1.0f;

    public AudioClip fireSound;
    public AudioClip splatSound;

    public GameObject PaintSplatter;

    private void Start()
    {
        var ni = GetComponent<NetworkIdentity>();

        // Remove bullets that were spawned by the server on behalf of us
        // Since we spawn the bullets locally for the fastest visual feedback, we don't need to show these
        if (ni.hasAuthority && ni.isClient) {
            gameObject.SetActive(false);
            return;
        }

        AudioSource.PlayClipAtPoint(fireSound, transform.position);
    }

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
            // Draw a paint splatter
            // Todo: don't draw if hit another bullet
            createSplat(collision);

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

    void createSplat(Collision collision)
    {
        AudioSource.PlayClipAtPoint(splatSound, transform.position);
    }
}
