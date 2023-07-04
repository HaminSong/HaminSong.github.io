using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public bool isRecycle;
	public GameObject explosionPrefab; //���� ������
	public float explosionRange = 5;
	public float explosionPower = 1;
	public float explosionUpPower = 1;
	public float explosionDamage = 1;

    private GameObject explosionEffect; //���� ����Ʈ
    private Collider[] overlapCol = new Collider[32];
    private bool isExploded = false; // �����ߴ��� üũ
    [HideInInspector]
    public int indexNumber;
    private void ChainExplode()
    {
        StartCoroutine(CoroutineChainExplode());
    }


    private IEnumerator CoroutineChainExplode()
	{
        yield return GameSingletonItems.WFEOF; //�������� �ȿ� �ٰ��� ���������� ���ӵ� ���� ����
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

            if (rb == null || rb.isKinematic) //���������� ���ų� Ű�׸�ƽ���¶�� �ǳʶ�
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
                ex.ChainExplode(); //���߹��� ���� �� ���� ����
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
        //���� ����Ʈ ���� �� ��ź ������Ʈ ����
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
        if (GeneralMgr.isServer&& GameSingletonItems.isNetGame) return; //������ ����Ʈ ���� x

        explosionEffect = Instantiate(explosionPrefab, transform.position, transform.rotation);
        explosionEffect.transform.SetParent(transform.parent);
        explosionEffect.transform.localScale *= explosionRange;
        explosionEffect.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (GameSingletonItems.isNetGame && GeneralMgr.isServer == false) return;//Ŭ�󿡼��� ��� ���ϰ� ����
        if (collision.gameObject.CompareTag("Ground")) return;
        Explode();
    }

    private void OnDrawGizmos() // ���� ���� ǥ��
	{
		Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(gameObject.transform.position, explosionRange);
	}
}
