using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebrisDestroy : MonoBehaviour
{
    private IEnumerator destroyThis;
    private IEnumerator DestroyThisObj()
    {
        yield return GameSingletonItems.WT_1point5sec;
        gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        if(destroyThis !=null) StopCoroutine(destroyThis);
        destroyThis = DestroyThisObj();
        StartCoroutine(destroyThis);
    }
}
