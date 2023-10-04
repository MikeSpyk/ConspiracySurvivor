using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatastrophicEvent_SingleThunder : CatastrophicEvent_Base
{
    public override void fireMajorEvent()
    {
        SoundManager.singleton.server_playGlobalSound(13);
    }
}
