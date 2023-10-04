using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDummy : Player_local
{

    [SerializeField] private float cameraOffsetY = 0;
    [SerializeField] private float cameraOffsetZ = 0;
    [SerializeField] private float PlayerAccSpeed = 1.0f;
    [SerializeField] private float maxPlayerSpeed = 1.0f;
    [SerializeField] private float SprintFactor = 1.5f;
    [SerializeField] private float PlayerBakingMinSpeed = 1.0f;
    [SerializeField] private float PlayerBakingAcc = 1.0f;
    [SerializeField] private float playerJumpForce = 7;
    [SerializeField] private float jumpForceTime = .1f;
    [SerializeField] private float playerRadius = 1.0f;
    [SerializeField] private float GroundedCheckRayDistance = .8f;
    [SerializeField] private bool Grounded = false;
    [SerializeField] private bool jumped = false;
    [SerializeField] private Vector3 resultingForce;
    [SerializeField] private float gravity = -9;
    [SerializeField] private Vector3 velocity;
    [SerializeField] private float Speed;
    [SerializeField] private float friction = 1;
    [SerializeField] private bool bakingX = false;
    [SerializeField] private bool bakingZ = false;
    [SerializeField] private bool noInputX = false;
    [SerializeField] private bool noInputZ = false;
    [SerializeField] private bool flyMode = false;
    [SerializeField] private float flyMoveFactor = 1;

    protected float externalCameraRotX;
    private float externalCameraRotY;
    private Vector3 cameraRotationX;
    private Vector3 nextCameraRotationX;
    private Transform cameraTransform;


    private short velocityXSign; // 1 or -1
    private short velocityZSign; // 1 or -1

    private Transform playerHitBox;

    private Quaternion oldRotation;
    private Vector3 oldPosition;

    private float nextCameraRotationXEuler;
    private float deltaEulerX;

    private float LastJumpTime;

    private RaycastHit underneathPlayer;

    private float mouseX = 0;
    private float mouseY = 0;

    private float CurrentSpeedXZMag = 0;
    private float NextFrameMaxSpeed = 0;

    private Rigidbody rigidbody;



    // Use this for initialization
    void Start()
    {

        cameraTransform = Camera.main.transform;
        cameraRotationX = Vector3.forward;
        rigidbody = GetComponent<Rigidbody>();

        resultingForce = Vector3.zero;
        jumped = false;
        playerHitBox = GetComponent<Transform>();

        GetComponent<Collider>().material.dynamicFriction = friction;
        GetComponent<Collider>().material.staticFriction = friction;
    }

    void Update()
    {
        base.Update();

        Speed = rigidbody.velocity.magnitude;
        velocity = rigidbody.velocity;

        externalCameraRotX = 0;
        externalCameraRotY = 0;

        foreach (Vector2 currentVec in externalCameraRot)
        {
            externalCameraRotX += currentVec.x;
            externalCameraRotY += currentVec.y;
        }
        externalCameraRot.Clear();


        if (velocity.x > 0)
        {
            velocityXSign = 1;
        }
        else // <= 0
        {
            velocityXSign = -1;
        }
        if (velocity.z > 0)
        {
            velocityZSign = 1;
        }
        else // <= 0
        {
            velocityZSign = -1;
        }

        resultingForce = Vector3.zero;

        mouseX = Input.GetAxis("Mouse X");
        mouseY = -Input.GetAxis("Mouse Y");

        CurrentSpeedXZMag = new Vector2(rigidbody.velocity.x, rigidbody.velocity.z).magnitude;

        if (Input.GetKey(KeyCode.LeftControl)) // sneak
        {
            NextFrameMaxSpeed = maxPlayerSpeed * .5f;
        }
        else if (Input.GetKey(KeyCode.LeftShift)) // sprint
        {
            NextFrameMaxSpeed = maxPlayerSpeed * SprintFactor;
        }
        else
        {
            NextFrameMaxSpeed = maxPlayerSpeed;
        }

        if (Mathf.Abs(CurrentSpeedXZMag) < maxPlayerSpeed)
        {
            resultingForce.x = Input.GetAxis("Horizontal");
            resultingForce.z = Input.GetAxis("Vertical");
            resultingForce.Normalize();
        }

        resultingForce *= PlayerAccSpeed * Mathf.Max((NextFrameMaxSpeed - CurrentSpeedXZMag) / NextFrameMaxSpeed, 0);

        bakingX = false;
        bakingZ = false;

        noInputX = false;
        noInputZ = false;


        if (Input.GetAxis("Vertical") == 0) // no player input
        {
            noInputX = true;

        }
        if (Input.GetAxis("Horizontal") == 0) // no player input
        {
            noInputZ = true;

        }

        if (noInputX && noInputZ)
        {
            if (Mathf.Abs(velocity.z) > PlayerBakingMinSpeed) // not too slow so the player will accelerate backwards
            {
                resultingForce.z = PlayerBakingAcc * velocityZSign * -1 * velocity.z / maxPlayerSpeed;
                bakingZ = true;
            }
            if (Mathf.Abs(velocity.x) > PlayerBakingMinSpeed) // not too slow so the player will accelerate backwards
            {
                resultingForce.x = PlayerBakingAcc * velocityXSign * -1 * velocity.x / maxPlayerSpeed;
                bakingX = true;
            }
        }

        if (Input.GetKey(KeyCode.Space))
        {
            if (!jumped && Grounded)
            {
                jumped = true;
                LastJumpTime = Time.time;
            }
            if (LastJumpTime + jumpForceTime > Time.time)
            {
                resultingForce.y += playerJumpForce;
            }

        }

        if (Physics.SphereCast(transform.position + new Vector3(0.0f, .5f, 0.0f), playerRadius, Vector3.down, out underneathPlayer, GroundedCheckRayDistance))  // make the spherecastsize depending on size instead of a constant value
        {
            Grounded = true;
            if (!Input.GetKey(KeyCode.Space)) // prevent auto jump
            {
                jumped = false;
            }
        }
        else
        {
            Grounded = false;
        }

        if (!flyMode)
        {
            rigidbody.AddForce(gravity * rigidbody.mass * Vector3.up); // gravity
        }

        playerHitBox.Rotate(0.0f, mouseX + externalCameraRotX, 0.0f);
        resultingForce = playerHitBox.rotation * resultingForce;
        resultingForce *= rigidbody.mass;

        if (!flyMode)
        {
            rigidbody.AddForce(resultingForce, ForceMode.Impulse);
        }

        if (flyMode)
        {
            float ySpeed;
            if (Input.GetKey(KeyCode.Space))
            {
                ySpeed = 1;
            }
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                ySpeed = -1;
            }
            else
            {
                ySpeed = 0;
            }
            rigidbody.velocity = (playerHitBox.rotation * new Vector3(Input.GetAxis("Horizontal"), ySpeed, Input.GetAxis("Vertical"))) * flyMoveFactor;
        }

        nextCameraRotationX = Quaternion.Euler(mouseY + externalCameraRotY, 0.0f, 0.0f) * cameraRotationX;
        nextCameraRotationXEuler = Quaternion.LookRotation(nextCameraRotationX).eulerAngles.x;
        deltaEulerX = Quaternion.Angle(Quaternion.LookRotation(playerHitBox.rotation * Vector3.forward), Quaternion.LookRotation(playerHitBox.rotation * nextCameraRotationX));

        if (deltaEulerX < 90.0f)
        {
            cameraRotationX = nextCameraRotationX;
        }

        cameraTransform.position = playerHitBox.position + new Vector3(0, cameraOffsetY, cameraOffsetZ);
        cameraTransform.rotation = playerHitBox.rotation * Quaternion.LookRotation(cameraRotationX);

        oldRotation = cameraTransform.rotation;
        oldPosition = cameraTransform.position;
    }



    void FixedUpdate()
    {
        Player_local_FixedUpdate();
    }
}
