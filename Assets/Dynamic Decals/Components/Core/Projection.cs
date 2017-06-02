using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/**
* Essentially the core of the entire system. The base abstract class all other projections inherit from. Defines the priority and masking information of the projection.
*/
[ExecuteInEditMode]
public abstract class Projection : MonoBehaviour {

    //Priority
    /**
    * Defines whether this projection appears above or below other projections. Higher priority projections will appear above lower priority projections. ie. a priority 10 projection will appear to overlap a priority 5 projection. 
    * Values should be positive and less than 40, when lighting needs to be applied. 
    * Erasers, omni-decals and unlit decals can use values as high as 200 without issue.
    */
    public int Priority
    {
        get { return priority; }
        set
        {
            priority = value;
            Reprioritise();
        }
    }

    //Transparency
    /**
    * Defines the transparency method. 
    * Cutout will simply cull any pixels under a certain alpha value and is usually the cheaper method.
    * Blend will blend the projection with the surface it's drawing on based on the alpha value. In the Deferred shading rendering path Blend will require an additional pass.
    */
    public TransparencyType TransparencyType
    {
        get { return transparencyType; }
        set
        {
            transparencyType = value;
            switch (transparencyType)
            {
                case TransparencyType.Blend:

                    break;
                case TransparencyType.Cutout:
                    cutoff = 0.2f;
                    break;
            }
            ReplaceMaterial();
            UpdateMaterial();
        }
    }
    /**
    * The alpha cutoff of the projection.
    * Any pixels with an alpha value below this value will not be rendered.
    */
    public float AlphaCutoff
    {
        get { return cutoff; }
        set
        {
            cutoff = Mathf.Clamp01(value);
            UpdateMaterial();
        }
    }

    //Masking
    /**
    * Defines which masking method we should apply to this projection. Either "DrawOnEverythingExcept" or "OnlyDrawOn".
    * Draw On Everything Except - will draw on all surface except those in the selected mask layers.
    * Only Draw On - will only draw on surfaces that are part  of the selected mask layers.
    */
    public MaskMethod MaskMethod
    {
        get { return maskMethod; }
        set
        {
            maskMethod = value;
            UpdateMaterial();
        }
    }
    /**
    * Defines whether this projection is affected by the first masking layer. 
    * To add surfaces to this mask layer add a Mask component to a renderable gameObject and toggle on the appropriate mask layer.
    */
    public bool MaskLayer1
    {
        get { return masks[0]; }
        set
        {
            masks[0] = value;
            UpdateMaterial();
        }
    }
    /**
    * Defines whether this projection is affected by the second masking layer.
    * To add surfaces to this mask layer add a Mask component to a renderable gameObject and toggle on the appropriate mask layer.
    */
    public bool MaskLayer2
    {
        get { return masks[1]; }
        set
        {
            masks[1] = value;
            UpdateMaterial();
        }
    }
    /**
    * Defines whether this projection is affected by the third masking layer.
    * To add surfaces to this mask layer add a Mask component to a renderable gameObject and toggle on the appropriate mask layer.
    */
    public bool MaskLayer3
    {
        get { return masks[2]; }
        set
        {
            masks[2] = value;
            UpdateMaterial();
        }
    }
    /**
    * Defines whether this projection is affected by the fourth masking layer.
    * To add surfaces to this mask layer add a Mask component to a renderable gameObject and toggle on the appropriate mask layer.
    */
    public bool MaskLayer4
    {
        get { return masks[3]; }
        set
        {
            masks[3] = value;
            UpdateMaterial();
        }
    }

    //Pooling
    public PoolItem PoolItem
    {
        get { return poolItem; }
        set { poolItem = value; }
    }
    /**
    * A modification to the size of the projection to get the final local scale. Essentially allows you to adjust the projections size as a percentaile of it's original size. 
    * Used internally to fade in/out pooled projectiles. If the projection was not generated from the in-built pool you can use this to modify the projection size yourself.
    */
    public float ScaleModifier
    {
        get { return scaleModifier; }
        set { scaleModifier = value; }
    }
    /**
    * A modification to the alpha color of the projection to get the final transparency. Essentially allows you to adjust the opacity of the projection as a percentile of it's original size.
    * Used internally to fade in/out pooled projectiles. If the projection was not generated from the in-built pool you can use this to modify the projection opacity yourself.
    */
    public float AlphaModifier
    {
        get { return alphaModifier; }
        set
        {
            alphaModifier = Mathf.Clamp01(value);
            UpdateMaterial();
        }
    }

