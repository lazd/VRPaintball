using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaintSplatter : MonoBehaviour {
    public float destroyTime = 30f;
	// Use this for initialization
	void Start () {
        Destroy(gameObject, destroyTime);
	}
}
