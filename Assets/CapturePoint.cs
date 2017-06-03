using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class CapturePoint : NetworkBehaviour {

    [SyncVar(hook = "SetColor")]
    Color color = new Color(0.3f, 0.3f, 0.3f);

    public Light haloLight;
    public Light areaLight;
    public GameObject sphere;
    
    [SyncVar]
    public int health = 100;

    public int maxHealth = 100;

    private float dampVeclocity = 0.0F;
    private float smoothTime = 0.25f;

    private float intensity = 1f;

    private void Start()
    {
        SetColor(color);
    }

    private void Update()
    {
        var healthFraction = health / (float)maxHealth;
        intensity = Mathf.SmoothDamp(intensity, healthFraction, ref dampVeclocity, smoothTime);

        sphere.GetComponent<MeshRenderer>().material.SetColor("_Color", (color / 2) * intensity);
        sphere.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", (color / 2) * intensity);
        haloLight.intensity = intensity;
        areaLight.intensity = intensity * 4;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Get a reference to the object that collided with us
        var hitObject = collision.collider.gameObject;

        // If it's a bullet, take on its color
        Bullet bullet = hitObject.GetComponent<Bullet>();
        if (bullet)
        {
            if (bullet.color == color)
            {
                // Increase health 
                health += bullet.damage;

                // Never have more than max health
                if (health > maxHealth)
                {
                    health = maxHealth;
                }
            }
            else
            {
                // Subtract health
                health -= bullet.damage;
            }

            if (health <= 0)
            {
                // Start with a tiny bit of health so the fade looks nice
                intensity = 0.2f;
                health = 20;

                // Take over a domination point if health reaches zero
                color = bullet.color;
                SetColor(color);
            }
        }

    }

    public void SetColor(Color newColor)
    {
        // Set the capture point color
        areaLight.color = newColor;
        haloLight.color = newColor;
    }
}
