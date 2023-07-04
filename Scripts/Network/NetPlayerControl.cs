using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetPlayerControl : MonoBehaviour
{
    public AudioSource[] audioSource;

    public float Speed { get; set; }
    public Vector3 PlayerVelocity { get; set; }

    private Vector3 prePosition;

    [HideInInspector]
    public bool isAlive;

    [HideInInspector]
    public bool isGround = false;
    private void Awake()
    {
        audioSource = GetComponents<AudioSource>();
        prePosition = transform.position;
    }
    private void Update()
    {
        Vector3 tempVelocity = transform.position - prePosition;
        Speed = tempVelocity.magnitude / Time.deltaTime;
        PlayerVelocity = tempVelocity.normalized * Speed;
        if (isGround)
        {
            audioSource[0].volume = Speed / 30f;
        }
        else
        {
            audioSource[0].volume = 0;
        }
        if(prePosition != transform.position)
            prePosition = transform.position;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGround = true;
        }
    }
    private void OnCollisionExit(Collision collision) 
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGround = false;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("DeathZone"))
        {
            if (GeneralMgr.isServer)
                isAlive = false;
            if (audioSource[1].isPlaying == false)
            {
                audioSource[1].Play();
            }
        }
    }
    private void OnDisable()
    {
        audioSource[1].Stop();
    }
}
