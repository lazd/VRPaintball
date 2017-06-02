using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Knife : MonoBehaviour
{
    public int damage = 100;

    public AudioClip hitSound;

    public float stabsPerSecond = 20;
    private float lastDraw = 0;

    private void Update()
    {
        // Limit the number of stabs
        if (Time.time < lastDraw + 1/stabsPerSecond)
        {
            return;
        }

        lastDraw = Time.time;

        // Check if the knife is stabby
        RaycastHit hit;
        if (Physics.Raycast(transform.position - (transform.up * 0.2f), transform.up, out hit, 0.65f))
        {
            var hitObject = hit.collider.gameObject;

            if (hitObject == gameObject)
            {
                // Don't let the knife paint itself...
                return;
            }

            // You can't stab yourself
            if (!transform.IsChildOf(hit.collider.gameObject.transform.root))
            {
                // Subtract health
                if (hit.rigidbody)
                {
                    var health = hit.rigidbody.gameObject.GetComponent<Health>();
                    if (health != null)
                    {
                        health.TakeDamage(damage);

                        AudioSource.PlayClipAtPoint(hitSound, transform.position);
                    }
                }

                // Add a splat
                GetComponent<BasicCollisionPrinter>().PrintCollision(hit.point, hit.normal, hitObject);
            }
        }
    }
}
