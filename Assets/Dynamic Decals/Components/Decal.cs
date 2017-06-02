using UnityEngine;
using System.Collections;
using System;

/**
* The Decal Component. This is the namesake of the entire system, though only one of a few projections available. It projects a material orthogonally in the objects forward direction (+Z axis).
*/
[ExecuteInEditMode]
public class Decal : Projection {

    //General
    /**
    * Additional decal options are available when using deferred rendering.
    * Full is the default option and the only one available in Forward Rendering. It will render as you expect a decal too.
    * Roughness will only render to the roughness of the surface. This allows us to change how shiny or glossy a surface appears without changing its specular color. Useful for making surfaces appear wet.
    * Normal will only render normal details to the surface. This allows us to add cracks or other details to a surface without baking them into the original textures.
    */
    public DecalType DecalType
    {
        get { return decalType; }
        set
        {
            decalType = value;
            ReplaceMaterial();
            UpdateMaterial();
        }
    }
    /**
    * Defines how the Decal should be lit. PBR (Physically Based Rrendering) or Unlit.
    * PBR mimics unity's in-built standard shader. This is the default, though is more expensive than Unlit.
    * Unlit will render the decal without receiving lights or shadows. This is cheaper & useful for projected UI elements.
    */
    public LightingModel LightModel
    {
        get { return lightingModel; }
        set
        {
            lightingModel = value;
            ReplaceMaterial();
            UpdateMaterial();
        }
    }
    /**
    * Defines the PBR method (when using the PBR lighting model). 
    * Metallic will automatically calculate your specular color & adjust your albedo color based on a metallic value.
    * Specular allows you to set your specular and albedo colors directly. This provides more control, though it's usually not required, and is less intuitive.
    */
    public GlossType GlossType
    {
        get { return glossType; }
        set
        {
            glossType = value;
            ReplaceMaterial();
            UpdateMaterial();
        }
    }
    /**
    * Defines whether or not this decal has emissive properties.
    * Emission allows us to make a decal appear as if it's emitting light. When used with bloom can be used to cheaply and effectively fake detail lighting.
    */
    public bool Emissive
    {
        get { return emissive; }
        set
        {
            emissive = value;
            UpdateMaterial();
        }
    }

    //Shape
    /**
    * Determines the shape and transparency details of the projection when using Roughness or normal decal types.
    * Multiplied with the shapeMultiplier to determine the occlusion of the decal.
    * Texture provided should be a single channel greyscale map. Black will appear transparent while white opaque.
    */
    public Texture2D ShapeMap
    {
        get { return shapeTex; }
        set
        {
            shapeTex = value;
            UpdateMaterial();
        }
    }
    /**
    * Multiplied with the shape map to determine the occlusion of the decal.
    * A value of 0 will appear transparent while 1 will appear opaque.
    */
    public float ShapeMultiplier
    {
        get { return shapeMultiplier; }
        set
        {
            shapeMultiplier = value;
            UpdateMaterial();
        }
    }

    //Albedo channel
    /**
    * The primary color texture of your decal. Multiplied with the albedo color. 
    * The alpha channel of this texture is used to determine the decals transparency.
    */
    public Texture2D AlbedoMap
    {
        get { return albedoTex; }
        set
        {
            albedoTex = value;
            UpdateMaterial();
        }
    }
    /**
    * The primary color of your decal. Multiplied with the albedo map. 
    * The alpha channel is used to determine the decals transparency.
    */
    public Color AlbedoColor
    {
        get { return albedoColor; }
        set
        {
            albedoColor = value;
            UpdateMaterial();
        }
    }

    //Gloss channel
    /**
    * How rough/smooth the surface of the decal appears. Range between 0 & 1.
    * Only used when using the roughness decal type. Multiplied with the smoothness.
    * Texture provided should be a single channel greyscale map. Black will appear rough as sharkskin, while white smooth as polished glass.
    */
    public Texture2D SmoothnessMap
    {
        get { return smoothnessTex; }
        set
        {
            smoothnessTex = value;
            UpdateMaterial();
        }

    }
    /**
    * How rough/smooth the surface of the decal appears. Range between 0 & 1.
    * 0 will make the decal surface appear rough as sharkskin.
    * 1 will make the decal surface appear smooth as polished glass.
    */
    public float Smoothness
    {
        get { return smoothness; }
        set
        {
            smoothness = Mathf.Clamp01(value);
            UpdateMaterial();
        }
    }

