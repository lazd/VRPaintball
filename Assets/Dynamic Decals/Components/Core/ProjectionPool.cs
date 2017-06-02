using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
* In-built projection pooling class. Use ProjectionPool.GetPool() to get a reference to a pool instance or use ProjectionPool.Default to get a reference to the default pool.
* You can the request projections from the pool as you see fit. Once you are done with them, instead of deleting them, use the Return method to return them back to the pool.
*/
public class ProjectionPool
{
    //Get Pool
    /**
     * Returns a pool with the specified name, if it exists. If it doesn't, returns the default pool.
     * @param Title The title of the pool to be returned.
     */
    public static ProjectionPool GetPool(string Title)
    {
        return DynamicDecals.System.GetPool(Title);
    }
    /**
     * Returns a pool with the specified ID, if it exists. If it doesn't, returns the default pool.
     * @param ID The ID of the pool to be returned.
     */
    public static ProjectionPool GetPool(int ID)
    {
        return DynamicDecals.System.GetPool(ID);
    }

    /**
    * Returns the default pool.
    */
    public static ProjectionPool Default
    {
        get
        {
            if (defaultPool == null) { defaultPool = DynamicDecals.System.PoolFromInstance(DynamicDecals.System.Settings.pools[0]); }
            return defaultPool;
        }
    }
    private static ProjectionPool defaultPool;

    //Constructor
    public ProjectionPool(PoolInstance Instance)
    {
        instance = Instance;
    }

    //Pool Details
    public string Title
    {
        get { return instance.title; }
    }
    public int ID
    {
        get { return instance.id; }
    }

    private PoolInstance instance;
    private int Limit
    {
        get
        {
            return instance.limits[QualitySettings.GetQualityLevel()];
        }
    }

    //Parent
    internal Transform Parent
    {
        get
        {
            if (parent == null)
            {
                parent = new GameObject(instance.title + " Pool").transform;
            }
            return parent;
        }
    }
    private Transform parent;

    //Pool
    private List<PoolItem> activePool;
    private List<PoolItem> inactivePool;

    //Update
    internal void Update(float DeltaTime)
    {
        //Update Pool Items
        if (activePool != null && activePool.Count > 0)
        {
            for (int i = activePool.Count - 1; i >= 0; i--)
            {
                //Remove deleted pool objects
                if (activePool[i].GameObject == null)
                {
                    activePool.RemoveAt(i);
                }
                else
                {
                    activePool[i].Update(DeltaTime);
                }
            }
        }
    }

    //Check Intersecting
    /**
    * Checks to see if a point is intersecting with any of the projections in the pool.
    * Returns true if an intersecting projection is found, otherwise returns false.
    * @param Point The type of projection being requested.
    * @param intersectionStrength How far within the bounds of the projection the point must be before it's considered an intersection. 0 will consider a point anywhere within a projections bounds as an intersections. 1 will only a point as intersecting if it is perfectly at the center of a projections bounds.
    */
    public bool CheckIntersecting(Vector3 Point, float intersectionStrength)
    {
        if (activePool != null && activePool.Count > 0)
        {
            for (int i = 0; i < activePool.Count; i++)
            {
                if (activePool[i].Projection.CheckIntersecting(Point) > intersectionStrength) return true;
            }
        }
        return false;
    }

    //Request
    /**
    * Returns a projection of the specified type from the pool.
    * Projection will be enabled and ready to use. Use the return method once your done with it, do not delete it.
    * @param Type The type of projection being requested.
    */
    public Projection Request(ProjectionType Type)
    {
        //Initialize active pool if required
        if (activePool == null) activePool = new List<PoolItem>();

        if (inactivePool != null && inactivePool.Count > 0)
        {
            //Grab the first item in the inactive pool
            PoolItem item = inactivePool[0];

            //Remove it from the inactive pool
            inactivePool.RemoveAt(0);

            //Add to the active pool
            activePool.Add(item);

            //Initialize pool item
            item.Reset(Type);

            return item.Projection;
        }
        else if (activePool.Count < Limit)
        {
            //Create our pool item
            PoolItem item = new PoolItem(this);

            //Initialize it
            item.Reset(Type);

            //Add it to the active pool
            activePool.Add(item);

            return item.Projection;
        }
        else
        {
            //Grab the oldest projection in the active pool
            PoolItem item = activePool[0];

            //Move it to the end of the pool
            activePool.RemoveAt(0);
            activePool.Add(item);

            //Initialize pool item
            item.Reset(Type);

            return item.Projection;
        }
    }

    /**
    * Returns a copy of the specified projection generated from the pool.
    * Projection will be enabled and ready to use. Use the return method once your done with it, do not delete it.
    * @param Projection The projection to copy. In 90% of use cases this should be a prefab.
    */
    public Projection RequestCopy(Projection Projection)
    {
        //Null Check
        if (Projection == null) return null;

        //Type Check
        if (Projection.GetType() == typeof(Decal))
        {
            Decal decal = (Decal)Request(ProjectionType.Decal);
            decal.CopyAllProperties((Decal)Projection);
            return decal;
        }
        if (Projection.GetType() == typeof(Eraser))
        {
            Eraser eraser = (Eraser)Request(ProjectionType.Eraser);
            eraser.CopyAllProperties((Eraser)Projection);
            return eraser;
        }
        if (Projection.GetType() == typeof(OmniDecal))
        {
            OmniDecal omnidecal = (OmniDecal)Request(ProjectionType.OmniDecal);
            omnidecal.CopyAllProperties((OmniDecal)Projection);
            return omnidecal;
        }

        throw new System.NotImplementedException("Projection Type not recognized, If your implementing your own projection types, you need to implement a copy method like the projection types above");
    }

