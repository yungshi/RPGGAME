using UnityEngine;
using UnityEngine.AI;

public class Boss : Enemy //Enemy 클래스 기반
{
    public GameObject missile;
    public Transform missilePortA;
    public Transform missilePortB;
    public bool isLook;

    Vector3 lookVec;
    Vector3 tauntVec;
    
    void Awake()//게임 실행 동안 
    {
        rigid = GetComponent<Rigidbody>();//게임 내부 컴포넌트 가져오기1
        boxCollider = GetComponent<BoxCollider>();//게임 내부 컴포넌트 가져오기2
        meshs = GetComponentsInChildren<MeshRenderer>();//게임 내부 컴포넌트 가져오기3
        nav = GetComponent<NavMeshAgent>();//게임 내부 컴포넌트 가져오기4
        anim = GetComponentInChildren<Animator>();//게임 내부 컴포넌트 가져오기5
        
        nav.isStopped = true;
        StartCoroutine(Think());
    }

    void Update()//매 프래임마다 업데이트
    {
        if (isDead) //생사여부확인
        {
            StopAllCoroutines();
            return;
        }
        if (isLook)//일정 범위 내에 타겟이 있는지 감지
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            lookVec = new Vector3(h, 0, v) * 5f;
            transform.LookAt(Target.position + lookVec);
        }
        else
            nav.SetDestination(tauntVec);
    }

    IEnumerator Think()
    {
        yield return new WaitForSeconds(0.1f);

        int ranAction = Random.Range(0, 5);//0~4까지 무작위 정수 숫자
        switch (ranAction)
        {
            case 0:
            case 1:
                StartCoroutine(MissileShot());//공격재생1
                break;
            case 2:
            case 3:
                StartCoroutine(RockShot());//공격재생2
                break;
            case 4:
                StartCoroutine(Taunt());//공격재생3
                break;
        }
    }

    IEnumerator MissileShot()//미사일 발사 형식
    {
        anim.SetTrigger("doShot");
        yield return new WaitForSeconds(0.2f);
        GameObject instantMissileA = Instantiate(missile, missilePortA.position, missilePortA.rotation);
        BossMissile bossMissileA = instantMissileA.GetComponent<BossMissile>();
        bossMissileA.target = Target;

        yield return new WaitForSeconds(0.3f);
        GameObject instantMissileB = Instantiate(missile, missilePortB.position, missilePortB.rotation);
        BossMissile bossMissileB = instantMissileB.GetComponent<BossMissile>();
        bossMissileB.target = Target;

        yield return new WaitForSeconds(2f);
        StartCoroutine(Think());
    }

    IEnumerator RockShot()//RockShot 공격 형식
    {
        isLook = false;
        anim.SetTrigger("doBigShot");
        Instantiate(bullet, transform.position, transform.rotation); 
        yield return new WaitForSeconds(3f);
        
        isLook = true;
        StartCoroutine(Think());
    }

    IEnumerator Taunt()//플레이어에게 도발
    {
        tauntVec = Target.position + lookVec;
        
        isLook = false;
        nav.isStopped = false;
        boxCollider.enabled = false;
        anim.SetTrigger("doTaunt");
        yield return new WaitForSeconds(1.5f);
        meleeArea.enabled = true;
        
        yield return new WaitForSeconds(0.5f);
        meleeArea.enabled = false;

        yield return new WaitForSeconds(1f);
        isLook = true;
        nav.isStopped = true;
        boxCollider.enabled = true;

        StartCoroutine(Think());
    }
}
