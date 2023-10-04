using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinHeightTranslator : MonoBehaviour {

	[SerializeField]
	private float minHeight = 0f;
	[SerializeField]
	private float maxOffset = 1f;
	[SerializeField]
	private float speed = 1f;

	private Vector3 position = new Vector3(0,0,0);

	// Update is called once per frame
	void Update () 
	{
		position.x = transform.position.x;
		position.z = transform.position.z;
		position.y = minHeight +(1+ Mathf.Sin(Time.time * speed)) * maxOffset /2;
		transform.position = position;
	}
}
