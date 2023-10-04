using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldViewPoint : MonoBehaviour
{
    [Header("World Mesh Renderer")]
	[SerializeField] private int[] m_outerSize;
    [SerializeField] private bool[] m_renderThisStage;
    [Header("Networking")]
    [SerializeField] private float m_sendPositionInterval = 1;
    [Header("Misc")]
    [SerializeField, ReadOnly] private int m_owningPlayerID = -2;

    private Vector3 m_lastSendPosition = Vector3.zero;
    private float m_lastTimeSendPosition = 0;

    public int owningPlayerGameID
    {
        get
        {
            return m_owningPlayerID;
        }
        set
        {
            m_owningPlayerID = value;
        }
    }

    public int outerSizeLength
    {
        get
        {
            return m_outerSize.Length;
        }
    }

    public int renderThisStageLength
    {
        get
        {
            return m_renderThisStage.Length;
        }
    }

    private void Start()
    {
		//WorldManager.singleton.GetComponent<WorldManager>().addWorldViewpoint(gameObject);
	}

    private void onDestroy()
	{
        //WorldManager.singleton.removeWoldViewpoint(gameObject);

        PlayerManager.singleton.removeWorldViewPoint(m_owningPlayerID);
	}

    private void Update()
    {
        if (GameManager_Custom.singleton.isClient)
        {
            if (transform.position != m_lastSendPosition && Time.time > m_lastTimeSendPosition + m_sendPositionInterval)
            {
                m_lastSendPosition = transform.position;
                m_lastTimeSendPosition = Time.time;
                NetworkingManager.singleton.client_sendPlayerViewpointPosition(transform.position);
            }
        }
    }

    void FixedUpdate()
    {
        if (m_renderThisStage.Length != m_outerSize.Length || m_renderThisStage.Length != m_renderThisStage.Length)
        {
            Debug.LogWarning("WorldViewpoint: input-settings-arrays differ in size");
        }
    }

    public int getOuterSize(int qualityLevel)
	{
		if(qualityLevel < 1)
		{
			Debug.LogError("illegal qualityLevel \"" + qualityLevel + "\": qualityLevel identifier too low: qualityLevels start with 1!"  );
		}
		else if(qualityLevel > m_outerSize.Length)
		{
			Debug.LogError("illegal qualityLevel \"" + qualityLevel + "\": qualityLevel identifier too high: max identifier is " + m_outerSize.Length);
		}
		else
		{
			return m_outerSize[qualityLevel-1];
		}
		return int.MinValue;
	}

	public bool getRenderState(int qualityLevel)
	{
		if(qualityLevel < 1)
		{
			Debug.LogError("illegal qualityLevel \"" + qualityLevel + "\": qualityLevel identifier too low: qualityLevels start with 1!"  );
		}
		else if(qualityLevel > m_outerSize.Length)
		{
			Debug.LogError("illegal qualityLevel \"" + qualityLevel + "\": qualityLevel identifier too high: max identifier is " + m_renderThisStage.Length);
		}
		else
		{
			return m_renderThisStage[qualityLevel-1];
		}
		return false;
	}

}
