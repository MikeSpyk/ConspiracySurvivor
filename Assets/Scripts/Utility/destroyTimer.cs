using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class destroyTimer : MonoBehaviour
{
    [SerializeField] private bool m_start = false;
    [SerializeField] private bool m_active = false;
    [SerializeField] private float m_time = 10;
    [SerializeField, ReadOnly] private float m_timeLeft = 0;

    private float m_startTime = 0;

    // Update is called once per frame
    void Update()
    {
        if(m_start)
        {
            m_startTime = Time.time;
            m_start = false;
            m_active = true;
        }

        if(m_active)
        {
            m_timeLeft = Mathf.Abs(m_startTime + m_time - Time.time);

            if(Time.time > m_startTime + m_time)
            {
                Destroy(gameObject);
            }
        }
    }
}
