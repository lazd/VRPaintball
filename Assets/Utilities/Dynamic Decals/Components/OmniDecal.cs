using UnityEngine;
using System.Collections;
using System;

/**
* The OmniDecal Component. This projection allows us to project in all directions at once. Instead of projecting a texture, it projects a gradient, and samples it based on how far from the projection point the surface lies. Perfect for spherical projections, ripples or explosion effects.
*/
[ExecuteInEditMode]
public class OmniDecal : Projection
{
    //Color
    /**
    * The primary color texture of your omni-decal. Multiplied with the color. 
    * The alpha channel is used to determine the transparency.
    */
    public Texture2D MainTex
    {
        get { return mainTex; }
        set
        {
            mainTex = value;
            UpdateMaterial();
        }
    }
    /**
    * The primary color of your omni-decal. Multiplied with the color texture. 
    * The alpha channel is used to determine the transparency.
    */
    public Color Color
    {
        get { return color; }
        set
        {
            color = value;
            UpdateMaterial();
        }
    }

    #region Rendering
    public override Material RenderMaterial
    {
        get { return (transparencyType == TransparencyType.Blend) ? DynamicDecals.System.Mat_OmniDecal : DynamicDecals.System.Mat_OmniDecalCutout; }
    }

    public override int DeferredPass
    {
        get { return 1; }
    }
    public override bool DeferredPrePass
    {
        get { return (transparencyType == TransparencyType.Blend); }
    }
    #endregion

    #region Backing Fields
    //Color
    [SerializeField]
    public Texture2D mainTex;
    [SerializeField]
    public Color color = Color.white;

    //Rendering
    private bool[] buffers = new bool[] { true, true, false, true };
    #endregion
    #region Ids
    int _MainTex;
    int _Color;

    protected override void GrabIds()
    {
        base.GrabIds();

        _MainTex = Shader.PropertyToID("_MainTex");
        _Color = Shader.PropertyToID("_Color");
    }
    #endregion

    #region Materials
    protected override void UpdateMaterialProperties()
    {
        base.UpdateMaterialProperties();

        UpdateColor();
    }
    private void UpdateColor()
    {
        if (mainTex != null) materialProperties.SetTexture(_MainTex, mainTex);

        //Modify our albedo by the alpha modifier (Generally used to fade out pooled decals)
        Color modifiedColor = color;
        modifiedColor.a *= AlphaModifier;
        materialProperties.SetColor(_Color, modifiedColor);
    }

    protected override void UpdateDeferredRendering()
    {
        //Apply our buffers
        DeferredBuffers = buffers;

        //Update new Material Properties
        base.UpdateDeferredRendering();
    }
    protected override void UpdateForwardRendering(MeshRenderer Renderer)
    {
        //Make sure we have the correct material count
        if (Renderer.sharedMaterials.Length != 1) Renderer.sharedMaterials = new Material[1];

        //Assign new material
        if (transparencyType == TransparencyType.Blend) Renderer.sharedMaterial = new Material(DynamicDecals.System.Mat_OmniDecal);
        else Renderer.sharedMaterial = new Material(DynamicDecals.System.Mat_OmniDecalCutout);

        //Update new Material Properties
        base.UpdateForwardRendering(Renderer);
    }
    #endregion

    #region Helper Methods
    /**
    * Copies all properites of the target omni-decal to this omni-decal.
    * @param Target Defines the omni-decal whose properties we want to copy to this omni-decal.
    * @param IncludeTextures setting this to false will prevent textures from being copied.
    */
    public void CopyAllProperties(OmniDecal Target, bool IncludeTextures = true)
    {
        if (Target != null)
        {
            //Base Properties
            CopyBaseProperties(Target);

            //Others
            CopyProperties(Target, IncludeTextures);
            CopyMaskProperties(Target);
        }
        else
        {
            Debug.LogWarning("No Decal found to copy from");
        }
    }
    /**
    * Copies properites exclusive to the omni-decal from the target omni-decal to this omni-decal.
    * @param Target Defines the omni-decal whose properties we want to copy to this omni-decal.
    * @param IncludeTextures setting this to false will prevent textures from being copied.
    */
    public void CopyProperties(OmniDecal Target, bool IncludeTextures = true)
    {
        if (IncludeTextures) MainTex = Target.MainTex;
        Color = Target.Color;
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

            //Draw selection sphere
            if (!Selected)
            {
                color.a = 0.0f;
                Gizmos.color = color;
                Gizmos.DrawSphere(Vector3.zero, 0.5f);
            }

            //Draw selection box frame
            color.a = Selected ? 0.5f : 0.05f;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
        }
    }
    #endif
    #endregion
}