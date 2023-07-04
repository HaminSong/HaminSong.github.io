using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetDamage_ThornyTrap : GetDamage_Object
{
    public float trapDamage = 0;

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.rigidbody) return;

        if (collision.rigidbody.constraints == RigidbodyConstraints.FreezeAll) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<GetDamage_Player>().GetDamaged(trapDamage); //플레이어와 닿으면 플레이어에게 데미지를 줌
            ObjectDestroy();
        }
    }
}
