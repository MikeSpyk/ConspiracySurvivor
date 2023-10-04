using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager singleton;

    [SerializeField] private GameObject m_worldViewPointPrefab;
    [SerializeField] private bool m_DEBUG_mode = false;

    Dictionary<int, WorldViewPoint> m_gameID_worldViewPoint = new Dictionary<int, WorldViewPoint>(); // server knows the viewpoints for all clients

    private void Awake()
    {
        singleton = this;
    }

    private void Update()
    {
        if (GameManager_Custom.singleton.isClient)
        {
            if (m_DEBUG_mode)
            {
                if (m_gameID_worldViewPoint.Count > 1)
                {
                    string text = "PlayerManager: Update: Warning: There are multiple WorldViewPoints on client side !: ";

                    foreach (KeyValuePair<int, WorldViewPoint> pair in m_gameID_worldViewPoint)
                    {
                        text += pair.Key + ", ";
                    }

                    Debug.LogWarning(text);
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="playerGameID">for local player "-1"</param>
    /// <returns></returns>
    public WorldViewPoint getWorldViewPoint(int playerGameID)
    {
        WorldViewPoint result;

        if (m_gameID_worldViewPoint.TryGetValue(playerGameID, out result))
        {
            return result;
        }
        else
        {
            GameObject newViewPoint = Instantiate(m_worldViewPointPrefab, Vector3.zero, Quaternion.identity) as GameObject;
            newViewPoint.name = "World View Point Player " + playerGameID;
            WorldViewPoint script = newViewPoint.GetComponent<WorldViewPoint>();
            script.owningPlayerGameID = playerGameID;
            m_gameID_worldViewPoint.Add(playerGameID, script);
            return script;
        }
    }

    public void removeWorldViewPoint(int playerGameID)
    {
        if (m_gameID_worldViewPoint.ContainsKey(playerGameID))
        {
            if (m_gameID_worldViewPoint[playerGameID] != null)
            {
                Destroy(m_gameID_worldViewPoint[playerGameID].gameObject);
            }

            m_gameID_worldViewPoint.Remove(playerGameID);
        }
    }

    public List<Vector3> getAllViewPointsPositions()
    {
        List<Vector3> viewPositions = new List<Vector3>();

        foreach (KeyValuePair<int, WorldViewPoint> pair in m_gameID_worldViewPoint)
        {
            viewPositions.Add(pair.Value.transform.position);
        }

        return viewPositions;
    }
    public void getAllViewPointsPositions(out List<Vector3> positions, out List<int> gameIDs)
    {
        positions = new List<Vector3>();
        gameIDs = new List<int>();

        foreach (KeyValuePair<int, WorldViewPoint> pair in m_gameID_worldViewPoint)
        {
            gameIDs.Add(pair.Key);
            positions.Add(pair.Value.transform.position);
        }
    }
    public void getAllViewPointsPositions(out List<Vector3> positions, out List<int> gameIDs, out List<float> viewDistances)
    {
        positions = new List<Vector3>();
        gameIDs = new List<int>();
        viewDistances = new List<float>();

        foreach (KeyValuePair<int, WorldViewPoint> pair in m_gameID_worldViewPoint)
        {
            gameIDs.Add(pair.Key);
            positions.Add(pair.Value.transform.position);

            viewDistances.Add(NetworkingManager.singleton.server_getClientViewDistance(pair.Key));
        }
    }

    public List<Vector2> getAllViewPointsPositionsXZ()
    {
        List<Vector2> viewPositions = new List<Vector2>();

        foreach (KeyValuePair<int, WorldViewPoint> pair in m_gameID_worldViewPoint)
        {
            viewPositions.Add(new Vector2(pair.Value.transform.position.x, pair.Value.transform.position.z));
        }

        return viewPositions;
    }

    public List<Vector2> getAllViewPointsPositionsExceptLocalPlayerXZ()
    {
        List<Vector2> viewPositions = new List<Vector2>();

        foreach (KeyValuePair<int, WorldViewPoint> pair in m_gameID_worldViewPoint)
        {
            if (pair.Key != -1)
            {
                viewPositions.Add(new Vector2(pair.Value.transform.position.x, pair.Value.transform.position.z));
            }
        }

        return viewPositions;
    }
}
