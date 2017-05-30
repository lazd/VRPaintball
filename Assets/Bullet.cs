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

    public float splatScale = 1.0f;

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
            var contact = collision.contacts[0];
            createSplat(hitObject, contact);

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

    void createSplat(GameObject obj, ContactPoint hit) {
        var splatManager = obj.GetComponent<SplatManager>();
        if (splatManager) {
            // Get how many splats are in the splat atlas
            int splatsX = splatManager.splatsX;
            int splatsY = splatManager.splatsY;
            Vector4 channelMask = new Vector4(1,0,0,0);

            // if( Input.GetKeyDown (KeyCode.Alpha1) ){
                // Orange
                // channelMask = new Vector4(1,0,0,0);
            // }
            
            // if( Input.GetKeyDown (KeyCode.Alpha2) ){
                // Red
                channelMask = new Vector4(0,1,0,0);
            // }
            
            // if( Input.GetKeyDown (KeyCode.Alpha3) ){
            //     channelMask = new Vector4(0,0,1,0);
            // }
            
            // if( Input.GetKeyDown (KeyCode.Alpha4) ){
            //     channelMask = new Vector4(0,0,0,1);
            // }

            Vector3 leftVec = Vector3.Cross ( hit.normal, Vector3.up );
            float randScale = Random.Range(0.5f,1.5f);
            
            GameObject newSplatObject = new GameObject();
            newSplatObject.transform.position = hit.point;
            if( leftVec.magnitude > 0.001f ){
                newSplatObject.transform.rotation = Quaternion.LookRotation( leftVec, hit.normal );
            }
            newSplatObject.transform.RotateAround( hit.point, hit.normal, Random.Range(-180, 180 ) );
            newSplatObject.transform.localScale = new Vector3( randScale, randScale * 0.5f, randScale ) * splatScale;

            Splat newSplat;
            newSplat.splatMatrix = newSplatObject.transform.worldToLocalMatrix;
            newSplat.channelMask = channelMask;

            float splatscaleX = 1.0f / splatsX;
            float splatscaleY = 1.0f / splatsY;
            float splatsBiasX = Mathf.Floor( Random.Range(0,splatsX * 0.99f) ) / splatsX;
            float splatsBiasY = Mathf.Floor( Random.Range(0,splatsY * 0.99f) ) / splatsY;

            newSplat.scaleBias = new Vector4(splatscaleX, splatscaleY, splatsBiasX, splatsBiasY );

            splatManager.splatManager.AddSplat (newSplat);

            GameObject.Destroy( newSplatObject );
        }
        else {

        }
    }
}