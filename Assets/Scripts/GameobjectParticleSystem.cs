using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameobjectParticleSystem
{
    public GameobjectParticleSystem() { }

    public GameobjectParticleSystem(GameObject gameObject, ParticleSystem particleSystem)
    {
        m_gameObject = gameObject;
        m_particleSystem = particleSystem;
    }

    public GameObject m_gameObject;
    public ParticleSystem m_particleSystem;
    public int m_prefabIndex;
}
