using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct PlayerConfigurationData
{
	public KeyCode ForwardKey;
}

public class Player_dummy : MonoBehaviour
{

    [SerializeField] private float m_health;
	public bool m_isExternalPlayer = false; // is this player instance a client connected to this server (or is it controlled thru this script)
    private float m_test_lastHealthGUI = 0f;

	private PlayerConfigurationData PlayerConfi;

	public float cameraOffsetY = 0;
	public float cameraOffsetZ = 0;

	public float PlayerAccSpeed = 1.0f;
	public float maxPlayerSpeed = 1.0f;
	public float SprintFactor = 1.5f;

	public float PlayerBakingMinSpeed = 1.0f;
	public float PlayerBakingAcc = 1.0f;

	public float playerJumpForce = 7;
	public float jumpForceTime = .1f;

	private short velocityXSign; // 1 or -1
	private short velocityZSign; // 1 or -1


	public Transform cameraTransform;
	public Transform playerHitBox;
	public float playerRadius = 1.0f;
	public float GroundedCheckRayDistance = .8f;
	public bool Grounded = false;
	private bool jumped = false;

	private Quaternion oldRotation;
	private Vector3 oldPosition;

	public Vector3 resultingForce; // private
	private Vector3 cameraRotationX;
	private Vector3 nextCameraRotationX;
	private float nextCameraRotationXEuler;
	private float deltaEulerX;

	public float gravity = -9;


	private float LastJumpTime;

	private float deltaRotationZCamera;
	private float deltaRotationXPlayer;
	private float deltaRotationZPlayer;

	private float rotationX;
	private float rotationY;
	private RaycastHit underneathPlayer;

	public Vector3 velocity;
	public float Speed;

	private float mouseX = 0;
	private float mouseY = 0;

	float CurrentSpeedXZMag = 0;
	float NextFrameMaxSpeed = 0;

	public float MovementForceModeLimit = 1f;
	public float MovementStartVelocity = 1f;

	private Rigidbody rigidbody;
	 
	public float friction = 1;
	public float CurrentSpeedXZ;

	public bool bakingX= false;
	public bool bakingZ = false;
		
	public bool noInputX = false;
	public bool noInputZ = false;

	public bool flyMode = false;
	public float flyMoveFactor = 1;

	public float fellThroughGroundHeight = -3000f;

	private List<Vector2> externalCameraRot = new List<Vector2>();

	// Use this for initialization
	void Start () 
	{
		rigidbody = GetComponent<Rigidbody>();

		cameraTransform = Camera.main.transform;
		cameraRotationX = Vector3.forward;
		resultingForce = Vector3.zero;
		jumped = false;
		playerHitBox = GetComponent<Transform> ();

	}

