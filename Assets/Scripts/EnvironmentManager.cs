using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System;

public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager singleton = null;

    [Header("Time")]
    [SerializeField] private float m_timeScale = 1;
    [SerializeField] [Range(0, 23)] private int m_startTimeHour = 12;
    [SerializeField, ReadOnly] private int m_day_output;
    [SerializeField, ReadOnly] private int m_hour_output;
    [SerializeField, ReadOnly] private int m_minute_output;
    [SerializeField, ReadOnly] private int m_secound_output;
    [Header("Day Night Cyle")]
    [SerializeField] private GameObject m_environmentLightGameobject;
    [SerializeField] private float m_sunMoonStartAngle = 60;
    [SerializeField] private float m_sunMoonEndAngle = 270;
    [SerializeField] private float m_maxLightIntensity = 10;
    [Header("Day")]
    [SerializeField] private float m_sunStartHour = 6;
    [SerializeField] private float m_sunEndHour = 20;
    [SerializeField] private Gradient m_dayColorCurve;
    [SerializeField] private Gradient m_dayHorizonColorCurve;
    [SerializeField] private AnimationCurve m_skyMultiplierDay;
    [SerializeField] private AnimationCurve m_lightIntensityDay;
    [Header("Night")]
    [SerializeField] private float m_moonStartHour = 21;
    [SerializeField] private float m_moonEndHour = 5;
    [SerializeField] private Gradient m_nightColorCurve;
    [SerializeField] private Gradient m_nightHorizonColorCurve;
    [SerializeField] private AnimationCurve m_skyMultiplierNight;
    [SerializeField] private AnimationCurve m_lightIntensityNight;
    [Header("Fog And Sky")]
    [SerializeField] private float m_maxSkyMultiplier = 4.32f;
    [SerializeField] private float m_fogDistance = 0;
    [SerializeField] private bool m_forceFogDistanceUpdate = false;
    [SerializeField] private AnimationCurve m_cloudAlpha;
    [SerializeField] private ParticleSystem m_cloudsParticleSystem;
    [Header("Networking")]
    [SerializeField] private float m_networkUpdateTime = 10;
    [Header("References")]
    [SerializeField] private Volume m_sceneSettingsVol;
    [SerializeField] private GameObject m_stars;
    [SerializeField] private GameObject m_moonObject;

    private Light m_environmentLight;
    private float m_passedTimeSinceAdding = 0;
    private DateTime m_currentDateTime;
    private float m_lastTimeNetworkUpdate = 0;
    private Sound m_windSound = null;
    private VolumetricFog m_fog = null;
    private ProceduralSky m_proceduralSky = null;

    public DateTime currentDateTime { get { return m_currentDateTime; } }
    public float timeScale { get { return m_timeScale; } set { m_timeScale = value; } }

    private void Awake()
    {
        singleton = this;
        m_sceneSettingsVol.profile.TryGet<VolumetricFog>(out m_fog);
        m_sceneSettingsVol.profile.TryGet<ProceduralSky>(out m_proceduralSky);

        m_currentDateTime = new DateTime(1, 1, 1, m_startTimeHour, 0, 0);
    }

    private void Start()
    {
        m_environmentLight = m_environmentLightGameobject.GetComponent<Light>();
    }

    private void Update()
    {
        if (!GameManager_Custom.singleton.isGameInitialized)
        {
            return;
        }

        // Time Calculation

        m_passedTimeSinceAdding += Time.deltaTime * m_timeScale;

        float timeGradient = -1; // 0.1 - 1.0

        if (m_passedTimeSinceAdding > 1.0f)
        {
            m_currentDateTime = m_currentDateTime.AddSeconds((int)m_passedTimeSinceAdding);
            m_passedTimeSinceAdding -= (int)m_passedTimeSinceAdding;

            m_day_output = m_currentDateTime.Day;
            m_hour_output = m_currentDateTime.Hour;
            m_minute_output = m_currentDateTime.Minute;
            m_secound_output = m_currentDateTime.Second;

            timeGradient = (float)(new DateTime(1, 1, 1, m_hour_output, m_minute_output, m_secound_output) - new DateTime(1, 1, 1, 0, 0, 0)).TotalSeconds / 86400;
        }

        // Sun Moon

        int currentTimeSecounds = (m_currentDateTime.Hour * 60 + m_currentDateTime.Minute) * 60 + m_currentDateTime.Second;

        float sunTimeStartSecounds = m_sunStartHour * 60 * 60;
        float sunTimeEndSecounds = m_sunEndHour * 60 * 60;

        float moonTimeStartSecounds = m_moonStartHour * 60 * 60;
        float moonTimeEndSecounds = m_moonEndHour * 60 * 60;

        bool isAM = m_currentDateTime.Hour < 12;

        if (currentTimeSecounds > sunTimeStartSecounds && currentTimeSecounds < sunTimeEndSecounds)
        {
            // day

            float cyleLength = sunTimeEndSecounds - sunTimeStartSecounds;
            float cyleState = (currentTimeSecounds - sunTimeStartSecounds) / cyleLength; // 0.0 - 1.0
            float currentAngle = Mathf.Lerp(m_sunMoonStartAngle, m_sunMoonEndAngle, cyleState);

            m_environmentLightGameobject.transform.rotation = Quaternion.Euler(currentAngle, 0, 0);

            m_environmentLight.color = m_dayColorCurve.Evaluate(cyleState);
            m_environmentLight.intensity = m_lightIntensityDay.Evaluate(cyleState) * m_maxLightIntensity;

            if (m_proceduralSky != null)
            {
                m_proceduralSky.groundColor.value = m_dayHorizonColorCurve.Evaluate(cyleState);

                if (timeGradient > -1)
                {
                    m_proceduralSky.multiplier.value = m_skyMultiplierDay.Evaluate(cyleState) * m_maxSkyMultiplier;
                }
            }

            if (m_fog != null)
            {
                m_fog.color.value = m_dayHorizonColorCurve.Evaluate(cyleState);
            }

            if (m_stars.activeSelf)
            {
                m_stars.SetActive(false);
            }

            if (m_cloudsParticleSystem != null)
            {
                float alphaValue = m_cloudAlpha.Evaluate(cyleState);
                ParticleSystem.ColorOverLifetimeModule colorLifetime = m_cloudsParticleSystem.colorOverLifetime;

                Gradient cloudGradient = new Gradient();
                cloudGradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 0) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(alphaValue, 0.2f), new GradientAlphaKey(alphaValue, 0.8f), new GradientAlphaKey(0, 1) }
                    );

                colorLifetime.color = cloudGradient;
            }
        }
        else if ((isAM && currentTimeSecounds < moonTimeEndSecounds) || (!isAM && currentTimeSecounds > moonTimeStartSecounds))
        {
            // night

            DateTime moonStartTime;
            DateTime moonEndTime;

            if (isAM)
            {
                moonStartTime = (new DateTime(m_currentDateTime.Year, m_currentDateTime.Month, m_currentDateTime.Day - 1, 0, 0, 0)).AddSeconds(moonTimeStartSecounds);
                moonEndTime = (new DateTime(m_currentDateTime.Year, m_currentDateTime.Month, m_currentDateTime.Day, 0, 0, 0)).AddSeconds(moonTimeEndSecounds);
            }
            else
            {
                moonStartTime = (new DateTime(m_currentDateTime.Year, m_currentDateTime.Month, m_currentDateTime.Day, 0, 0, 0)).AddSeconds(moonTimeStartSecounds);
                moonEndTime = (new DateTime(m_currentDateTime.Year, m_currentDateTime.Month, m_currentDateTime.Day + 1, 0, 0, 0)).AddSeconds(moonTimeEndSecounds);
            }

            float cyleLength = (float)(moonEndTime - moonStartTime).TotalSeconds;
            float cyleState = Mathf.Abs((float)(moonStartTime - m_currentDateTime).TotalSeconds / cyleLength); // 0.0 - 1.0
            float currentAngle = Mathf.Lerp(m_sunMoonStartAngle, m_sunMoonEndAngle, cyleState);

            m_environmentLightGameobject.transform.rotation = Quaternion.Euler(currentAngle, 0, 0);
            m_moonObject.transform.rotation = Quaternion.Euler(currentAngle - 90, 0, 0);

            m_environmentLight.color = m_nightColorCurve.Evaluate(cyleState);
            m_environmentLight.intensity = m_lightIntensityNight.Evaluate(cyleState) * m_maxLightIntensity;

            if (m_proceduralSky != null)
            {
                m_proceduralSky.groundColor.value = m_nightHorizonColorCurve.Evaluate(cyleState);

                if (timeGradient > -1)
                {
                    m_proceduralSky.multiplier.value = m_skyMultiplierNight.Evaluate(cyleState) * m_maxSkyMultiplier;
                }
            }

            if (m_fog != null)
            {
                m_fog.color.value = m_nightHorizonColorCurve.Evaluate(cyleState);
            }

            if (!m_stars.activeSelf)
            {
                m_stars.SetActive(true);
            }
        }

        if (timeGradient > -1)
        {
            m_stars.transform.rotation = Quaternion.Euler(Mathf.Lerp(360, 0, timeGradient), 0, 0);
        }

        // Fog Calculation

        if (m_forceFogDistanceUpdate)
        {
            setFogDistance(m_fogDistance);
        }

        Vector3 currentPlayerPos = EntityManager.singleton.getLocalPlayerPosition();

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            if (Time.time > m_lastTimeNetworkUpdate + m_networkUpdateTime)
            {
                server_sendTime();
            }
        }

        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            if (m_windSound == null)
            {
                m_windSound = SoundManager.singleton.playGlobalSound(25, Sound.SoundPlaystyle.loop);
            }
        }
    }

    public void setTime(int day, int hour, int minute, int secound)
    {
        m_currentDateTime = new DateTime(m_currentDateTime.Year, m_currentDateTime.Month, day, hour, minute, secound);

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            server_sendTime();
        }
    }
    public void setTime(int hour, int minute, int secound)
    {
        m_currentDateTime = new DateTime(m_currentDateTime.Year, m_currentDateTime.Month, m_currentDateTime.Day, hour, minute, secound);

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            server_sendTime();
        }
    }

    public void setFogDistance(float newDistance)
    {
        m_fogDistance = newDistance;

        //m_fogSSMSFogScript.fogEnd = newDistance / m_endFogScaleDivider;
        //m_fogSSMSFogScript.fogStart = m_fogSSMSFogScript.fogEnd / m_FogRangeDivider;

        //m_fog.fogEnd = new MinFloatParameter( newDistance,0,true);
        //m_fog.fogStart = new MinFloatParameter((newDistance / 4) * 3, 0,true);
    }

    private void server_sendTime()
    {
        m_lastTimeNetworkUpdate = Time.time;
        NetworkingManager.singleton.server_sendEnvironmentTime(m_currentDateTime.Day, m_currentDateTime.Hour, m_currentDateTime.Minute, m_currentDateTime.Second);
    }
}
