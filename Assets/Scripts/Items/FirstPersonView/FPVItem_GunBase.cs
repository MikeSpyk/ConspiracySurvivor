using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPVItem_GunBase : FPVItem_Base
{
    [SerializeField] private float m_damage;
    [SerializeField] private float m_projectileSpeed;
    [SerializeField] private float m_bulletAirResistance = 1;
    [SerializeField] private float m_bulletGravityFactor = 1;
    [SerializeField] private int m_projectileManagerPrefabIndex; // unique ID
    [SerializeField] private Vector2[] m_sprayPattern;
    [SerializeField] protected int m_shotSoundIndex = 0;
    [SerializeField] private int m_magazinCapacity = 1;
    [SerializeField] protected int m_magazinLoadedAmmo = 1;
    [SerializeField] private float m_reloadingTime = 1;
    [SerializeField] private int m_fireEmptySoundIndex = 0;
    [SerializeField] protected float m_zoomedFOV = 65;
    [SerializeField] protected float m_zoomSpeed = 1;
    private float m_reloadStartTime = 0;
    private int m_currentSprayPatternIndex = 0;
    private float m_timeTillSprayReset = 0.3f;
    private bool m_isReloading = false;
    private float m_lastTimeFiredEmpty = 0;
    protected bool m_reloadingBlocked = false;
    protected bool m_shootingPrimaryBlocked = false;
    public float airResistance { get { return m_bulletAirResistance; } }
    public float gravityFactor { get { return m_bulletGravityFactor; } }
    public float damage { get { return m_damage; } }
    public float projectileSpeed { get { return m_projectileSpeed; } }

    public int shotSoundIndex
    {
        get
        {
            return m_shotSoundIndex;
        }
    }

    protected void Start()
    {
        base.Start();

        Debug.LogWarning("TODO: Server-client implementation");
    }

    protected void Update()
    {
        if (!GUIManager.singleton.coursorActive)
        {
            if (m_isReloading)
            {
                if (Time.time > m_reloadStartTime + m_reloadingTime)
                {
                    m_magazinLoadedAmmo = m_magazinCapacity;
                    m_isReloading = false;
                    onEndReloading();
                }
            }
            else
            {
                if (Input.GetKeyDown(m_keySource.getReloadKey()) && !m_reloadingBlocked)
                {
                    m_isReloading = true;
                    m_reloadStartTime = Time.time;
                    onStartReloading();
                }
            }

            if (m_primaryUsageMode == ItemUsageMode.Continuous)
            {
                if (Input.GetKey(m_keySource.getPrimaryActionKey()) && !m_shootingPrimaryBlocked)
                {
                    if (Time.time > m_lastTimeItemUsagePrimary + m_timeBetweenItemUsagePrimary && m_magazinLoadedAmmo > 0 && !m_isReloading)
                    {
                        onItemUsagePrimary();
                        m_lastTimeItemUsagePrimary = Time.time;
                    }
                    else
                    {
                        if (m_magazinLoadedAmmo < 1 || m_isReloading)
                        {
                            m_lastTimeItemUsagePrimaryEnded = Time.time;
                            onItemUsagePrimaryEnded();
                        }
                    }
                }
                else
                {
                    m_lastTimeItemUsagePrimaryEnded = Time.time;
                    onItemUsagePrimaryEnded();
                }
            }
            else
            {
                if (Input.GetKeyDown(m_keySource.getPrimaryActionKey()))
                {
                    onItemUsagePrimary();
                    m_lastTimeItemUsagePrimary = Time.time;
                }
            }

            if (m_secondaryUsageMode == ItemUsageMode.Continuous)
            {
                if (Input.GetKey(m_keySource.getSecondaryActionKey()))
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
            else
            {
                if (Input.GetKeyDown(m_keySource.getSecondaryActionKey()))
                {
                    onItemUsageSecondary();
                    m_lastTimeItemUsageSecondary = Time.time;
                }
            }
        }
    }

    protected override void onItemDeactivated()
    {
        m_isReloading = false;
    }

    protected override void onItemUsagePrimary()
    {
        if (!m_isReloading)
        {
            if (m_magazinLoadedAmmo > 0)
            {
                onGunFire();
            }
            else if (m_lastTimeItemUsagePrimaryEnded > m_lastTimeFiredEmpty)
            {
                m_lastTimeFiredEmpty = Time.time;
                SoundManager.singleton.playGlobalSound(m_fireEmptySoundIndex, Sound.SoundPlaystyle.Once);
            }
        }
    }

    protected void startShot()
    {
        Player_local player = EntityManager.singleton.getLocalPlayer();

        if (Time.time > m_lastTimeItemUsagePrimary + m_timeTillSprayReset)
        {
            m_currentSprayPatternIndex = 0;
        }

        ProjectileManager.singleton.client_createGunShot(Camera.main.transform.position, Camera.main.transform.forward.normalized, m_projectileManagerPrefabIndex);
        player.addExternalCameraRotation(m_sprayPattern[m_currentSprayPatternIndex] / -100);
        m_currentSprayPatternIndex++;
        if (m_currentSprayPatternIndex > m_sprayPattern.Length - 1)
        {
            m_currentSprayPatternIndex = (int)Mathf.Max(0, m_currentSprayPatternIndex - 2); // if at the end of spray pattern: always switch beetween the last 2 entries
        }

        m_magazinLoadedAmmo--;
        if (m_magazinLoadedAmmo < 1)
        {
            onEndMagazinEmpty();
        }
    }

    protected virtual void onGunFire() { }
    protected virtual void onStartReloading() { }
    protected virtual void onEndReloading() { }
    protected virtual void onEndMagazinEmpty() { }
}
