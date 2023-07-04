using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public bool isRecycle;
	public GameObject explosionPrefab; //폭발 프리펩
	public float explosionRange = 5;
	public float explosionPower = 1;
	public float explosionUpPower = 1;
	public float explosionDamage = 1;

    private GameObject explosionEffect; //폭발 이펙트
    private Collider[] overlapCol = new Collider[32];
    private bool isExploded = false; // 폭발했는지 체크
    [HideInInspector]
    public int indexNumber;
    private void ChainExplode()
    {
        StartCoroutine(CoroutineChainExplode());
    }


    private IEnumerator CoroutineChainExplode()
	{
        yield return GameSingletonItems.WFEOF; //한프레임 안에 다같이 터져버리면 게임도 같이 터짐
        Explode();
    }

	public void Explode()
	{
        if (isExploded) 
            return;

        isExploded = true;
        int layerMask = 1 << LayerMask.NameToLayer("Object") | 1 << LayerMask.NameToLayer("WallObject") | 1 << LayerMask.NameToLayer("Character");

        int lapNum = Physics.OverlapSphereNonAlloc(transform.position, explosionRange, overlapCol, layerMask);

        for (int i = 0; i < lapNum; i++)
        {
            Rigidbody rb = overlapCol[i].GetComponent<Rigidbody>();
            Explosion ex = overlapCol[i].GetComponent<Explosion>();
            GetDamage gd = overlapCol[i].GetComponent<GetDamage>();
            GetDamage_Player gd_Player = overlapCol[i].GetComponent<GetDamage_Player>();

            if (rb == null || rb.isKinematic) //물리엔진이 없거나 키네마틱상태라면 건너뜀
                continue;

            Vector3 explodeVector = overlapCol[i].transform.position - transform.position;

            float proportionalDistance = Mathf.Clamp(explodeVector.magnitude * 1.5f / explosionRange, 0.5f, 1);

            explodeVector.y = 0;
            explodeVector.Normalize();

            explodeVector *= explosionPower * proportionalDistance;
            explodeVector.y = explosionUpPower * proportionalDistance;
            rb.AddForce(explodeVector, ForceMode.Impulse);

            if (ex != null)
            {
                ex.ChainExplode(); //폭발물이 있을 시 연쇄 폭발
            }
            
            if(gd_Player != null)
            {
                if (rb.constraints == RigidbodyConstraints.FreezeAll) continue;
                gd_Player.GetDamaged(explosionDamage * proportionalDistance);
            }
            else if (gd != null)
            {
                gd.GetDamaged(explosionDamage * proportionalDistance);
            }
        }
        ExplosionEffectOn();
    }

    public void ExplosionEffectOn()
    {
        //폭발 이펙트 생성 및 폭탄 오브젝트 삭제
        if (explosionEffect != null)
        {
            explosionEffect.transform.position = transform.position;
            explosionEffect.SetActive(true);
            if (isRecycle)
            {
                explosionEffect.GetComponent<ParticleSystem>().Play();
                explosionEffect.GetComponent<AudioSource>().Play();
            }
        }
        if (isRecycle == false)
        {
            GameSingletonItems.destroyObj.Enqueue(gameObject);
        }
        isExploded = false;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (explosionPrefab == null) return;
        if (GeneralMgr.isServer&& GameSingletonItems.isNetGame) return; //서버면 이펙트 생성 x

        explosionEffect = Instantiate(explosionPrefab, transform.position, transform.rotation);
        explosionEffect.transform.SetParent(transform.parent);
        explosionEffect.transform.localScale *= explosionRange;
        explosionEffect.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (GameSingletonItems.isNetGame && GeneralMgr.isServer == false) return;//클라에서는 계산 못하게 막음
        if (collision.gameObject.CompareTag("Ground")) return;
        Explode();
    }

    private void OnDrawGizmos() // 폭발 범위 표시
	{
		Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(gameObject.transform.position, explosionRange);
	}
}
