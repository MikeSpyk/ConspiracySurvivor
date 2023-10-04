using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity_RandomLootBarrel : Entity_damageable
{
    public int m_rasterFieldID = -1;

    protected override void onDamaged(float damage)
    {
        base.onDamaged(damage);

        if (m_health < 0)
        {
            StorableItem item = getRandomDrop();

            EntityManager.singleton.server_spawnDroppedItemWorld(item.WorldPrefabIndex, item, transform.position + Vector3.up * 0.5f);
            WorldManager.singleton.unregisterResource(FieldResources.ResourceType.RandomLootBarrel, m_rasterFieldID, new Vector2(transform.position.x, transform.position.z));

            Destroy(gameObject);
        }
    }

    private StorableItem getRandomDrop()
    {
        StorableItem item = ItemManager.singleton.getRandomItemLootContainer(0);

        return item;
    }

    public override DataEntity_Base getNewDefaultDataEntity()
    {
        return new DataEntity_RandomLootBarrel();
    }

    protected override void updateDataEntity()
    {
        base.updateDataEntity();

        DataEntity_RandomLootBarrel dataEntity = m_dataEntity as DataEntity_RandomLootBarrel;

        dataEntity.m_rasterFieldID = m_rasterFieldID;
    }

    protected override void updateEntity()
    {
        base.updateEntity();

        DataEntity_RandomLootBarrel dataEntity = m_dataEntity as DataEntity_RandomLootBarrel;

        m_rasterFieldID = dataEntity.m_rasterFieldID;
    }
}
