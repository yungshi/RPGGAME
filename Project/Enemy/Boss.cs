using UnityEngine;
using UnityEngine.AI;
using System.Collections;
public class Boss : Enemy //Enemy 클래스 기반
{
    public GameObject missile;//미사일 오브젝트
    public Transform missilePortA;//미사일 A 위치
    public Transform missilePortB;//미사일 B 위치
    public bool isLook;
    public float bigShotDistance = 0f;   //멀리 점프 거리

    Vector3 lookVec;//플레이어 추적
    Vector3 tauntVec;//도발기
    
    Vector3 modelScale = Vector3.one * 3f;//보스 크기 확대

    void LateUpdate()//모델 크기 복구
    {
        if (anim != null)
            anim.transform.localScale = modelScale;
    }

    void Awake()//게임 실행 동안 
    {
        rigid = GetComponent<Rigidbody>();//게임 내부 컴포넌트 가져오기1
        boxCollider = GetComponent<BoxCollider>();//게임 내부 컴포넌트 가져오기2
        meshs = GetComponentsInChildren<MeshRenderer>();//게임 내부 컴포넌트 가져오기3
        nav = GetComponent<NavMeshAgent>();//게임 내부 컴포넌트 가져오기4
        anim = GetComponentInChildren<Animator>();//게임 내부 컴포넌트 가져오기5

        if (nav != null && nav.isOnNavMesh) nav.isStopped = true;
        
        StartCoroutine(Think());
    }

    void Update()//매 프래임 업데이트
    {
        
        if (enemyType == Enemy.Type.D) return;

        if (isDead) //생사여부
        {
            StopAllCoroutines();
            return;
        }
        if (isLook)//타겟 감지
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            lookVec = new Vector3(h, 0, v) * 5f;
            transform.LookAt(Target.position + lookVec);
        }
        else
            MoveToward(tauntVec);
    }

    
    void MoveToward(Vector3 dest)//상태별 이동 방식
    {
        if (nav != null && nav.enabled && nav.isOnNavMesh)
        {
            nav.SetDestination(dest);
        }
        else
        {
            Vector3 dir = dest - transform.position;
            dir.y = 0f;
            float spd = nav != null ? nav.speed : 3.5f;
            if (dir.sqrMagnitude > 0.01f)
                transform.position += dir.normalized * spd * Time.deltaTime;
        }
    }


    IEnumerator Think()//보스 공격 패턴
    {
        yield return new WaitForSeconds(0.1f);
        if (isDead) yield break;

        int ranAction = Random.Range(0, 5);
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

    IEnumerator MissileShot()//미사일 발사 
    {
        if (isDead) yield break;
        anim.SetTrigger("doShot");
        yield return new WaitForSeconds(0.2f);
        if (isDead) yield break;
        GameObject instantMissileA = Instantiate(missile, missilePortA.position, missilePortA.rotation);
        BossMissile bossMissileA = instantMissileA.GetComponent<BossMissile>();
        bossMissileA.target = Target;

        yield return new WaitForSeconds(0.3f);
        GameObject instantMissileB = Instantiate(missile, missilePortB.position, missilePortB.rotation);
        BossMissile bossMissileB = instantMissileB.GetComponent<BossMissile>();
        bossMissileB.target = Target;

        yield return new WaitForSeconds(2f);
        if (!isDead)
            StartCoroutine(Think());
    }

    IEnumerator RockShot()//RockShot 공격 
    {
        if (isDead) yield break;
        isLook = false;
        anim.SetTrigger("doBigShot");
        Instantiate(bullet, transform.position + transform.forward * bigShotDistance, transform.rotation);
        yield return new WaitForSeconds(3f);
        if (isDead) yield break;

        isLook = true;
        StartCoroutine(Think());
    }

    IEnumerator Taunt()//도발기
    {
        if (isDead) yield break;
        tauntVec = Target.position + lookVec;

        isLook = false;
        if (nav != null && nav.isOnNavMesh) nav.isStopped = false;
        boxCollider.enabled = false;
        anim.SetTrigger("doTaunt");
        yield return new WaitForSeconds(1.5f);
        if (isDead) yield break;
        meleeArea.enabled = true;

        yield return new WaitForSeconds(0.5f);
        meleeArea.enabled = false;

        yield return new WaitForSeconds(1f);
        isLook = true;
        if (nav != null && nav.isOnNavMesh) nav.isStopped = true;
        boxCollider.enabled = true;

        if (!isDead)
            StartCoroutine(Think());
    }
}
