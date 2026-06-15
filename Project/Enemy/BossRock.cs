using UnityEngine;
using System.Collections;
public class BossRock : Bullet//Bullet 클래스 기반
{
    Rigidbody rigid;
    public float flightTime = 1.2f;   //목표가ㅣ 도달하는데 소유시간
    public float launchSpeed = 15f;   // 보스 직진속도
    public bool homeToTarget = false; // false=직진,True=추적
    float angularPower = 2;//회전힘
    float scaleValue = 0.1f;//크기
    bool isShoot;//발사확인(기본값 Fasle)
    Transform target;//발사 목표(플레이어)

    void Start()//시작시
    {
        if (homeToTarget)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) target = p.transform;
        }
        if (damage <= 0) damage = 40;
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))//다른 몬스터는 통과(관통)
            foreach (var ec in e.GetComponentsInChildren<Collider>())
                foreach (var mc in GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(mc, ec, true);
        StartCoroutine(LaunchWhenReady());
    }

    IEnumerator LaunchWhenReady()//발사 준비후 발사
    {
        yield return new WaitUntil(() => isShoot);
        if (target != null && homeToTarget)
        {
           
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

    void OnCollisionEnter(Collision col)//플레이어에게 데미지줌
    {
        if (!col.gameObject.CompareTag("Player")) return;
        Items items = col.gameObject.GetComponent<Items>();
        if (items == null) items = col.gameObject.GetComponentInParent<Items>();
        if (items != null) items.TakeDamage(damage);
        Destroy(gameObject);
    }

    void Awake()//가장 먼저 실해하는 것
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
