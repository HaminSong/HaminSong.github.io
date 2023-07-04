using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PlayerControl : MonoBehaviour
{
    public float acceleration = 100f;
    public float jumpPower = 55f;

    private bool isGround; //땅에 붙어있는지 체크

    private Vector3 p_MoveVec; //플레이어가 이동할 방향

    public Transform cam_Transform { get; set; }
    private Rigidbody p_Rigidbody;
    [HideInInspector]
    public AudioSource[] audioSource;

    public void PlayerReset() //플레이어 초기화
    {
        isGround = false;
        p_MoveVec = Vector3.zero;
        p_Rigidbody.velocity= Vector3.zero;
        p_Rigidbody.angularVelocity= Vector3.zero;
        GetComponent<GetDamage_Player>().HpReset();
    }

    private void P_InputMove(ref Vector3 moveVec) //플레이어 움직임
    {
        Vector3 p_Forward = transform.position - cam_Transform.position;
        p_Forward.y = 0;
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");
        moveVec = p_Forward * vertical + Quaternion.AngleAxis(90, Vector3.up) * p_Forward * horizontal;
        moveVec.Normalize();
    }

    private void P_Jump(float force) //플레이어 점프
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            p_Rigidbody.AddForce(Vector3.up * force, ForceMode.Impulse);
        }
    }

    private void Awake()
    {
        p_Rigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponents<AudioSource>();
    }
    void Start()
    {
            audioSource[0].volume = 0;
    }

    private void FixedUpdate()
    {
        if(GameSingletonItems.isSettingOpen == false)
            p_Rigidbody.AddForceAtPosition(p_MoveVec * acceleration, transform.position + Vector3.up * 0.1f);
    }

    void Update()
    {
        if (GameSingletonItems.isPlayerAlive && GameSingletonItems.isSettingOpen == false)
        {
            if (isGround)
            {
                P_InputMove(ref p_MoveVec);
                P_Jump(jumpPower);

                audioSource[0].volume = p_Rigidbody.velocity.magnitude / 30f;
            }
            else
            {
                audioSource[0].volume = 0;
            }
            
        }
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
            p_MoveVec = Vector3.zero;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("DeathZone") && GameSingletonItems.isPlayerAlive)
        {
            GameSingletonItems.isPlayerAlive = false;
            audioSource[1].Play();
        }
    }
}
