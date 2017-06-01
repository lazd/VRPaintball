using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Printer))]
public class PrinterEditor : Editor
{
    protected SerializedProperty prints;
    protected SerializedProperty printLayers;
    protected SerializedProperty printTags;
    protected SerializedProperty printMethod;

    protected SerializedProperty poolID;

    protected SerializedProperty parent;

    protected SerializedProperty overlaps;

    protected SerializedProperty fadeMethod;
    protected SerializedProperty inDuration;
    protected SerializedProperty fadeDelay;
    protected SerializedProperty outDuration;

    protected SerializedProperty cullMethod;
    protected SerializedProperty cullDuration;

    protected SerializedProperty frequencyTime;
    protected SerializedProperty frequencyDistance;

    protected SerializedProperty destroyOnPrint;

    public virtual void OnEnable()
    {
        prints = serializedObject.FindProperty("prints");
        printLayers = serializedObject.FindProperty("printLayers");
        printTags = serializedObject.FindProperty("printTags");
        printMethod = serializedObject.FindProperty("printMethod");

        poolID = serializedObject.FindProperty("poolID");

        parent = serializedObject.FindProperty("parent");

        overlaps = serializedObject.FindProperty("overlaps");

        fadeMethod = serializedObject.FindProperty("fadeMethod");
        inDuration = serializedObject.FindProperty("inDuration");
        fadeDelay = serializedObject.FindProperty("fadeDelay");
        outDuration = serializedObject.FindProperty("outDuration");

        cullMethod = serializedObject.FindProperty("cullMethod");
        cullDuration = serializedObject.FindProperty("cullDuration");

        frequencyTime = serializedObject.FindProperty("frequencyTime");
        frequencyDistance = serializedObject.FindProperty("frequencyDistance");

        destroyOnPrint = serializedObject.FindProperty("destroyOnPrint");
    }

    public override void OnInspectorGUI()
    {
        //Update object
        serializedObject.Update();

        PrintGUI();
        PoolGUI();
        ParentGUI();
        OverlapGUI();
        FadeGUI();
        FrequencyGUI();

        //Apply modified properties
        serializedObject.ApplyModifiedProperties();
    }

    protected void PrintGUI()
    {
        //Top Space
        EditorGUILayout.Space();

        //Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Projections", "The possible Projections to print & the method used to select amongst them."), GUILayout.MaxWidth(120));
        GUILayout.FlexibleSpace();
        EditorGUI.BeginChangeCheck();
        int printSize = EditorGUILayout.IntSlider(new GUIContent("", "The number of projections available to print"), prints.arraySize, 1, 10, GUILayout.MaxWidth(120));
        if (EditorGUI.EndChangeCheck() || printLayers.arraySize != prints.arraySize)
        {
            prints.arraySize = printSize;
            printLayers.arraySize = prints.arraySize;
            printTags.arraySize = prints.arraySize;
        }
        
        EditorGUILayout.EndHorizontal();
        //Body
        EditorGUI.indentLevel++;
        //Selection method is only relevant if theres more than 1 print to choose from
        if (prints.arraySize > 1)
        {
            EditorGUILayout.PropertyField(printMethod, new GUIContent("Selection Method", "The method used to select amongst the prints."));
            EditorGUILayout.Space();
        }
            
        //Prints
        for (int i = 0; i < prints.arraySize; i++)
        {
            if (prints.arraySize > 1 && printLayers.arraySize > 1 && printMethod.enumValueIndex == 2)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(prints.GetArrayElementAtIndex(i), new GUIContent("", "Projection to print"));
                EditorGUILayout.PropertyField(printLayers.GetArrayElementAtIndex(i), new GUIContent("", "Layer to print on"),GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }
            else if (prints.arraySize > 1 && printLayers.arraySize > 1 && printMethod.enumValueIndex == 3)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(prints.GetArrayElementAtIndex(i), new GUIContent("", "Projection to print"));
                if (i == 0)
                {
                    EditorGUILayout.LabelField(new GUIContent("Default", "Tag to print on"), GUILayout.Width(100));
                }
                else
                {
                    printTags.GetArrayElementAtIndex(i).stringValue = EditorGUILayout.TagField(new GUIContent("", "Tag to print on"), printTags.GetArrayElementAtIndex(i).stringValue, GUILayout.Width(100));
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.PropertyField(prints.GetArrayElementAtIndex(i), new GUIContent("", "Projection to print"));
            }
        }
        EditorGUI.indentLevel--;
       
        EditorGUILayout.Space();
    }
    protected void PoolGUI()
    {
        EditorGUILayout.LabelField(new GUIContent("Pool", "The pool the printed projections belong to"));
        EditorGUI.indentLevel++;
        PoolSelection(poolID, new GUIContent(""));
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }
    protected void ParentGUI()
    {
        EditorGUILayout.LabelField(new GUIContent("Parent", "The transform to attach the prints to"));
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(parent, new GUIContent("", "The transform to attach the prints to"));
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }
    protected void OverlapGUI()
    {
        //Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Prevent Overlap", "Should we try and prevent printing over similar projections?"), GUILayout.MaxWidth(120));
        GUILayout.FlexibleSpace();
        overlaps.arraySize = EditorGUILayout.IntSlider(new GUIContent("", "Should we try and prevent printing over similar projections?"), overlaps.arraySize, 0, 10, GUILayout.MaxWidth(120));
        EditorGUILayout.EndHorizontal();
        if (overlaps.arraySize > 0)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < overlaps.arraySize; i++)
            {
                //Grab intersection properties
                SerializedProperty intersectionID = overlaps.GetArrayElementAtIndex(i).FindPropertyRelative("poolId");
                SerializedProperty intersectionStrength = overlaps.GetArrayElementAtIndex(i).FindPropertyRelative("intersectionStrength");

                //Draw in Horizontal
                EditorGUILayout.BeginHorizontal();
                PoolSelection(intersectionID, new GUIContent("", "The pool of projections we are testing against for overlap."), GUILayout.MaxWidth(120));
                GUILayout.FlexibleSpace();
                intersectionStrength.floatValue = EditorGUILayout.Slider(new GUIContent("", "How much overlap should there be before cancel a print. 0 will cancel with anytime a projection is positioned within another, while 1 will only cancel if a projection is in the exact same position."), intersectionStrength.floatValue, 0, 1, GUILayout.MaxWidth(140));
                EditorGUILayout.EndHorizontal();
            }

