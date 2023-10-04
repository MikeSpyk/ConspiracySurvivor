using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gunshot : MonoBehaviour {


	private Rigidbody myRigidbody;
	public float damage = 0;
	public float bulletDrop = 0;
	public int playerID = 0;

	private const float maxLifeTime = 100f;
	private float shotStartTime = 0;
	[SerializeField]
	private float startTrailAfterDistance = .1f;
	private Vector3 startPos = Vector3.zero;

	/*

	void Awake()
	{
		myRigidbody = GetComponent<Rigidbody>();
	}

	void Start()
	{
		GetComponent<TrailRenderer>().enabled = false;
	}
		
	void Update () 
	{
		myRigidbody.AddForce(myRigidbody.velocity.normalized * -1 * (bulletDrop/10000) * Mathf.Pow( myRigidbody.velocity.magnitude,2), ForceMode.VelocityChange);

		if(!GetComponent<TrailRenderer>().enabled)
		{
			if(Vector3.Distance(startPos, transform.position) > startTrailAfterDistance)
			{
				GetComponent<TrailRenderer>().Clear();
				GetComponent<TrailRenderer>().enabled = true;
			}
		}

	}

	void FixedUpdate()
	{
		if(Time.time > shotStartTime + maxLifeTime)
		{
			disableShot();
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if(other.gameObject.tag != "Player")
		{
			Debug.DrawRay(transform.position, Vector3.up * 10f, Color.red, 2f);
			disableShot();
		}
	}

	public void initialize(Vector3 velocity,float InBulletDrop, float inDamage, int inPlayerID)
	{
		startPos = transform.position;
		shotStartTime = Time.time;
		myRigidbody.velocity = velocity;
		bulletDrop = InBulletDrop;
		damage = inDamage;
		playerID = inPlayerID;
	}

	public void disableShot()
	{
		GetComponent<TrailRenderer>().enabled = false;
		ProjectileManager.singleton.recyleGunShot(gameObject);
	}

*/
}
