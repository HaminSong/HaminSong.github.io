using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackerBird : Bird
{
    private IEnumerator hitAnythingIEnumerator;
    public IEnumerator HitAnything()
    {
        yield return GameSingletonItems.WT_1sec;
        gameObject.SetActive(false);
    }
    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        if (hitAnythingIEnumerator != null) StopCoroutine(hitAnythingIEnumerator);
        hitAnythingIEnumerator = HitAnything();
        StartCoroutine(hitAnythingIEnumerator);
    }
}