            if (EditorGUI.EndChangeCheck())
            {

            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
    }
    protected void FadeGUI()
    {
        EditorGUILayout.LabelField(new GUIContent("Fade & Cull", "Should we fade the projection out after a set duration?"));
        EditorGUI.indentLevel++;

        //Fade
        EditorGUILayout.PropertyField(fadeMethod, new GUIContent("Fade Method", "How should we fade it out?"));
        if (fadeMethod.enumValueIndex != 0)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(inDuration, new GUIContent("In Duration", "How long we take to fade in the projection"));
            EditorGUILayout.PropertyField(fadeDelay, new GUIContent("Delay", "How long we hold the projection at full size"));
            EditorGUILayout.PropertyField(outDuration, new GUIContent("Out Duration", "How long we takes to fade the projection out"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();

        //Cull
        EditorGUILayout.PropertyField(cullMethod, new GUIContent("Cull Method", "Should we remove the projection after if it's been offscreen for a set duration?"));
        if (cullMethod.enumValueIndex != 0)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cullDuration, new GUIContent("Duration", "How long should the projection be off-screen for before we remove it?"));
            EditorGUI.indentLevel--;
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }
    protected void FrequencyGUI()
    {
        EditorGUILayout.PropertyField(destroyOnPrint, new GUIContent("Destroy On Print", "Destroy the attached GameObject on print?"));
        EditorGUILayout.Space();
        if (!destroyOnPrint.boolValue)
        {
            EditorGUILayout.LabelField(new GUIContent("Frequency", "How frequently the projection can be printed."));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(frequencyTime, new GUIContent("Time", "The minimum time between consecutive projection prints."));
            EditorGUILayout.PropertyField(frequencyDistance, new GUIContent("Distance", "The minimum distance between consecutive projection prints."));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void PoolSelection(SerializedProperty ID, GUIContent Label, params GUILayoutOption[] Options)
    {
        EditorGUI.BeginChangeCheck();
        GUIContent[] pools = new GUIContent[DynamicDecals.System.Settings.pools.Length];
        int index = 0;
        for (int i = 0; i < DynamicDecals.System.Settings.pools.Length; i++)
        {
            pools[i] = new GUIContent(DynamicDecals.System.Settings.pools[i].title);
            if (ID.intValue == DynamicDecals.System.Settings.pools[i].id) index = i;
        }
        index = EditorGUILayout.Popup(Label, index, pools, Options);
        if (EditorGUI.EndChangeCheck() || ID.intValue != 0 && index == 0)
        {
            ID.intValue = DynamicDecals.System.Settings.pools[index].id;
        }
    }
}