using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_sleeper : Player_base
{
    public bool m_suppressEntityUnregister = false;

    protected void Awake()
    {
        base.Awake();

        m_isStatic = true;

        //Debug.Log("Player_sleeper: spawned ! " + Time.frameCount);
    }

    protected void OnDestroy()
    {
        //Debug.Log("Player_sleeper: destroyed ! Time: " + Time.frameCount + "; ID: " + m_gameID);
        base.OnDestroy();
    }

    protected override void onDamaged(float damage)
    {
        if (!m_noDamage)
        {
            m_health -= damage;

            if (m_health < 0)
            {
                if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                {
                    server_dropAllItems();
                }
                Destroy(gameObject);
            }
        }
    }

    protected override void onHeal(float heal)
    {
        float temphealth;
        temphealth = heal + m_health;

        if (temphealth >= 100)
        {
            m_health = 100;
        }
        else
        {
            m_health = temphealth;
        }
    }

    protected override void onUnregisterEntity()
    {
        if (!m_suppressEntityUnregister)
        {
            base.onUnregisterEntity();
        }
    }
}
