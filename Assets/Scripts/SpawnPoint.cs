using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private int m_SpawnGroupID = -1;

    public void setSpawnGroupID(int newID)
    {
        m_SpawnGroupID = newID;
    }

}
