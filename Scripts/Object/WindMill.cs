using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindMill : MonoBehaviour
{
    private float distance; //오브젝트로부터 거리
    private float maxDistance; //최대 바람 거리


    public enum WindModes
    {
        windForce,
        windImpulse,
        windBoost
    }
    public WindModes windMode = WindModes.windForce;

    [Range(0, 100)]
    public float ob_WindPower = 0; //바람 세기

    private void Start()
    {
        maxDistance = GetComponent<BoxCollider>().size.z / 2 + transform.localPosition.z;
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<Rigidbody>())
        {
            if (other.GetComponent<Rigidbody>().constraints == RigidbodyConstraints.FreezeAll) 
                return;
            distance = Vector3.Dot(other.transform.position - transform.parent.position, transform.forward);
            float proportionalDistance = (maxDistance - distance) / maxDistance;
            switch (windMode)
            {
                case WindModes.windForce:
                    other.GetComponent<Rigidbody>().AddForce(transform.forward * ob_WindPower * proportionalDistance, ForceMode.Force);
                    break;
                case WindModes.windImpulse:
                    other.GetComponent<Rigidbody>().AddForce(transform.forward * ob_WindPower * proportionalDistance, ForceMode.Impulse);
                    break;
                case WindModes.windBoost:
                    other.GetComponent<Rigidbody>().velocity = transform.forward * ob_WindPower;
                    break;
                default:
                    break;
            }
        }
    }
}
