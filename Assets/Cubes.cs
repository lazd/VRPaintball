using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cubes : MonoBehaviour {
    GameObject[] cubes = new GameObject[6];
    Vector3[] positions = new Vector3[6];

	// Use this for initialization
	void Start () {
        cubes = GameObject.FindGameObjectsWithTag("Cube");

        for (int i = 0; i < cubes.Length; i++)
        {
            positions[i] = cubes[i].transform.position;
        }
    }
	
	// Update is called once per frame
	void Update () {
		if (Input.GetButton("Fire1"))
        {
            resetPositions();
        }
	}

    void resetPositions()
    {
        for (int i = 0; i < cubes.Length; i++)
        {
            cubes[i].transform.position = positions[i];
            cubes[i].transform.rotation = new Quaternion(0, 0, 0, 0);
            cubes[i].GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }
}
