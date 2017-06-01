using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/*! \mainpage Introduction
 * Welcome to the Dynamic Decals script documentation. Here you will find everything you need to interact with the front end of the Dynamic Decals system.
 * The Documentation covers all projections available within the system, as well as all printers, projectors & the in-built pooling system.
 *
 * If you want to extend or tinker with the system, the backend code is completely open and thoroughly commented & so are all the shaders, so feel free. 
 * However please note that as it's not necessary for 99% of users, is subject to change and would clutter the documentation, the backend of the system & shaders have been left out of the documentation.
 *
 * If you get stuck hit me up at Support@LlockhamIndustustries.com. We've all been stuck at one stage or another, I wouldn't wish it on anyone. Always more than happy to help. 
 * If you use the system to make something cool, send my screenshots, I want to see.
 *
 * Happy scripting, Dan.
 */

/**
* The core class of the system, responsible for the majority of the systems functionality. 
* For scripting purposes, it's almost entirely a black box, you should rarely need to access or modify anything within it.
* It's well stuctured and commented all the same though, so if your interested, open it up and have a look around.
*/
[ExecuteInEditMode]
public class DynamicDecals : MonoBehaviour
{
    //MultiScene Editor Singleton
    public static DynamicDecals System
    {
        get
        {
            if (system == null)
            {
                //Create our system
                GameObject go = new GameObject("Dynamic Decals");
                go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                go.AddComponent<DynamicDecals>();
            }
            return system;
        }
    }
    private static DynamicDecals system;

