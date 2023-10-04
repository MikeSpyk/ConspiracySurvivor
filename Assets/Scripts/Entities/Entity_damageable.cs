using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class Entity_damageable : Entity_base
{
    [Header("Entity_damageable")]
    [SerializeField] private float m_NPCAimOffsetY = 0; // for example: if the origin of an objects is at the ground a NPC would try to shoot the ground rather than the object above. with this value the aim height can be raised
    [SerializeField] protected float m_health = 100;
    [SerializeField] protected bool m_noDamage = false;
    [SerializeField] protected bool m_noHeal = false;
    [SerializeField] private bool m_projectileImpuls = false; // projectile hit will induce a force
    [SerializeField] private string m_groupName = "UNDEFINED";
    [SerializeField] public bool m_projectileManagerIgnore = false; // projectiles will not hit this object
    [SerializeField] private int m_hitSoundIndex = 0;
    [SerializeField] private int m_cachedImpactEffektIndex = 0;
    [SerializeField] private bool m_hitCallback = false; // show a hitmarker if this is entity is hit by a player
    protected Rigidbody m_rigidbody;

    protected void Entity_damageable_Start()
    {
        base.Start();
        m_rigidbody = GetComponent<Rigidbody>();
        initializeChildrenHitboxes(gameObject.transform);
    }

    public float getAimOffsetY()
    {
        return m_NPCAimOffsetY;
    }

    /// <summary>
    /// position of this transform + aim offset
    /// </summary>
    /// <returns></returns>
    public Vector3 getTargetPosition()
    {
        return transform.position + Vector3.up * m_NPCAimOffsetY;
    }

    public string getGroupName()
    {
        return m_groupName;
    }

    public void setGroupName(string newName)
    {
        m_groupName = newName;
    }

    public Transform getTransform()
    {
        return transform;
    }

    public void addImpact(Vector3 position, Vector3 force, float damage)
    {
        onDamaged(damage);
        addForce(position, force);
        NetworkingManager.singleton.server_sendParticleEffectToAllInRange(m_cachedImpactEffektIndex, position, Quaternion.LookRotation(-force.normalized));
    }

    public void addHeal(float heal)
    {
        onHeal(heal);
    }

    public bool hitCallback
    {
        get
        {
            return m_hitCallback;
        }
    }

    protected virtual void initializeChildrenHitboxes(Transform parent)
    {
        EntityHitbox hitbox = parent.GetComponent<EntityHitbox>();

        if (hitbox != null)
        {
            hitbox.setParentEntity(this);
        }

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            initializeChildrenHitboxes(parent.transform.GetChild(i));
        }
    }

    protected virtual void onDamaged(float damage)
    {
        if (!m_noDamage)
        {
            m_health -= damage;
        }
    }

    protected virtual void onHeal(float heal)
    {
        if (!m_noHeal)
        {
            m_health += heal;
        }
    }

    protected void addForce(Vector3 position, Vector3 force)
    {
        if (m_projectileImpuls)
        {
            if (m_rigidbody != null)
            {
                m_rigidbody.AddForceAtPosition(force / m_rigidbody.mass, position, ForceMode.Impulse);
            }
        }
        if (m_hitSoundIndex > -1)
        {
            if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
            {
                NetworkingManager.singleton.server_sendWorldSoundToAllInRange(m_hitSoundIndex, position);
            }
        }
    }

    virtual public void onChildHitboxHit(Vector3 position, Vector3 force, float damage, EntityHitbox.HitboxBodyPart bodyPart)
    {
        addImpact(position, force, damage);
    }

    public override bool client_receivedCustomNetworkMessage(NetworkMessage message)
    {
        return base.client_receivedCustomNetworkMessage(message);
    }

    public override bool server_receivedCustomNetworkMessage(NetworkMessage message)
    {
        return base.server_receivedCustomNetworkMessage(message);
    }

    protected override void updateDataEntity()
    {
        base.updateDataEntity();

        DataEntity_Damageable dataEntity = m_dataEntity as DataEntity_Damageable;

        dataEntity.m_health = m_health;
    }

    protected override void updateEntity()
    {
        base.updateEntity();

        DataEntity_Damageable dataEntity = m_dataEntity as DataEntity_Damageable;

        m_health = dataEntity.m_health;
    }

    public override DataEntity_Base getNewDefaultDataEntity()
    {
        return new DataEntity_Damageable();
    }

}
