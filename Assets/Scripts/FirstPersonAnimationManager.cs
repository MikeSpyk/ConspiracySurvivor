using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonAnimationManager : MonoBehaviour {

    public static FirstPersonAnimationManager singleton;

	[SerializeField] private GameObject m_parent;

	[SerializeField] private float m_follow_RadiansPerSecoundMax = 1;
	[SerializeField] private float m_follow_RadiansMaxMagnitudeDelta = 1;
	[SerializeField] private float m_follow_DistancePerSecoundMax = 1;
	[SerializeField] private float m_follow_maxDistanceDifference = 1;
	[SerializeField] private float m_follow_maxAngleDifference = 10;
	[SerializeField] private float m_follow_AngleOverMaxFactor = 1;
	[SerializeField] private AnimationCurve m_follow_RotationSpeed;
	[SerializeField] private AnimationCurve m_follow_TranslateSpeed;

    [SerializeField] private GameObject[] m_firstPersonViewItemsPrefab;

    private GameObject[] m_firstPersonViewItemsCache;
    private GameObject m_currentlyEquippedItem = null;
    private Animator m_currentlyEquippedItemAnimator = null;
	private ParticleSystem m_currentlyEquippedItemMuzzleFlashEffect = null;
	private Dictionary<string,int> m_currentAnimator_hash_IDs = new Dictionary<string, int>();

    void Awake()
    {
        singleton = this;
    }

    // Use this for initialization
    void Start ()
    {
        m_firstPersonViewItemsCache = new GameObject[m_firstPersonViewItemsPrefab.Length];
    }
		
	// Update is called once per frame
	void Update () 
	{
		
	}

	void LateUpdate()
	{
		//Vector3 assumedNewPosition = Vector3.MoveTowards(transform.position, m_parent.transform.position,m_follow_DistancePerSecound * Time.deltaTime);
		Vector3 positionDelta = transform.position - m_parent.transform.position;
		float distanceBetweenPositions = positionDelta.magnitude;

		Vector3 nextPosition;

		if(distanceBetweenPositions > m_follow_maxDistanceDifference)
		{
			nextPosition = m_parent.transform.position + positionDelta.normalized * m_follow_maxDistanceDifference;
		}
		else
		{
			nextPosition = Vector3.MoveTowards(transform.position, m_parent.transform.position,m_follow_TranslateSpeed.Evaluate(distanceBetweenPositions/m_follow_maxDistanceDifference) * m_follow_DistancePerSecoundMax * Time.deltaTime);
		}

		transform.position = nextPosition;

		float angleBetweenRotations = Vector3.Angle(transform.forward, m_parent.transform.forward);
		float nextRotationAngles;

		if(angleBetweenRotations > m_follow_maxAngleDifference)
		{
			nextRotationAngles = ((angleBetweenRotations - m_follow_maxAngleDifference) + m_follow_RadiansPerSecoundMax) * m_follow_AngleOverMaxFactor;
		}
		else
		{
			nextRotationAngles = m_follow_RotationSpeed.Evaluate(angleBetweenRotations/m_follow_maxAngleDifference) * m_follow_RadiansPerSecoundMax * Time.deltaTime;
		}


		transform.rotation = Quaternion.LookRotation( Vector3.RotateTowards( transform.forward, m_parent.transform.forward,nextRotationAngles ,m_follow_RadiansMaxMagnitudeDelta));
	}

    public void changeFirstPersonViewItem(int prefabIndex)
    {
        if (prefabIndex >= m_firstPersonViewItemsPrefab.Length)
        {
            Debug.LogError("index \"" + prefabIndex + "\" out of range. range : 0 - " + (m_firstPersonViewItemsPrefab.Length - 1));
        }
        else
        {
            if (m_firstPersonViewItemsCache[prefabIndex] == null)
            {
                m_firstPersonViewItemsCache[prefabIndex] = Instantiate(m_firstPersonViewItemsPrefab[prefabIndex]) as GameObject;
				m_firstPersonViewItemsCache[prefabIndex].transform.parent = gameObject.transform;
            }
            m_currentlyEquippedItem = m_firstPersonViewItemsCache[prefabIndex];
            m_currentlyEquippedItemAnimator = m_currentlyEquippedItem.GetComponent<Animator>();
			if(m_currentlyEquippedItem.transform.Find("MuzzleFlash") == null)
			{
				m_currentlyEquippedItemMuzzleFlashEffect = null;
			}
			else
			{
				m_currentlyEquippedItemMuzzleFlashEffect = m_currentlyEquippedItem.transform.Find("MuzzleFlash").GetComponent<ParticleSystem>();
			}
        }
		m_currentAnimator_hash_IDs.Clear();
		m_currentAnimator_hash_IDs.Add("input_fire", Animator.StringToHash("input_fire"));
		m_currentAnimator_hash_IDs.Add("input_aim", Animator.StringToHash("input_aim"));

    }

    public void currentItemAnimationSetBool(string name, bool newState)
    {
		if(m_currentAnimator_hash_IDs.ContainsKey(name))
		{
			m_currentlyEquippedItemAnimator.SetBool(m_currentAnimator_hash_IDs[name], newState);
		}
		else
		{
			Debug.LogWarning("not cached animation hash-ID (bad performance)");
        	m_currentlyEquippedItemAnimator.SetBool(name, newState);
		}
    }

	public void playMuzzleFlashEffect()
	{
		if(m_currentlyEquippedItemMuzzleFlashEffect != null)
		{
			m_currentlyEquippedItemMuzzleFlashEffect.Play();
		}
	}
}
