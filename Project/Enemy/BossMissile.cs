using UnityEngine;
using UnityEngine.AI;

public class BossMissile : Bullet //Bullet 클래스 기반
{
    public Transform target;//터겟 위치 변수
    
    float speed = 10f;
NavMeshAgent nav;
    void Awake()//실행시 먼저 실행
    {
        nav = GetComponent<NavMeshAgent>();
        if (nav != null)
        {
            if (nav.speed > 0f) speed = nav.speed; 
            nav.enabled = false;          
        }
    }
    void Start()
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


    void Update()//위치 업데이트
    {
        if (target == null) return;
        Vector3 dir = target.position - transform.position;//실시간 추적
        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.position += dir.normalized * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }
    }
}
