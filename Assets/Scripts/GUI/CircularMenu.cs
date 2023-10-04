using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class CircularMenu : MonoBehaviour
{
    [SerializeField] private Image m_circularButtonPrefab;
    [SerializeField] private Text m_headlineText;
    //[SerializeField] private Vector2 m_referenceResolution = new Vector2(1920, 1080);
    [SerializeField, Min(1)] private int m_buttonsCount = 2;
    [SerializeField, Range(0, 1)] private float m_minDistance = 0.5f;
    [SerializeField] private Color m_highlightedColor = Color.red;
    [SerializeField] private Color m_baseColor = Color.gray;
    [SerializeField] private Vector2Int m_imagePixels = new Vector2Int(1000, 1000); // how many pixels is the circular menu occupying on screen. could probably get calculated (currently fixed for only one screen resolution)
    [SerializeField] private float m_colorFadeTime = 1;
    [SerializeField] private string[] m_buttonHeadlineTexts;
    [SerializeField] private Image[] m_buttonImages;

    private Image[] m_imageButtons = null;
    private Image m_lastHighlightedButton = null;
    private GameObject[] m_gameobjectButtons = null;
    private Vector2 m_screenMiddle = Vector2.zero;
    private float m_rotationStep = 0;
    private float m_angleOffset = 0;
    private int m_imagePixelsDiagonal = 0; // how many pixels does image have on screen in diagonal

    private List<Image> m_fadingColors_image = new List<Image>();
    private List<float> m_fadingColors_time = new List<float>();

    public event EventHandler<StringEventArgs> buttonClickedHeadlineEvent;

    private void Start()
    {
        Debug.LogWarning("TODO: remove m_imagePixels");

        m_imagePixelsDiagonal = (int)Mathf.Sqrt(Mathf.Pow(m_imagePixels.x, 2) + Mathf.Pow(m_imagePixels.y, 2));

        m_imageButtons = new Image[m_buttonsCount];
        m_gameobjectButtons = new GameObject[m_buttonsCount];

        m_screenMiddle = new Vector2(Screen.width / 2, Screen.height / 2);

        m_rotationStep = 360f / m_buttonsCount;
        float rotation = 0;

        int circleRadius = (int)(m_imagePixels.x / 2.25f); // ?????????
        Vector2Int pictureRadiusVec = new Vector2Int(0, -circleRadius);
        pictureRadiusVec = VectorTools.Rotate(pictureRadiusVec, -m_rotationStep/2);

        for (int i = 0; i < m_imageButtons.Length; i++)
        {
            GameObject temp_circleGameObject = Instantiate(m_circularButtonPrefab.gameObject);
            temp_circleGameObject.transform.SetParent(transform, false);
            temp_circleGameObject.transform.rotation = Quaternion.Euler(new Vector3(0, 0, rotation));

            Image temp_cirleImage = temp_circleGameObject.GetComponent<Image>();
            temp_cirleImage.fillAmount = m_rotationStep / 360f;
            temp_cirleImage.color = m_baseColor;

            m_gameobjectButtons[i] = temp_circleGameObject;
            m_imageButtons[i] = temp_cirleImage;
            rotation += m_rotationStep;
        }

        for (int i = 0; i < m_imageButtons.Length; i++)
        {
            GameObject temp_circlePicture = Instantiate(m_buttonImages[i].gameObject);
            temp_circlePicture.transform.SetParent(transform, false);
            temp_circlePicture.transform.position = new Vector3(m_screenMiddle.x + pictureRadiusVec.x, m_screenMiddle.y + pictureRadiusVec.y, 0);
            //temp_circlePicture.GetComponent<Image>().color = new Color( (float)i/ m_imageButtons.Length, 0, 0,1);
            pictureRadiusVec = VectorTools.Rotate(pictureRadiusVec, m_rotationStep);
        }

        m_angleOffset = getAngleOffset(m_buttonsCount);
    }

    private void Update()
    {
        updateSelectedButton();
        fadeColors();

        if (m_lastHighlightedButton == null)
        {
            m_headlineText.text = "";
        }
        else
        {
            int buttonIndex = getIndexForButton(m_lastHighlightedButton);
            m_headlineText.text = m_buttonHeadlineTexts[buttonIndex];
        }

        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            if (m_lastHighlightedButton != null)
            {
                for (int i = 0; i < m_imageButtons.Length; i++)
                {
                    if (m_lastHighlightedButton == m_imageButtons[i])
                    {
                        onButtonPressed(m_buttonHeadlineTexts[i]);

                        //Debug.Log("Button \"" + m_buttonHeadlineTexts[i] + "\" selected");
                        break;
                    }
                }
            }
        }
    }

    private void updateSelectedButton()
    {
#if UNITY_EDITOR
        m_screenMiddle = new Vector2(Screen.width / 2, Screen.height / 2); // for rescaling screen size dynamically
#endif        
        Vector2 mousePos = Input.mousePosition;

        if (Vector2.Distance(mousePos, m_screenMiddle) > m_imagePixelsDiagonal * m_minDistance)
        {
            float angle = formatAngle(Vector2.SignedAngle(Vector2.up, mousePos - m_screenMiddle) - m_angleOffset);
            int buttonIndex = (int)(angle / m_rotationStep);

            if (m_lastHighlightedButton != null)
            {
                m_fadingColors_image.Add(m_lastHighlightedButton);
                m_fadingColors_time.Add(Time.time);
            }

            for (int i = 0; i < m_fadingColors_image.Count; i++)
            {
                if (m_fadingColors_image[i] == m_imageButtons[buttonIndex])
                {
                    m_fadingColors_image.RemoveAt(i);
                    m_fadingColors_time.RemoveAt(i);
                    break;
                }
            }

            m_imageButtons[buttonIndex].color = m_highlightedColor;
            m_lastHighlightedButton = m_imageButtons[buttonIndex];
        }
        else
        {
            if (m_lastHighlightedButton != null)
            {
                m_fadingColors_image.Add(m_lastHighlightedButton);
                m_fadingColors_time.Add(Time.time);
                m_lastHighlightedButton = null;
            }
        }
    }

    private void fadeColors()
    {
        for (int i = 0; i < m_fadingColors_image.Count; i++)
        {
            if (Time.time >= m_fadingColors_time[i] + m_colorFadeTime)
            {
                m_fadingColors_image[i].color = m_baseColor;
                m_fadingColors_image.RemoveAt(i);
                m_fadingColors_time.RemoveAt(i);
                i--;
            }
            else
            {
                m_fadingColors_image[i].color = Color.Lerp(m_highlightedColor, m_baseColor, (Time.time - m_fadingColors_time[i]) / m_colorFadeTime);
            }
        }
    }

    private int getIndexForButton(Image button)
    {
        for (int i = 0; i < m_imageButtons.Length; i++)
        {
            if (button == m_imageButtons[i])
            {
                return i;
            }
        }

        throw new Exception("The provieded button is not part of the array");
        return -1;
    }

    private void onButtonPressed(string buttonHeadline)
    {
        EventHandler<StringEventArgs> handler = buttonClickedHeadlineEvent;

        if (handler != null)
        {
            StringEventArgs args = new StringEventArgs();
            args.m_string = buttonHeadline;

            buttonClickedHeadlineEvent(this, args);
        }
    }

    private static float formatAngle(float angle)
    {
        angle -= 360 * (int)(angle / 360f);

        if (angle < 0)
        {
            angle += 360;
        }

        return angle;
    }

    private static float getAngleOffset(int size)
    {
        // determined by trial. not the best way (probably)

        switch (size)
        {
            default:
                {
                    Debug.LogError("CircularMenu: getAngleOffset: angle offset not determined for this button count");
                    return 0;
                }
            case 1:
                {
                    return 0;
                }
            case 2:
                {
                    return 0;
                }
            case 3:
                {
                    return 60;
                }
            case 4:
                {
                    return 90;
                }
            case 5:
                {
                    return 108;
                }
            case 6:
                {
                    return 120;
                }
            case 7:
                {
                    return 128;
                }
            case 8:
                {
                    return 135;
                }
            case 9:
                {
                    return 140;
                }
        }
    }
}
