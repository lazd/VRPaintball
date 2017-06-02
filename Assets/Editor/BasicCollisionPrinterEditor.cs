using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(BasicCollisionPrinter))]
public class BasicCollisionPrinterEditor : PrinterEditor
{
    SerializedProperty rotationSource;
    public override void OnEnable()
    {
        rotationSource = serializedObject.FindProperty("rotationSource");
        base.OnEnable();
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

        RotationGUI();

        //Apply modified properties
        serializedObject.ApplyModifiedProperties();
    }
    private void RotationGUI()
    {
        EditorGUILayout.LabelField(new GUIContent("Rotation Source", "What determines the rotation of our decal?"));
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(rotationSource, new GUIContent("", "What determines the rotation of our decal?"));
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }
}