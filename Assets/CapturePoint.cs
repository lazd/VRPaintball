using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class CapturePoint : NetworkBehaviour {

    [SyncVar(hook = "SetColor")]
    Color color = Color.gray;

    public Light haloLight;
    public Light areaLight;

    private void Start()
    {
        SetColor(color);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Get a reference to the object that collided with us
        var hitObject = collision.collider.gameObject;

        // If it's a bullet, take on its color
        Bullet bullet = hitObject.GetComponent<Bullet>();
        if (bullet)
        {
            color = bullet.color;
            SetColor(color);
        }
    }

    public void SetColor(Color newColor)
    {
        // Set the capture point color
        GetComponent<MeshRenderer>().material.SetColor("_Color", newColor);
        GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", newColor/10);
        areaLight.color = newColor;
        haloLight.color = newColor;
    }
}
