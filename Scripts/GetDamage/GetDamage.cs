using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetDamage : MonoBehaviour
{
    public float maxHp = 300;
    protected float curHp;

    public virtual void GetDamaged()
    {
        if (curHp > 0)
            curHp = 0;
    }

    public virtual void GetDamaged(float damage)
    {
        if (curHp > 0)
            curHp -= damage;
    }

    protected virtual void Start()
    {
        curHp = maxHp;
    }
}
