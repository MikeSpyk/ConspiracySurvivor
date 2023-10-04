using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatastrophicEvent_Base : MonoBehaviour
{
    [SerializeField] private bool m_isInitialEvent = false;
    [SerializeField] private int m_mainEventCost = 0;
    [SerializeField] private string[] m_preEventsClassNames;

    private List<string> m_pendingPreEvents = new List<string>();

    protected void Awake()
    {
        for (int i = 0; i < m_preEventsClassNames.Length; i++)
        {
            m_pendingPreEvents.Add(m_preEventsClassNames[i]);
        }
    }

    /// <summary>
    /// can this event be triggered as the first one on a new game ?
    /// </summary>
    public bool isInitialEvent { get { return m_isInitialEvent; } }

    /// <summary>
    /// how expensiv is this (main/major) event
    /// </summary>
    public int mainEventCost { get { return m_mainEventCost; } }

    /// <summary>
    ///  event-class-names that need to get fired bevor the main event gets fired
    /// </summary>
    public List<string> pendingPreEvents { get { return m_pendingPreEvents; } }

    public virtual void fireMajorEvent()
    {
        throw new System.NotImplementedException("You need to override this method in a derived class !");
    }
}