    private void OnEnable()
    {
        //Singleton
        if (system == null) system = this;
        else if (system != this)
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject, true);
            return;
        }

        //Initialize the system
        Initialize();
    }
    private void OnDisable()
    {
        //Terminate the system
        Terminate();
    }
    private void Start()
    {
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);
    }

    #if UNITY_EDITOR
    private void OnApplicationQuit()
    {
        //Reset the system when transitioning back to edit mode
        Terminate();
        Initialize();
    }
    #endif

    #region Rendering
    //Rendering Path
    public SystemPath SystemPath
    {
        get { return (Settings.forceForward) ? SystemPath.Forward : renderingPath; }
    }
    public SystemPath renderingPath;

    //Methods
    private void UpdateRenderingPath()
    {
        //Get our primary camera
        Camera target = null;
        if (Camera.main != null) target = Camera.main;
        else if (Camera.current != null) target = Camera.current;

        if (target != null)
        {
            //Determine our rendering method
            if (target.actualRenderingPath == RenderingPath.Forward || target.actualRenderingPath == RenderingPath.DeferredShading)
            {
                switch (target.actualRenderingPath)
                {
                    case RenderingPath.Forward:
                        renderingPath = SystemPath.Forward;
                        break;
                    case RenderingPath.DeferredShading:
                        renderingPath = SystemPath.Deferred;
                        break;
                }
            }
            else Debug.LogWarning("Current Rendering Path not supported! Please use either Forward or Deferred");
        }
    }
    public void RestoreDepthTextureModes()
    {
        //Iterate over every camera and restore it to it's original depth texture mode
        for (int i = 0; i < cameraData.Count; i++)
        {
            Camera camera = cameraData.ElementAt(i).Key;
            if (camera != null) cameraData.ElementAt(i).Value.RestoreDepthTextureMode(camera);
        }
    }
    #endregion
    #region Settings
    public DynamicDecalSettings Settings
    {
        get
        {
            //Try load our settings
            if (settings == null) settings = Resources.Load<DynamicDecalSettings>("Settings");

            //If not found create them
            if (settings == null) settings = ScriptableObject.CreateInstance<DynamicDecalSettings>();
            return settings;
        }
    }
    private DynamicDecalSettings settings;
    #endregion
    #region Warnings
    private bool cameraClipping = false;
    #endregion

    #region Shaders
    public void UpdateLODs()
    {
        Shader.Find("Decal/Metallic").maximumLOD = (Settings.forceForward) ? 0 : 1000;
        Shader.Find("Decal/Specular").maximumLOD = (Settings.forceForward) ? 0 : 1000;
        Shader.Find("Decal/Unlit").maximumLOD = (Settings.forceForward) ? 0 : 1000;
        Shader.Find("Decal/OmniDecal").maximumLOD = (Settings.forceForward) ? 0 : 1000;
        Shader.Find("Decal/Eraser/Read").maximumLOD = (Settings.forceForward) ? 0 : 1000;
    }

    public Shader NormalShader
    {
        get
        {
            if (normalShader == null)
            {
                normalShader = Shader.Find("Decal/Internal/DepthTexture/Normal");
            }
            return normalShader;
        }
    }
    private Shader normalShader;
    #endregion
    #region Materials
    public Material Mat_DeferredBlit
    {
        get
        {
            if (deferredBlit == null) deferredBlit = new Material(Shader.Find("Decal/Internal/Blit"));
            return deferredBlit;
        }
    }

    public Material Mat_Decal_Metallic
    {
        get
        {
            if (metallic == null)
            {
                Shader shader = Shader.Find("Decal/Metallic");
                if (Settings.forceForward) shader.maximumLOD = 0;
                else shader.maximumLOD = 1000;

                metallic = new Material(shader);
                metallic.DisableKeyword("_AlphaTest");
                metallic.EnableKeyword("_Blend");
            }
            return metallic;
        }
    }
    public Material Mat_Decal_MetallicCutout
    {
        get
        {
            if (metallicCutout == null)
            {
                Shader shader = Shader.Find("Decal/Metallic");
                if (Settings.forceForward) shader.maximumLOD = 0;
                else shader.maximumLOD = 1000;

                metallicCutout = new Material(shader);
                metallicCutout.EnableKeyword("_AlphaTest");
                metallicCutout.DisableKeyword("_Blend");                
            }
            return metallicCutout;
        }
    }
    public Material Mat_Decal_Specular
    {
        get
        {
            if (specular == null)
            {
                Shader shader = Shader.Find("Decal/Specular");
                if (Settings.forceForward) shader.maximumLOD = 0;
                else shader.maximumLOD = 1000;

                specular = new Material(shader);
                specular.DisableKeyword("_AlphaTest");
                specular.EnableKeyword("_Blend");
            }
            return specular;
        }
    }
    public Material Mat_Decal_SpecularCutout
    {
        get
        {
            if (specularCutout == null)
            {
                Shader shader = Shader.Find("Decal/Specular");
                if (Settings.forceForward) shader.maximumLOD = 0;
                else shader.maximumLOD = 1000;

                specularCutout = new Material(shader);
                specularCutout.EnableKeyword("_AlphaTest");
                specularCutout.DisableKeyword("_Blend");
            }
            return specularCutout;
        }
    }
    public Material Mat_Decal_Unlit
    {
        get
        {
            if (unlit == null)
            {
                Shader shader = Shader.Find("Decal/Unlit");
                if (Settings.forceForward) shader.maximumLOD = 0;
                else shader.maximumLOD = 1000;

                unlit = new Material(shader);
                unlit.DisableKeyword("_AlphaTest");
                unlit.EnableKeyword("_Blend");
            }
            return unlit;
        }
    }
    public Material Mat_Decal_UnlitCutout
    {
        get
        {
            if (unlitCutout == null)
            {
                Shader shader = Shader.Find("Decal/Unlit");
                if (Settings.forceForward) shader.maximumLOD = 0;
                else shader.maximumLOD = 1000;

                unlitCutout = new Material(shader);
                unlitCutout.EnableKeyword("_AlphaTest");
                unlitCutout.DisableKeyword("_Blend");
            }
            return unlitCutout;
        }
    }

    public Material Mat_Decal_Roughness
    {
        get
        {
            if (roughness == null)
            {
                roughness = new Material(Shader.Find("Decal/Roughness"));
                roughness.DisableKeyword("_AlphaTest");
                roughness.EnableKeyword("_Blend");
            }
            return roughness;
        }
    }
    public Material Mat_Decal_RoughnessCutout
    {
        get
        {
            if (roughnessCutout == null)
            {
                roughnessCutout = new Material(Shader.Find("Decal/Roughness"));
                roughnessCutout.EnableKeyword("_AlphaTest");
                roughnessCutout.DisableKeyword("_Blend");
            }
            return roughnessCutout;
        }
    }

    public Material Mat_Decal_Normal
    {
        get
        {
            if (normal == null)
            {
                normal = new Material(Shader.Find("Decal/Normal"));
                normal.DisableKeyword("_AlphaTest");
                normal.EnableKeyword("_Blend");
            }
            return normal;
        }
    }
    public Material Mat_Decal_NormalCutout
    {
        get
        {
            if (normalCutout == null)
            {
                normalCutout = new Material(Shader.Find("Decal/Normal"));
                normalCutout.EnableKeyword("_AlphaTest");
                normalCutout.DisableKeyword("_Blend");
            }
            return normalCutout;
        }
    }

    public Material Mat_OmniDecal
    {
        get
        {
            if (omnidecal == null)
            {
                Shader shader = Shader.Find("Decal/OmniDecal");
                if (Settings.forceForward) shader.maximumLOD = 0;
                else shader.maximumLOD = 1000;

                omnidecal = new Material(shader);
                omnidecal.DisableKeyword("_AlphaTest");
                omnidecal.EnableKeyword("_Blend");
            }
            return omnidecal;
        }
    }
    public Material Mat_OmniDecalCutout
    {
        get
        {
            if (omnidecalCutout == null)
            {
                Shader shader = Shader.Find("Decal/OmniDecal");
                if (Settings.forceForward) shader.maximumLOD = 0;
                else shader.maximumLOD = 1000;

                omnidecalCutout = new Material(shader);
                omnidecalCutout.EnableKeyword("_AlphaTest");
                omnidecalCutout.DisableKeyword("_Blend");
            }
            return omnidecalCutout;
        }
    }

    public Material Mat_Eraser
    {
        get
        {
            if (eraser == null)
            {
                Shader shader = Shader.Find("Decal/Eraser/Read");
                if (Settings.forceForward) shader.maximumLOD = 0;
                else shader.maximumLOD = 1000;

                eraser = new Material(shader);
                eraser.DisableKeyword("_AlphaTest");
                eraser.EnableKeyword("_Blend");
            }
            return eraser;
        }
    }
    public Material Mat_EraserCutout
    {
        get
        {
            if (eraserCutout == null)
            {
                Shader shader = Shader.Find("Decal/Eraser/Read");
                if (Settings.forceForward) shader.maximumLOD = 0;
                else shader.maximumLOD = 1000;

                eraserCutout = new Material(shader);
                eraserCutout.EnableKeyword("_AlphaTest");
                eraserCutout.DisableKeyword("_Blend");
            }
            return eraserCutout;
        }
    }
    public Material Mat_EraserGrab
    {
        get
        {
            if (eraserGrab == null)
            {
                eraserGrab = new Material(Shader.Find("Decal/Eraser/Write"));
            }
            return eraserGrab;
        }
    }

    //Backing Fields
    private Material deferredBlit;

    private Material metallic;
    private Material metallicCutout;

    private Material specular;
    private Material specularCutout;

    private Material unlit;
    private Material unlitCutout;

    private Material roughness;
    private Material roughnessCutout;

    private Material normal;
    private Material normalCutout;

    private Material omnidecal;
    private Material omnidecalCutout;

    private Material eraser;
    private Material eraserCutout;
    private Material eraserGrab;
    #endregion
    #region Meshes
    public Mesh Cube
    {
        get
        {
            if (cube == null) cube = Resources.Load<Mesh>("Decal");
            return cube;
        }
    }
    private Mesh cube;

    public Mesh CameraBlit
    {
        get
        {
            if (cameraBlit == null)
            {
                cameraBlit = Mesh.Instantiate(Cube);
                ScaleMesh(cameraBlit, 100);
            }
            return cameraBlit;
        }
    }
    private Mesh cameraBlit;

    private void ScaleMesh(Mesh Mesh, float Scale)
    {
        var vertices = new Vector3[Mesh.vertices.Length];

        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = Mesh.vertices[i];
            vertex.x = vertex.x * Scale;
            vertex.y = vertex.y * Scale;
            vertex.z = vertex.z * Scale;

            vertices[i] = vertex;
        }

        Mesh.vertices = vertices;
        Mesh.RecalculateBounds();
    }
    #endregion
    #region Projections
    //Add or Remove Projections
    public void AddProjection(Projection Projection)
    {
        //Initialize list
        if (projections == null) projections = new List<Projection>();

        //Add projection
        if (!projections.Contains(Projection))
        {
            //If count is 0 add
            if (projections.Count == 0)
            {
                projections.Add(Projection);
            }
            else
            {
                //Projection are ordered from lowest priority to highest
                for (int i = 0; i < projections.Count; i++)
                {
                    //If we are lower priority than i, insert before i
                    if (Projection.Priority < projections[i].Priority)
                    {
                        projections.Insert(i, Projection);
                        break;
                    }
                    //If we are of equal priority to i, but of lower timeID, insert before i
                    if (Projection.Priority == projections[i].Priority && Projection.timeID < projections[i].timeID)
                    {
                        projections.Insert(i, Projection);
                        break;
                    }
                    //If we aren't lower than anything in the list, just add ourself
                    if (i == projections.Count - 1)
                    {
                        projections.Add(Projection);
                        break;
                    }
                }
            }

            //Static check
            if (Projection.GetType() == typeof(Eraser)) staticCount++;
        }
    }
    public void RemoveProjection(Projection Projection)
    {
        if (projections != null)
        {
            //Remove projection
            if (projections.Remove(Projection))
            {
                //Static check
                if (Projection.GetType() == typeof(Eraser)) staticCount = Mathf.Clamp(staticCount - 1, 0, 10000000);
            }
        }
    }

    //Sort Projections
    public void Sort()
    {
        sort = true;
    }
    private bool sort;
    private void ReorderProjections()
    {
        //We only need to sort in deferred rendering.
        //In forward rendering the order of our projections is meaningless.
        if (sort && renderingPath == SystemPath.Deferred)
        {
            //Sort from lowest priority to highest
            projections.Sort((x, y) =>
            {
                if (x.Priority > y.Priority) return 1;
                else if (x.Priority < y.Priority) return -1;
                else if (x.timeID > y.timeID) return 1;
                else if (x.timeID < y.timeID) return -1;
                else return 0;
            });

            //No longer need to sort
            sort = false;
        }
    }

    //Update Projections
    private void UpdateProjections()
    {
        if (projections != null)
        {
            for (int i = 0; i < projections.Count; i++)
            {
                projections[i].UpdateProjection();
            }
        }
    }

    //Projections
    private List<Projection> projections;

    //Projection Culling Spheres
    private BoundingSphere[] projectionSpheres;

    //Static Requirements
    public bool StaticPass
    {
        get { return (staticCount > 0) ? true : false; }
    }
    private int staticCount;
    #endregion
    #region Masks
    //Masks
    private List<ProjectionMask> masks;

    //Add / Remove Masks
    public void AddMask(ProjectionMask Mask)
    {
        if (masks == null)
        {
            masks = new List<ProjectionMask>();
            masks.Add(Mask);
        }
        else if (!masks.Contains(Mask))
        {
            masks.Add(Mask);
        }
        
    }
    public void RemoveMask(ProjectionMask Mask)
    {
        if (masks != null && masks.Count > 0)
        {
            masks.Remove(Mask);
        }
    }

    //Mask Materials
    private Dictionary<MaskValue, Material> MaskMaterials;
    public Material GetMaskMaterial(MaskValue Value)
    {
        //Initialize material dictionary
        if (MaskMaterials == null) MaskMaterials = new Dictionary<MaskValue, Material>();

        Material material;
        if (!MaskMaterials.TryGetValue(Value, out material))
        {
            material = new Material(Shader.Find("Decal/Internal/Mask"));
            material.SetFloat("_Layer1", (Value.layer1) ? 1 : 0);
            material.SetFloat("_Layer2", (Value.layer2) ? 1 : 0);
            material.SetFloat("_Layer3", (Value.layer3) ? 1 : 0);
            material.SetFloat("_Layer4", (Value.layer4) ? 1 : 0);
        }
        return material;
    }
    private void ClearMaskMaterials()
    {
        if (MaskMaterials != null)
        {
            foreach (Material material in MaskMaterials.Values)
            {
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material, true);
            }
            MaskMaterials.Clear();
        }
    }
    #endregion
    #region Cameras
    //Camera Data
    internal Dictionary<Camera, CameraData> cameraData = new Dictionary<Camera, CameraData>();
    internal CameraData GetData(Camera Camera)
    {
        //Declare our Camera Data
        CameraData data = null;

        //Check if this camera already has camera data
        if (!cameraData.TryGetValue(Camera, out data))
        {
            //Generate data
            data = new CameraData(Camera);

            //Store data
            cameraData[Camera] = data;
        }

        //Initialize if required
        if (data != null)
        {
            if (!data.enabled && Camera.GetComponent<ProjectionBlocker>() == null) data.Initialize(Camera, this);
            else if (data.enabled && Camera.GetComponent<ProjectionBlocker>() != null) data.Terminate(Camera);
        }
            

        //Return our updated Camera Data
        return data;
    }

    //Custom Rendering Camera
    private Camera CustomCamera
    {
        get
        {
            if (customCamera == null)
            {
                GameObject cameraObject = new GameObject("Custom Camera");
                customCamera = cameraObject.AddComponent<Camera>();

                cameraObject.AddComponent<ProjectionBlocker>();
                customCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                customCamera.enabled = false;
            }
            return customCamera;
        }
    }
    private Camera customCamera;

    //Default CameraRect
    public Rect FullRect = new Rect(0, 0, 1, 1);
    #endregion
    #region Pools
    private Dictionary<int, ProjectionPool> Pools;
    internal ProjectionPool PoolFromInstance(PoolInstance Instance)
    {
        //Make sure we are initialized
        if (Pools == null) Pools = new Dictionary<int, ProjectionPool>();

        ProjectionPool pool;
        if (!Pools.TryGetValue(Instance.id, out pool))
        {
            pool = new ProjectionPool(Instance);
            Pools.Add(Instance.id, pool);
        }
        return pool;
    }

    /**
     * Returns a pool with the specified name, if it exists. If it doesn't, returns the default pool.
     * @param Title The title of the pool to be returned.
     */
    public ProjectionPool GetPool(string Title)
    {
        //Check Settings for an ID
        for (int i = 0; i < Settings.pools.Length; i++)
        {
            if (settings.pools[i].title == Title)
            {
                return PoolFromInstance(settings.pools[i]);
            }
        }
        //No valid pool set up, log a Warning and return the default pool
        Debug.LogWarning("No valid pool with the title : " + Title + " found. Returning default pool");
        return PoolFromInstance(settings.pools[0]);
    }
    /**
     * Returns a pool with the specified ID, if it exists. If it doesn't, returns the default pool.
     * @param ID The ID of the pool to be returned.
     */
    public ProjectionPool GetPool(int ID)
    {
        //Check Settings for an ID
        for (int i = 0; i < Settings.pools.Length; i++)
        {
            if (settings.pools[i].id == ID)
            {
                return PoolFromInstance(settings.pools[i]);
            }
        }
        //No valid pool set up, log a Warning and return the default pool
        Debug.LogWarning("No valid pool with the ID : " + ID + " found. Returning default pool");
        return PoolFromInstance(settings.pools[0]);
    }
    #endregion

    //Initialize / Terminate
    private void Initialize()
    {
        //Register our projection events to all cameras
        Camera.onPreCull += CullProjections;
        Camera.onPreRender += RenderProjections;

        //Initialize mask material dictionary
        MaskMaterials = new Dictionary<MaskValue, Material>();
    }
    private void Terminate()
    {
        //Deregister our projection events
        Camera.onPreCull -= CullProjections;
        Camera.onPreRender -= RenderProjections;

        //Iterate over our camera data
        foreach (var cb in cameraData)
        {
            //Terminate camera data
            cb.Value.Terminate(cb.Key);
        }

        //Clear camera Data
        cameraData.Clear();

        //Clear mask materials
        ClearMaskMaterials();
    }

    //Primary Methods
    private void LateUpdate()
    {
        #if UNITY_EDITOR
        //If in editor, settings can change dynamically, update them constantly
        settings = Resources.Load<DynamicDecalSettings>("Settings");
        #endif

        //Update our Pools
        if (Pools != null && Pools.Count > 0) for (int i = 0; i < Pools.Count; i++) Pools.ElementAt(i).Value.Update(Time.deltaTime);

        //Check our Rendering Path
        UpdateRenderingPath();

        //Reorder our projections
        ReorderProjections();

        //Update our projections
        UpdateProjections();

        //Update Culling Spheres
        RequestCullUpdate();
    }
    private void CullProjections(Camera Camera)
    {
        //Grab our camera data
        CameraData data = GetData(Camera);

        //Check if valid
        if (data != null && data.enabled && (data.sceneCamera || data.previewCamera || Camera.isActiveAndEnabled))
        {
            //Update to the correct rendering path
            data.UpdateRenderingMethod(Camera, this);

            //Set projection culling spheres
            if (renderingPath == SystemPath.Deferred && projections != null && projections.Count > 0)
            {
                data.projectionCulling.SetBoundingSpheres(projectionSpheres);
            }
        }
    }
    private void RenderProjections(Camera Camera)
    {
        //Grab our camera data
        CameraData data = GetData(Camera);

        //Check if valid
        if (data != null && data.enabled && (data.sceneCamera || data.previewCamera || Camera.isActiveAndEnabled))
        {            
            //Camera far clipping plane warning
            if (!cameraClipping && !data.sceneCamera && !data.previewCamera && Camera.farClipPlane > 1000000)
            {
                Debug.LogWarning("Cameras far clipping plane is too high to maintain an accurate Depth Buffer - Projections may appear strange or not at all. You'll also have a host of other issues, z-fighting among your objects etc.");
                cameraClipping = true;
            }

            //Update out mask buffer
            UpdateMaskBuffer(Camera, data);

            //Update our projection buffer
            UpdateProjectionBuffer(Camera, data);

            //Draw our custom normals
            CustomNormals(Camera, data);
        }
    }

    //Culling
    internal void RequestCullUpdate()
    {
        //Projections - Only required in deferred, unity handles culling in forward
        if (renderingPath == SystemPath.Deferred && projections != null && projections.Count > 0)
        {
            //Initialize || Resize array as necessary
            if (projectionSpheres == null || projectionSpheres.Length < projections.Count)
            {
                projectionSpheres = new BoundingSphere[projections.Count * 2];
            }

            //Update array & Cache cull position
            for (int i = 0; i < projections.Count; i++)
            {
                //Cache
                Transform projection = projections[i].transform;
                Vector3 scale = projection.lossyScale;

                //Update projections culling
                projectionSpheres[i].position = projection.position;
                projectionSpheres[i].radius = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            }
        }
    }

    //High Precision Normals
    private void CustomNormals(Camera Camera, CameraData Data)
    {
        //Custom High Precision Normals
        if (Data.customDTM == CustomDepthTextureMode.Normal)
        {
            //Grab our temporary render texture
            RenderTexture Normal = RenderTexture.GetTemporary(Camera.pixelWidth, Camera.pixelHeight, 24, RenderTextureFormat.ARGB2101010);

            //Set up our camera
            CustomCamera.CopyFrom(Camera);
            CustomCamera.targetTexture = Normal;
            CustomCamera.renderingPath = RenderingPath.Forward;
            CustomCamera.depthTextureMode = DepthTextureMode.None;
            CustomCamera.clearFlags = CameraClearFlags.SolidColor;

            //Always use standard rect to draw
            CustomCamera.rect = FullRect;

            //Render into out temporary render textures
            CustomCamera.RenderWithShader(NormalShader, "RenderType");

            //Assign & release our Camera Normal Texture
            Normal.SetGlobalShaderProperty("_CameraNormalTexture");
            RenderTexture.ReleaseTemporary(Normal);

            //Tell Shaders to use HighPrecision depth normals
            Shader.DisableKeyword("_LowPrecision");
            Shader.EnableKeyword("_HighPrecision");
        }
        else
        {
            //Tell Shaders to use LowPrecision depth normals
            Shader.DisableKeyword("_HighPrecision");
            Shader.EnableKeyword("_LowPrecision");
        }
    }

    //Mask Buffer
    private void UpdateMaskBuffer(Camera Camera, CameraData Data)
    {
        //Clear buffer
        Data.maskBuffer.Clear();

        //Draw our masks
        switch (Camera.actualRenderingPath)
        {
            //Forward Rendering Path
            case RenderingPath.Forward:
                DrawMasks(Camera, Data.maskBuffer, BuiltinRenderTextureType.CurrentActive);
                break;

            //Deferred Rendering Path
            case RenderingPath.DeferredShading:
                DrawMasks(Camera, Data.maskBuffer, BuiltinRenderTextureType.CameraTarget);
                break;
        }
    }
    private void DrawMasks(Camera Camera, CommandBuffer Buffer, RenderTargetIdentifier DepthSource)
    {
        //Create mask render texture
        int MaskBuffer = Shader.PropertyToID("_MaskBuffer");

        Buffer.GetTemporaryRT(MaskBuffer, -1, -1);

        //Set as render target
        Buffer.SetRenderTarget(MaskBuffer, DepthSource);

        //Clear Buffer
        Buffer.ClearRenderTarget(false, true, Color.clear);

        //Fill the mask render texture
        if (masks != null && masks.Count > 0)
        {
            for (int i = masks.Count - 1; i >= 0; i--)
            {
                DrawMask(Camera, Buffer, masks[i]);
            }
        }

        //Release mask render texture
        Buffer.ReleaseTemporaryRT(MaskBuffer);
    }
    private void DrawMask(Camera Camera, CommandBuffer Buffer, ProjectionMask Mask)
    {
        //Grab our mask data
        List<MaskInstance> instances = Mask.Instances;
        Material material = Mask.Material;

        //Check if mask is valid
        if (instances != null && instances.Count > 0)
        {
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i].renderer != null && instances[i].renderer.isVisible)
                {
                    for (int j = 0; j < instances[i].subMeshCount; j++)
                    {
                        Buffer.DrawRenderer(instances[i].renderer, material, j, 0);
                    }
                }
            }
        }
    }

    //Projection Buffer
    private void UpdateProjectionBuffer(Camera Camera, CameraData Data)
    {
        if (Data.method == RenderingMethod.Deferred)
        {
            //Clear buffer
            Data.projectionBuffer.Clear();

            //Make sure we have projections to draw
            if (projections != null && projections.Count > 0)
            {
                //Blit in the screen before we start to change it - Static Pass
                if (staticRts == null || staticRts.Length < 4) staticRts = new int[4];

                if (StaticPass)
                {
                    staticRts[0] = Shader.PropertyToID("_StcAlbedo");
                    staticRts[1] = Shader.PropertyToID("_StcGloss");
                    staticRts[2] = Shader.PropertyToID("_StcNormal");
                    staticRts[3] = Shader.PropertyToID("_StcAmbient");

                    Data.projectionBuffer.GetTemporaryRT(staticRts[0], -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB32);
                    Data.projectionBuffer.GetTemporaryRT(staticRts[1], -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB32);
                    Data.projectionBuffer.GetTemporaryRT(staticRts[2], -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010);
                    if (Camera.hdr) Data.projectionBuffer.GetTemporaryRT(staticRts[3], -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010);
                    else Data.projectionBuffer.GetTemporaryRT(staticRts[3], -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf);

                    MultiChannelFullScreenBlit(Camera, Data.projectionBuffer, staticRts);
                }
                else
                {
                    staticRts[2] = Shader.PropertyToID("_StcNormal");
                    Data.projectionBuffer.GetTemporaryRT(staticRts[2], -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010);

                    StaticNormalFullScreenBlit(Camera, Data.projectionBuffer, staticRts[2]);
                }

                //Set up our Dynamic RenderTextures
                if (dynamicRts == null || dynamicRts.Length < 4) dynamicRts = new int[4];

                dynamicRts[0] = Shader.PropertyToID("_DynAlbedo");
                dynamicRts[1] = Shader.PropertyToID("_DynGloss");
                dynamicRts[2] = Shader.PropertyToID("_DynNormal");
                dynamicRts[3] = Shader.PropertyToID("_DynAmbient");

                Data.projectionBuffer.GetTemporaryRT(dynamicRts[0], -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB32);
                Data.projectionBuffer.GetTemporaryRT(dynamicRts[1], -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB32);
                Data.projectionBuffer.GetTemporaryRT(dynamicRts[2], -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010);
                if (Camera.hdr) Data.projectionBuffer.GetTemporaryRT(dynamicRts[3], -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010);
                else Data.projectionBuffer.GetTemporaryRT(dynamicRts[3], -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf);

                //Iterate over all projections
                for (int i = 0; i < projections.Count; i++)
                {
                    //Draw using the command buffer
                    try
                    {
                        if (Data.projectionCulling.IsVisible(i) && ((Camera.cullingMask & (1 << projections[i].gameObject.layer)) != 0))
                        {
                            //Draw the Projection
                            DrawDeferredProjection(Camera, Data.projectionBuffer, projections[i], dynamicRts, i);
                            //Set visibiility
                            projections[i].SetVisibility(true);
                        }
                        else
                        {
                            //Set visibiility
                            projections[i].SetVisibility(false);
                        }
                    }
                    catch (System.IndexOutOfRangeException)
                    {
                        //Draw Projection
                        DrawDeferredProjection(Camera, Data.projectionBuffer, projections[i], dynamicRts, i);
                    }
                }

                //Release our static RenderTextures
                if (StaticPass)
                {
                    Data.projectionBuffer.ReleaseTemporaryRT(staticRts[0]);
                    Data.projectionBuffer.ReleaseTemporaryRT(staticRts[1]);
                    Data.projectionBuffer.ReleaseTemporaryRT(staticRts[2]);
                    Data.projectionBuffer.ReleaseTemporaryRT(staticRts[3]);
                }
                else
                {
                    Data.projectionBuffer.ReleaseTemporaryRT(staticRts[2]);
                }

                //Release our dynamic RenderTextures
                Data.projectionBuffer.ReleaseTemporaryRT(dynamicRts[0]);
                Data.projectionBuffer.ReleaseTemporaryRT(dynamicRts[1]);
                Data.projectionBuffer.ReleaseTemporaryRT(dynamicRts[2]);
                Data.projectionBuffer.ReleaseTemporaryRT(dynamicRts[3]);
            }
        }
        else
        {
            //Clear buffer
            if (Data.projectionBuffer.sizeInBytes > 0)
            {
                Data.projectionBuffer.Clear();
            }
        }
    }
    private void DrawDeferredProjection(Camera Camera, CommandBuffer Buffer, Projection Projection, int[] Rts, int Index)
    {
        //Only draw if enabled & valid
        if (Projection.isActiveAndEnabled && Projection.RenderMaterial != null)
        {
            if (Projection.DeferredBuffers != null && Projection.DeferredBuffers.Length > 0)
            {
                //Check if the projection requires a pre-pass (usually for blended transparency) 
                if (Projection.DeferredPrePass)
                {
                    //Blit the buffers in there current state into the shader
                    MultiChannelBlit(Camera, Buffer, Projection, Rts);
                }
                //Set the render targets of our buffer
                if (Camera.hdr)
                {
                    //Set to HDR rendertargets
                    Buffer.SetRenderTarget(Projection.DeferredHDRTargets, BuiltinRenderTextureType.CameraTarget);
                }
                else
                {
                    //Set to normal rendertargets
                    Buffer.SetRenderTarget(Projection.DeferredTargets, BuiltinRenderTextureType.CameraTarget);
                }

                //Draw our Projection
                Buffer.DrawMesh(Cube, Projection.RenderMatrix, Projection.RenderMaterial, 0, Projection.DeferredPass, Projection.MaterialProperties);
            }
        }
    }

    #region Passes To Targets
    //Creating new arrays allocates memory, reuse cached ones instead
    private RenderTargetIdentifier[] one = new RenderTargetIdentifier[1];
    private RenderTargetIdentifier[] two = new RenderTargetIdentifier[2];
    private RenderTargetIdentifier[] three = new RenderTargetIdentifier[3];
    private RenderTargetIdentifier[] four = new RenderTargetIdentifier[4];

    public RenderTargetIdentifier[] PassesToTargets(bool[] Channels, bool HDR)
    {
        //How large an array do we require
        int count = 0;
        if (Channels[0]) count += 2;
        if (Channels[1]) count++;
        if (Channels[2]) count++;

        //Determine the cached array to use (Creating temporary arrays allocates a ton of memory)
        RenderTargetIdentifier[] buffers = null;
        switch (count)
        {
            case 0: return null;
            case 1: buffers = one; break;
            case 2: buffers = two; break;
            case 3: buffers = three; break;
            case 4: buffers = four; break;
        }

        //Reset count
        count = 0;

        //Assign new targets
        if (Channels[0])
        {
            buffers[count] = BuiltinRenderTextureType.GBuffer0;
            count++;
        }

        if (Channels[1])
        {
            buffers[count] = BuiltinRenderTextureType.GBuffer1;
            count++;
        }
        if (Channels[2])
        {
            buffers[count] = BuiltinRenderTextureType.GBuffer2;
            count++;
        }
        if (Channels[0])
        {
            if (HDR)
            {
                buffers[count] = BuiltinRenderTextureType.CameraTarget;
                count++;
            }
            else
            {
                buffers[count] = BuiltinRenderTextureType.GBuffer3;
                count++;
            }
        }
        return buffers;
    }
    #endregion
    #region Blit
    //Render Texture
    private int[] staticRts;
    private int[] dynamicRts;

    private void MultiChannelBlit(Camera Camera, CommandBuffer Buffer, Projection Projection, int[] Rts)
    {
        //Draw to the temporary render textures
        if (Projection.DeferredBuffers[0] && Projection.DeferredBuffers[1] && Projection.DeferredBuffers[2])
        {
            four[0] = Rts[0]; four[1] = Rts[1]; four[2] = Rts[2]; four[3] = Rts[3];
            DrawPrePass(Camera, Buffer, four, 6, Projection);
        }
        else if (Projection.DeferredBuffers[1] && Projection.DeferredBuffers[2])
        {
            two[0] = Rts[1]; two[1] = Rts[2];
            DrawPrePass(Camera, Buffer, two, 5, Projection);
        }
        else if (Projection.DeferredBuffers[0] && Projection.DeferredBuffers[2])
        {
            three[0] = Rts[0]; three[1] = Rts[2]; three[2] = Rts[3];
            DrawPrePass(Camera, Buffer, three, 4, Projection);
        }
        else if (Projection.DeferredBuffers[0] && Projection.DeferredBuffers[1])
        {
            three[0] = Rts[0]; three[1] = Rts[1]; three[2] = Rts[3];
            DrawPrePass(Camera, Buffer, three, 3, Projection);
        }
        else if (Projection.DeferredBuffers[2])
        {
            one[0] = Rts[2];
            DrawPrePass(Camera, Buffer, one, 2, Projection);
        }
        else if (Projection.DeferredBuffers[1])
        {
            one[0] = Rts[1];
            DrawPrePass(Camera, Buffer, one, 1, Projection);
        }
        else
        {
            two[0] = Rts[0]; two[1] = Rts[3];
            DrawPrePass(Camera, Buffer, two, 0, Projection);
        }
    }
    private void DrawPrePass(Camera Camera, CommandBuffer Buffer, RenderTargetIdentifier[] Rts, int Pass, Projection Projection)
    {
        //Copies the gbuffer values to render textures only where the projection is present.
        if (Rts.Length > 0)
        {
            //Set Render Targets
            Buffer.SetRenderTarget(Rts, BuiltinRenderTextureType.CameraTarget);

            //Draw
            Buffer.DrawMesh(Cube, Projection.RenderMatrix, Mat_DeferredBlit, 0, Pass);
        }
    }

    private void StaticNormalFullScreenBlit(Camera Camera, CommandBuffer Buffer, int Rt)
    {
        //Set Render Targets
        Buffer.SetRenderTarget(Rt, BuiltinRenderTextureType.CameraTarget);

        //Draw
        Buffer.DrawMesh(CameraBlit, Camera.transform.localToWorldMatrix, Mat_DeferredBlit, 0, 2);
    }
    private void MultiChannelFullScreenBlit(Camera Camera, CommandBuffer Buffer, int[] Rts)
    {
        //Convert int array into renderTexture array
        four[0] = Rts[0]; four[1] = Rts[1]; four[2] = Rts[2]; four[3] = Rts[3];

        //Set Render Targets
        Buffer.SetRenderTarget(four, BuiltinRenderTextureType.CameraTarget);

        //Draw
        Buffer.DrawMesh(CameraBlit, Camera.transform.localToWorldMatrix, Mat_DeferredBlit, 0, 6);
    }
    #endregion
}

