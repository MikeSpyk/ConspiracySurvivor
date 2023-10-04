using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Item_equippable : MonoBehaviour
{
    protected enum ItemUsageMode { Once, Continuous }

    [SerializeField] private int firstPersonPrefabIndex;
    [SerializeField] private float m_timeBetweenItemUsagePrimary = 1f;

    public float timeBetweenItemUsagePrimary { get { return m_timeBetweenItemUsagePrimary; } }

    protected bool m_isActive = true;
    protected ClientSettingsManager keySource;

    protected float m_lastTimeItemUsagePrimary = 0f;
    protected float m_lastTimeItemUsagePrimaryEnded = 0f;
    protected ItemUsageMode m_primaryUsageMode = ItemUsageMode.Continuous;

    protected float m_timeBetweenItemUsageSecondary = 1f;
    protected float m_lastTimeItemUsageSecondary = 0f;
    protected ItemUsageMode m_secondaryUsageMode = ItemUsageMode.Continuous;

    protected GameObject m_carrier = null;
    protected Player_local m_carrierPlayer;

    protected void Start()
    {
        m_carrier = transform.parent.gameObject;
        setItemActivity(true);
        m_carrierPlayer = m_carrier.GetComponent<Player_local>();
        keySource = ClientSettingsManager.singleton;
    }

    protected void Update()
    {
        if (m_isActive && !GUIManager.singleton.coursorActive)
        {
            if (m_primaryUsageMode == ItemUsageMode.Continuous)
            {
                if (Input.GetKey(keySource.getPrimaryActionKey()))
                {
                    if (Time.time > m_lastTimeItemUsagePrimary + m_timeBetweenItemUsagePrimary)
                    {
                        onItemUsagePrimary();
                        m_lastTimeItemUsagePrimary = Time.time;
                    }
                }
                else
                {
                    m_lastTimeItemUsagePrimaryEnded = Time.time;
                    onItemUsagePrimaryEnded();
                }
            }

            if (m_secondaryUsageMode == ItemUsageMode.Continuous)
            {
                if (Input.GetKey(keySource.getSecondaryActionKey()))
                {
                    if (Time.time > m_lastTimeItemUsageSecondary + m_timeBetweenItemUsageSecondary)
                    {
                        onItemUsageSecondary();
                        m_lastTimeItemUsageSecondary = Time.time;
                    }
                }
                else
                {
                    onItemUsageSecondaryEnded();
                }
            }
        }
    }

    public void setItemActivity(bool newState)
    {
        m_isActive = newState;
        if (newState == true)
        {
            FirstPersonAnimationManager.singleton.changeFirstPersonViewItem(firstPersonPrefabIndex);
            onItemActivated();
        }
        else
        {
            onItemDeactivated();
        }
    }

    protected virtual void onItemUsagePrimary() { }

    protected virtual void onItemUsagePrimaryEnded() { }

    protected virtual void onItemUsageSecondary() { }

    protected virtual void onItemUsageSecondaryEnded() { }

    protected virtual void onItemActivated() { }

    protected virtual void onItemDeactivated() { }

}
