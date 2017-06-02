using UnityEngine;
using System.Collections;
using System;

/**
* The Eraser Component. This projection allows us to dynamically hide parts of other projections. It projects in the same manner as the decal (orthogonally in the objects forward direction (+Z axis)).
*/
[ExecuteInEditMode]
public class Eraser : Projection
{
    //Shape
    /**
    * The Shape Texture. Multiplied by the alpha multiplier. 
    * Defines the shape and opacity of the eraser. 
    * This texture should be a single channel/greyscale texture. Black will appear transparent while white opaque.
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
    * The Occlusion multiplier. Modifies the occlusion texture.
    * This can be used to tweak the opacity of the eraser as a whole.
    */
    public float AlphaMultiplier
    {
        get { return multiplier; }
        set
        {
            multiplier = Mathf.Clamp01(value);
            UpdateMaterial();
        }
    }

    //Projection
    /**
    * The normal cutoff angle of the eraser.
    * If the angle between the surface and the inverse direction of projection is beyond this limit, the pixel will not be rendered. 
    * This is designed to prevent your erasers from stretching when they project onto near parralel surfaces, or surfaces in which they would appear streched.
    * Setting this to 180 will render(erase) all pixels.
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

    #region Rendering
    //Rendering
    public override Material RenderMaterial
    {
        get { return renderMaterial; }
    }

    //Deferred Rendering
    public override int DeferredPass
    {
        get { return 1; }
    }
    public override bool DeferredPrePass
    {
        get { return (transparencyType == TransparencyType.Blend) ? true : false; }
    }
    #endregion

    #region Backing Fields
    [SerializeField]
    private Texture2D mainTex;
    [SerializeField]
    private float multiplier = 1;

    //Projection
    [SerializeField]
    private float projectionLimit = 80;

    //Rendering
    private Material renderMaterial;

    //Deferred Rendering
    private int deferredPass;
    private bool[] buffers;
    #endregion
    #region Ids
    int _MainTex;
    int _Multiplier;
    int _NormalCutoff;

    protected override void GrabIds()
    {
        base.GrabIds();

        _MainTex = Shader.PropertyToID("_MainTex");
        _Multiplier = Shader.PropertyToID("_Multiplier");
        _NormalCutoff = Shader.PropertyToID("_NormalCutoff");
    }
    #endregion

    #region Material
    protected override void UpdateMaterialProperties()
    {
        base.UpdateMaterialProperties();

        UpdateShape();
        UpdateProjectionClipping();
    }

    private void UpdateShape()
    {
        if (mainTex != null) materialProperties.SetTexture(_MainTex, mainTex);
        materialProperties.SetFloat(_Multiplier, multiplier * AlphaModifier);
    }
    private void UpdateProjectionClipping()
    {
        float normalCutoff = Mathf.Cos(Mathf.Deg2Rad * projectionLimit);
        materialProperties.SetFloat(_NormalCutoff, normalCutoff);
    }

    protected override void UpdateDeferredRendering()
    {
        //Assign Material
        if (transparencyType == TransparencyType.Blend) renderMaterial = DynamicDecals.System.Mat_Eraser;
        else renderMaterial = DynamicDecals.System.Mat_EraserCutout;

        //Initialize deferredBuffers if required
        if (buffers == null || buffers.Length != 3)
        {
            buffers = new bool[3];

            //Calculate render buffers
            buffers[0] = true;
            buffers[1] = true;
            buffers[2] = true;
        }
        //Apply our buffers
        DeferredBuffers = buffers;

        //Update new Material Properties
        base.UpdateDeferredRendering();
    }
    protected override void UpdateForwardRendering(MeshRenderer Renderer)
    {
        //Setup Materials
        Material[] materials = new Material[2];

        if (transparencyType == TransparencyType.Blend) materials[0] = new Material(DynamicDecals.System.Mat_Eraser);
        else materials[0] = new Material(DynamicDecals.System.Mat_EraserCutout);

        materials[1] = new Material(DynamicDecals.System.Mat_EraserGrab);

        //Assign Materials
        Renderer.sharedMaterials = materials;

        //Update new Material Properties
        base.UpdateForwardRendering(Renderer);
    }
    #endregion

    #region Helper Methods
    /**
    * Copies all properites of the target eraser to this eraser.
    * @param Target Defines the eraser whose properties we want to copy to this eraser.
    */
    public void CopyAllProperties(Eraser Target)
    {
        if (Target != null)
        {
            //Base Properties
            CopyBaseProperties(Target);

            //Projection
            ProjectionLimit = Target.ProjectionLimit;

            //Shape
            mainTex = Target.mainTex;
            multiplier = Target.multiplier;

            //Others
            CopyMaskProperties(Target);
        }
        else
        {
            Debug.LogWarning("No Eraser found to copy from");
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