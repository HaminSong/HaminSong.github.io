using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

public class Bird_Spawner : MonoBehaviour
{
    public int birdNum = 8;
    public float birdSpeed = 10;
    public GameObject birdPrefab;

    private float mapLength_X;
    private float mapLength_Z;
    private GameObject[] birdGroup;
    private float[] birdSpeedGroup;


    private IEnumerator BirdReplace(Transform birdTransform)
    {
        WaitUntil WU_BirdArrive = new WaitUntil(() => birdTransform.localPosition.z > mapLength_X);
        float random_Z;
        float random_Y;
        while (true)
        {
            random_Z = Random.Range(-1f, 1f) * mapLength_Z * 0.5f;
            random_Y = Random.Range(-20, 20);
            birdTransform.position = new Vector3(transform.position.x - random_Z * 0.5f, transform.position.y + random_Y, transform.position.z);
            yield return WU_BirdArrive;
        }
    }

    private void Start()
    {
        birdGroup = new GameObject[birdNum];
        birdSpeedGroup = new float[birdNum];
        mapLength_Z = transform.parent.GetChild(0).lossyScale.z * 8000;
        mapLength_X = transform.parent.GetChild(0).lossyScale.x * 10000;
        for (int i = 0;  i < birdNum; i++)
        {
            float randomSpeed = Random.Range(birdSpeed * 0.2f, birdSpeed * 1f);
            GameObject bird = Instantiate(birdPrefab, transform.position, transform.rotation);
            birdGroup[i] = bird;
            birdSpeedGroup[i] = randomSpeed;
            bird.transform.SetParent(transform);
            StartCoroutine(BirdReplace(bird.transform));
        }
    }

    private void Update()
    {
        for(int i = 0; i< birdGroup.Length; i++)
        {
            birdGroup[i].transform.position += birdGroup[i].transform.forward * birdSpeedGroup[i];
        }
    }
}
