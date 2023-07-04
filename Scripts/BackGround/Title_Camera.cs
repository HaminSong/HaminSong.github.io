using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;

public class Title_Camera : MonoBehaviour
{
    public Transform TitleRock;
    public Title_Manager title_Mgr;
    public float CameraSensitivity { get; set; } = 2f; //카메라 민감도

    private bool isStorySelect = false;
    private float p_Radius;
    private float MouseX;
    private float MouseY;
    private float camAngleMax = 50;
    private float camAngleMin = -50;
    private Vector3 offset;
    private Vector3 cameraEulerAngles = Vector3.zero;

    private IEnumerator tempIe;
    private void CameraControl()
    {
        MouseX = Input.GetAxis("Mouse X");
        MouseY = Input.GetAxis("Mouse Y");
        cameraEulerAngles.y = transform.eulerAngles.y + MouseX;
        cameraEulerAngles.x = Mathf.Clamp(cameraEulerAngles.x - MouseY, camAngleMin, camAngleMax);

        transform.eulerAngles = cameraEulerAngles;

    }

    private void SetOffset() //플레이어 크기에 비례하게 카메라 거리 조절
    {
        p_Radius = TitleRock.GetComponent<SphereCollider>().radius;

        Vector3 tempVector = transform.up - Vector3.forward *5;

        offset = p_Radius * tempVector.normalized * 4;
    }

    private IEnumerator ExitStory()
    {
        yield return StartCoroutine(GameSingletonItems.FadeIn(title_Mgr.ui_Fade, 1));
        StopCoroutine(tempIe);
        title_Mgr.StoryEnd();
        yield return StartCoroutine(GameSingletonItems.FadeOut(title_Mgr.ui_Fade, 1));
    }

    // Start is called before the first frame update
    void Start()
    {
        SetOffset();
    }

    // Update is called once per frame
    void Update()
    {
        if (title_Mgr.isStoryStart) 
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (Input.GetKeyDown(KeyCode.Escape))
                StartCoroutine(ExitStory());
            isStorySelect = false;
            return;
        }
        if (Input.GetMouseButton(1))
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            CameraControl();

        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (Input.GetMouseButtonDown(0) && isStorySelect == false)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.name.Contains("TitleGold"))
                {
                    isStorySelect = true;
                    tempIe = title_Mgr.Story_Main_Coroutine();
                    StartCoroutine(tempIe);
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (title_Mgr.isStoryStart)
        {
            transform.position = TitleRock.position + offset;
            transform.LookAt(TitleRock.position + Vector3.up * p_Radius);
        }
    }
}
