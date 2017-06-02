using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.VR;

public class PlayerController : NetworkBehaviour
{
    public GameObject head;
    public GameObject body;
    public Transform bulletSpawn;

    public GameObject paintballMarker;
    public GameObject paintbrush;

    public OVRInput.Controller dominantController = OVRInput.Controller.RTouch;
    public OVRInput.Controller supportController = OVRInput.Controller.LTouch;

    protected GameObject dominantControllerObject;
    protected GameObject supportControllerObject;

    public GameObject primaryWeapon;
    public GameObject secondaryWeapon;

    public float moveForce = 15f;
    public float jumpForce = 2f;
    public float maxSpeed = 10f;

    public bool jetpack = false;

    public bool controlMovement = false;

    protected Transform headPosition;

    private float nextPrimaryFireTime = 0f;
    private float nextSecondaryFireTime = 0f;

    private Rigidbody rb;

    private RaycastHit hit;

    private Bullet primaryBulletInstance;
    private Bullet secondaryBulletInstance;

    private OVRInput.Controller dominantTouch;
    private OVRInput.Controller supportTouch;

    private Vector3 moveDirection;

    [SyncVar(hook = "SetColor")]
    public Color color;

    void SetColor(Color newColor)
    {
        body.GetComponent<Renderer>().material.SetColor("_Color", newColor);
        body.GetComponent<Renderer>().material.SetColor("_EmissionColor", newColor / 10);
        head.GetComponent<Renderer>().material.SetColor("_Color", newColor);
        head.GetComponent<Renderer>().material.SetColor("_EmissionColor", newColor / 10);
        paintbrush.GetComponent<Renderer>().material.SetColor("_Color", newColor);
        paintbrush.GetComponent<Renderer>().material.SetColor("_EmissionColor", newColor/10);
        paintballMarker.GetComponent<Renderer>().material.SetColor("_Color", newColor);

        color = newColor;
    }

    void Start()
    {
        // Disable the MouseLook component by default
        var mouseLook = GetComponent<SmoothMouseLook>();
        mouseLook.enabled = false;

        // Get the rigidbody associated with the player
        rb = GetComponent<Rigidbody>();

        // Don't rotate unless we say so
        rb.freezeRotation = true;

        // Get the associated game objects
        dominantControllerObject = transform.Find("BodyRoot/CameraRoot/DominantTouch").gameObject;
        supportControllerObject = transform.Find("BodyRoot/CameraRoot/SupportTouch").gameObject;

        // Get instances from the objects
        primaryBulletInstance = primaryWeapon.GetComponent(typeof(Bullet)) as Bullet;
        secondaryBulletInstance = secondaryWeapon.GetComponent(typeof(Bullet)) as Bullet;

        if (isLocalPlayer)
        {
            var cameraRoot = transform.Find("BodyRoot/CameraRoot");

            // Put the camera inside of the player
            // This makes it so moving the player moves the headset
            Camera.main.transform.parent = cameraRoot;
            Camera.main.transform.localPosition = Vector3.zero;
            Camera.main.transform.localRotation = Quaternion.Euler(Vector3.zero);

            // Put the head inside of the camera's head position
            // This handles rotation and position of the head relative to the headset
            headPosition = Camera.main.transform.Find("HeadPosition");

            if (!VRDevice.isPresent)
            {
                // Enable MouseLook
                mouseLook.enabled = true;

                // Hide the cursor and lock it to the window when the player spawns
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                // Move the head position back so the head rotates in the right place 
                Camera.main.transform.Find("HeadPosition").localPosition = Vector3.zero;

                // Move the marker itself into the camera
                var marker = transform.Find("BodyRoot/CameraRoot/DominantTouch/PaintballMarker");
                marker.parent = Camera.main.transform;

                // Reset the rotation
                marker.localRotation = Quaternion.Euler(Vector3.zero);

                // Remove the second hand
                transform.Find("BodyRoot/CameraRoot/SupportTouch").gameObject.SetActive(false);

                // Move the swatter itself into the camera
                //var swatter = transform.Find("BodyRoot/CameraRoot/SupportTouch/FlySwatter");
                //swatter.parent = Camera.main.transform;
            }
        }

        // Set the color
        SetColor(color);
    }

    void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (jetpack)
        {
            rb.useGravity = false;
        }

        // Move the head object to the head position
        // Instead of having the head as a child, we need to do this for network purposes
        head.transform.position = headPosition.position;
        head.transform.rotation = headPosition.rotation;

        // Movement
        if (VRDevice.isPresent)
        {
            // Move hands into the right place
            dominantControllerObject.transform.localPosition = OVRInput.GetLocalControllerPosition(dominantController);
            dominantControllerObject.transform.localRotation = OVRInput.GetLocalControllerRotation(dominantController);
            supportControllerObject.transform.localPosition = OVRInput.GetLocalControllerPosition(supportController);
            supportControllerObject.transform.localRotation = OVRInput.GetLocalControllerRotation(supportController);

            // Get the position of the stick
            var moveStickPosition = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, supportController);

            // Keep the body under the head
            body.transform.position = new Vector3(head.transform.position.x, body.transform.position.y, head.transform.position.z);
            
            // Turn the body to face towards the hand
            // This will generally match the position of the body when holding a weapon
            var bodyRotateTowards = supportControllerObject.transform.position;

            // Reset the Y position so the body doesn't tilt up
            bodyRotateTowards.y = body.transform.position.y;

            // Get direction to the hand
            var direction = (bodyRotateTowards - body.transform.position).normalized;

