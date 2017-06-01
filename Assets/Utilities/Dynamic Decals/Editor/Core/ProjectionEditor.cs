using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Projection))]
public class ProjectionEditor : Editor
{
    //Placeholder serialized properties - Need to be assigned by inheriting classes
    protected enum TransparencyStyle { Alpha, Shape };
    protected TransparencyStyle TransStyle = TransparencyStyle.Alpha;
    protected SerializedProperty TransTex;
    protected SerializedProperty TransColor;
    protected SerializedProperty TransMultiplier;

    //Properties
    private SerializedProperty priority;

    private SerializedProperty transparencyType;
    private SerializedProperty cutoff;

    private SerializedProperty maskMethod;
    private SerializedProperty masks;

    //UI functionality
    protected Texture2D OcclusionPreview;

    protected bool priorityChange;
    protected bool occlusionChange = true;

    //UI States
    private static bool priorityInfo;
    private static bool shapeInfo;
    private static bool occlusionFo;
    private static bool maskingFo;

    private static bool occlusionInfo;
    private static bool maskingInfo;

    //Core Methods
    protected virtual void OnEnable()
    {
        //Grab out properties
        priority = serializedObject.FindProperty("priority");

        transparencyType = serializedObject.FindProperty("transparencyType");
        cutoff = serializedObject.FindProperty("cutoff");

        maskMethod = serializedObject.FindProperty("maskMethod");
        masks = serializedObject.FindProperty("masks");

        //Initialize our texture preveiws
        OcclusionPreview = new Texture2D(100, 100, TextureFormat.RGBA32, false, true);

        //Sample texture preveiws
        occlusionChange = true;

        //Register to Undo callback
        Undo.undoRedoPerformed += OnUndoRedo;
    }
    protected virtual void OnDisable()
    {
        //Deregister to Undo callback
        Undo.undoRedoPerformed -= OnUndoRedo;
    }
    protected virtual void OnUndoRedo()
    {
        //Sort projections
        ((Projection)target).Reprioritise();

        //Resample texture preveiws
        occlusionChange = true;
    }

