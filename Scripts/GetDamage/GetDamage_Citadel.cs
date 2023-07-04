using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetDamage_Citadel : GetDamage_Wall
{
    protected override float CalculateImpulse(Collision collision)
    {
        Vector3 contactVector = transform.position - collision.transform.position; // ���� ������Ʈ�̱� ������ ������ ���� ���͸� ���Ѵ�.
        contactVector.y = 0;
        contactVector.Normalize();
        if(collision.rigidbody.constraints == RigidbodyConstraints.FreezeAll)
        {
            return Vector3.Dot(collision.gameObject.GetComponent<NetPlayerControl>().PlayerVelocity, contactVector); // �÷��̾��� �ӵ��� �������� ���͸� ���Ѵ�.
        }
        return Vector3.Dot(collision.relativeVelocity, contactVector); // �÷��̾��� �ӵ��� �������� ���͸� ���Ѵ�.
    }
}
