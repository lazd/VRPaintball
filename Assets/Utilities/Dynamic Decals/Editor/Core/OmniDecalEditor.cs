using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(OmniDecal))]
public class OmniDecalEditor : ProjectionEditor
{
    #region Properties
    SerializedProperty mainTex;
    SerializedProperty color;
    #endregion

    private Texture2D Preview;
    private Texture2D Result;

    private bool textureChange;

    private static bool occlusionInfo;
    private static bool colorInfo;

    protected override void OnEnable()
    {
        base.OnEnable();

        mainTex = serializedObject.FindProperty("mainTex");
        color = serializedObject.FindProperty("color");

        //Initialize our texture preveiws
        Preview = new Texture2D(100, 100, TextureFormat.RGBA32, false, true);
        Result = new Texture2D(100, 100, TextureFormat.RGBA32, false, true);

        //Sample texture preveiws
        textureChange = true;
    }
    protected override void OnDisable()
    {
        base.OnDisable();

        //Destroy temporary textures
        DestroyImmediate(Preview);
        DestroyImmediate(Result);
    }
    protected override void OnUndoRedo()
    {
        base.OnUndoRedo();

        //Resample texture preveiws
        textureChange = true;
    }

    public override void OnInspectorGUI()
    {
        //Update Object
        serializedObject.Update();

        //Assign shapeStyle, shapeTexture & shapeMultiplier 
        //The Shape and Transparency Sections will use these as a base
        TransStyle = TransparencyStyle.Alpha;
        TransTex = mainTex;
        TransColor = color;

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        Priority(100);
        Color();
        Transparency();
        Masking();
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
    
    private void Color()
    {
        //Header
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Color"), EditorStyles.boldLabel);
        
        GUILayout.FlexibleSpace();
        
        //Info Toggle
        if (colorInfo)
        {
            if (GUILayout.Button("i", HideInfo, GUILayout.Width(18))) colorInfo = false;
        }
        else
        {
            if (GUILayout.Button("i", ShowInfo, GUILayout.Width(18))) colorInfo = true;
        }
        EditorGUILayout.EndHorizontal();

        //Body
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginHorizontal();

        //Properties
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(mainTex, new GUIContent(""), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
        color.colorValue = EditorGUILayout.ColorField("", color.colorValue, GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
        if (EditorGUI.EndChangeCheck())
        {
            ((OmniDecal)target).UpdateMaterial();
            textureChange = true;
        }

        EditorGUILayout.EndVertical();

        //Spacer
        GUILayout.FlexibleSpace();

        //Preview
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(new GUIContent(Preview), GUILayout.Width(previewSize), GUILayout.Height(previewSize * 0.8f));
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        //Info
        if (colorInfo)
        {
            EditorGUILayout.HelpBox("The base color of your omni-decal. It's made up of a texture combined with a color, but either can be left blank/white if you wish to rely solely on the other.", MessageType.Info);
            EditorGUILayout.HelpBox("The alpha of the color texture is used to determine the transparency.", MessageType.Info);             
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }
    protected override void UpdateTexturePreviews()
    {
        //Update base texture previews
        base.UpdateTexturePreviews();

        if (textureChange) Preview.GetColoredProperty(mainTex, color.colorValue, false);
        textureChange = false;
    }
}