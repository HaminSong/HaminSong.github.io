using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetDamage_Object : GetDamage
{
    public GameObject fragmentsPrefab; //���� ������

    private GameObject fragmaents = null; //���� ������Ʈ

    [HideInInspector]
    public int indexNumber;
    public override void GetDamaged()
    {
        base.GetDamaged();
        if (curHp <= 0)
        {
            ObjectDestroy();
        }
    }
    public override void GetDamaged(float damage)
    {
        base.GetDamaged(damage);
        if (curHp <= 0)
        {
            ObjectDestroy();
        }
    }

    public void ObjectDestroy()
    {
        if (fragmaents != null)
        {
            fragmaents.SetActive(true);
        }

        GameSingletonItems.destroyObj.Enqueue(gameObject);
        gameObject.SetActive(false);
    }
    protected override void Start()
    {
        base.Start();
        if (fragmentsPrefab != null)
        {
            if (GameSingletonItems.isNetGame&& GeneralMgr.isServer)
            {
                return; //������ ���� ���� x
            }
            fragmaents = Instantiate(fragmentsPrefab, transform.position, transform.rotation);
            fragmaents.transform.SetParent(transform.parent);
            fragmaents.SetActive(false);
        }
    }
}
