using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Decal))]
public class DecalEditor : ProjectionEditor
{
    #region Properties
    SerializedProperty decalType;
    SerializedProperty lightingModel;
    SerializedProperty glossType;

    SerializedProperty emissive;

    SerializedProperty shapeTex;
    SerializedProperty shapeMultiplier;

    SerializedProperty albedoTex;
    SerializedProperty albedoColor;

    SerializedProperty normalTex;
    SerializedProperty normalStrength;

    SerializedProperty smoothnessTex;
    SerializedProperty smoothness;

    SerializedProperty specularTex;
    SerializedProperty specularColor;

    SerializedProperty metallicTex;
    SerializedProperty metallicity;

    SerializedProperty emissionTex;
    SerializedProperty emissionColor;
    SerializedProperty emissionIntensity;

    SerializedProperty projectionLimit;
    #endregion

    private Texture2D AlbedoPreview;
    private Texture2D SmoothnessPreview;
    private Texture2D SpecularColorPreview;
    private Texture2D NormalPreview;
    private Texture2D EmissionPreview;

    private bool albedoChange = true;
    private bool glossChange = true;
    private bool normalChange = true;
    private bool emissiveChange = true;

    private static bool albedoFo;
    private static bool glossFo;
    private static bool normalFo;
    private static bool emissiveFo;
    private static bool projectionFo;

    private static bool typeInfo;
    private static bool lightingInfo;
    private static bool albedoInfo;
    private static bool glossInfo;
    private static bool normalInfo;
    private static bool emissiveInfo;
    private static bool projectionInfo;

    protected override void OnEnable()
    {
        base.OnEnable();

        decalType = serializedObject.FindProperty("decalType");
        lightingModel = serializedObject.FindProperty("lightingModel");
        
        glossType = serializedObject.FindProperty("glossType");

        emissive = serializedObject.FindProperty("emissive");

        shapeTex = serializedObject.FindProperty("shapeTex");
        shapeMultiplier = serializedObject.FindProperty("shapeMultiplier");

        albedoTex = serializedObject.FindProperty("albedoTex");
        albedoColor = serializedObject.FindProperty("albedoColor");

        normalTex = serializedObject.FindProperty("normalTex");
        normalStrength = serializedObject.FindProperty("normalStrength");

        smoothnessTex = serializedObject.FindProperty("smoothnessTex");
        smoothness = serializedObject.FindProperty("smoothness");

        specularTex = serializedObject.FindProperty("specularTex");
        specularColor = serializedObject.FindProperty("specularColor");

        metallicTex = serializedObject.FindProperty("metallicTex");
        metallicity = serializedObject.FindProperty("metallicity");

        emissionTex = serializedObject.FindProperty("emissionTex");
        emissionColor = serializedObject.FindProperty("emissionColor");
        emissionIntensity = serializedObject.FindProperty("emissionIntensity");

        projectionLimit = serializedObject.FindProperty("projectionLimit");

        //Initialize our texture preveiws
        AlbedoPreview = new Texture2D(100, 100, TextureFormat.RGBA32, false, true);
        SmoothnessPreview = new Texture2D(100, 100, TextureFormat.RGBA32, false, true);
        SpecularColorPreview = new Texture2D(100, 100, TextureFormat.RGBA32, false, true);
        NormalPreview = new Texture2D(100, 100, TextureFormat.RGBA32, false, true);
        EmissionPreview = new Texture2D(100, 100, TextureFormat.RGBA32, false, true);

        //Sample texture preveiws
        albedoChange = true;
        glossChange = true;
        normalChange = true;
        emissiveChange = true;
    }
    protected override void OnDisable()
    {
        base.OnDisable();

        //Destroy temporary textures
        DestroyImmediate(OcclusionPreview);
        DestroyImmediate(AlbedoPreview);
        DestroyImmediate(SmoothnessPreview);
        DestroyImmediate(SpecularColorPreview);
        DestroyImmediate(NormalPreview);
        DestroyImmediate(EmissionPreview);
    }
    protected override void OnUndoRedo()
    {
        base.OnUndoRedo();

        //Resample texture preveiws
        albedoChange = true;
        glossChange = true;
        normalChange = true;
        emissiveChange = true;
    }

