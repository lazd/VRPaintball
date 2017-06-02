using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;


[CustomEditor(typeof(GrassPlacementScript))]
public class GrassPlacemant : Editor {


//	private static GameObject[] ObjectToPlace;
//	private static GameObject ParentObject;
//	private static float[] scale;
//	bool groupEnabled;

	
	// Add menu item named "My Window" to the Window menu
	//[MenuItem("UserHelp/PlaceObject")]
//	public static void ShowWindow()
//	{
//		//Show existing window instance. If one doesn't exist, make one.
//		EditorWindow.GetWindow(typeof(GrassPlacemant));
//	}
	
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		//EditorGUILayout.PropertyField(serializedObject.FindProperty("groupEnabled"),true);
		serializedObject.FindProperty("groupEnabled").boolValue = EditorGUILayout.BeginToggleGroup ("Enable Placement", serializedObject.FindProperty("groupEnabled").boolValue);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("ParentObject"),true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("parentname"),true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("ObjectsToPlace"),true);
			//EditorGUILayout.PropertyField(serializedObject.FindProperty("scale"),true);
		EditorGUILayout.EndToggleGroup ();
		serializedObject.ApplyModifiedProperties();

//		DrawDefaultInspector();
//		GUILayout.Label ("GrassPlacement", EditorStyles.boldLabel);
//
//		GUILayout.BeginHorizontal();
//		EditorGUILayout.LabelField("Local Scale",GUILayout.Width(100));
//		scale = EditorGUILayout.FloatField(scale,GUILayout.Width(35));
//		GUILayout.EndHorizontal();
//
//		groupEnabled = EditorGUILayout.BeginToggleGroup ("Enable Placement", groupEnabled);
//		ParentObject = (GameObject)EditorGUILayout.ObjectField("Parent GameObject",ParentObject,typeof(GameObject),true);
//		ObjectToPlace = (GameObject)EditorGUILayout.ObjectField("Object to be placed",ObjectToPlace,typeof(GameObject),true);
//		EditorGUILayout.EndToggleGroup ();
	}
//	public static void Show (SerializedProperty list, bool showListSize = true) {
//		EditorGUILayout.PropertyField(list);
//		EditorGUI.indentLevel += 1;
//		if (list.isExpanded) {
//			if (showListSize) {
//				EditorGUILayout.PropertyField(list.FindPropertyRelative("Array.size"));
//			}
//			for (int i = 0; i < list.arraySize; i++) {
//				EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i));
//			}
//		}
//		EditorGUI.indentLevel -= 1;
//	}
	void OnSceneGUI()
	{
		GrassPlacementScript grass = target as GrassPlacementScript;
	
		if(grass.groupEnabled)
		{
			if (grass.ObjectsToPlace.Length > 0 && Event.current.type == EventType.MouseDown && Event.current.button == 1)
			{
				Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				RaycastHit hitInfo;
				if(grass.ParentObject == null)
				{
					grass.ParentObject =  new GameObject(grass.parentname);
				}
				if (Physics.Raycast(worldRay, out hitInfo, 10000))
				{
					Undo.RegisterSceneUndo("PlaceObject");
					int temp = Random.Range(0,grass.ObjectsToPlace.Length);
					GameObject prefab_instance = PrefabUtility.InstantiatePrefab(grass.ObjectsToPlace[temp].Object) as GameObject;
					prefab_instance.transform.localScale = new Vector3(grass.ObjectsToPlace[temp].scale,grass.ObjectsToPlace[temp].scale,grass.ObjectsToPlace[temp].scale);
					prefab_instance.transform.localEulerAngles = new Vector3(prefab_instance.transform.localEulerAngles.x,Random.Range(0,360),prefab_instance.transform.localEulerAngles.z);
					prefab_instance.transform.parent = grass.ParentObject.transform;
					prefab_instance.transform.position = hitInfo.point;
					Selection.activeObject = grass;
				}
				Event.current.Use();
			}
			
		}
	}
}
		