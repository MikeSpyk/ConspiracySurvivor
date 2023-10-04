using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetToPlayerPosition : MonoBehaviour
{
    [SerializeField] private Vector3 m_offset = Vector3.zero;

    private void Update()
    {
        if (PlayerManager.singleton != null)
        {
            transform.position = PlayerManager.singleton.getWorldViewPoint(-1).transform.position + m_offset;
        }
    }
}
