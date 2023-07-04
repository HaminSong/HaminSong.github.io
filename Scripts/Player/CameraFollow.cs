using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform P_Transform { get; set; }
    public float CameraSensitivity { get; set; } = 2f; //카메라 민감도

    private int layerMask;
    private float shakeTimer = 0;
    private float camAngleMax = 80;
    private float camAngleMin = -30;
    private float camDistance = 4;//6.5f;//카메라의 초기 위치 설정 시 플레이어로부터 얼만큼 떨어져 있는지
    private float p_Radius;
    private float MouseX;
    private float MouseY;

    private Vector3 camForward;//카메라의 정면
    private Vector3 offset;
    private Vector3 pastPosition;

    private Transform Listener;
    public void SetOffset() //플레이어 크기에 비례하게 카메라 거리 조절
    {
        shakeTimer = 0;
        if (camForward == Vector3.zero)
            camForward = transform.forward;

        p_Radius = P_Transform.GetComponent<SphereCollider>().radius;

        offset = p_Radius * -camForward * camDistance;

        transform.position = P_Transform.position + offset;
    }

    private void CameraControl(Vector3 forward, ref Vector3 offset)
    {
        if (P_Transform.gameObject.activeSelf == false) return;
        MouseX = Input.GetAxis("Mouse X");
        MouseY = Input.GetAxis("Mouse Y");

        forward.y = 0;

        Vector3 offsetDelta = Quaternion.AngleAxis(CameraSensitivity * MouseY * Time.timeScale, Quaternion.AngleAxis(-90, Vector3.up) * forward) * offset;//카메라를 위 아래로 움직였을 때 플레이어 오브젝트로부터 카메라가 떨어진 위치
        float angle = Mathf.Atan2(offsetDelta.y, new Vector2(offsetDelta.x, offsetDelta.z).magnitude);

        if (angle < camAngleMax && angle > camAngleMin) //카메라의 위, 아래의 각도가 일정량이상 벗어나지 않으면 값 적용
        {
            offset = offsetDelta;
        }
        offset = Quaternion.AngleAxis(CameraSensitivity * MouseX * Time.timeScale, Vector3.up) * offset;
    }

    private Vector3 CameraViewCheck(Vector3 origin, Vector3 direction)
    {
        RaycastHit groundHit;
        float offsetDistance = direction.magnitude;
        if (Physics.Raycast(origin, direction, out groundHit, offsetDistance + p_Radius, layerMask))
        {
            float hitDistance = Vector3.Distance(origin, groundHit.point);
            offsetDistance = hitDistance * 0.8f;
            return origin + direction.normalized * offsetDistance;
        }
        else
        {
            return origin + direction;
        }
    }

    private void CameraShake(float shakeDegree)
    {
        Vector2 ranVec = Random.insideUnitCircle * shakeDegree;
        transform.position += transform.up * ranVec.x + transform.right * ranVec.y;
    }

    public void SetCameraShakeTimer(float shakeTime)
    {
        if(shakeTime > shakeTimer)
            shakeTimer = shakeTime;
        else if(shakeTime  == 0)
            shakeTimer = 0;
    }


    private void Awake()
    {
        Listener = transform.GetChild(0);
        Listener.SetParent(transform.parent);
        //Listener.transform.position = Vector3.one * 10000;
    }
    private void Start()
    {
        camForward = transform.forward;
        layerMask = 1 << LayerMask.NameToLayer("Ground") |1 << LayerMask.NameToLayer("WallObject");
        camAngleMax *= Mathf.PI / 180;
        camAngleMin *= Mathf.PI / 180;
    }

    private void LateUpdate()
    {
        if (P_Transform == null) return;
        if (GameSingletonItems.isPlayerAlive)
        {
            Vector3 p_Forward = P_Transform.position - transform.position;

            if (GameSingletonItems.isSettingOpen == false)
                CameraControl(p_Forward, ref offset);

            transform.position = CameraViewCheck(P_Transform.position, offset);
            pastPosition = transform.position;
            if (GameSingletonItems.g_State == GameState.Play)
                Listener.position = transform.position;
        }
        else
        {
            transform.position = pastPosition;
        }


        if (P_Transform.gameObject.activeSelf)
        {
            Vector3 LookPosition = P_Transform.position;
            LookPosition.y += p_Radius;
            transform.LookAt(LookPosition);
        }
            

        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            CameraShake(0.5f);
        }
    }
}
