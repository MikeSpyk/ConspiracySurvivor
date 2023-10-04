using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityHitbox : MonoBehaviour
{
    public enum HitboxBodyPart { Undefined, Head, Body, LeftUpperArm, RightUpperArm, LeftForArm, RightForArm, LeftHand, RightHand, LeftThigh, RightThigh, LeftKnee, RightKnee, LeftAnkle, RightAnkle }

    private Entity_damageable m_parentEntity;
    [SerializeField] private HitboxBodyPart m_bodyPart = HitboxBodyPart.Undefined;
    [Header("Outputs")]
    [SerializeField] private bool DEBUG_ParentSet = false;
    [SerializeField] public int m_gameID = -1;

    public void setParentEntity(Entity_damageable newParent)
    {
        m_parentEntity = newParent;

        if(m_parentEntity == null)
        {
            DEBUG_ParentSet = false;
        }
        else
        {
            DEBUG_ParentSet = true;
        }
    }

    public void addImpact(Vector3 position, Vector3 force, float damage)
    {
        if (m_parentEntity == null)
        {
            Debug.LogWarning("EntityHitbox: Parent not set !");
        }
        else
        {
            m_parentEntity.onChildHitboxHit(position, force, damage, m_bodyPart);
        }
    }
}