internal class CameraData
{
    //Current Path
    public RenderingMethod method;

    //Buffers
    public CommandBuffer maskBuffer;
    public CommandBuffer projectionBuffer;

    //Culling Groups
    public CullingGroup projectionCulling;

    //Camera state
    public bool enabled;

    //Camera type
    public bool sceneCamera;
    public bool previewCamera;

    //Forward Rendering Only - Depth/Normal Source
    public CustomDepthTextureMode customDTM = CustomDepthTextureMode.None;
    public DepthTextureMode? originalDTM = null;
    public DepthTextureMode? desiredDTM = null;

    public CameraData(Camera Camera)
    {
        sceneCamera = (Camera.name == "SceneCamera");
        previewCamera = (Camera.name == "Preview Camera");
    }

    public void Initialize(Camera Camera, DynamicDecals System)
    {
        //Create & register our buffers
        maskBuffer = new CommandBuffer();
        maskBuffer.name = "Dynamic Decals - Masking";

        projectionBuffer = new CommandBuffer();
        projectionBuffer.name = "Dynamic Decals - Projection";

        //Create and register our culling group
        projectionCulling = new CullingGroup();
        projectionCulling.targetCamera = Camera;

        //Enable
        enabled = true;

        //Register it to our rendering path
        InitializeRenderingMethod(Camera);
    }
    public void Terminate(Camera Camera)
    {
        //Restore cameras depthTexture mode
        RestoreDepthTextureMode(Camera);

        //Remove command buffers from all cameras in the dictionary
        TerminateRenderingMethod(Camera);

        //Dispose of culling group
        if (projectionCulling != null)
        {
            projectionCulling.Dispose();
            projectionCulling = null;
        }

        //Disable
        enabled = false;
    }

