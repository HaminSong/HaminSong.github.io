using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetDamage_Wall : GetDamage_Object
{
    public Material[] crackedMaterials;//금이 간 벽 메테리얼

    protected virtual void CrackedWall()
    {
        if (crackedMaterials.Length < 2 || GameSingletonItems.isNetGame) return;
        float hpPercent = curHp / maxHp;

        if (hpPercent < 0.4)
            gameObject.GetComponent<MeshRenderer>().material = crackedMaterials[1];

        else if (hpPercent < 0.8)
            gameObject.GetComponent<MeshRenderer>().material = crackedMaterials[0];
    }

    public override void GetDamaged(float damage)
    {
        base.GetDamaged(damage);
        if (curHp > 0)
            CrackedWall();
    }

    protected virtual float CalculateImpulse(Collision collision)
    {
        if (collision.rigidbody.constraints == RigidbodyConstraints.FreezeAll)
        {
            return Vector3.Dot(collision.gameObject.GetComponent<NetPlayerControl>().PlayerVelocity, transform.forward);
        }

        return Vector3.Dot(collision.relativeVelocity, transform.forward); // 플레이어의 속도와 벽의 정면 벡터를 내적한다.
    }
    protected virtual void ObjectHpCheck(Collision collision, float impulse)
    {
        if (collision.rigidbody.constraints == RigidbodyConstraints.FreezeAll) return;
        collision.gameObject.GetComponent<GetDamage_Player>().GetDamaged(impulse);
        if (impulse > curHp)
        {
            curHp = 0;
            ObjectDestroy();
        }
        else
        {
            curHp -= (int)impulse;
            CrackedWall();

        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.rigidbody) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            float impulse = CalculateImpulse(collision);
            if (impulse < 0) impulse = -impulse;

            if (gameObject.activeSelf)
            {
                GetComponent<AudioSource>().volume = impulse / 10;
                GetComponent<AudioSource>().Play();
            }

            ObjectHpCheck(collision, impulse);
        }
    }
}
