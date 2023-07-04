using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bird : MonoBehaviour
{
    private GameObject BackLight;

    public IEnumerator PlayerEnterTarget(float fadeTime)
    {
        BackLight.GetComponent<AudioSource>().Play();
        ParticleSystem.MainModule particleSystem = BackLight.GetComponent<ParticleSystem>().main;
        ParticleSystem.MainModule childParticleSystem = BackLight.transform.GetChild(0).GetComponent<ParticleSystem>().main;
        Color color = Color.white;
        float timer = 0;
        while (true)
        {
            if (timer > fadeTime)
            {
                color.a = 0;
                particleSystem.startColor = color;
                childParticleSystem.startColor = color;
                break;
            }
            color.a = 1 - timer / fadeTime;
            particleSystem.startColor = color;
            childParticleSystem.startColor = color;
            timer += Time.deltaTime;
            yield return GameSingletonItems.WFEOF;
        }
        gameObject.SetActive(false);
    }

    private void Awake()
    {
        if (transform.childCount == 0) return;
        if (transform.GetChild(transform.childCount - 1).name.Contains("BackLight"))
        {
            BackLight = transform.GetChild(transform.childCount - 1).gameObject;
            BackLight.transform.SetParent(transform.parent);
        }
    }
    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (gameObject.CompareTag("Bullet"))
        {
            GetComponent<AudioSource>().Play();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathZone"))
            gameObject.SetActive(false);
    }
}
