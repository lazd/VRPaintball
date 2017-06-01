using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(BasicCollisionPrinter))]
public class BasicCollisionPrinterEditor : PrinterEditor
{
    SerializedProperty rotationSource;
    SerializedProperty layers;

    public override void OnEnable()
    {
        base.OnEnable();
        rotationSource = serializedObject.FindProperty("rotationSource");
        layers = serializedObject.FindProperty("layers");
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

        LayersGUI();
        RotationGUI();

        //Apply modified properties
        serializedObject.ApplyModifiedProperties();
    }

    private void LayersGUI()
    {
        if (prints.arraySize > 1 && printMethod.enumValueIndex == 2)
        {
            int finalLayers = 0;
            foreach (SerializedProperty layermask in printLayers)
            {
                finalLayers = (finalLayers | layermask.intValue);
            }
            layers.intValue = finalLayers;
        }
        else
        {
            EditorGUILayout.LabelField(new GUIContent("Layers", "Which layers to cast against"));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(layers, new GUIContent("", "Which layers to cast against"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
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