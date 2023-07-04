using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_BackGround : MonoBehaviour
{
    private CameraFollow PlayerCamera;
    private Vector3 playterCameraPastPos;
    public float camSpeed = 0.02f;

    private void Start()
    {
        PlayerCamera = FindObjectOfType<CameraFollow>();
        if (PlayerCamera == null) return;
        playterCameraPastPos = PlayerCamera.transform.position;
    }
    // Update is called once per frame
    void Update()
    {
        if (PlayerCamera == null) return;
        transform.rotation = PlayerCamera.transform.rotation;
        transform.position += (PlayerCamera.transform.position - playterCameraPastPos) * camSpeed;
        playterCameraPastPos = PlayerCamera.transform.position;
    }
}
