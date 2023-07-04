using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartLineSound : MonoBehaviour
{

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            GetComponent<AudioSource>().Play();
        }
    }
}
