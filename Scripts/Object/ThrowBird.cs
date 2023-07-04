using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;
using static CommonDataClass;

public class ThrowBird : MonoBehaviour
{
    public float delayTime = 1;
    public float detectionRange = 10;
    public float detectionDegree = 70;
    public float birdSpeed = 10;
    public GameObject birdPrefab;

    private int birdCount = 0;
    private Vector3 spawnPosition;
    private GameObject[] BulletArray;

    public Queue<Vector3> direction;
    private void Awake()
    {
        spawnPosition = transform.GetChild(transform.childCount - 1).transform.position;
        BulletArray = new GameObject[GameSingletonItems.bulletAmount];
        for (int i = 0; i < GameSingletonItems.bulletAmount; i++)
        {
            BulletArray[i] = Instantiate(birdPrefab);
            BulletArray[i].SetActive(false);
        }
    }
    private void Start()
    {
        if (GameSingletonItems.isNetGame == false)
            StartCoroutine(TrackingTarget(delayTime));
        if (GeneralMgr.isServer)
            StartCoroutine(NetworkTrackingTarget(delayTime));
    }
    /// <summary>
    /// Ÿ���� ���� �ȿ� ���� �ִ��� üũ�ϴ� �Լ�
    /// </summary>
    /// <param name="targetTf">Ÿ�� Ʈ������</param>
    /// <param name="startPos">������ġ, ���� ��ġ</param>
    /// <returns></returns>
    private bool TargetEnterCheck(Transform targetTf, Vector3 startPos)
    {
       return Vector3.Distance(targetTf.position,startPos) < detectionRange
            && Vector3.Angle((targetTf.position - startPos), transform.forward) < detectionDegree 
            && targetTf.gameObject.activeSelf;
    }

    /// <summary>
    /// Ÿ���� ���� �ȿ� ���� �ִ��� üũ�ϰ� ���� �����̿� �ִ� ���� ã�Ƴ� �ش� ���� ����
    /// </summary>
    /// <param name="targetTfArray">Ÿ�� Ʈ������ ���</param>
    /// <param name="startPos">������ġ, ������ġ</param>
    /// <returns></returns>
    private bool AllTargetEnterCheck(GameObject[] targetTfArray, Vector3 startPos, ref int targetNumber)
    {
        float shortestDistance = detectionRange + 1;
        bool result = false;
        for (int i = 0;i < targetTfArray.Length;i++)
        {
            if (TargetEnterCheck(targetTfArray[i].transform, startPos))
            {
                float distance = Vector3.Distance(targetTfArray[i].transform.position, startPos);
                if (distance < shortestDistance)
                {
                    targetNumber = i; //Ÿ�� �ѹ��� �˷��ش�.
                    shortestDistance = distance;
                }
                result = true;
            }
        }
        return result;
    }
    /// <summary>
    /// ź�� ��ü�� velocity�� ���� �ο��Ͽ� �ش��ϴ� �������� ������
    /// </summary>
    /// <param name="direction">��ǥ ����</param>
    public void BulletThrow(Vector3 direction)
    {
        if (birdCount >= GameSingletonItems.bulletAmount) birdCount = 0;
        BulletArray[birdCount].transform.position = spawnPosition;
        BulletArray[birdCount].transform.rotation = Quaternion.LookRotation(direction);
        BulletArray[birdCount].GetComponent<Rigidbody>().velocity = direction.normalized * birdSpeed;
        BulletArray[birdCount].SetActive(true);
        birdCount++;
    }
    /// <summary>
    /// Ÿ���� �����ϸ� ���� ������ �ڷ�ƾ
    /// </summary>
    /// <param name="throwDelayTime">������ �ֱ�</param>
    /// <returns></returns>
    private IEnumerator TrackingTarget(float throwDelayTime)
    {
        Transform p_Transform = GameObject.FindGameObjectWithTag("Player").transform;
        WaitUntil WU_FindTarget = new WaitUntil(() => TargetEnterCheck(p_Transform, spawnPosition));
        WaitForSeconds WT_ThrowDelay = new WaitForSeconds(throwDelayTime);

        while (true)
        {
            yield return WU_FindTarget;
            
            BulletThrow(p_Transform.position - spawnPosition);
            yield return WT_ThrowDelay;
        }
    }

    /// <summary>
    /// Ÿ���� �����ϸ� ���� ����� Ÿ�ٿ��� ���� ������ �ڷ�ƾ
    /// </summary>
    /// <param name="throwDelayTime">������ �ֱ�</param>
    /// <returns></returns>
    private IEnumerator NetworkTrackingTarget(float throwDelayTime)
    {
        if (GeneralMgr.isServer == false) //Ŭ��� �ı�
            yield break;

        int targetNumber = 0;
        
        GameObject[] p_Objects = GameObject.FindGameObjectsWithTag("Player");
        WaitUntil WU_FindTarget = new WaitUntil(() => AllTargetEnterCheck(p_Objects, spawnPosition, ref targetNumber));
        WaitForSeconds WT_ThrowDelay = new WaitForSeconds(throwDelayTime);

        stBirdThrowMsg stBirdThrowInfo = new stBirdThrowMsg();
        stBirdThrowInfo.MsgID = CommonData.MessageID_ServerToClient.ServerToClientID;
        stBirdThrowInfo.MsgSubID = CommonData.MessageID_ServerToClient.BirdThrow;
        stBirdThrowInfo.PacketSize = (ushort)Marshal.SizeOf(stBirdThrowInfo);
        stBirdThrowInfo.IndexNumber = (ushort)GetComponent<GetDamage_Object>().indexNumber;
        while (true)
        {
            yield return WU_FindTarget;
            stBirdThrowInfo.Direction = p_Objects[targetNumber].transform.position - spawnPosition;
            Server.stBirdThrowList.Enqueue(stBirdThrowInfo);
            BulletThrow(p_Objects[targetNumber].transform.position - spawnPosition);
            yield return WT_ThrowDelay;
        }
    }

    private void OnDrawGizmos() // ���� ���� ǥ��
    {
        Transform spawnTransform = transform.GetChild(transform.childCount - 1);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnTransform.position, detectionRange);
        Gizmos.DrawLine(spawnTransform.position, spawnTransform.position + Quaternion.AngleAxis(detectionDegree, spawnTransform.up) * spawnTransform.forward * detectionRange);
        Gizmos.DrawLine(spawnTransform.position, spawnTransform.position + Quaternion.AngleAxis(-detectionDegree, spawnTransform.up) * spawnTransform.forward * detectionRange);
        Gizmos.DrawLine(spawnTransform.position, spawnTransform.position + Quaternion.AngleAxis(detectionDegree, spawnTransform.right) * spawnTransform.forward * detectionRange);
        Gizmos.DrawLine(spawnTransform.position, spawnTransform.position + Quaternion.AngleAxis(-detectionDegree, spawnTransform.right) * spawnTransform.forward * detectionRange);
    }
}
