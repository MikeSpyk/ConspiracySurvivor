using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class HeadlessServerManager : MonoBehaviour
{
    public static HeadlessServerManager singleton = null;

    private bool? m_isHeadlessServer = null;
    private int m_fpsCounter = 0;
    private float m_lastTimeFpsCountStart = 0;
    private List<int> m_fpsValues = new List<int>();

    [SerializeField] private int m_fpsToConsoleAfterSecounds = 60;

    public bool isHeadlessServer
    {
        get
        {
            if (m_isHeadlessServer == null)
            {
                m_isHeadlessServer = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
                return m_isHeadlessServer == true;
            }
            else if (m_isHeadlessServer == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else
        {
            Debug.LogError("HeadlessServerManager: Awake: spawned a singleton script multiple times");
        }

        if (isHeadlessServer)
        {
            Debug.Log("is headless server");
        }
        else
        {
            Debug.Log("is NOT headless server");
        }
    }

    private void Start()
    {
        if (isHeadlessServer)
        {
            GameManager_Custom.singleton.startAsServer(1000, 4096, 2302, false);
            Application.targetFrameRate = 120;
        }
    }

    private void Update()
    {
        if (isHeadlessServer)
        {
            m_fpsCounter++;

            if (Time.realtimeSinceStartup > m_lastTimeFpsCountStart + 1)
            {
                float deltaTime = Time.realtimeSinceStartup - m_lastTimeFpsCountStart;

                float fps = m_fpsCounter / deltaTime;

                m_fpsValues.Add((int)fps);

                m_fpsCounter = 0;
                m_lastTimeFpsCountStart = Time.realtimeSinceStartup;
            }

            if (m_fpsValues.Count > m_fpsToConsoleAfterSecounds)
            {
                int minValue = int.MaxValue;
                int sum = 0;

                for (int i = 0; i < m_fpsValues.Count; i++)
                {
                    minValue = Mathf.Min(minValue, m_fpsValues[i]);
                    sum += m_fpsValues[i];
                }

                sum /= m_fpsValues.Count;

                Debug.Log("HeadlessServerManager: FPS-log: Average: " + sum + " fps, min: " + minValue + " fps");

                m_fpsValues.Clear();
            }
        }
    }

}
