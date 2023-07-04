using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetDamage_CastleGate : GetDamage_Wall
{
    [HideInInspector]
    public bool isCracked;
    protected override void Start()
    {
        base.Start();
        transform.parent.GetComponent<BoxCollider>().enabled = false;
    }

    public void CrackedGate()
    {
        gameObject.GetComponent<MeshRenderer>().material = crackedMaterials[0];
    }

    protected override void CrackedWall()
    {
        if (crackedMaterials.Length < 1) return;
        float hpPercent = curHp / maxHp;

        if (hpPercent < 0.6)
        {
            gameObject.GetComponent<MeshRenderer>().material = crackedMaterials[0];
            isCracked = true;
        }
    }

    protected override void ObjectHpCheck(Collision collision, float impulse)
    {
        if (impulse > curHp)
        {
            if (GameSingletonItems.isNetGame == false)
            {
                transform.parent.GetComponent<BoxCollider>().enabled = true;
            }
            curHp = 0;
            ObjectDestroy();
        }
        else
        {
            if (GameSingletonItems.isNetGame)
            {
                if (GeneralMgr.isServer)
                {
                    collision.gameObject.GetComponent<NetPlayerControl>().isAlive = false;
                    curHp -= (int)impulse;
                    CrackedWall();
                }
                else if (collision.gameObject.GetComponent<GetDamage_Player>().isClientPlayer)
                {
                    FindObjectOfType<CameraFollow>().SetCameraShakeTimer(2);
                }
            }
            else
            {
                curHp -= (int)impulse;
                CrackedWall();
                FindObjectOfType<CameraFollow>().SetCameraShakeTimer(2);
                GameSingletonItems.isPlayerAlive = false;
            }
            
        }
    }
}
