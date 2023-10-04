using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity_indestructible : Entity_damageable
{
    private void Start()
    {
        Entity_damageable_Start();
    }

    protected override void onDamaged(float damage)
    {
        // no damage recognition
    }

}
