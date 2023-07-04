using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debris_Fall : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision) //조각이 어딘가에 닿으면 사라지게 하기
    {
        if (collision.transform.CompareTag("Debris")) //조각들 끼리 부딪히면 실행하지 않음
            return;
        
        gameObject.SetActive(false);
    }
}
