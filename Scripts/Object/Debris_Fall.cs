using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debris_Fall : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision) //������ ��򰡿� ������ ������� �ϱ�
    {
        if (collision.transform.CompareTag("Debris")) //������ ���� �ε����� �������� ����
            return;
        
        gameObject.SetActive(false);
    }
}
