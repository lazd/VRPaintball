using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchController : MonoBehaviour {
    // Which controller to use
    public OVRInput.Controller Controller;

	// Update is called once per frame
	void FixedUpdate () {
        if (OVRManager.isHmdPresent)
        {
            transform.localPosition = OVRInput.GetLocalControllerPosition(Controller);
            transform.localRotation = OVRInput.GetLocalControllerRotation(Controller);
        }
    }
}