    //Metallic
    /**
    * The metallic texture. Multiplied with the metallicity. Only the R channel is used.
    * How metallic the surface of the decal appears.
    * black will make the decal surface appear like platic.
    * white will make the decal surface appear metallic.
    */
    public Texture2D MetallicMap
    {
        get { return metallicTex; }
        set
        {
            metallicTex = value;
            UpdateMaterial();
        }
    }
    /**
    * The metallicity. Multiplied with the metallic map. 
    * How metallic the surface of the decal appears.
    * 0 will make the decal surface appear like platic.
    * 1 will make the decal surface appear metallic.
    */
    public float Metallicity
    {
        get { return metallicity; }
        set
        {
            metallicity = value;
            UpdateMaterial();
        }
    }

    //Specular
    /**
    * The specular color texture of your decal. Multiplied with the specular color.
    * Tints all reflections that appear on the decal surface. Note - rough surfaces still reflect light.
    */
    public Texture2D SpecularMap
    {
        get { return specularTex; }
        set
        {
            specularTex = value;
            UpdateMaterial();
        }
    }
    /**
    * The specular color of your decal. Multiplied with the specular color map.
    * Tints all reflections that appear on the decal surface. Note - rough surfaces still reflect light.
    */
    public Color SpecularColor
    {
        get { return specularColor; }
        set
        {
            specularColor = value;
            UpdateMaterial();
        }
    }

    //Normal channel
    /**
    * The normal texture of your decal. modified by the normal strength. 
    * Normals determine how the surface of your decal interacts with lights.
    */
    public Texture2D NormalMap
    {
        get { return normalTex; }
        set
        {
            normalTex = value;
            UpdateMaterial();
        }
    }
    /**
    * Modifies how prominent the normal map appears.
    * Normals determine how the surface of your decal interacts with lights.
    */
    public float NormalStrength
    {
        get { return normalStrength; }
        set
        {
            normalStrength = Mathf.Clamp(value, 0, 4);
            UpdateMaterial();
        }
    }

    //Emissive
    /**
    * The emission texture of your decal. Multiplied by the emission color.
    * Emission allows us to make a decal appear as if it's emitting light.
    */
    public Texture2D EmissionMap
    {
        get { return emissionTex; }
        set
        {
            emissionTex = value;
            UpdateMaterial();
        }
    }
    /**
    * The emission color of your decal. Multiplied by the emission map.
    * Emission allows us to make a decal appear as if it's emitting light.
    */
    public Color EmissionColor
    {
        get { return emissionColor; }
        set
        {
            emissionColor = value;
            UpdateMaterial();
        }
    }
    /**
    * The emission intensity of your decal. Primarily used for HDR.
    * Emission output is multiplied by Intensity, the stronger this value, the more prominent it's influence in HDR.
    */
    public float EmissionIntensity
    {
        get { return emissionIntensity; }
        set
        {
            emissionIntensity = value;
            UpdateMaterial();
        }
    }

    //Projection
    /**
    * The normal cutoff angle of the decal.
    * If the angle between the surface and the inverse direction of projection is beyond this limit, the pixel will not be rendered. 
    * This is designed to prevent your decals from stretching when they project onto near parralel surfaces, or surfaces in which they would appear streched.
    * Setting this to 180 will render all pixels.
    */
    public float ProjectionLimit
    {
        get { return projectionLimit; }
        set
        {
            projectionLimit = Mathf.Clamp(value, 0, 180);
            UpdateMaterial();
        }
    }

    #region Update
    public override void UpdateProjection()
    {
        //Update Core of Projection
        base.UpdateProjection();

        //Update Normal Flip
        UpdateNormalFlip();
    }
    #endregion

    #region Rendering
    public override Material RenderMaterial
    {
        get { return renderMaterial; }
    }

    public override int DeferredPass
    {
        get { return deferredPass; }
    }
    public override bool DeferredPrePass
    {
        get
        {
            if (DecalType == DecalType.Roughness) return true;
            else return (transparencyType == TransparencyType.Blend) ? true : false;
        }
    }
    #endregion

    #region Backing Fields
    //General
    [SerializeField]
    private DecalType decalType = DecalType.Full;
    [SerializeField]
    private LightingModel lightingModel = LightingModel.PBR;
    [SerializeField]
    private GlossType glossType;

    //Shape
    [SerializeField]
    private Texture2D shapeTex;
    [SerializeField]
    private float shapeMultiplier = 1;

    //Albedo channel
    [SerializeField]
    private Texture2D albedoTex;
    [SerializeField]
    private Color albedoColor = Color.grey;

    //Gloss channel
    [SerializeField]
    private Texture2D smoothnessTex;
    [SerializeField]
    private float smoothness = 0.2f;

    //Specular
    [SerializeField]
    private Texture2D specularTex;
    [SerializeField]
    private Color specularColor = Color.white;

