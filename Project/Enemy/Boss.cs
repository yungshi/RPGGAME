using UnityEngine;
using UnityEngine.AI;
using System.Collections;
public class Boss : Enemy //Enemy 클래스 기반
{
    public GameObject missile;//미사일 오브젝트
    public Transform missilePortA;//미사일 A 위치
    public Transform missilePortB;//미사일 B 위치
    public bool isLook;
    public float bigShotDistance = 0f;   // 빅샷(락) 생성 위치를 보스 전방으로 띄우는 거리 (0=보스 위치)

    Vector3 lookVec;//플레이어 추적 백터
    Vector3 tauntVec;//도발기 백터
    
    Vector3 modelScale = Vector3.one * 3f;//원래 보스 크기(애니메이션 클립이 스케일을 1로 덮어써 작아지는 것 방지)

    void LateUpdate()//애니메이션 적용 후 모델 크기 원상복구
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
        // 원거리 공격 패턴(미사일/락/도발). Type.D 도 포함 — 이동은 EnemyD 가 NavMesh 로 담당.
        StartCoroutine(Think());
    }

    void Update()//매 프래임마다 업데이트
    {
        // Type.D 는 EnemyD.cs 가 이동/회전을 전담 → Boss 이동 루프 비활성화
        if (enemyType == Enemy.Type.D) return;

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
            MoveToward(tauntVec);
    }

    // NavMesh 가 있으면 경로 이동, 없으면(미배치) 직접 이동 — SetDestination 오류 방지 + 돌진 유지
    void MoveToward(Vector3 dest)
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

    IEnumerator RockShot()//RockShot 공격 형식
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

    IEnumerator Taunt()//플레이어에게 도발
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