	private float externalCameraRotX;
	private float externalCameraRotY;
	void Update () 
	{
		if(!m_isExternalPlayer)
		{
            if(m_health != m_test_lastHealthGUI)
            {
                GUIManager.singleton.setHealth(m_health);
                m_test_lastHealthGUI = m_health;
            }


		GetComponent<Collider>().material.dynamicFriction = friction;
		GetComponent<Collider>().material.staticFriction = friction;

		Speed = GetComponent<Rigidbody>().velocity.magnitude;
		velocity = GetComponent<Rigidbody>().velocity;

		externalCameraRotX = 0;
		externalCameraRotY = 0;

		foreach(Vector2 currentVec in externalCameraRot)
		{
			externalCameraRotX += currentVec.x;
			externalCameraRotY += currentVec.y;
		}
		externalCameraRot.Clear();

		if(velocity.x > 0)
		{
			velocityXSign = 1;
		}
		else // <= 0
		{
			velocityXSign = -1;
		}
		if(velocity.z > 0)
		{
			velocityZSign = 1;
		}
		else // <= 0
		{
			velocityZSign = -1;
		}

		resultingForce = Vector3.zero;

		mouseX = Input.GetAxis ("Mouse X");
		mouseY = -Input.GetAxis ("Mouse Y");

		CurrentSpeedXZMag = new Vector2(GetComponent<Rigidbody>().velocity.x,GetComponent<Rigidbody>().velocity.z).magnitude;

		if(Input.GetKey(KeyCode.LeftControl)) // sneak
		{
			NextFrameMaxSpeed = maxPlayerSpeed * .5f;
		}
		else if(Input.GetKey(KeyCode.LeftShift)) // sprint
		{
			NextFrameMaxSpeed = maxPlayerSpeed * SprintFactor;
		}
		else
		{
			NextFrameMaxSpeed = maxPlayerSpeed;
		}

		if( Mathf.Abs(CurrentSpeedXZMag) < maxPlayerSpeed	)
		{
			resultingForce.x = Input.GetAxis ("Horizontal")  ;
			resultingForce.z = Input.GetAxis ("Vertical") 	 ;
			resultingForce.Normalize();

			//resultingForce.x = Input.GetAxis ("Horizontal") * PlayerAccSpeed ;
			//resultingForce.z = Input.GetAxis ("Vertical") * PlayerAccSpeed  ;

		}
			
		resultingForce *= PlayerAccSpeed * Mathf.Max((NextFrameMaxSpeed - CurrentSpeedXZMag) /NextFrameMaxSpeed, 0);

		bakingX = false;
		bakingZ = false;

		noInputX = false;
		noInputZ = false;


		if( Input.GetAxis ("Vertical") == 0) // no player input
		{
			noInputX = true;

		}
		if(Input.GetAxis ("Horizontal") == 0) // no player input
		{
			noInputZ = true;

		}

		if(noInputX && noInputZ)
		{
			if(Mathf.Abs(velocity.z) > PlayerBakingMinSpeed) // not too slow so the player will accelerate backwards
			{
				resultingForce.z = PlayerBakingAcc * velocityZSign * -1 * velocity.z/maxPlayerSpeed;
				bakingZ = true;
			}
			if(Mathf.Abs(velocity.x) > PlayerBakingMinSpeed) // not too slow so the player will accelerate backwards
			{
				resultingForce.x = PlayerBakingAcc * velocityXSign * -1 * velocity.x/maxPlayerSpeed;
				bakingX = true;
			}
		}
			

		if(Input.GetKey(KeyCode.Space) )
		{
			if(!jumped && Grounded)
			{
				jumped = true;
				LastJumpTime = Time.time;
			}
			if(LastJumpTime + jumpForceTime > Time.time)
			{
				resultingForce.y += playerJumpForce;
			}

		}


		if (Physics.SphereCast (transform.position + new Vector3(0.0f, .5f, 0.0f), playerRadius, Vector3.down, out underneathPlayer, GroundedCheckRayDistance)) 	// make the spherecastsize depending on size instead of a constant value
		{
			Grounded = true;
			if(!Input.GetKey(KeyCode.Space)) // prevent auto jump
			{
				jumped = false;
			}
		} 
		else 
		{
			Grounded = false;
		}
		if(!flyMode)
		{
		rigidbody.AddForce( gravity * rigidbody.mass * Vector3.up); // gravity
		}
		playerHitBox.Rotate (0.0f, mouseX + externalCameraRotX, 0.0f);

		//if(Mathf.Abs(CurrentSpeedXYMag) > MovementForceModeLimit)
		//{
			resultingForce = playerHitBox.rotation * resultingForce;
			resultingForce *= rigidbody.mass;
		if(!flyMode)
		{
			rigidbody.AddForce (resultingForce, ForceMode.Impulse);
		}
		/*}
		else
		{
			Vector3 nextVelocity = playerHitBox.rotation * new Vector3( Input.GetAxis ("Horizontal") * MovementStartVelocity ,rigidbody.velocity.y ,Input.GetAxis ("Vertical") * MovementStartVelocity);
			rigidbody.velocity = nextVelocity;
		}*/

		if(flyMode)
		{
			float ySpeed;
			if(Input.GetKey(KeyCode.Space))
			{
				ySpeed = 1;
			}
			else if(Input.GetKey(KeyCode.LeftControl))
			{
				ySpeed = -1;
			}
			else
			{
				ySpeed = 0;
			}
			GetComponent<Rigidbody>().velocity = (playerHitBox.rotation* new Vector3(Input.GetAxis ("Horizontal"),ySpeed, Input.GetAxis ("Vertical"))) * flyMoveFactor;
		}


		nextCameraRotationX = Quaternion.Euler (mouseY + externalCameraRotY, 0.0f, 0.0f) * cameraRotationX;
		nextCameraRotationXEuler = Quaternion.LookRotation (nextCameraRotationX).eulerAngles.x;
		deltaEulerX = Quaternion.Angle (Quaternion.LookRotation(playerHitBox.rotation * Vector3.forward), Quaternion.LookRotation (playerHitBox.rotation * nextCameraRotationX));

		if (deltaEulerX < 90.0f	)
		{
			cameraRotationX = nextCameraRotationX;
		}
			
		cameraTransform.position = playerHitBox.position + new Vector3(0,cameraOffsetY,cameraOffsetZ);
		cameraTransform.rotation = playerHitBox.rotation * Quaternion.LookRotation(cameraRotationX);

		oldRotation = cameraTransform.rotation;
		oldPosition = cameraTransform.position;

		}
	}

	public void addExternalCameraRotation(Vector2 rotXY)
	{
		externalCameraRot.Add(rotXY);
	}

	public int getUniqPlayerID()
	{
		return 0;
	}

	void FixedUpdate()
	{
		if(transform.position.y < fellThroughGroundHeight)
		{
			Debug.LogWarning("player fell through ground");

			RaycastHit hit;
			Physics.Raycast(new Vector3(transform.position.x,100000, transform.position.z),Vector3.down, out hit);

			transform.position = hit.point + new Vector3(0,5,0);
			GetComponent<Rigidbody>().velocity = Vector3.zero;
		}
	}
}
