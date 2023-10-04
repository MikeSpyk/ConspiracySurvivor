using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatastropheManager : MonoBehaviour
{
    public static CatastropheManager singleton = null;

    [SerializeField] private float m_creditsIncreaseSpeed = 1f;
    [SerializeField] private float m_credits = 0; // used to pay the cost of an event
    [SerializeField] private GameObject[] m_catastrophicEventsPrefabs;

    private CatastrophicEvent_Base[] m_catastrophicEventsPrefabsScripts;
    private Dictionary<CatastrophicEvent_Base, GameObject> m_eventScript_eventPrefab = new Dictionary<CatastrophicEvent_Base, GameObject>();
    private Dictionary<string, CatastrophicEvent_Base> m_eventClassName_eventClass = new Dictionary<string, CatastrophicEvent_Base>();
    private CatastrophicEvent_Base m_nextMajorEvent = null;
    private System.Random m_random = null;
    private List<CatastrophicEvent_Base> m_possibleInitialEvents = new List<CatastrophicEvent_Base>();

    private void Awake()
    {
        if (singleton != null)
        {
            Debug.LogError("CatastropheManager: Awake: singleton is already set !");
        }
        singleton = this;

        m_random = new System.Random();

        m_catastrophicEventsPrefabsScripts = new CatastrophicEvent_Base[m_catastrophicEventsPrefabs.Length];

        for (int i = 0; i < m_catastrophicEventsPrefabs.Length; i++)
        {
            CatastrophicEvent_Base script = m_catastrophicEventsPrefabs[i].GetComponent<CatastrophicEvent_Base>();
            m_catastrophicEventsPrefabsScripts[i] = script;

            if (script == null)
            {
                Debug.LogError("m_catastrophicEventsPrefabs[" + i + "] has no component of type CatastrophicEvent attached !");
            }

            m_eventClassName_eventClass.Add(script.GetType().Name, script);
            m_eventScript_eventPrefab.Add(script, m_catastrophicEventsPrefabs[i]);
        }

        for (int i = 0; i < m_catastrophicEventsPrefabsScripts.Length; i++)
        {
            if (m_catastrophicEventsPrefabsScripts[i].isInitialEvent)
            {
                m_possibleInitialEvents.Add(m_catastrophicEventsPrefabsScripts[i]);
            }
        }
    }

    private void Update()
    {
        if ((GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient) && GameManager_Custom.singleton.isGameInitialized)
        {
            m_credits += Time.deltaTime * m_creditsIncreaseSpeed;

            nextMajorEventUpdate();
        }

        if(GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            //GUIManager.singleton.setTimeUntilEnd(new System.TimeSpan())
        }
    }

    private void nextMajorEventUpdate()
    {
        if (m_nextMajorEvent == null)
        {
            if (m_credits > 0)
            {
                if (m_possibleInitialEvents.Count < 1)
                {
                    //Debug.LogWarning("CatastropheManager: nextMajorEventUpdate: no more initial events available !");
                }
                else
                {
                    int nextEventIndex = m_random.Next(0, m_possibleInitialEvents.Count - 1);

                    GameObject nextMajorEventGameobject = Instantiate(m_eventScript_eventPrefab[m_possibleInitialEvents[nextEventIndex]]);
                    nextMajorEventGameobject.transform.SetParent(transform);
                    m_possibleInitialEvents.RemoveAt(nextEventIndex);

                    m_nextMajorEvent = nextMajorEventGameobject.GetComponent<CatastrophicEvent_Base>();
                    Debug.Log("CatastropheManager: nextMajorEventUpdate: next major event has been choosen: \"" + m_nextMajorEvent.GetType().Name + "\"");
                }
            }
        }
        else
        {
            if (m_credits > 0)
            {
                if (m_nextMajorEvent.pendingPreEvents.Count > 0)
                {
                    int nextEventIndex = m_random.Next(0, m_nextMajorEvent.pendingPreEvents.Count - 1);

                    string nextPreEventName = m_nextMajorEvent.pendingPreEvents[nextEventIndex];
                    startSubEvent(nextPreEventName);
                    m_nextMajorEvent.pendingPreEvents.RemoveAt(nextEventIndex);
                }
                else
                {
                    m_nextMajorEvent.fireMajorEvent();
                    m_credits -= m_nextMajorEvent.mainEventCost;
                    m_nextMajorEvent = null;
                }
            }
        }
    }

    private void startSubEvent(string eventClassName)
    {
        if (m_eventClassName_eventClass.ContainsKey(eventClassName))
        {
            CatastrophicEvent_Base subEventScript = m_eventClassName_eventClass[eventClassName];

            GameObject subEvent = Instantiate(m_eventScript_eventPrefab[subEventScript]);
            subEvent.transform.SetParent(m_nextMajorEvent.transform);

            m_credits -= subEventScript.mainEventCost;
            subEvent.GetComponent<CatastrophicEvent_Base>().fireMajorEvent();

            Debug.Log("CatastropheManager: startSubEvent: next sub event of \"" + m_nextMajorEvent.GetType().Name + "\" has been choosen: \"" + subEventScript.GetType().Name + "\"");
        }
        else
        {
            Debug.LogError("CatastropheManager: startSubEvent: Class-Name \"" + eventClassName + "\" is unknown !");
        }
    }

}