    //Metallic
    [SerializeField]
    private Texture2D metallicTex;
    [SerializeField]
    private float metallicity = 0.5f;

    //Normal channel
    [SerializeField]
    private Texture2D normalTex;
    [SerializeField]
    private float normalStrength = 1;

    //Emission
    [SerializeField]
    private bool emissive = false;
    [SerializeField]
    private Texture2D emissionTex;
    [SerializeField]
    private Color emissionColor = Color.white;
    [SerializeField]
    private float emissionIntensity = 1;

    //Projection
    [SerializeField]
    private float projectionLimit = 80;

    //Rendering
    private Material renderMaterial;

    private int deferredPass;
    private bool[] buffers;
    #endregion
    #region Ids
    int _Glossiness;
    int _GlossTex;
    int _MainTex;
    int _Multiplier;
    int _Color;
    int _MetallicGlossMap;
    int _Metallic;
    int _SpecGlossMap;
    int _SpecColor;
    int _BumpMap;
    int _BumpScale;
    int _EmissionMap;
    int _EmissionColor;
    int _NormalCutoff;
    int _BumpFlip;

    protected override void GrabIds()
    {
        base.GrabIds();

        _Glossiness = Shader.PropertyToID("_Glossiness");
        _GlossTex = Shader.PropertyToID("_GlossTex");
        _MainTex = Shader.PropertyToID("_MainTex");
        _Multiplier = Shader.PropertyToID("_Multiplier");
        _Color = Shader.PropertyToID("_Color");
        _MetallicGlossMap = Shader.PropertyToID("_MetallicGlossMap");
        _Metallic = Shader.PropertyToID("_Metallic");
        _SpecGlossMap = Shader.PropertyToID("_SpecGlossMap");
        _SpecColor = Shader.PropertyToID("_SpecColor");
        _BumpMap = Shader.PropertyToID("_BumpMap");
        _BumpScale = Shader.PropertyToID("_BumpScale");
        _EmissionMap = Shader.PropertyToID("_EmissionMap");
        _EmissionColor = Shader.PropertyToID("_EmissionColor");
        _NormalCutoff = Shader.PropertyToID("_NormalCutoff");
        _BumpFlip = Shader.PropertyToID("_BumpFlip");
    }
    #endregion

    #region Material
    protected override void UpdateMaterialProperties()
    {
        //Update base properties
        base.UpdateMaterialProperties();
        UpdateProjectionClipping();

        switch (decalType)
        {
            case DecalType.Full:
                //PBR properties
                if (lightingModel == LightingModel.PBR)
                {
                    UpdateGloss();
                    UpdateNormal();
                    UpdateEmissive();
                }
                //Color
                UpdateColor();
                break;

            case DecalType.Roughness:
                UpdateShape();
                materialProperties.SetFloat(_Glossiness, smoothness);
                if (smoothnessTex != null) materialProperties.SetTexture(_GlossTex, smoothnessTex);
                break;

            case DecalType.Normal:
                UpdateShape();
                UpdateNormal();
                break;
        }
    }

    private void UpdateShape()
    {
        if (shapeTex != null) materialProperties.SetTexture(_MainTex, shapeTex);
        materialProperties.SetFloat(_Multiplier, shapeMultiplier * AlphaModifier);
    }
    private void UpdateColor()
    {
        if (albedoTex != null) materialProperties.SetTexture(_MainTex, albedoTex);

        //Modify our albedo by the alpha modifier (Generally used to fade out pooled decals)
        Color modifiedColor = albedoColor;
        modifiedColor.a *= AlphaModifier;
        materialProperties.SetColor(_Color, modifiedColor);
    }
    private void UpdateGloss()
    {
        materialProperties.SetFloat(_Glossiness, smoothness);

        switch (GlossType)
        {
            case GlossType.Metallic:
                if (metallicTex != null) materialProperties.SetTexture(_MetallicGlossMap, metallicTex);
                else materialProperties.SetTexture(_MetallicGlossMap, Texture2D.whiteTexture);

                materialProperties.SetFloat(_Metallic, metallicity);
                break;
            case GlossType.Specular:
                if (specularTex != null) materialProperties.SetTexture(_SpecGlossMap, specularTex);
                else materialProperties.SetTexture(_SpecGlossMap, Texture2D.whiteTexture);

                materialProperties.SetColor(_SpecColor, specularColor);
                break;
        }
    }
    private void UpdateNormal()
    {
        if (normalTex != null) materialProperties.SetTexture(_BumpMap, normalTex);
        materialProperties.SetFloat(_BumpScale, normalStrength);
    }
    private void UpdateEmissive()
    {
        if (emissive)
        {
            if (emissionTex != null) materialProperties.SetTexture(_EmissionMap, emissionTex);
            materialProperties.SetColor(_EmissionColor, emissionColor * emissionIntensity);
        }
    }
    private void UpdateProjectionClipping()
    {
        float normalCutoff = Mathf.Cos(Mathf.Deg2Rad * projectionLimit);
        materialProperties.SetFloat(_NormalCutoff, normalCutoff);
    }