    //Return
    /**
    * Returns the specified projection back to the pool. 
    * The projection will no longer be active or useable until it is requested from the pool again. All cached references to it should be nullified.
    * @param Projection The projection to return.
    */
    public void Return(Projection Projection)
    {
        if (Projection.PoolItem != null) Return(Projection.PoolItem);
    }
    internal void Return(PoolItem Item)
    {
        //Initialize if required
        if (inactivePool == null) inactivePool = new List<PoolItem>();

        //Remove it from the active pool
        activePool.Remove(Item);

        //Disable if still available (Projections can be deleted before items cleaned up on game end)
        if (Item.GameObject != null) Item.GameObject.SetActive(false);

        //Return projection to the inactive pool
        inactivePool.Add(Item);
    }
}
public class PoolItem
{
    //Pool
    public ProjectionPool Pool
    {
        get { return pool; }
    }
    private ProjectionPool pool;

    //GameObject
    public GameObject GameObject
    {
        get { return gameObject; }
    }
    private GameObject gameObject;

    //Projection
    public Projection Projection
    {
        get { return projection; }
    }
    private Projection projection;

    //Fade
    private FadeMethod fadeMethod;
    private float delay;
    private float inDuration;
    private float outDuration;

    //Culled
    private CullMethod cullMethod;
    private float cullDuration;

    //Tracking
    private float timeElapsed;
    private float timeSinceSeen;

    //Initializer
    public PoolItem(ProjectionPool Pool)
    {
        pool = Pool;
    }

    //Reset
    public void Reset(ProjectionType Type)
    {
        //Make sure we have a gameObject
        if (gameObject == null) gameObject = new GameObject("Projection");

        //Set parent
        gameObject.transform.SetParent(pool.Parent);

        //Make sure we are enabled
        gameObject.SetActive(true);

        //Just initialized, no time has elapsed yet
        timeElapsed = 0;

        //Reset fade properties
        fadeMethod = FadeMethod.None;
        inDuration = 0;
        delay = 0;
        outDuration = 0;

        //Reset culled properties
        cullMethod = CullMethod.None;
        timeSinceSeen = 0;
        cullDuration = 0;

        //Grab Projections
        Eraser eraser = gameObject.GetComponent<Eraser>();
        OmniDecal omnidecal = gameObject.GetComponent<OmniDecal>();
        Decal decal = gameObject.GetComponent<Decal>();

        //Disable all projections
        if (eraser != null) eraser.enabled = false;
        if (omnidecal != null) omnidecal.enabled = false;
        if (decal != null) decal.enabled = false;

        //Add correct projection
        switch (Type)
        {
            case ProjectionType.Decal:
                if (decal == null) decal = gameObject.AddComponent<Decal>();
                projection = decal;
                break;
            case ProjectionType.Eraser:
                if (eraser == null) eraser = gameObject.AddComponent<Eraser>();
                projection = eraser;
                break;
            case ProjectionType.OmniDecal:
                if (omnidecal == null) omnidecal = gameObject.AddComponent<OmniDecal>();
                projection = omnidecal;
                break;
        }

        //Update reference
        projection.PoolItem = this;

        //Enable selected projection
        projection.enabled = true;

        //Reset
        projection.AlphaModifier = 1;
        projection.ScaleModifier = 1;
    }

    //Update (run every update on active poolitems)
    public void Update(float deltaTime)
    {
        //Fade
        if (fadeMethod != FadeMethod.None)
        {
            float fadeValue = 1;
            timeElapsed += deltaTime;

            //Calculate FadeValue
            if (timeElapsed < inDuration) fadeValue = (1 - ((inDuration - timeElapsed) / inDuration));
            if (timeElapsed > inDuration + delay) fadeValue = ((inDuration + delay + outDuration - timeElapsed) / outDuration);

            //Fade out the projection
            if (fadeMethod == FadeMethod.Alpha || fadeMethod == FadeMethod.Both) projection.AlphaModifier = fadeValue;
            if (fadeMethod == FadeMethod.Scale || fadeMethod == FadeMethod.Both) projection.ScaleModifier = fadeValue;

            if (timeElapsed >= (inDuration + delay + outDuration))
            {
                //Return this item to the pool
                pool.Return(this);
            }
        }
        else
        {
            projection.AlphaModifier = 1;
            projection.ScaleModifier = 1;
        }

        if (cullMethod != CullMethod.None)
        {
            if (projection.Visible)
            {
                timeSinceSeen = 0;
            }
            else
            {
                timeSinceSeen += deltaTime;
            }


            if (timeSinceSeen > cullDuration)
            {
                //Return this item to the pool
                pool.Return(this);
            }
        }
    }

    //Utility
    public void Fade(FadeMethod Method, float InDuration, float Delay, float OutDuration)
    {
        fadeMethod = Method;
        delay = Delay;
        inDuration = InDuration;
        outDuration = OutDuration;
    }
    public void Culled(CullMethod Method, float Duration)
    {
        cullMethod = Method;
        cullDuration = Duration;
    }
    public void Return()
    {
        pool.Return(this);
    }
}

public enum FadeMethod { None, Alpha, Scale, Both };
public enum CullMethod { None, Remove };