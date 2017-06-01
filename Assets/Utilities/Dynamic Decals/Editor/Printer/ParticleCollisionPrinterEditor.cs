using UnityEngine;
using UnityEditor;
using System.Collections;

[CanEditMultipleObjects]
[CustomEditor(typeof(ParticleCollisionPrinter))]
public class ParticleCollisionPrinterEditor : PrinterEditor
{
    SerializedProperty rotationSource;

    public override void OnEnable()
    {
        base.OnEnable();

        rotationSource = serializedObject.FindProperty("rotationSource");
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

        RotationSourceGUI();

        //Apply modified properties
        serializedObject.ApplyModifiedProperties();
    }

    private void RotationSourceGUI()
    {
        EditorGUILayout.LabelField(new GUIContent("Rotation Source", "What should determine how the printed decal is orientated"));
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(rotationSource, new GUIContent("", "What should determine how the printed decal is orientated"));
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }
}
