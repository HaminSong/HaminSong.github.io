using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NameText : MonoBehaviour
{
    private GameObject camPos;
    private Transform[] childTransforms;
    private GameObject[] playerTransforms;
    // Start is called before the first frame update
    void Start()
    {
        camPos = FindObjectOfType<CameraFollow>().gameObject;
        playerTransforms = FindObjectOfType<NetGameMgr>().PlayerGroup;
        childTransforms = new Transform[transform.childCount];
        for(int i = 0; i < transform.childCount; i++)
        {
            childTransforms[i] = transform.GetChild(i);
        }
    }

    private void LateUpdate()
    {
        for (int i = 0; i < childTransforms.Length; i++)
        {
            if (playerTransforms[i].activeSelf == false)
            {
                continue;
            }
            childTransforms[i].transform.LookAt(camPos.transform.position);
            childTransforms[i].transform.position = playerTransforms[i].transform.position + Vector3.up * 3;
        }
    }
}
