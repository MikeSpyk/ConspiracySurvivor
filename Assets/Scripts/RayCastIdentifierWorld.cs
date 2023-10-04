using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RayCastIdentifierWorld : MonoBehaviour
{
    public event EventHandler<PlayerBaseEventArgs> playerRayCastHitEvent;
    public event EventHandler<PlayerHarvestEventArgs> playerHarvestEvent;

    [ReadOnly] public MonoBehaviour m_parentScript = null;

    public void rayCastHit(Player_base source)
    {
        EventHandler<PlayerBaseEventArgs> handler = playerRayCastHitEvent;

        if (handler != null)
        {
            PlayerBaseEventArgs args = new PlayerBaseEventArgs();
            args.playerBase = source;

            playerRayCastHitEvent(this, args);
        }
    }

    public void playerHarvest(Player_base source, float harvestAmount)
    {
        EventHandler<PlayerHarvestEventArgs> handler = playerHarvestEvent;

        if (handler != null)
        {
            PlayerHarvestEventArgs args = new PlayerHarvestEventArgs();
            args.playerBase = source;
            args.harvestAmount = harvestAmount;

            playerHarvestEvent(this, args);
        }
    }
}
