using UnityEngine;
using System.Collections;
public class BossRock : Bullet//Bullet 클래스 기반
{
    Rigidbody rigid;
    public float flightTime = 1.2f;   // 락 발사 후 목표 도달까지 시간(작을수록 빠르고 직선에 가까움)
    public float launchSpeed = 15f;   // 목표 미사용 시 보스 앞쪽 직진 속도
    public bool homeToTarget = false; // true 면 플레이어를 추적, false 면 직진
    float angularPower = 2;//회전력
    float scaleValue = 0.1f;//크기
    bool isShoot;//발사확인 변수(False 기본)
    Transform target;//발사 목표(플레이어)

    void Start()
    {
        if (homeToTarget)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) target = p.transform;
        }
        if (damage <= 0) damage = 40;//데미지 기본값
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))//다른 몬스터는 통과(관통)
            foreach (var ec in e.GetComponentsInChildren<Collider>())
                foreach (var mc in GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(mc, ec, true);
        StartCoroutine(LaunchWhenReady());
    }

    IEnumerator LaunchWhenReady()//충전 완료 후 플레이어에게 발사
    {
        yield return new WaitUntil(() => isShoot);
        if (target != null && homeToTarget)
        {
            // 중력을 고려한 탄도 속도 계산 → 거리와 무관하게 flightTime 뒤 목표 지점에 도달(멀리 있어도 명중)
            float T = Mathf.Max(0.1f, flightTime);
            Vector3 disp = target.position - transform.position;
            Vector3 v = disp / T - 0.5f * Physics.gravity * T;
            rigid.linearVelocity = v;
        }
        else
        {
            rigid.linearVelocity = transform.forward * launchSpeed;
        }
        Destroy(gameObject, 5f);
    }

    void OnCollisionEnter(Collision col)//플레이어 피해
    {
        if (!col.gameObject.CompareTag("Player")) return;
        Items items = col.gameObject.GetComponent<Items>();
        if (items == null) items = col.gameObject.GetComponentInParent<Items>();
        if (items != null) items.TakeDamage(damage);
        Destroy(gameObject);
    }

    void Awake()//최초 실행동작
    {
        rigid = GetComponent<Rigidbody>();
        StartCoroutine(GainPowerTimer());
        StartCoroutine(GainPower());
    }

    IEnumerator GainPowerTimer()//2.2 대기후 발사 루프
    {
        yield return new WaitForSeconds(2.2f);
        isShoot = true;
    }

    IEnumerator GainPower()//크기를 커지게함
    {
        while (!isShoot)
        {
            angularPower += 0.02f;
            scaleValue += 0.005f;
            transform.localScale = Vector3.one * scaleValue;
            rigid.AddTorque(transform.right * angularPower, ForceMode.Acceleration);
            yield return null; 
        }
    }
    
}
