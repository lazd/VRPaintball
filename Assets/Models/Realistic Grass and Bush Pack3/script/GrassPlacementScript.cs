using UnityEngine;
using System.Collections;
[System.Serializable]

public class Objects {
	
	public GameObject Object;
	public float scale = 1.0f;
	public Objects()
	{
		scale = 1.0f;
	}
}

public class GrassPlacementScript : MonoBehaviour {
	public Objects[] ObjectsToPlace;
	public GameObject ParentObject;
	public bool groupEnabled;
	public string parentname = "Parent";
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
