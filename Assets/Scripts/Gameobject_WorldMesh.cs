using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gameobject_WorldMesh
{
    public Gameobject_WorldMesh()
    {
        m_gameObject = null;
        m_World_Mesh = null;
    }

    public Gameobject_WorldMesh(GameObject gameObject, World_Mesh world_mesh)
    {
        m_gameObject = gameObject;
        m_World_Mesh = world_mesh;
    }

    public GameObject m_gameObject;
    public World_Mesh m_World_Mesh;
}