    public override void OnInspectorGUI()
    {
        //Update Object
        serializedObject.Update();

        //Assign shapeStyle, shapeTexture & shapeMultiplier 
        //The Shape and Transparency Sections will use these as a base
        switch (decalType.enumValueIndex)
        {
            case 0:
                TransStyle = TransparencyStyle.Alpha;
                TransTex = albedoTex;
                TransColor = albedoColor;
                break;
            default:
                TransStyle = TransparencyStyle.Shape;
                TransTex = shapeTex;
                TransMultiplier = shapeMultiplier;
                break;
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();       


        Priority((lightingModel.enumValueIndex == 0) ? 100 : 40);
        Type();
        switch (decalType.enumValueIndex)
        {
            case 0:
                Lighting();
                Color();
                if (lightingModel.enumValueIndex == 1)
                {
                    Gloss();
                    Normal();
                    Emission();
                }
                break;
            case 1:
                Shape();
                Roughness();
                break;
            case 2:
                Shape();
                Normal();
                break;
        }
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
    
    private void Type()
    {
        //Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Type", "When using deferred rendering multiple decal types are available. See info for details."), EditorStyles.boldLabel, GUILayout.Width(60));

        //Lighting Type Selection
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(decalType, new GUIContent(""), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
        if (EditorGUI.EndChangeCheck())
        {
            ((Decal)target).ReplaceMaterial();
            ((Decal)target).UpdateMaterial();
        }

        //Info Toggle
        if (typeInfo)
        {
            if (GUILayout.Button("i", HideInfo, GUILayout.Width(18))) typeInfo = false;
        }
        else
        {
            if (GUILayout.Button("i", ShowInfo, GUILayout.Width(18))) typeInfo = true;
        }
        EditorGUILayout.EndHorizontal();

        //Body
        EditorGUI.indentLevel++;

        //Info
        if (typeInfo)
        {
            EditorGUILayout.HelpBox("The type determines how the decal functions. In forward rendering only the Full type is supported, which is the standard decal.", MessageType.Info);
            EditorGUILayout.HelpBox("Normal will only render normal details. This allows us to project normal details, like cracks or bumps, onto the surface of other geometry.", MessageType.Info);
            EditorGUILayout.HelpBox("Roughness will only modify the roughness of the surface it's projection onto. This can be useful, for example, to make a surface appear wet.", MessageType.Info);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }
    private void Lighting()
    {
        //Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Lighting", "Determines how the object is lit."), EditorStyles.boldLabel, GUILayout.Width(60));

        //Lighting Type Selection
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(lightingModel, new GUIContent(""), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
        if (EditorGUI.EndChangeCheck())
        {
            ((Decal)target).ReplaceMaterial();
            ((Decal)target).UpdateMaterial();
        }

        //Info Toggle
        if (lightingInfo)
        {
            if (GUILayout.Button("i", HideInfo, GUILayout.Width(18))) lightingInfo = false;
        }
        else
        {
            if (GUILayout.Button("i", ShowInfo, GUILayout.Width(18))) lightingInfo = true;
        }
        EditorGUILayout.EndHorizontal();

        //Body
        EditorGUI.indentLevel++;

        //Info
        if (lightingInfo)
        {
            EditorGUILayout.HelpBox("The lighting model determines how the object will appear to be lit.", MessageType.Info);
            EditorGUILayout.HelpBox("Unlit will show the model as if no lighting is occuring, great for cheap emmisive objects, like lasers, as well as world space UI elements.", MessageType.Info);
            EditorGUILayout.HelpBox("PBR, or physically based rendering, will show the model as if lit by Unitys standard shader.", MessageType.Info);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }
    private void Color()
    {
        //Header
        EditorGUILayout.BeginHorizontal();

        if (lightingModel.enumValueIndex == 0)
        {
            albedoFo = EditorGUILayout.Foldout(albedoFo, new GUIContent("Color"), BoldFoldout);
        }
        else
        {
            albedoFo = EditorGUILayout.Foldout(albedoFo, new GUIContent("Albedo"), BoldFoldout);
        }
        
        GUILayout.FlexibleSpace();
        
        //Info Toggle
        if (albedoInfo)
        {
            if (GUILayout.Button("i", HideInfo, GUILayout.Width(18))) albedoInfo = false;
        }
        else
        {
            if (GUILayout.Button("i", ShowInfo, GUILayout.Width(18))) albedoInfo = true;
        }
        EditorGUILayout.EndHorizontal();

        //Body
        if (albedoFo)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();

            //Properties
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(albedoTex, new GUIContent(""), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
            albedoColor.colorValue = EditorGUILayout.ColorField("", albedoColor.colorValue, GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
            if (EditorGUI.EndChangeCheck())
            {
                ((Decal)target).UpdateMaterial();
                albedoChange = true;
                occlusionChange = true;
            }

            EditorGUILayout.EndVertical();

            //Spacer
            GUILayout.FlexibleSpace();

            //Preview
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(new GUIContent(AlbedoPreview), GUILayout.Width(previewSize), GUILayout.Height(previewSize * 0.8f));
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            //Info
            if (albedoInfo)
            {
                if(lightingModel.enumValueIndex == 0)
                {
                    EditorGUILayout.HelpBox("The base color of your decal. It's made up of a texture combined with a color, but either can be left blank/white if you wish to rely solely on the other.", MessageType.Info);
                    EditorGUILayout.HelpBox("The alpha of the color texture is used to determine the decals transparency.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("The albedo channel determines the base apperance of your decal. It's made up of a texture combined with a color, but either can be left blank/white if you wish to rely solely on the other.", MessageType.Info);
                    EditorGUILayout.HelpBox("The alpha of the albedo channel is used to determine the decals transparency.", MessageType.Info);
                }
                
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
    }
    private void Gloss()
    {
        //Header
        EditorGUILayout.BeginHorizontal();
        glossFo = EditorGUILayout.Foldout(glossFo, new GUIContent("Gloss", "Render to the Deffered SpecSmooth Buffer?"), BoldFoldout);
        GUILayout.FlexibleSpace();
        //InfoToggle
        if (glossInfo)
        {
            if (GUILayout.Button("i", HideInfo, GUILayout.Width(18))) glossInfo = false;
        }
        else
        {
            if (GUILayout.Button("i", ShowInfo, GUILayout.Width(18))) glossInfo = true;
        }
        EditorGUILayout.EndHorizontal();

        //Body
        if (glossFo)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space();

            //General Properties
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(glossType, new GUIContent(""));
            if (EditorGUI.EndChangeCheck())
            {
                ((Decal)target).ReplaceMaterial();
                ((Decal)target).UpdateMaterial();
                glossChange = true;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            //Metallic Properties
            if (glossType.enumValueIndex == 0)
            {
                EditorGUILayout.LabelField(new GUIContent("Metalicity", "Determines how metallic the surface will appear. Derived from the R channel of the metallic texture and the metallic slider."), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
                EditorGUILayout.PropertyField(metallicTex, new GUIContent(""), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
                metallicity.floatValue = EditorGUILayout.Slider("", metallicity.floatValue, 0, 1, GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
            }
            //Specular Properties
            if (glossType.enumValueIndex != 0)
            {
                EditorGUILayout.LabelField(new GUIContent("Specular", "Determines the surfaces specular reflectivity. Derived from the RGB channels of the specular texture and the specular color."), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
                EditorGUILayout.PropertyField(specularTex, new GUIContent(""), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
                specularColor.colorValue = EditorGUILayout.ColorField("", specularColor.colorValue, GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
            }
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                ((Decal)target).UpdateMaterial();
                glossChange = true;
            }

            //Spacer
            GUILayout.FlexibleSpace();

            //Preview
            EditorGUILayout.LabelField(new GUIContent(SpecularColorPreview), GUILayout.Width(previewSize), GUILayout.Height(previewSize * 0.8f));
            EditorGUILayout.EndHorizontal();

            if(glossInfo)
            {
                if (glossType.enumValueIndex == 0)
                {
                    EditorGUILayout.HelpBox("Metallicity determines how metallic the decal will appear. Metallic objects will have darker desaturated albedos and instead exhibit much of there original albedo as a tint to the specular reflection", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Specular color manifests as a tint to the specular reflections.", MessageType.Info);
                }
            }

            //Glossiness
            EditorGUILayout.BeginHorizontal();

            //Properties
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(new GUIContent("Glossiness", "Determines the perceived glossiness or roughness of a surface."), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
            smoothness.floatValue = EditorGUILayout.Slider("", smoothness.floatValue, 0, 1, GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));

            EditorGUILayout.EndVertical();

            //Spacer
            GUILayout.FlexibleSpace();

            //Preview
            EditorGUILayout.LabelField(new GUIContent(SmoothnessPreview), GUILayout.Width(previewSize), GUILayout.Height(previewSize * 0.8f));

            EditorGUILayout.EndHorizontal();

            if (glossInfo)
            {
                EditorGUILayout.HelpBox("Glossiness determines the roughness - smoothness of your object. A value of 0 (Black) will be completely rough and cause no reflections while a value of 1 (White) will cause perfectly clear reflections", MessageType.Info);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
    }
    private void Roughness()
    {
        //Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Roughness", "How rough or smooth the projected surface should appear."), EditorStyles.boldLabel, GUILayout.Width(120));
        GUILayout.FlexibleSpace();
        //InfoToggle
        if (glossInfo)
        {
            if (GUILayout.Button("i", HideInfo, GUILayout.Width(18))) glossInfo = false;
        }
        else
        {
            if (GUILayout.Button("i", ShowInfo, GUILayout.Width(18))) glossInfo = true;
        }
        EditorGUILayout.EndHorizontal();

        //Body
        EditorGUI.indentLevel++;
        EditorGUILayout.Space();

        //Glossiness
        EditorGUILayout.BeginHorizontal();

        //Properties
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField(new GUIContent("Smoothness", "Determines the perceived glossiness or roughness of a surface."), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
        EditorGUILayout.PropertyField(smoothnessTex, new GUIContent(""), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
        smoothness.floatValue = EditorGUILayout.Slider("", smoothness.floatValue, 0, 1, GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));

        EditorGUILayout.EndVertical();

        //Spacer
        GUILayout.FlexibleSpace();

        //Preview
        EditorGUILayout.LabelField(new GUIContent(SmoothnessPreview), GUILayout.Width(previewSize), GUILayout.Height(previewSize * 0.8f));

        EditorGUILayout.EndHorizontal();

        if (glossInfo)
        {
            EditorGUILayout.HelpBox("Glossiness determines the roughness - smoothness of your object. A value of 0 (Black) will be completely rough and cause no reflections while a value of 1 (White) will cause perfectly clear reflections", MessageType.Info);
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }
    private void Normal()
    {
        //Header
        EditorGUILayout.BeginHorizontal();
        normalFo = EditorGUILayout.Foldout(normalFo, new GUIContent("Normal", "Render to the Deffered Normal Buffer?"), BoldFoldout);
        GUILayout.FlexibleSpace();
        
        //Info Toggle
        if (normalInfo)
        {
            if (GUILayout.Button("i", HideInfo, GUILayout.Width(18))) normalInfo = false;
        }
        else
        {
            if (GUILayout.Button("i", ShowInfo, GUILayout.Width(18))) normalInfo = true;
        }
        EditorGUILayout.EndHorizontal();

        //Body
        if (normalFo)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();

            //Properties
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(normalTex, new GUIContent(""), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
            normalStrength.floatValue = EditorGUILayout.Slider("", normalStrength.floatValue, 0, 4, GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
            if (EditorGUI.EndChangeCheck())
            {
                ((Decal)target).UpdateMaterial();
                normalChange = true;
            }
            EditorGUILayout.EndVertical();

            //Spacer
            GUILayout.FlexibleSpace();

            //Preview
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(new GUIContent(NormalPreview), GUILayout.Width(previewSize), GUILayout.Height(previewSize * 0.8f));
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            //Info
            if (normalInfo)
            {
                EditorGUILayout.HelpBox("Normals change how the surface of the object appears in the lighting and reflection passes and thus, change how it is lit. The strength slider can be used to either blend or accentuate there effect.", MessageType.Info);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
    }
    private void Emission()
    {
        //Header
        EditorGUILayout.BeginHorizontal();
        emissiveFo = EditorGUILayout.Foldout(emissiveFo, new GUIContent("Emission", "Render Emissions over the Ambient / GI Buffer?"), BoldFoldout);
        GUILayout.FlexibleSpace();
        //Toggle
        EditorGUI.BeginChangeCheck();
        emissive.boolValue = EditorGUILayout.Toggle("", emissive.boolValue, GUILayout.Width(20));
        if (EditorGUI.EndChangeCheck())
        {
            ((Decal)target).UpdateMaterial();
            emissiveChange = true;
        }
        //Info Toggle
        if (emissiveInfo)
        {
            if (GUILayout.Button("i", HideInfo, GUILayout.Width(18))) emissiveInfo = false;
        }
        else
        {
            if (GUILayout.Button("i", ShowInfo, GUILayout.Width(18))) emissiveInfo = true;
        }
        EditorGUILayout.EndHorizontal();

        if (emissiveFo)
        {
            //Store GUI state
            bool guiState = GUI.enabled;
            //If buffer is disbled disable the gui
            if (!emissive.boolValue)
            {
                GUI.enabled = false;
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();

            //Properties
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(emissionTex, new GUIContent(""), GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
            emissionColor.colorValue = EditorGUILayout.ColorField("", emissionColor.colorValue, GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
            emissionIntensity.floatValue = EditorGUILayout.Slider("", emissionIntensity.floatValue, 0, 100, GUILayout.Width(EditorGUIUtility.currentViewWidth - (previewSize + 35)));
            if (EditorGUI.EndChangeCheck())
            {
                ((Decal)target).UpdateMaterial();
                emissiveChange = true;
            }
            EditorGUILayout.EndVertical();

            //Spacer
            GUILayout.FlexibleSpace();

            //Preview
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(new GUIContent(EmissionPreview), GUILayout.Width(previewSize), GUILayout.Height(previewSize * 0.8f));
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            //Info
            if (emissiveInfo)
            {
                EditorGUILayout.HelpBox("Emissions are added on top of ambient light in the prepass lighting buffer. They can make an object appear lit, or appear to ignore lighting. For best results combine with bloom.", MessageType.Info);
            }
            EditorGUI.indentLevel--;

            //Restore GUI state
            GUI.enabled = guiState;
        }
        EditorGUILayout.Space();
        
    }
    private void Projection()
    {
        //Header
        EditorGUILayout.BeginHorizontal();
        projectionFo = EditorGUILayout.Foldout(projectionFo, new GUIContent("Projection", "Determines how this decal is projected"), BoldFoldout);
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
            if (EditorGUI.EndChangeCheck()) ((Decal)target).UpdateMaterial();
            

            //Info
            if (projectionInfo)
            {
                EditorGUILayout.HelpBox("The projection limit determines at what angle (between the decals forward vector & the surface normal) we stop drawing the decal. Essentially it prevents decals from drawing in situations in which they would be stretched. 0 draws nothing, 180 everything. Default value is 80.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
    }

    protected override void UpdateTexturePreviews()
    {
        //Update base texture previews
        base.UpdateTexturePreviews();

        if (albedoChange || occlusionChange) AlbedoPreview.GetColoredProperty(albedoTex, albedoColor.colorValue, false);

        if (glossType.enumValueIndex == 0)
        {
            if (glossChange) SpecularColorPreview.GetMetallic(metallicTex, metallicity.floatValue);
            if (glossChange) SmoothnessPreview.GetSmoothness(metallicTex, smoothness.floatValue);
        }
        else
        {
            if (glossChange || albedoChange) SpecularColorPreview.GetColoredProperty(specularTex, specularColor.colorValue);
            if (glossChange) SmoothnessPreview.GetSmoothness(specularTex, smoothness.floatValue);
        }

        if (normalChange) NormalPreview.GetNormal(normalTex, normalStrength.floatValue);
        if (emissiveChange) EmissionPreview.GetColoredProperty(emissionTex, emissionColor.colorValue);

        //No longer need to apply changes
        albedoChange = false;
        glossChange = false;
        normalChange = false;
        emissiveChange = false;
    }
}