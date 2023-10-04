using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingSystemPreviewFurniture_SpawnPoint : BuildingSystemPreviewFurniture
{
    public override void server_spawnAssociatedEntity(Player_base builder)
    {
        GameObject spawnPointEntity = EntityManager.singleton.spawnEntity(m_entityIDToSpawn, transform.position, transform.rotation);

        Entity_SpawnpointPlayer script = spawnPointEntity.GetComponent<Entity_SpawnpointPlayer>();

        if(script == null)
        {
            Debug.LogWarning("BuildingSystemPreviewFurniture_SpawnPoint: server_spawnAssociatedEntity: script could not be found !");
        }
        else
        {
            script.server_registerSpawnpointForPlayer(builder.m_gameID);
        }
    }
}
