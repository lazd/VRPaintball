using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
* The CollisionPrinter Component. Prints a projection under set conditions related to the collision of the object attached to this printer.
*/
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class BasicCollisionPrinter : Printer
{
    /**
    * Defines the orientation of the projection relative to the surface of the collision. Velocity will orient the projection as if its up is the direction the collision object is moving in. Random will orient the projection as if its up is random.
    */
    public BasicRotationSource rotationSource;

    /**
    * The layers that, when collided with, cause a print.
    */
    public LayerMask layers;

    void OnCollisionEnter(Collision collision)
    {
        PrintCollision(collision);
    }

    public void PrintCollision(Collision collision)
    {
        //Calculate final position and surface normal (Not collision normal)
        ContactPoint hit = collision.contacts[0];
        Vector3 position = hit.point;
        Vector3 normal = hit.normal;
        Transform surface = hit.otherCollider.transform;

        //Calculate our rotation
        Vector3 rot;
        if (rotationSource == BasicRotationSource.Velocity && GetComponent<Rigidbody>().velocity != Vector3.zero) rot = GetComponent<Rigidbody>().velocity.normalized;
        else if (rotationSource == BasicRotationSource.Random) rot = Random.insideUnitSphere.normalized;
        else rot = Vector3.up;

        //Print
        Print(position, Quaternion.LookRotation(-normal, rot), hit.otherCollider.transform, hit.otherCollider.gameObject.layer);
    }
}

public enum BasicRotationSource { Velocity, Random }