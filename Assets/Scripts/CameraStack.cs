using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraStack : MonoBehaviour
{
    public static CameraStack m_singleton = null;
    private static List<Camera> m_cameras = new List<Camera>();

    public static Camera closestCamera { get { return m_cameras[0]; } }
    public static Camera distantCamera1 { get { return m_cameras[1]; } }
    public static Vector3 position { get { return m_singleton.transform.position; } set { setPosition(value); } }
    public static Quaternion rotation { get { return m_singleton.transform.rotation; } set { setRotation(value); } }
    public static GameObject gameobject { get { return m_singleton.gameObject; } }
    public static float fieldOfView { get { return closestCamera.fieldOfView; } }

    public static Camera getCamera(int index)
    {
        return m_cameras[index];
    }

    public static void setPosition(Vector3 position)
    {
        m_singleton.transform.position = position;
    }

    public static void setRotation(Quaternion rotation)
    {
        m_singleton.transform.rotation = rotation;
    }

    public static void setMultipleCameraMode(bool multipleCameras)
    {
        m_multipleCameras = multipleCameras;

        if (multipleCameras)
        {
            distantCamera1.gameObject.SetActive(true);
            distantCamera1.farClipPlane = m_viewDistance;
            closestCamera.farClipPlane = m_singleton.m_closeCameraMaxDistance;
            closestCamera.clearFlags = CameraClearFlags.Depth;
        }
        else
        {
            distantCamera1.gameObject.SetActive(false);
            closestCamera.farClipPlane = m_viewDistance;
            closestCamera.clearFlags = CameraClearFlags.Skybox;
        }
    }

    private float m_targetFOV = 60;
    private float m_FOVFadeSpeed = 1;
    private static bool m_multipleCameras = false;
    private static float m_viewDistance = 700;

    [SerializeField] private float m_closeCameraMaxDistance = 53;

    public void fadeFieldOfView(float newFOV, float speed)
    {
        m_targetFOV = newFOV;
        m_FOVFadeSpeed = speed;
    }

    public void fadeDefaultFieldOfView()
    {
        fadeDefaultFieldOfView(10);
    }
    public void fadeDefaultFieldOfView(float speed)
    {
        m_targetFOV = 60;
        m_FOVFadeSpeed = speed;
    }

    public static void setViewDistance(float newViewDistance)
    {
        m_viewDistance = newViewDistance;

        if (m_multipleCameras)
        {
            distantCamera1.farClipPlane = newViewDistance;
        }
        else
        {
            closestCamera.farClipPlane = newViewDistance;
        }
    }

    private void setFieldOfView(float newFOV)
    {
        for (int i = 0; i < m_cameras.Count; i++)
        {
            m_cameras[i].fieldOfView = newFOV;
        }
    }

    private void Awake()
    {
        if (m_singleton != null)
        {
            Debug.LogError("CameraStack: Awake: Multiple \"CameraStack\"-Scripts detected. you may use only one !");
        }

        m_singleton = this;

        m_cameras.Clear();

        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            Camera camera = gameObject.transform.GetChild(i).GetComponent<Camera>();

            if (camera != null)
            {
                m_cameras.Add(camera);
            }
        }
    }

    private void Update()
    {
        if (m_targetFOV != closestCamera.fieldOfView)
        {
            int sign;

            if (m_targetFOV >= closestCamera.fieldOfView)
            {
                sign = 1;
            }
            else
            {
                sign = -1;
            }

            float FOVFadeValue = m_FOVFadeSpeed * Time.deltaTime * sign;

            if (Mathf.Abs(m_targetFOV - closestCamera.fieldOfView) < FOVFadeValue)
            {
                setFieldOfView(m_targetFOV);
            }
            else
            {
                setFieldOfView(closestCamera.fieldOfView + FOVFadeValue);
            }
        }
    }
}