    public void InitializeRenderingMethod(Camera Camera)
    {
        //Scenes cameras always render in high-precision
        if (method == RenderingMethod.ForwardLow && (sceneCamera || previewCamera)) method = RenderingMethod.ForwardHigh;

        switch (method)
        {
            case RenderingMethod.ForwardLow:
                //Add mask command buffer
                Camera.AddCommandBuffer(CameraEvent.AfterDepthNormalsTexture, maskBuffer);

                //Low Precision Depth & Normals
                customDTM = CustomDepthTextureMode.None;
                desiredDTM = DepthTextureMode.DepthNormals;

                //Set our depth texture mode
                SetDepthTextureMode(Camera);
                break;

            case RenderingMethod.ForwardHigh:
                //Add mask command buffer
                Camera.AddCommandBuffer(CameraEvent.AfterDepthTexture, maskBuffer);

                //High Precision Depth & Normals
                customDTM = CustomDepthTextureMode.Normal;
                desiredDTM = DepthTextureMode.Depth;

                //Set our depth texture mode
                SetDepthTextureMode(Camera);
                break;

            case RenderingMethod.ForwardForced:
                //Add mask command buffer
                Camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, maskBuffer);

                //High Precision Depth & Normals
                customDTM = CustomDepthTextureMode.Normal;
                desiredDTM = DepthTextureMode.Depth;

                //Set our depth texture mode
                SetDepthTextureMode(Camera);
                break;

            case RenderingMethod.Deferred:
                //Add mask & projection command buffers
                Camera.AddCommandBuffer(CameraEvent.BeforeReflections, maskBuffer);
                Camera.AddCommandBuffer(CameraEvent.BeforeReflections, projectionBuffer);

                //No custom depth texture mode required
                customDTM = CustomDepthTextureMode.None;

                //Restore our depth texture mode
                RestoreDepthTextureMode(Camera);
                break;
        }
    }
    public void TerminateRenderingMethod(Camera Camera)
    {
        if (Camera != null)
        {
            switch (method)
            {
                case RenderingMethod.ForwardLow:
                    //Remove mask command buffer
                    if (maskBuffer != null) Camera.RemoveCommandBuffer(CameraEvent.AfterDepthNormalsTexture, maskBuffer);
                    break;

                case RenderingMethod.ForwardHigh:
                    //Remove mask command buffer
                    if (maskBuffer != null) Camera.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, maskBuffer);
                    break;

                case RenderingMethod.ForwardForced:
                    //Remove mask command buffer
                    if (maskBuffer != null) Camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, maskBuffer);
                    break;

                case RenderingMethod.Deferred:
                    //Remove mask & projection command buffers
                    if (maskBuffer != null) Camera.RemoveCommandBuffer(CameraEvent.BeforeReflections, maskBuffer);
                    if (projectionBuffer != null) Camera.RemoveCommandBuffer(CameraEvent.BeforeReflections, projectionBuffer);
                    break;
            }
        }
    }
    public void UpdateRenderingMethod(Camera Camera, DynamicDecals System)
    {
        //Determine our rendering method
        RenderingMethod renderingMethod;

        //Set the new rendering method
        if (System.renderingPath == SystemPath.Deferred)
        {
            if (System.Settings.forceForward) renderingMethod = RenderingMethod.ForwardForced;
            else renderingMethod = RenderingMethod.Deferred;
        }
        else
        {
            if (System.Settings.highPrecision) renderingMethod = RenderingMethod.ForwardHigh;
            else renderingMethod = RenderingMethod.ForwardLow;
        }

        if (method != renderingMethod)
        {
            //Remove all our previous command buffers
            TerminateRenderingMethod(Camera);

            //Update our rendering method
            method = renderingMethod;

            //Add ourself to the new rendering method
            InitializeRenderingMethod(Camera);
        }
    }

    public void SetDepthTextureMode(Camera Camera)
    {
        //If we have a desired value change to it.
        if (desiredDTM.HasValue)
        {
            if (Camera.depthTextureMode != desiredDTM)
            {
                //If we haven't already, Cache the original depth texture mode, otherwise revert to it.
                if (!originalDTM.HasValue) originalDTM = Camera.depthTextureMode;
                else Camera.depthTextureMode = originalDTM.Value;

                //Add our desired depth texture mode.
                Camera.depthTextureMode |= desiredDTM.Value;
            }
        }
        //If we have no desired value, switch back to the original value.
        else RestoreDepthTextureMode(Camera);
    }
    public void RestoreDepthTextureMode(Camera Camera)
    {
        //Restore the depth texture mode to the cached
        if (originalDTM.HasValue && Camera != null)
        {
            Camera.depthTextureMode = originalDTM.Value;
        }
    }
}

public enum SystemPath { Forward, Deferred };
public enum RenderingMethod { ForwardLow, ForwardHigh, ForwardForced, Deferred };
public enum ProjectionType { Decal, Eraser, OmniDecal };
public enum CustomDepthTextureMode { None, Normal };