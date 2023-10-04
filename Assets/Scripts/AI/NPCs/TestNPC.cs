using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestNPC : NPC_base
{
    [SerializeField] private float m_lastTimeRandomDestination = 0;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        if(Time.time > m_lastTimeRandomDestination + m_timeRandomDestination)
        {
            m_lastTimeRandomDestination = Time.time;

            Vector3 randomPos = transform.position;

            RaycastHit[] hit = Physics.RaycastAll(transform.position + new Vector3( Random.Range(-10,10), 100, Random.Range(-10, 10)), Vector3.down);

            for (int i = 0; i < hit.Length; i++)
            {
                if (hit[i].collider.gameObject.layer == 10)
                {
                    randomPos = hit[i].point;
                    break;
                }
            }

            startMovingTo(randomPos);
        }
    }
}
