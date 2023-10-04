using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudManager : MonoBehaviour {

	public static CloudManager singleton;

	[SerializeField]
	private GameObject CloudPrefab;
	[SerializeField]
	private float timeBetweenCloudSpawns = 1f;
	[SerializeField]
	private Vector3 cloudStartPos = Vector3.zero;
	[SerializeField]
	private Vector3 cloudDriftDir = Vector3.one;
	[SerializeField]
	private float cloudSpawnWidth = 100f;
	[SerializeField]
	private float killCloudAfterDistanceFromSpawn = 1000f;
	[SerializeField]
	private float timeBetweenCloudsTranslation= 1f;
	[SerializeField]
	private float cloudDriftSpeed = 1f;
	[SerializeField]
	private bool hideCloudsInHierarchy;

	private float lastTimeCloudSpawned = 0f;
	private float lastTimeCloudsTranslation= 0f;

	private List<GameObject> activeCloudObjs = new List<GameObject>();
	private List<GameObject> freeCloudObjs = new List<GameObject>();

	void Awake()
	{
		singleton = this;
	}
		
	void FixedUpdate()
	{
		// spawn new clouds
		if(Time.time > lastTimeCloudSpawned + timeBetweenCloudSpawns)
		{
			lastTimeCloudSpawned = Time.time;

			Vector3 spawnOnLineVec = Quaternion.Euler(0,90,0) * cloudDriftDir;
			float spawnDistance = (Random.value * cloudSpawnWidth * 2) -cloudSpawnWidth;

			Vector3 spawnPos = cloudStartPos + spawnOnLineVec.normalized * spawnDistance;

			GameObject currentCloud;

			if(freeCloudObjs.Count > 0)
			{
				currentCloud = freeCloudObjs[0];
				freeCloudObjs.RemoveAt(0);
				currentCloud.SetActive(true);
				currentCloud.transform.position = spawnPos;
			}
			else
			{
				currentCloud = Instantiate(CloudPrefab, spawnPos, Quaternion.identity) as GameObject;
			}

			if(hideCloudsInHierarchy)
			{
				currentCloud.hideFlags = HideFlags.HideInHierarchy;
			}

			activeCloudObjs.Add(currentCloud);
		}

		// move existing clouds or remove
		if(Time.time > lastTimeCloudsTranslation + timeBetweenCloudsTranslation)
		{
			lastTimeCloudsTranslation = Time.time;

			for(int i = 0; i < activeCloudObjs.Count; i++)
			{
				if(Vector3.Distance(activeCloudObjs[i].transform.position, cloudStartPos) > killCloudAfterDistanceFromSpawn)
				{
					recyleCloud(activeCloudObjs[i]);
				}
				else
				{
					activeCloudObjs[i].transform.position += cloudDriftDir.normalized * cloudDriftSpeed;
				}
			}
		}
	}

	private void recyleCloud(GameObject cloudObj)
	{
		activeCloudObjs.Remove(cloudObj);
		freeCloudObjs.Add(cloudObj);
		cloudObj.SetActive(false);
	}
}
