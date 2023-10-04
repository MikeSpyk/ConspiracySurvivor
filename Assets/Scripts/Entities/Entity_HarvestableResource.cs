using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity_HarvestableResource : Entity_base
{
    [Header("Entity_HarvestableResource")]
    [SerializeField] private float m_resouceAmount = 1000;
    [SerializeField] private FieldResources.ResourceType m_resourceType = FieldResources.ResourceType.Undefined;
    [SerializeField, ReadOnly] private int DEBUG_m_childColliderCount = 0;

    public int m_rasterFieldID = -1;

    protected void Start()
    {
        base.Start();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform childTransform = transform.GetChild(i);
            RayCastIdentifierWorld childIdentifier = childTransform.GetComponent<RayCastIdentifierWorld>();
            if (childIdentifier != null)
            {
                childIdentifier.m_parentScript = this;
                childIdentifier.playerHarvestEvent += onChildColliderHarvested;
                DEBUG_m_childColliderCount++;
            }
        }
    }

    #region Data Entity

    public override DataEntity_Base getNewDefaultDataEntity()
    {
        return new DataEntity_HarvestableResource();
    }

    protected override void updateDataEntity()
    {
        base.updateDataEntity();
        DataEntity_HarvestableResource dataEntity = m_dataEntity as DataEntity_HarvestableResource;

        dataEntity.m_resourceAmount = m_resouceAmount;
        dataEntity.m_resourceType = m_resourceType;
        dataEntity.m_rasterFieldID = m_rasterFieldID;
    }

    protected override void updateEntity()
    {
        base.updateEntity();

        DataEntity_HarvestableResource dataEntity = m_dataEntity as DataEntity_HarvestableResource;

        m_resouceAmount = dataEntity.m_resourceAmount;
        m_resourceType = dataEntity.m_resourceType;
        m_rasterFieldID = dataEntity.m_rasterFieldID;
    }

    #endregion

    public void onChildColliderHarvested(object obj, PlayerHarvestEventArgs args)
    {
        harvest(args.playerBase, args.harvestAmount);
    }

    public void harvest(Player_base harvester, float amount)
    {
        float delta;

        if (m_resouceAmount - amount > 0)
        {
            delta = amount;
            m_resouceAmount -= amount;

            onAddtoHarvester(harvester, (int)delta);
        }
        else
        {
            delta = m_resouceAmount - amount;
            delta = amount - Mathf.Abs(delta);
            onDepleted();

            if (delta > 0)
            {
                onAddtoHarvester(harvester, (int)delta);
            }
        }

        onAmountChanged();
    }

    protected virtual void onDepleted() { }

    protected virtual void onAmountChanged() { }

    protected virtual void onAddtoHarvester(Player_base harvester, int amount) { }

}
