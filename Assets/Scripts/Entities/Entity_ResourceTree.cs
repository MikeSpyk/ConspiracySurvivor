using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity_ResourceTree : Entity_HarvestableResource
{
    private void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        base.Start();
    }

    private void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override void onAddtoHarvester(Player_base harvester, int amount)
    {

        harvester.tryAddItem(ItemManager.singleton.createNewStorableItem(1, amount));

        Debug.Log("TODO: Add to Harvester");
    }
}
