using UnityEngine;
using UnityEngine.AI;

public class BossMissile : Bullet //Bullet 클래스 기반
{
    public Transform target;//타겟 위치 변수
    
    float speed = 10f;//이동 속도 (NavMeshAgent.speed 에서 승계)
NavMeshAgent nav;//길을 찾기 위해 선언
    void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        if (nav != null)
        {
            if (nav.speed > 0f) speed = nav.speed; // 인스펙터에 설정한 속도 유지
            nav.enabled = false;                   // 비행 미사일 → NavMesh 미사용(직접 호밍)
        }
    }
    void Start()//데미지 기본값 + 안전 수명
    {
        if (damage <= 0) damage = 20;
        Destroy(gameObject, 12f);
    }

    void OnTriggerEnter(Collider other) { HitPlayer(other.gameObject); }
    void OnCollisionEnter(Collision col) { HitPlayer(col.gameObject); }

    void HitPlayer(GameObject other)//플레이어에게 피해 전달
    {
        if (other.CompareTag("Player"))
        {
            Items items = other.GetComponent<Items>();
            if (items == null) items = other.GetComponentInParent<Items>();
            if (items != null) items.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }


    void Update()
    {
        if (target == null) return;
        Vector3 dir = target.position - transform.position;//플레이어 실시간 추적(직접 호밍)
        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.position += dir.normalized * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }
    }
}
