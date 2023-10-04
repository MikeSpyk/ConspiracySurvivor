using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POIEditor : MonoBehaviour
{
    [SerializeField] private GameObject m_POITemplate;
    [SerializeField] private bool m_ApplyPOIChanges = false;
    [SerializeField] private bool m_ShowHiddenObjects = false;
    [SerializeField] private GameObject m_circlePrefab;
    [SerializeField] private float circleHeight = 100;
    [SerializeField] private float verticesDistance = 0.5f;
    [SerializeField] bool m_groupAll = false;
    [SerializeField] bool m_ungroupAll = false;

    private GameObject innerCircleObj = null;
    private GameObject blurCircleObj = null;

    private void OnValidate()
    {
        if(m_ShowHiddenObjects)
        {
            m_ShowHiddenObjects = false;
            GameObject[] allObjectsScene = FindObjectsOfType<GameObject>();

            for (int i = 0; i < allObjectsScene.Length; i++)
            {
                if (allObjectsScene[i].hideFlags == HideFlags.HideInHierarchy)
                {
                    allObjectsScene[i].hideFlags = HideFlags.None;
                }
            }
        }

        if(m_groupAll)
        {
            m_groupAll = false;
            groupAllToPOI();
        }

        if (m_ungroupAll)
        {
            m_ungroupAll = false;
            ungroupAllPOI();
        }

        if (m_ApplyPOIChanges)
        {
            if(innerCircleObj == null)
            {
                innerCircleObj = Instantiate(m_circlePrefab);
                innerCircleObj.transform.position = new Vector3(0, circleHeight/2, 0);
                innerCircleObj.transform.parent = gameObject.transform;
                innerCircleObj.hideFlags = HideFlags.HideInHierarchy;
                innerCircleObj.tag = "EditorUtility";
            }

            if(blurCircleObj == null)
            {
                blurCircleObj = Instantiate(m_circlePrefab);
                blurCircleObj.transform.position = new Vector3(0, circleHeight/2, 0);
                blurCircleObj.transform.parent = gameObject.transform;
                blurCircleObj.hideFlags = HideFlags.HideInHierarchy;
                blurCircleObj.tag = "EditorUtility";
            }

            if (m_POITemplate != null && m_POITemplate.GetComponent<PointOfInterest>() != null)
            {
                innerCircleObj.transform.localScale = new Vector3(m_POITemplate.GetComponent<PointOfInterest>().radius * verticesDistance, circleHeight, m_POITemplate.GetComponent<PointOfInterest>().radius * verticesDistance);
                blurCircleObj.transform.localScale = new Vector3((m_POITemplate.GetComponent<PointOfInterest>().radius + m_POITemplate.GetComponent<PointOfInterest>().blurDistance) * verticesDistance, circleHeight, (m_POITemplate.GetComponent<PointOfInterest>().radius + m_POITemplate.GetComponent<PointOfInterest>().blurDistance)* verticesDistance);
            }

            m_ApplyPOIChanges = false;
        }
    }

    private void groupAllToPOI()
    {
        if (m_POITemplate == null)
        {
            Debug.LogWarning("POIEditor: POI-Template Null");
            return;
        }

        GameObject[] allObjectsScene = FindObjectsOfType<GameObject>();

        for (int i = 0; i < allObjectsScene.Length; i++)
        {
            if (allObjectsScene[i].tag == "Untagged" && allObjectsScene[i].transform.parent == null)
            {
                allObjectsScene[i].transform.parent = m_POITemplate.transform;
            }
        }
    }

    private void ungroupAllPOI()
    {
        if (m_POITemplate == null)
        {
            Debug.LogWarning("POIEditor: POI-Template Null");
            return;
        }

        GameObject[] allObjectsScene = FindObjectsOfType<GameObject>();

        for (int i = 0; i < allObjectsScene.Length; i++)
        {
            if (allObjectsScene[i].transform.parent == m_POITemplate.transform)
            {
                allObjectsScene[i].transform.parent = null;
            }
        }
    }

}
