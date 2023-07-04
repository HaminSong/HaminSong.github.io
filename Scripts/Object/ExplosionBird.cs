using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionBird : Bird
{
    protected override void OnCollisionEnter(Collision collision)
    {
        if (GameSingletonItems.isNetGame && GeneralMgr.isServer == false)
            GetComponent<Explosion>().ExplosionEffectOn();
        
        else
            GetComponent<Explosion>().Explode();
    }
}