    //visibility
    /**
    * Returns true if the object is currently in view, false if it's being culled.
    * Culling is asynchronous, so if the projection was created this frame, we will need to wait a frame before we know if the projection is visible. When this happens true will be returned.
    */
    public bool Visible
    {
        get
        {
            //In forward rendering use Unitys default culling
            if (this != null && DynamicDecals.System.SystemPath != SystemPath.Deferred)
            {
                MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer != null) return meshRenderer.isVisible;
                else return true;
            }
            else
            //If deferred rendering use our visibility flag
            {
                switch (visibility)
                {
                    case Visibility.Unknown | Visibility.Visible:
                        return true;
                    case Visibility.NotVisible:
                        return false;
                }
            }
            //Defaults to visible
            return true;
        }
    }

    //Rendering
    public Matrix4x4 RenderMatrix
    {
        //The local to world matrix of the decal
        get
        {
            if (scaleModifier == 1) return transform.localToWorldMatrix;
            return transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(scaleModifier, scaleModifier, scaleModifier));
        }
    }
    public abstract Material RenderMaterial
    {
        //The material used to render the projection
        get;
    }

    //Deferred Rendering
    public abstract int DeferredPass
    {
        get;
    }
    public abstract bool DeferredPrePass
    {
        get;
    }
    public bool[] DeferredBuffers
    {
        get;
        set;
    }
    public RenderTargetIdentifier[] DeferredTargets
    {
        get
        {
            if (deferredTargets == null || deferredTargets.Length < 1)
            {
                deferredTargets = (RenderTargetIdentifier[])DynamicDecals.System.PassesToTargets(DeferredBuffers, false).Clone();
            }
            return deferredTargets;
        }
        set { deferredTargets = value; }
    }
    public RenderTargetIdentifier[] DeferredHDRTargets
    {
        get
        {
            if (deferredHDRTargets == null || deferredHDRTargets.Length < 1)
            {
                deferredHDRTargets = (RenderTargetIdentifier[])DynamicDecals.System.PassesToTargets(DeferredBuffers, true).Clone();
            }
            return deferredHDRTargets;
        }
        set { deferredHDRTargets = value; }
    }

    #region Backing Fields    
    //Draw Order
    [SerializeField]
    private int priority;

    //Transparency
    [SerializeField]
    protected TransparencyType transparencyType;
    [SerializeField]
    private float cutoff = 0.2f;

    //Masking
    [SerializeField]
    private MaskMethod maskMethod;
    [SerializeField]
    private bool[] masks = new bool[4];
    Color MaskLayers;

    //Pooling
    private PoolItem poolItem;
    private float scaleModifier = 1;
    private float alphaModifier = 1;

    //Visibility
    private Visibility visibility = Visibility.Unknown;

    //Forward Transform
    protected Transform forwardRenderer;

    //Deferred Targets - Cached on startup
    private RenderTargetIdentifier[] deferredTargets;
    private RenderTargetIdentifier[] deferredHDRTargets;
    #endregion
    #region Ids
    int _Cutoff;
    int _MaskBase;
    int _MaskLayers;

    protected virtual void GrabIds()
    {
        _Cutoff = Shader.PropertyToID("_Cutoff");
        _MaskBase = Shader.PropertyToID("_MaskBase");
        _MaskLayers = Shader.PropertyToID("_MaskLayers");
    }
    #endregion

    #region Initialization
    private void Start()
    {
        Initialize();
    }
    private void OnEnable()
    {
        Initialize();
    }
    private void OnDisable()
    {
        //Destroy renderer & all active materials
        DestroyRenderer(false);

        //Deregister ourself
        Deregister();
    }

    private void Initialize()
    {
        //Grab our Material Ids
        GrabIds();

        //Setup our materials
        UpdateMaterialImmeditately();

        //Setup our renderer
        UpdateRenderer();

        //Register our decal
        Register();
    }
    #endregion
    #region Registration
    public float timeID
    {
        get;
        private set;
    }

    private void Register()
    {
        if (this != null)
        {
            #if UNITY_EDITOR
            PrefabType prefabType = PrefabUtility.GetPrefabType(gameObject);
            if (prefabType == PrefabType.ModelPrefab || prefabType == PrefabType.Prefab) return;
            #endif

            timeID = Time.timeSinceLevelLoad;
            DynamicDecals.System.AddProjection(this);
        }
    }
    private void Deregister()
    {
        if (this != null)
        {
            #if UNITY_EDITOR
            PrefabType prefabType = PrefabUtility.GetPrefabType(gameObject);
            if (prefabType == PrefabType.ModelPrefab || prefabType == PrefabType.Prefab) return;
            #endif

            DynamicDecals.System.RemoveProjection(this);
        }
    }

    public void Reprioritise()
    {
        //Reprioritises immediately
        Reprioritise(false);
    }
    public void Reprioritise(bool DelayedSort)
    {
        //Its cheaps to reprioritise immediately by simply removing it from the list and putting it back in the correct position.
        //However, if you know you will be reprioritising a large amount of projections at once, its cheaper to just sort all the projections at the end of the frame.
        //As this rarely happens though, we default to the cheap, single use method.
        if (DelayedSort)
        {
            //Sort all the projections at the end of the frame, before we render
            DynamicDecals.System.Sort();
        }
        else
        {
            //Remove ourself from the list of projections
            Deregister();
            //Add ourself back in the correct position
            Register();
        }
    }
    #endregion
    #region Update
    public virtual void UpdateProjection()
    {
        //Reset visibility flag
        visibility = Visibility.Unknown;

        //Update our Renderer - If it is no longer required, destroy it
        if (DynamicDecals.System.SystemPath == SystemPath.Forward && RequiresRenderer) UpdateRenderer();
        else DestroyRenderer();
    }
    #endregion

    #region Material
    //Replace - Called whenever the shader required needs to be changed
    public void ReplaceMaterial()
    {
        replaceDeferred = true;
        replaceForward = true;
    }
    private bool replaceDeferred = true;
    private bool replaceForward = true;

    //Update - Called whenever a material property needs to be changed
    public void UpdateMaterialImmeditately()
    {
        //Update our material properties
        UpdateMaterialProperties();

        //Update no longer required
        materialUpdated = true;
        materialApplied = false;
    }
    public void UpdateMaterial()
    {
        materialUpdated = false;
        materialApplied = false;
    }

    private bool materialUpdated = false;
    private bool materialApplied = false;

    public MaterialPropertyBlock MaterialProperties
    {
        get
        {
            //Update our material properties
            if (!materialUpdated)
            {
                UpdateMaterialProperties();
            }

            //Update no longer required
            materialUpdated = true;

            //Return updated properties
            return materialProperties;
        }
    }
    protected MaterialPropertyBlock materialProperties;

    protected virtual void UpdateMaterialProperties()
    {
        //Create our material property block if null
        if (materialProperties == null) materialProperties = new MaterialPropertyBlock();
        else materialProperties.Clear();

        //Update general properties
        UpdateTransparency();
        UpdateMasking();

        if (replaceDeferred)
        {
            //Replace our deferred material
            UpdateDeferredRendering();
            //No longer need to replace
            replaceDeferred = false;
        }
    }
    private void UpdateTransparency()
    {
        materialProperties.SetFloat(_Cutoff, cutoff);
    }
    private void UpdateMasking()
    {
        switch (maskMethod)
        {
            case MaskMethod.DrawOnEverythingExcept:
                materialProperties.SetFloat(_MaskBase, 1);

                MaskLayers.r = (masks[0]) ? 0 : 0.5f;
                MaskLayers.g = (masks[1]) ? 0 : 0.5f;
                MaskLayers.b = (masks[2]) ? 0 : 0.5f;
                MaskLayers.a = (masks[3]) ? 0 : 0.5f;

                materialProperties.SetVector(_MaskLayers, MaskLayers);
                break;
            case MaskMethod.OnlyDrawOn:
                materialProperties.SetFloat(_MaskBase, 0);

                MaskLayers.r = (masks[0]) ? 1 : 0.5f;
                MaskLayers.g = (masks[1]) ? 1 : 0.5f;
                MaskLayers.b = (masks[2]) ? 1 : 0.5f;
                MaskLayers.a = (masks[3]) ? 1 : 0.5f;

                materialProperties.SetVector(_MaskLayers, MaskLayers);
                break;
        }
    }

    protected virtual void UpdateDeferredRendering()
    {
        //Cache RenderTargets from buffers
        DeferredTargets = (RenderTargetIdentifier[])DynamicDecals.System.PassesToTargets(DeferredBuffers, false).Clone();
        DeferredHDRTargets = (RenderTargetIdentifier[])DynamicDecals.System.PassesToTargets(DeferredBuffers, true).Clone();
    }
    protected virtual void UpdateForwardRendering(MeshRenderer Renderer)
    {
        //Assign properties
        Renderer.sharedMaterial.renderQueue = 2455 + Priority;
    }
    #endregion
    #region Renderer
    //Overloadable bool to deny using a renderer under certain conditions
    protected virtual bool RequiresRenderer
    {
        get { return true; }
    }

    //Update & Destroy
    private void UpdateRenderer()
    {
        //Declare forward mesh renderer
        MeshRenderer meshRenderer = null;

        //Make sure we have an active renderer
        if (forwardRenderer == null)
        {
            //Check if already exists
            foreach (Transform child in transform) if (child.name == "Forward Renderer") forwardRenderer = child;

            //Create a new one if none exist
            if (forwardRenderer == null)
            {
                //Create our forward renderer
                forwardRenderer = new GameObject("Forward Renderer").transform;
                forwardRenderer.SetParent(transform, false);
                forwardRenderer.gameObject.layer = gameObject.layer;
                forwardRenderer.gameObject.hideFlags = HideFlags.HideAndDontSave;

                //Setup our components
                MeshFilter meshFilter = forwardRenderer.gameObject.AddComponent<MeshFilter>();
                meshFilter.mesh = DynamicDecals.System.Cube;

                meshRenderer = forwardRenderer.gameObject.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;

                #if UNITY_EDITOR && !UNITY_5_4
                EditorUtility.SetSelectedRenderState(meshRenderer, EditorSelectedRenderState.Hidden);
                #endif

                //Tell the system it needs to update it's material
                UpdateMaterial();
            }
        }

        //Grab our renderer
        if (meshRenderer == null) meshRenderer = forwardRenderer.GetComponent<MeshRenderer>();

        //Check if we need to replace our material
        if (replaceForward || meshRenderer.sharedMaterial == null)
        {
            //Destory old materials
            DestroyMaterials(meshRenderer);

            //Apply new materials
            UpdateForwardRendering(meshRenderer);
            replaceForward = false;
        }

        if (!materialApplied)
        {
            //Update MaterialProperties
            meshRenderer.SetPropertyBlock(MaterialProperties);

            //Material update has been applied
            materialApplied = true;
        }

        //Update Scale
        float scale = Mathf.Clamp(scaleModifier, 0.00000001f, 1000000000);
        forwardRenderer.localScale = new Vector3(scale, scale, scale);
    }
    private void DestroyRenderer(bool ForceDestroy = true)
    {
        //Destroy Attached forward renderer & materials
        if (forwardRenderer != null && Application.isPlaying)
        {
            DestroyMaterials(forwardRenderer.GetComponent<MeshRenderer>());
            GameObject.Destroy(forwardRenderer.gameObject);
        }

        #if UNITY_EDITOR
        if (Application.isEditor && !Application.isPlaying)
        {
            EditorApplication.delayCall += () =>
            {
                if (forwardRenderer != null)
                {
                    DestroyMaterials(forwardRenderer.GetComponent<MeshRenderer>());
                    GameObject.DestroyImmediate(forwardRenderer.gameObject, true);
                }
            };
        }
        #endif
    }

    //Destroys all materials attached to the provided renderer
    private void DestroyMaterials(MeshRenderer Renderer)
    {
        if (Renderer.sharedMaterials != null && Renderer.sharedMaterials.Length > 0)
        {
            for (int i = 0; i < Renderer.sharedMaterials.Length; i++)
            {
                if (Application.isPlaying) Destroy(Renderer.sharedMaterials[i]);
                else DestroyImmediate(Renderer.sharedMaterials[i], true);
            }
        }
    }
    #endregion

    #region Visibility
    public void SetVisibility(bool Visible)
    {
        //If Unknown can be set to not visible
        if (!Visible && visibility == Visibility.Unknown) visibility = Visibility.NotVisible;

        //Once known can only be set to visible
        if (Visible) visibility = Visibility.Visible;
    }
    #endregion

    #region Pool Methods
    /**
    * Allows us to set a fade in and out duration for our projection, as well as a delay between. How we fade the projection depends on the fade method chosen.
    * @param Method Defines how a projection is Faded in or out. Either Alpha (Opacity), Scalar (Scale), Both (Opacity & Scale) or None (Disabled / Default).
    * @param InDuration Defines how long, in seconds, it takes to fade in the projection.
    * @param Delay Defines how long, in seconds, between fading in the projection and fading it out.
    * @param OutDuration Defines how long, in seconds, it takes to fade out the projection.
    */
    public void Fade(FadeMethod Method, float InDuration, float Delay, float OutDuration)
    {
        if (poolItem != null)
        {
            poolItem.Fade(Method, InDuration, Delay, OutDuration);

            //Setup our for our Fade
            float fadeValue = 1;
            if (InDuration > 0) fadeValue = 0;
            else fadeValue = 1;

            //Fade out the projection
            if (Method == FadeMethod.Alpha || Method == FadeMethod.Both) AlphaModifier = fadeValue;
            if (Method == FadeMethod.Scale || Method == FadeMethod.Both) ScaleModifier = fadeValue;

            //Apply Immediately
            UpdateMaterialImmeditately();
        }
    }
    /**
    * Allows us to remove this projection when it has been offscreen for a set duration. Only works on projections that where created using the in-built pool.
    * @param Method Defines if you want this pooling feature enabled or not. Either Remove (Enabled), or None (Disabled / Default).
    * @param Duration Defines how long, in seconds, the projection needs to be offscreen before we return it back to the pool.
    */
    public void Culled(CullMethod Method, float Duration)
    {
        if (poolItem != null)
        {
            poolItem.Culled(Method, Duration);
        }
    }
    /**
    * Returns a pooled projection back to the pool so it can be used again.
    * This is used when a projection that has been generated from the in-built pool is no longer required. Instead of deleting it, we return it back to the pool to be used again.
    */
    public void Return()
    {
        if (poolItem != null)
        {
            poolItem.Return();
        }
    }
    #endregion

    #region Helper Methods
    /**
    * Checks to see how much a point is intersecting with the projection bounds.
    * The closer the point is to the center of the projection bounds, the higher the returned value will be.
    * A perfectly intersecting point (ie. At the centre of the projection bounds) will return 1, while a non-intersecting point will return 0.
    * We can use this method to cheaply determine how much overlap is occuring between projections, or to see if some other object would be projected onto and act accordingly.
    * @param Point defines the point (in world space) to check.
    */
    public float CheckIntersecting(Vector3 Point)
    {
        Vector3 localPoint = transform.InverseTransformPoint(Point);
        return Mathf.Clamp01(2 * (0.5f - Mathf.Max(Mathf.Abs(localPoint.x), Mathf.Abs(localPoint.y), Mathf.Abs(localPoint.z))));
    }
    /**
    * Copies the base properites (Priority, Scale & Transparency) of the target projection to this projection.
    * @param Target defines the projection whose properties we want to copy to this projection.
    */
    public void CopyBaseProperties(Projection Target)
    {
        //Priority
        Priority = Target.Priority;

        //Scale
        transform.localScale = Target.transform.localScale;

        //Transparency
        TransparencyType = Target.TransparencyType;
        AlphaCutoff = Target.AlphaCutoff;
    }
    /**
    * Copies the mask properites of the target projection to this projection.
    * @param Target defines the projection whose properties we want to copy to this projection.
    */
    public void CopyMaskProperties(Projection Target)
    {
        MaskMethod = Target.MaskMethod;
        MaskLayer1 = Target.MaskLayer1;
        MaskLayer2 = Target.MaskLayer2;
        MaskLayer3 = Target.MaskLayer3;
        MaskLayer4 = Target.MaskLayer4;
    }
    #endregion
    #region Projection Prefabs
    #if UNITY_EDITOR
    private void OnValidate()
    {
        //Only applies to prefabs
        if (PrefabUtility.GetPrefabType(gameObject) == PrefabType.Prefab)
        {
            //Check if it has a renderer attached
            foreach (Transform child in transform) if (child.name == "Forward Renderer") forwardRenderer = child;

            //Destroy Materials
            if (forwardRenderer != null)
            {
                //Destory renderer materials
                DestroyMaterials(forwardRenderer.GetComponent<MeshRenderer>());

                //Delayed Call
                EditorApplication.delayCall += () =>
                {
                    //Destroy Attached forward renderer
                    DestroyImmediate(forwardRenderer.gameObject, true);

                    //Trigger re-import to update prefab
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));

                    //Set Prefab Icon
                    SerializedObject prefab = new SerializedObject(gameObject);
                    prefab.FindProperty("m_Icon").objectReferenceValue = null;
                    EditorUtility.SetDirty(gameObject);
                    prefab.ApplyModifiedProperties();
                };
            }
        }
    }
    #endif
    #endregion
}

public enum MaskMethod { DrawOnEverythingExcept, OnlyDrawOn };
public enum TransparencyType { Cutout, Blend };
public enum GlossType { Metallic, Specular };
public enum Visibility { Unknown, NotVisible, Visible }