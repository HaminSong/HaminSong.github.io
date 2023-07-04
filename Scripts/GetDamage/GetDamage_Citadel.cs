using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetDamage_Citadel : GetDamage_Wall
{
    protected override float CalculateImpulse(Collision collision)
    {
        Vector3 contactVector = transform.position - collision.transform.position; // 원형 오브젝트이기 때문에 접촉한 곳의 벡터를 구한다.
        contactVector.y = 0;
        contactVector.Normalize();
        if(collision.rigidbody.constraints == RigidbodyConstraints.FreezeAll)
        {
            return Vector3.Dot(collision.gameObject.GetComponent<NetPlayerControl>().PlayerVelocity, contactVector); // 플레이어의 속도와 접촉점의 벡터를 구한다.
        }
        return Vector3.Dot(collision.relativeVelocity, contactVector); // 플레이어의 속도와 접촉점의 벡터를 구한다.
    }
}