    private bool flipNormals = false;
    private void UpdateNormalFlip()
    {
        Transform t = transform;
        Vector3 ls = t.localScale;

        if (t.hasChanged)
        {
            bool flip = false;
            if (Mathf.Sign(ls.x) == -1) flip = !flip;
            if (Mathf.Sign(ls.y) == -1) flip = !flip;
            if (Mathf.Sign(ls.z) == -1) flip = !flip;

            if (flipNormals != flip)
            {
                flipNormals = flip;
                materialProperties.SetFloat(_BumpFlip, (flipNormals) ? 1 : 0);
                UpdateMaterial();
            }
        }
    }

    protected override void UpdateDeferredRendering()
    {
        //Initialize deferredBuffers if required
        if (buffers == null || buffers.Length != 3) buffers = new bool[3];

        switch (decalType)
        {
            case DecalType.Full:
                //Material & Pass change with lighting model
                if (lightingModel == LightingModel.Unlit)
                {
                    //Unlit
                    if (TransparencyType == TransparencyType.Blend) renderMaterial = DynamicDecals.System.Mat_Decal_Unlit;
                    else renderMaterial = DynamicDecals.System.Mat_Decal_UnlitCutout;

                    //Render to all buffers
                    buffers[0] = true;
                    buffers[1] = true;
                    buffers[2] = false;

                    //First pass
                    deferredPass = 1;
                }
                else
                {
                    //Determine our material
                    if (GlossType == GlossType.Metallic)
                    {
                        //Metallic
                        if (TransparencyType == TransparencyType.Blend) renderMaterial = DynamicDecals.System.Mat_Decal_Metallic;
                        else renderMaterial = DynamicDecals.System.Mat_Decal_MetallicCutout;
                    }
                    else
                    {
                        //Specular
                        if (TransparencyType == TransparencyType.Blend) renderMaterial = DynamicDecals.System.Mat_Decal_Specular;
                        else renderMaterial = DynamicDecals.System.Mat_Decal_SpecularCutout;
                    }

                    //Render to all buffers
                    buffers[0] = true;
                    buffers[1] = true;
                    buffers[2] = true;

                    //Second pass
                    deferredPass = 2;
                }
                break;

            case DecalType.Roughness:
                //Unlit shader
                if (TransparencyType == TransparencyType.Blend) renderMaterial = DynamicDecals.System.Mat_Decal_Roughness;
                else renderMaterial = DynamicDecals.System.Mat_Decal_RoughnessCutout;

                //Only render to gloss channel
                buffers[0] = false;
                buffers[1] = true;
                buffers[2] = false;

                //Third pass
                deferredPass = 0;
                break;

            case DecalType.Normal:
                //Unlit shader
                if (TransparencyType == TransparencyType.Blend) renderMaterial = DynamicDecals.System.Mat_Decal_Normal;
                else renderMaterial = DynamicDecals.System.Mat_Decal_NormalCutout;

                //Only render to normal channel
                buffers[0] = false;
                buffers[1] = false;
                buffers[2] = true;

                //Second pass
                deferredPass = 0;
                break;
        }
        //Apply our buffers
        DeferredBuffers = buffers;

        //Update new Material Properties
        base.UpdateDeferredRendering();
    }
    protected override void UpdateForwardRendering(MeshRenderer Renderer)
    {
        //Make sure we have the correct material count
        if (Renderer.sharedMaterials.Length != 1) Renderer.sharedMaterials = new Material[1];

        //Assign new Material
        if (lightingModel == LightingModel.Unlit)
        {
            if (transparencyType == TransparencyType.Blend) Renderer.sharedMaterial = new Material(DynamicDecals.System.Mat_Decal_Unlit);
            else Renderer.sharedMaterial = new Material(DynamicDecals.System.Mat_Decal_UnlitCutout);
        }
        else
        {
            switch (glossType)
            {
                case GlossType.Metallic:
                    if (transparencyType == TransparencyType.Blend) Renderer.sharedMaterial = new Material(DynamicDecals.System.Mat_Decal_Metallic);
                    else Renderer.sharedMaterial = new Material(DynamicDecals.System.Mat_Decal_MetallicCutout);
                    break;
                case GlossType.Specular:
                    if (transparencyType == TransparencyType.Blend) Renderer.sharedMaterial = new Material(DynamicDecals.System.Mat_Decal_Specular);
                    else Renderer.sharedMaterial = new Material(DynamicDecals.System.Mat_Decal_SpecularCutout);
                    break;
            }
        }
        //Update new Material Properties
        base.UpdateForwardRendering(Renderer);
    }
    protected override bool RequiresRenderer
    {
        get { return (decalType == DecalType.Full); }
    }
    #endregion

