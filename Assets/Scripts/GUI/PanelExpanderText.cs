using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[ExecuteInEditMode]
public class PanelExpanderText : MonoBehaviour
{
    [SerializeField] private GameObject m_textFieldLinePrefab;
    [SerializeField] private float m_verticalOffset = 0;
    [SerializeField] private float m_minHeight = 100;
    [SerializeField] private int m_maxEntries = 20;
    [SerializeField] private bool m_DEBUG_addEntry = false;

    private List<GameObject> m_textChildren = new List<GameObject>();
    private List<RectTransform> m_textChildrenTransforms = new List<RectTransform>();
    private RectTransform m_rectTransform;

    private int DEBUG_counter = 0;

    private void Awake()
    {
        m_rectTransform = GetComponent<RectTransform>();

        registerCurrentTextChildren();
        updatePanelHeight();
    }

    private void Update()
    {
        if (!Application.isPlaying) // edit mode
        {
            registerCurrentTextChildren();
            updatePanelHeight();
        }

        if (m_DEBUG_addEntry)
        {
            m_DEBUG_addEntry = false;
            addNewText("NEW TEXT " + DEBUG_counter);
            DEBUG_counter++;
        }
    }

    private void registerCurrentTextChildren()
    {
        m_textChildren.Clear();
        m_textChildrenTransforms.Clear();

        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject tempObj = gameObject.transform.GetChild(i).gameObject;

            if (tempObj.GetComponent<Text>() != null)
            {
                m_textChildren.Add(tempObj);
                m_textChildrenTransforms.Add(tempObj.GetComponent<RectTransform>());
            }
        }
    }

    private void updatePanelHeight()
    {
        float newSize = 0;

        for (int i = 0; i < m_textChildrenTransforms.Count; i++)
        {
            if (m_textChildrenTransforms[i] == null)
            {
                m_textChildrenTransforms.RemoveAt(i);
                m_textChildren.RemoveAt(i);
                i--;
            }
            else
            {
                newSize += m_textChildrenTransforms[i].sizeDelta.y;
            }
        }

        if (newSize > m_minHeight)
        {
            if (newSize != m_rectTransform.sizeDelta.y)
            {
                m_rectTransform.sizeDelta = new Vector2(m_rectTransform.sizeDelta.x, newSize);
                m_rectTransform.position = new Vector3(m_rectTransform.position.x, m_verticalOffset + newSize / 2, m_rectTransform.position.z);
            }
        }
        else
        {
            m_rectTransform.sizeDelta = new Vector2(m_rectTransform.sizeDelta.x, m_minHeight);
            m_rectTransform.position = new Vector3(m_rectTransform.position.x, m_verticalOffset + m_minHeight / 2, m_rectTransform.position.z);
        }
    }

    public void addNewText(string text)
    {
        if (m_rectTransform == null)
        {
            return;
        }

        System.DateTime time = System.DateTime.Now;

        GameObject newObj = Instantiate(m_textFieldLinePrefab);
        Text objText = newObj.GetComponent<Text>();
        objText.text = text;
        newObj.transform.SetParent(m_rectTransform, true);

        m_textChildren.Add(newObj);
        m_textChildrenTransforms.Add(newObj.GetComponent<RectTransform>());

        if (m_textChildren.Count > m_maxEntries)
        {
            Destroy(m_textChildren[0]);
            m_textChildren.RemoveAt(0);
            m_textChildrenTransforms.RemoveAt(0);
        }

        updatePanelHeight();
    }

}
