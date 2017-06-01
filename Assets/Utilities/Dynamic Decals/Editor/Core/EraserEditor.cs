using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Eraser))]
public class EraserEditor : ProjectionEditor
{
    SerializedProperty mainTex;
    SerializedProperty multiplier;

    SerializedProperty projectionLimit;

    private static bool occlusionInfo;

    private static bool projectionFo;
    private static bool projectionInfo;

    private static bool buffersFo;
    private static bool buffersInfo;

    protected override void OnEnable()
    {
        base.OnEnable();

        mainTex = serializedObject.FindProperty("mainTex");
        multiplier = serializedObject.FindProperty("multiplier");

        projectionLimit = serializedObject.FindProperty("projectionLimit");

        //Initialize our texture preveiws
        OcclusionPreview = new Texture2D(100, 100, TextureFormat.RGBA32, false, true);

        //Sample texture preveiws
        occlusionChange = true;
    }
    protected override void OnDisable()
    {
        base.OnDisable();

        //Destroy temporary textures
        DestroyImmediate(OcclusionPreview);
    }
    protected override void OnUndoRedo()
    {
        base.OnUndoRedo();

        //Resample texture preveiws
        occlusionChange = true;
    }

    public override void OnInspectorGUI()
    {
        //Update Object
        serializedObject.Update();

        //Assign shapeStyle, shapeTexture & shapeMultiplier 
        //The Shape and Transparency Sections will use these as a base
        TransStyle = TransparencyStyle.Shape;
        TransTex = mainTex;
        TransMultiplier = multiplier;

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        Priority(101);
        Shape();
        Transparency();
        Masking();
        Projection();
        Pooling();

        EditorGUILayout.Space();

        //Apply Modified Properties
        serializedObject.ApplyModifiedProperties();

        //Update Projection Immeditately
        ((Projection)target).UpdateMaterialImmeditately();

        //Update Priority
        UpdateDrawOrder();

        //Update textures
        UpdateTexturePreviews();
    }

    private void Projection()
    {
        //Header
        EditorGUILayout.BeginHorizontal();
        projectionFo = EditorGUILayout.Foldout(projectionFo, new GUIContent("Projection", "Determines how this projection is projected"), BoldFoldout);
        GUILayout.FlexibleSpace();
        //Info Toggle
        if (projectionInfo)
        {
            if (GUILayout.Button("i", HideInfo, GUILayout.Width(18))) projectionInfo = false;
        }
        else
        {
            if (GUILayout.Button("i", ShowInfo, GUILayout.Width(18))) projectionInfo = true;
        }
        EditorGUILayout.EndHorizontal();

        if (projectionFo)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space();

            //Properties
            EditorGUI.BeginChangeCheck();
            projectionLimit.floatValue = EditorGUILayout.Slider(new GUIContent("Projection Limit", "Prevents stretching, see info for more details (i button)"), projectionLimit.floatValue, 0, 180);
            if (EditorGUI.EndChangeCheck()) ((Eraser)target).UpdateMaterial();
            

            //Info
            if (projectionInfo)
            {
                EditorGUILayout.HelpBox("The projection limit determines at what angle (between the projections forward vector & the surface normal) we stop drawing the projection. Essentially it prevents projections from drawing in situations in which they would be stretched. 0 draws nothing, 180 everything. Default value is 80.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
    }
}