    #region Helper Methods
    /**
    * Copies all properites of the target decal to this decal.
    * @param Target Defines the decal whose properties we want to copy to this decal.
    * @param IncludeTextures setting this to false will prevent textures from being copied.
    */
    public void CopyAllProperties(Decal Target, bool IncludeTextures = true)
    {
        if (Target != null)
        {
            //Base Properties
            CopyBaseProperties(Target);

            //Projection
            ProjectionLimit = Target.ProjectionLimit;

            //Type
            DecalType = Target.DecalType;

            //Lighing Model
            LightModel = Target.LightModel;

            //Shape
            if (IncludeTextures) ShapeMap = Target.ShapeMap;
            ShapeMultiplier = Target.ShapeMultiplier;

            //Others
            CopyAlbedoProperties(Target, IncludeTextures);
            CopyGlossProperties(Target, IncludeTextures);
            CopyNormalProperties(Target, IncludeTextures);
            CopyEmissiveProperties(Target, IncludeTextures);
            CopyMaskProperties(Target);
        }
        else
        {
            Debug.LogWarning("No Decal found to copy from");
        }        
    }
    /**
    * Copies the albedo properites of the target decal to this decal.
    * @param Target Defines the decal whose properties we want to copy to this decal.
    * @param IncludeTextures setting this to false will prevent textures from being copied.
    */
    public void CopyAlbedoProperties(Decal Target, bool IncludeTextures = true)
    {
        if (IncludeTextures) AlbedoMap = Target.AlbedoMap;
        AlbedoColor = Target.AlbedoColor;
    }
    /**
    * Copies the gloss properites of the target decal to this decal.
    * @param Target Defines the decal whose properties we want to copy to this decal.
    * @param IncludeTextures setting this to false will prevent textures from being copied.
    */
    public void CopyGlossProperties(Decal Target, bool IncludeTextures = true)
    {
        Smoothness = Target.Smoothness;
        GlossType = Target.GlossType;
        //Roughness
        if (IncludeTextures) MetallicMap = Target.MetallicMap;
        //Metallic
        if (IncludeTextures) MetallicMap = Target.MetallicMap;
        Metallicity = Target.Metallicity;
        //Specular
        if (IncludeTextures) SpecularMap = Target.SpecularMap;
        SpecularColor = Target.SpecularColor;
    }
    /**
    * Copies the normal properites of the target decal to this decal.
    * @param Target Defines the decal whose properties we want to copy to this decal.
    * @param IncludeTextures setting this to false will prevent textures from being copied.
    */
    public void CopyNormalProperties(Decal Target, bool IncludeTextures = true)
    {
        if (IncludeTextures) NormalMap = Target.NormalMap;
        NormalStrength = Target.NormalStrength;
    }
    /**
    * Copies the emissive properites of the target decal to this decal.
    * @param Target Defines the decal whose properties we want to copy to this decal.
    * @param IncludeTextures setting this to false will prevent textures from being copied.
    */
    public void CopyEmissiveProperties(Decal Target, bool IncludeTextures = true)
    {
        Emissive = Target.Emissive;
        if (Emissive)
        {
            if (IncludeTextures) EmissionMap = Target.EmissionMap;
            EmissionColor = Target.EmissionColor;
            EmissionIntensity = Target.EmissionIntensity;
        }
    }
    #endregion

    #region Selection Gizmo
    #if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        DrawGizmo();
    }
    private void DrawGizmo()
    {
        if (isActiveAndEnabled)
        {
            bool Selected = (UnityEditor.Selection.activeGameObject == gameObject);

            Color color = new Color(0.8f, 0.8f, 0.8f, 1.0f);
            Gizmos.matrix = transform.localToWorldMatrix;

            //Draw selection box
            if (!Selected)
            {
                color.a = 0.0f;
                Gizmos.color = color;
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }

            //Draw selection box frame
            color.a = Selected ? 0.5f : 0.05f;
            Gizmos.color = color;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
    #endif
    #endregion
}

public enum DecalType { Full, Roughness, Normal };
public enum LightingModel { Unlit, PBR };