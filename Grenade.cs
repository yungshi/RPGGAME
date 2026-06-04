using UnityEngine;

public class Grenade : MonoBehaviour
{
    public GameObject meshObj;//메시 오브젝트
    public GameObject effectObj;//효과 오브젝트
    public Rigidbody rigid;/리지드바디

    void Start()//실행 즉시 폭발
    {
        StartCoroutine(Explosion());
    }

    IEnumerator Explosion()//폭발 방식
    {
        yield return new WaitForSeconds(3f);
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
        meshObj.SetActive(false);
        effectObj.SetActive(true);

        RaycastHit[] rayHits = Physics.SphereCastAll(transform.position, 15, Vector3.up, 0f, LayerMask.GetMask("Enemy"));
        //폭팔 에니메이션 및 몬스터에게 공격
        foreach(RaycastHit hitObj in rayHits)
        {
            hitObj.transform.GetComponent<Enemy>().HitByGrenade(transform.position);
        }

        Destroy(gameObject, 5);
    }
}
