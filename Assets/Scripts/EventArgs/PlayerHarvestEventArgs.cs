using System;

public class PlayerHarvestEventArgs : EventArgs
{
    public Player_base playerBase { set; get; }
    public float harvestAmount { set; get; }
}