    public override void OnInspectorGUI()
    {
        //Update Object
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        //Shape & Transparency need a texture & color to work from. These should be assigned to shapeTex & shapeColor
        switch (TransStyle)
        {
            case TransparencyStyle.Alpha:
                throw new System.NotImplementedException();
            case TransparencyStyle.Shape:
                throw new System.NotImplementedException();
        }

        Priority(40);
        Shape();
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

    #region GUI Styles
    protected int previewSize = 70;
    protected GUIStyle infoStyle;
    protected GUIStyle foldoutStyle;

    protected GUIStyle ShowInfo
    {
        get
        {
            if (infoStyle == null) infoStyle = new GUIStyle(GUI.skin.button);
            infoStyle.fontStyle = FontStyle.Bold;
            infoStyle.normal.textColor = Color.white;

            return infoStyle;
        }
    }
    protected GUIStyle HideInfo
    {
        get
        {
            if (infoStyle == null) infoStyle = new GUIStyle(GUI.skin.button);
            infoStyle.fontStyle = FontStyle.Bold;
            infoStyle.normal.textColor = Color.grey;

            return infoStyle;
        }
    }
    protected GUIStyle BoldFoldout
    {
        get
        {
            if (foldoutStyle == null) foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;

            return foldoutStyle;
        }
    }
    #endregion

    //Priority
    protected void Priority(int PriorityLimit)
    {
        //Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Priority", "Determines whether this projection will be drawn over others"), EditorStyles.boldLabel);
        //Info Toggle
        if (priorityInfo)
        {
            if (GUILayout.Button("i", HideInfo, GUILayout.Width(18))) priorityInfo = false;
        }
        else
        {
            if (GUILayout.Button("i", ShowInfo, GUILayout.Width(18))) priorityInfo = true;
        }
        EditorGUILayout.EndHorizontal();

        //Body
        EditorGUI.indentLevel++;

        EditorGUI.BeginChangeCheck();
        priority.intValue = EditorGUILayout.IntSlider(new GUIContent("", "Determines whether this projection will be drawn over others"), priority.intValue, 0, PriorityLimit);
        if (EditorGUI.EndChangeCheck()) priorityChange = true;

        if (priorityInfo)
        {
            EditorGUILayout.HelpBox("Priority determines the projection draw order. Lower priority projections will be drawn first and appear to be behind those of a higher priority.", MessageType.Info);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }
    protected void UpdateDrawOrder()
    {
        if (priorityChange) ((Projection)target).Reprioritise();
        priorityChange = false;
    }

    //Shape
    protected void Shape()
    {
        //Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Shape", "Determines the shape and transparency details of the projection."), EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        //Info Toggle
        if (shapeInfo)
        {
            if (GUILayout.Button("i", HideInfo, GUILayout.Width(18))) shapeInfo = false;
        }
        else
        {
            if (GUILayout.Button("i", ShowInfo, GUILayout.Width(18))) shapeInfo = true;
        }
        EditorGUILayout.EndHorizontal();

        //Body
        EditorGUI.indentLevel++;

        //Properties
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(TransTex, new GUIContent(""));
        if (EditorGUI.EndChangeCheck())
        {
            ((Projection)target).UpdateMaterial();
            occlusionChange = true;
        }

        //Info
        if (shapeInfo)
        {
            EditorGUILayout.HelpBox("The shape is the basis for transparency. It determines which parts of the texture are visible and how opaque they are.", MessageType.Info);
            EditorGUILayout.HelpBox("Texture provided should be a single channel greyscale map. Black will appear transparent while white opaque.", MessageType.Info);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }

    //Transparency
    protected void Transparency()
    {
        //Header
        EditorGUILayout.BeginHorizontal();
        occlusionFo = EditorGUILayout.Foldout(occlusionFo, new GUIContent("Transparency", "Determines where on the surface to affect."), BoldFoldout);
        //Info Toggle
        if (occlusionInfo)
        {
            if (GUILayout.Button("i", HideInfo, GUILayout.Width(18))) occlusionInfo = false;
        }
        else
        {
            if (GUILayout.Button("i", ShowInfo, GUILayout.Width(18))) occlusionInfo = true;
        }
        EditorGUILayout.EndHorizontal();

        //Body
        if (occlusionFo)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();

            //Properties
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            //Transparency type selection
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(transparencyType, new GUIContent(""), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
            if (EditorGUI.EndChangeCheck())
            {
                if (transparencyType.enumValueIndex == 0)
                {
                    cutoff.floatValue = 0.2f;

                    if (TransStyle == TransparencyStyle.Shape) TransMultiplier.floatValue = 1;
                }
                else cutoff.floatValue = 0.01f;

                //Apply Change
                occlusionChange = true;
                ((Projection)target).ReplaceMaterial();
                ((Projection)target).UpdateMaterial();
            }

            //Transparency details
            EditorGUI.BeginChangeCheck();
            if (transparencyType.enumValueIndex == 0)
            {
                cutoff.floatValue = EditorGUILayout.Slider(new GUIContent(""), cutoff.floatValue, 0, 1, GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
            }
            else
            {
                
                switch (TransStyle)
                {
                    case TransparencyStyle.Alpha:
                        //Alpha modifies the alpha of our color
                        Color Occlusion = TransColor.colorValue;
                        Occlusion.a = EditorGUILayout.Slider("", Occlusion.a, 0, 1, GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
                        TransColor.colorValue = Occlusion;
                        break;
                    case TransparencyStyle.Shape:
                        //Shape modifies the multiplier
                        TransMultiplier.floatValue = EditorGUILayout.Slider("", TransMultiplier.floatValue, 0, 1, GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
                        break;
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                //Apply Change
                occlusionChange = true;
                ((Projection)target).UpdateMaterial();
            }

            EditorGUILayout.EndVertical();

            //Spacer
            GUILayout.FlexibleSpace();

            //Preview
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(new GUIContent(OcclusionPreview), GUILayout.Width(previewSize), GUILayout.Height(previewSize * 0.8f));
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            //Info
            if (occlusionInfo)
            {
                switch (TransStyle)
                {
                    case TransparencyStyle.Alpha:
                        EditorGUILayout.HelpBox("Transparency determines which pixels of your projection will be visible. It's based on the alpha channel of the albedo texture/color.", MessageType.Info);
                        break;
                    case TransparencyStyle.Shape:
                        EditorGUILayout.HelpBox("Transparency determines which pixels of your projection will be visible. It's based on the greyscale or red channel of the shape texture.", MessageType.Info);
                        break;
                }
                EditorGUILayout.HelpBox("Cutout transparency is the cheaper option. It simply doesn't draw pixels with an alpha below a cutoff value. This results in each pixels being either entirely opaque, or entirely transparent.", MessageType.Info);
                EditorGUILayout.HelpBox("Blend transparency is the expensive option. It blends the projection with whatever lies behind it to achieve partial transparency.", MessageType.Info);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
    }

    //Masking
    protected void Masking()
    {
        //Header
        EditorGUILayout.BeginHorizontal();
        maskingFo = EditorGUILayout.Foldout(maskingFo, new GUIContent("Masking", "Determines where this projection can and cannot be drawn."), BoldFoldout);
        GUILayout.FlexibleSpace();
        //Info Toggle
        if (maskingInfo)
        {
            if (GUILayout.Button("i", HideInfo, GUILayout.Width(18))) maskingInfo = false;
        }
        else
        {
            if (GUILayout.Button("i", ShowInfo, GUILayout.Width(18))) maskingInfo = true;
        }
        EditorGUILayout.EndHorizontal();

        if (maskingFo)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space();

            //Info
            if (maskingInfo)
            {
                EditorGUILayout.HelpBox("Mask layers dictate which objects can or cannot have projections projected onto them. To have a renderer be apart of a mask layer, attach a DecalMask component to it and select the desired layer.", MessageType.Info);
            }

            //Properties
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(maskMethod, new GUIContent(""));
            EditorGUI.indentLevel++;
            for (int i = 0; i < masks.arraySize; i++)
            {
                masks.GetArrayElementAtIndex(i).boolValue = EditorGUILayout.Toggle(new GUIContent(DynamicDecals.System.Settings.layerNames[i], ""), masks.GetArrayElementAtIndex(i).boolValue);
            }
            EditorGUI.indentLevel--;
            if (EditorGUI.EndChangeCheck()) ((Projection)target).UpdateMaterial();
            

            //Info
            if (maskingInfo)
            {
                if (maskMethod.enumValueIndex == 0)
                {
                    EditorGUILayout.HelpBox("Draw On Everything Except - will project projections to all objects except those within the selected layers. For example, bullet holes should effect everything except characters.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Only Draw On - will only project projections onto selected layers. For example, footprints should only effect the ground (In most use cases).", MessageType.Info);
                }
            }

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
    }

    //Pooling
    protected void Pooling()
    {
        if (Application.isPlaying)
        {
            Projection projection = (Projection)target;
            if (projection.PoolItem != null)
            {
                EditorGUILayout.LabelField(new GUIContent("Pool : " + projection.PoolItem.Pool.Title, "Determines the shape and transparency details of the projection."), EditorStyles.boldLabel);
                EditorGUILayout.Space();
            }
        }
    }

    //Texture Previews
    protected virtual void UpdateTexturePreviews()
    {
        //Occlusion Change
        if (occlusionChange)
        {
            switch (TransStyle)
            {
                case TransparencyStyle.Alpha:
                    if (TransTex != null && TransColor != null) OcclusionPreview.GetAlphaOcclusion(TransTex, TransColor.colorValue.a, cutoff.floatValue, transparencyType.enumValueIndex);                   
                    break;
                case TransparencyStyle.Shape:
                    if (TransTex != null && TransMultiplier != null) OcclusionPreview.GetShapeOcclusion(TransTex, TransMultiplier.floatValue, cutoff.floatValue, transparencyType.enumValueIndex);
                    break;
            }
            occlusionChange = false;
        }
    }
}