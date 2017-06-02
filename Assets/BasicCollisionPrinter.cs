using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
* The CollisionPrinter Component. Prints a projection under set conditions related to the collision of the object attached to this printer.
*/
[RequireComponent(typeof(Collider))]
public class BasicCollisionPrinter : Printer
{
    /**
    * Defines the orientation of the projection relative to the surface of the collision. Velocity will orient the projection as if its up is the direction the collision object is moving in. Random will orient the projection as if its up is random.
    */
    public RotationSource rotationSource;

    public float triggerDelayTime = 0.5f;

    private float lastStayTrigger = 0f;

    void OnCollisionEnter(Collision collision)
    {
        ContactPoint hit = collision.contacts[0];
        Vector3 position = hit.point;
        Vector3 normal = hit.normal;
        PrintCollision(position, normal, hit.otherCollider.gameObject);
    }

    public void PrintCollision(Vector3 position, Vector3 normal, GameObject hitObject)
    {
        //Calculate final position and surface normal (Not collision normal)
        Transform surface = hitObject.transform;

        //Calculate our rotation
        Vector3 rot;
        if (rotationSource == RotationSource.Velocity && GetComponent<Rigidbody>().velocity != Vector3.zero) rot = GetComponent<Rigidbody>().velocity.normalized;
        else if (rotationSource == RotationSource.Random) rot = Random.insideUnitSphere.normalized;
        else rot = Vector3.up;

        //Print
        Projection projection = Print(position, Quaternion.LookRotation(-normal, rot), hitObject.transform, hitObject.layer);

        // Set the color of the decal
        projection.GetComponent<Decal>().AlbedoColor = GetComponent<Bullet>().color;
        projection.GetComponent<Decal>().EmissionColor = GetComponent<Bullet>().color;
    }
}