            // Rotate the body towards the hand
            body.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            // Set the move direction relative to the position of the leading hand
            moveDirection = body.transform.rotation * new Vector3(
                moveStickPosition.x,
                0,
                moveStickPosition.y
            );
        }
        else
        {
            // Use sticks to control movement
            moveDirection = transform.rotation * new Vector3(
                Input.GetAxis("Horizontal"),
                0,
                Input.GetAxis("Vertical")
            );

            // Move the touch controller into to the correct position for network purposes
            dominantControllerObject.transform.position = transform.Find("BodyRoot/Head/FixedHandPosition").position;
            dominantControllerObject.transform.rotation = transform.Find("BodyRoot/Head/FixedHandPosition").rotation;
        }

        moveDirection = Vector3.ClampMagnitude(moveDirection * moveForce, maxSpeed);

        /*
        // This causes the character to ramp off of hills, but makes all hills climbable
        if (Physics.Raycast(transform.position, -transform.up, out hit, 2f))
        {
            // Always move parallel to the ground below us if we're grounded
            // This makes the character not slow down when climbing hills
            moveDirection = Quaternion.FromToRotation(transform.up, hit.normal) * moveDirection;
        }
        */

        if (controlMovement)
        {
            // Apply drag, but not in the Y direction
            // If drag is applied in the Y direction (or the Drag property is used on the rigidbody), the character falls too slowly
            var vel = rb.velocity;
            vel.x *= 0.8f;
            vel.z *= 0.8f;
            rb.velocity = vel;

            // Add forces to move the body relative to the position of the leading hand
            rb.AddForce(moveDirection, ForceMode.Impulse);

            // Add some extra gravity
            if (Physics.Raycast(transform.position, -transform.up, out hit, 3f))
            {
                rb.AddForce(-transform.up * 4000);
            }

            if (jetpack)
            {
                var jumpInput = Input.GetButton("Jump") ? 1 : OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, supportController);
                if (jumpInput > 0)
                {
                    rb.AddForce(transform.up * jumpForce * jumpInput, ForceMode.Impulse);
                }

                var descendInput = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, supportController);
                if (descendInput > 0)
                {
                    rb.AddForce(-transform.up * jumpForce * descendInput, ForceMode.Impulse);
                }
            }

            // Apply speed limit
            if (rb.velocity.magnitude > maxSpeed)
            {
                rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
            }
        }

        // Firing
        if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, dominantController) > 0 || Input.GetButton("Fire1"))
        {
            if (Time.time > nextPrimaryFireTime)
            {
                // Bullets shouldn't get all of the player's momentum added to them
                var bulletVelocity = rb.velocity * 0.1f;
                var ni = GetComponent<NetworkIdentity>();
                if (!ni.isServer) {
                    // Spawn a bullet client side
                    spawnBullet(bulletSpawn.position, bulletSpawn.rotation, bulletVelocity, Network.player.guid, color);
                }

                // Spawn a bullet server side
                CmdFirePrimary(bulletSpawn.position, bulletSpawn.rotation, bulletVelocity, Network.player.guid, color);

                nextPrimaryFireTime = Time.time + (1f / primaryBulletInstance.rate);
            }
        }
        else
        {
            if (OVRInput.Get(OVRInput.Button.One, dominantController) || Input.GetButton("Fire2"))
            {
                // Stab knife
            }
        }
    }

    public override void OnStartLocalPlayer()
    {
        // Disable your health meter
        //transform.Find("BodyRoot/Body/Healthbar Canvas").gameObject.SetActive(false);
    }

    GameObject spawnBullet(Vector3 position, Quaternion rotation, Vector3 velocity, string id, Color bulletColor)
    {
        var prefab = primaryWeapon;

        // Create the Bullet from the Bullet Prefab
        var instance = (GameObject)Instantiate(
            prefab,
            position,
            rotation
        );

        var bullet = instance.GetComponent<Bullet>();
        bullet.owner = id;
        bullet.color = bulletColor;

        bullet.SetColor(Color.blue);

        // Add velocity to the bullet
        instance.GetComponent<Rigidbody>().velocity = velocity + instance.transform.forward * bullet.speed;

        return instance;
    }

    [Command]
    void CmdFirePrimary(Vector3 position, Quaternion rotation, Vector3 velocity, string id, Color bulletColor)
    {
        var prefab = primaryWeapon;

        // Create the Bullet from the Bullet Prefab
        var instance = (GameObject)Instantiate(
            prefab,
            position,
            rotation
        );

        var bullet = instance.GetComponent<Bullet>();
        bullet.owner = id;
        bullet.color = bulletColor;

        // Add velocity to the bullet
        instance.GetComponent<Rigidbody>().velocity = velocity + instance.transform.forward * bullet.speed;

        // Spawn the bullet on the Clients
        NetworkServer.SpawnWithClientAuthority(instance, connectionToClient);

        // Destroy the bullet after it times out
        Destroy(instance, bullet.time);
    }

    [Command]
    void CmdFireSecondary(Vector3 position, Quaternion rotation, Vector3 velocity, string id, Color bulletColor)
    {
        var prefab = secondaryWeapon;

        // Create the Bullet from the Bullet Prefab
        var instance = (GameObject)Instantiate(
            prefab,
            position,
            rotation
        );

        var bullet = instance.GetComponent<Bullet>();
        bullet.owner = id;
        bullet.color = bulletColor;

        // Add velocity to the bullet
        instance.GetComponent<Rigidbody>().velocity = velocity + instance.transform.forward * bullet.speed;

        // Spawn the bullet on the Clients
        NetworkServer.Spawn(instance);

        // Destroy the bullet after it times out
        Destroy(instance, bullet.time);
    }


    public static float ClampAngle (float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp (angle, min, max);
    }
}
