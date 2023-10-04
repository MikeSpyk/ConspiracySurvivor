using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

public class ClientSettingsManager : MonoBehaviour
{
    public static ClientSettingsManager singleton;

    [Header("Key Assignment")]
    [SerializeField] private KeyCode m_primaryActionKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode m_secondaryActionKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode m_reloadKey = KeyCode.R;
    [SerializeField] private KeyCode m_inventoryKey = KeyCode.Tab;
    [SerializeField] private KeyCode m_useKey = KeyCode.E;
    [SerializeField] private KeyCode m_hotbarItem1Key = KeyCode.Alpha1;
    [SerializeField] private KeyCode m_hotbarItem2Key = KeyCode.Alpha2;
    [SerializeField] private KeyCode m_hotbarItem3Key = KeyCode.Alpha3;
    [SerializeField] private KeyCode m_hotbarItem4Key = KeyCode.Alpha4;
    [SerializeField] private KeyCode m_hotbarItem5Key = KeyCode.Alpha5;
    [SerializeField] private KeyCode m_hotbarItem6Key = KeyCode.Alpha6;
    [Header("Quality Settings")]
    [SerializeField] private ShadowQuality m_shadowQuality = ShadowQuality.All;
    [SerializeField] private ShadowResolution m_shadowResolution = ShadowResolution.VeryHigh;
    [SerializeField] private ShadowProjection m_shadowProjection = ShadowProjection.CloseFit;
    [SerializeField] private float m_shadowDistance = 1000f;
    [SerializeField] private int m_shadowCascades = 1;
    [SerializeField] private int m_masterTextureLimit = 0;
    [Header("Others")]
    [SerializeField] private SystemLanguage m_language = SystemLanguage.English;
    [SerializeField] private float m_viewDistance = 700;
    [SerializeField] private bool m_multipleCameraMode = false;
    [Header("References")]
    [SerializeField] private Volume m_sceneSettingsVol;
    [Header("Debug")]
    [SerializeField] private bool m_DEBUG_ApplyEverything = false;


    public KeyCode getPrimaryActionKey()
    {
        return m_primaryActionKey;
    }

    public KeyCode getSecondaryActionKey()
    {
        return m_secondaryActionKey;
    }

    public KeyCode getReloadKey()
    {
        return m_reloadKey;
    }

    public KeyCode getInventoryKey()
    {
        return m_inventoryKey;
    }

    public KeyCode getUseKey()
    {
        return m_useKey;
    }

    public KeyCode getHotbarItem1Key()
    {
        return m_hotbarItem1Key;
    }

    public KeyCode getHotbarItem2Key()
    {
        return m_hotbarItem2Key;
    }

    public KeyCode getHotbarItem3Key()
    {
        return m_hotbarItem3Key;
    }

    public KeyCode getHotbarItem4Key()
    {
        return m_hotbarItem4Key;
    }

    public KeyCode getHotbarItem5Key()
    {
        return m_hotbarItem5Key;
    }

    public KeyCode getHotbarItem6Key()
    {
        return m_hotbarItem6Key;
    }

    public SystemLanguage GetLanguage()
    {
        return m_language;
    }

    public float viewDistance
    {
        get
        {
            return m_viewDistance;
        }
        set
        {
            setViewDistance(value);
        }
    }

    public bool multipleCameraMode
    {
        set
        {
            m_multipleCameraMode = value;
            CameraStack.setMultipleCameraMode(value);
        }
    }

    public void setViewDistance(float newDistance)
    {
        if (newDistance < 0)
        {
            Debug.LogWarning("ClientSettingsManager: viewDistance < 0. setting to 0 !");
            newDistance = 0;
        }
        m_viewDistance = newDistance;

        if (WorldManager.singleton != null)
        {
            WorldManager.singleton.setMaxRenderDistance(newDistance);
        }
        CameraStack.setViewDistance(newDistance);
        if (EnvironmentManager.singleton != null)
        {
            EnvironmentManager.singleton.setFogDistance(newDistance);
        }
        EntityManager.singleton.m_clientPlayerViewDistance = newDistance;

        if (GameManager_Custom.singleton.isGameInitialized)
        {
            NetworkingManager.singleton.client_sendViewDistance(newDistance);
        }

        if(m_viewDistance < m_shadowDistance)
        {
            QualitySettings.shadowDistance = m_viewDistance;
        }
        else
        {
            QualitySettings.shadowDistance = m_shadowDistance;
        }
    }

    private void Awake()
    {
        singleton = this;

        //TODO: load settings from file
    }

    private void Start()
    {
        // apply settings

        setEverything();

        // apply on GUI

        GUIManager.singleton.updateOptionsFromClientSettings();
    }

    private void Update()
    {
        if (m_DEBUG_ApplyEverything)
        {
            setEverything();

            m_DEBUG_ApplyEverything = false;
        }
    }

    private void setEverything()
    {
        setViewDistance(m_viewDistance);
        QualitySettings.shadows = m_shadowQuality;
        QualitySettings.shadowResolution = m_shadowResolution;
        QualitySettings.shadowProjection = m_shadowProjection;
        QualitySettings.shadowDistance = m_shadowDistance;
        QualitySettings.shadowCascades = m_shadowCascades;
        QualitySettings.masterTextureLimit = m_masterTextureLimit;

        CameraStack.setMultipleCameraMode(m_multipleCameraMode);
    }
}
