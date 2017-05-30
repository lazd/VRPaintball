using UnityEngine;
using System.Collections;

[AddComponentMenu("Camera-Control/Mouse Look")]
public class SmoothMouseLook : MonoBehaviour {
	 
	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, 	MouseY = 2 };
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 15F;
	public float sensitivityY = 15F;
	 
	public float minimumX = -360F;
	public float maximumX = 360F;
	 
	public float minimumY = -60F;
	public float maximumY = 60F;
	 
	float rotationX = 0F;
	float rotationY = 0F;
	 
	Quaternion originalCameraRotation;
	Quaternion originalBodyRotation;
	 
	void Update()
	{
		// Read the mouse input axis
		rotationX += Input.GetAxis("Mouse X") * sensitivityX;
		rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
		 
		rotationX = ClampAngle(rotationX, minimumX, maximumX);
		rotationY = ClampAngle(rotationY, minimumY, maximumY);
		 
		Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
		Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);
		 
		// Rotate the camera up/down
		Camera.main.transform.localRotation = originalCameraRotation * yQuaternion;

		// Rotate the main character left/right
		transform.localRotation = originalBodyRotation * xQuaternion;
	}
	 
	void Start()
	{
		originalCameraRotation = Camera.main.transform.localRotation;
		originalBodyRotation = Camera.main.transform.localRotation;
	}
	 
	public static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360F)
			angle += 360F;
		if (angle > 360F)
			angle -= 360F;
		return Mathf.Clamp(angle, min, max);
	}
}
