using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(ProjectionMask))]
public class ProjectionMaskEditor : Editor
{
    SerializedProperty type;
    SerializedProperty layers;

    private void OnEnable()
    {
        type = serializedObject.FindProperty("type");
        layers = serializedObject.FindProperty("layers");

        //Register to Undo callback
        Undo.undoRedoPerformed += OnUndoRedo;
    }
    private void OnDisable()
    {
        //Deregister to Undo callback
        Undo.undoRedoPerformed -= OnUndoRedo;
    }
    private void OnUndoRedo()
    {
        ((ProjectionMask)target).Mark();
    }

    public override void OnInspectorGUI()
    {
        //Update Object
        serializedObject.Update();

        //Grab our target
        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField(new GUIContent("Type", "The type of mask determines what renderers are being masked. Standard will mask any attached renderers. Compound will mask the renderers of direct children."), EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(type, new GUIContent(""));
        EditorGUI.indentLevel--;

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            for (int i = 0; i < serializedObject.targetObjects.Length; i++)
            {
                ((ProjectionMask)serializedObject.targetObjects[i]).GrabInstances();
            }
        }
        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField(new GUIContent("Layers", "The mask layers avaliable to draw to"), EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        for (int i = 0; i < layers.arraySize; i++)
        {
            EditorGUILayout.PropertyField(layers.GetArrayElementAtIndex(i), new GUIContent(DynamicDecals.System.Settings.layerNames[i], "Draw to this mask layer?"));
        }
        EditorGUI.indentLevel--;

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            for (int i = 0; i < serializedObject.targetObjects.Length; i++)
            {
                ((ProjectionMask)serializedObject.targetObjects[i]).Mark();
            }
        }
        EditorGUILayout.Space();        
    }
}
