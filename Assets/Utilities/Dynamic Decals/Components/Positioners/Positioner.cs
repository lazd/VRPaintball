using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
* The base abstract class all other positioners inherit from. Positioners manage the position and rotation of a projection under different circumstances.
*/
public abstract class Positioner : MonoBehaviour
{
    /**
    * The instance of the projection that we are currently positioning. If you seek to modify the positioners current projection, modify this. Cannot be set, may be active or inactive.
    */
    public Projection Active
    {
        get { return proj; }
    }

    /**
    * The projection we want to position. This should usually be a prefab.
    */
    public Projection projection;

    /**
    * The layers we want to position onto. 
    * All positioners are based on raycasts, which layers should those rays collide with.
    */
    public LayerMask layers;

    /**
    * If enabled the projection will not be hidden when a raycast fails. It will simply be left where it was last.
    * If disabled the projection will be hidden when a raycats fails.
    */
    public bool alwaysVisible;

    private Projection proj;

    private void OnDisable()
    {
        if (proj != null) proj.gameObject.SetActive(false);
    }

    protected virtual void Start()
    {
        //Generate our Projection
        proj = ((GameObject)Instantiate(projection.gameObject, ProjectionPool.Default.Parent)).GetComponent<Projection>();
        proj.name = "Projection";
    }

    protected void Reproject(Ray Ray, float CastLength, Vector3 ReferenceUp)
    {
        RaycastHit hit;
        if (Physics.Raycast(Ray, out hit, Mathf.Infinity, layers.value))
        {
            //Make sure we are active
            proj.gameObject.SetActive(true);

            //Update our position & rotation
            proj.transform.rotation = Quaternion.LookRotation(-hit.normal, ReferenceUp);
            proj.transform.position = hit.point;
        }
        else if (!alwaysVisible)
        {
            proj.gameObject.SetActive(false);
        }
    }

    private Vector3 Divide(Vector3 A, Vector3 B)
    {
        return new Vector3(A.x / B.x, A.y / B.y, A.z / B.z);
    }
}