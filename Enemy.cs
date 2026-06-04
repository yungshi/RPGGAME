using UnityEngine;
using UnityEngine.AI;
public class Enemy : MonoBehaviour
{
    //기본 변수↓
    public enum Type { A, B, C, D };//몬스터 이름
    public Type enemyType;//몬스터 종류
    public int maxHealth;//최대채력
    public int curHealth;//현재채력
    public Transform Target;//타겟 위치
    public BoxCollider meleeArea;//박스콜라이더
    public GameObject bullet;//총알 오브젝트
    public bool isChase;//쫒아가는가?
    public bool isAttack;//공격하는가?
    public bool isDead;//죽었는가?

    public Rigidbody rigid;//리지드바디
    public BoxCollider boxCollider;//콜라이더
    public MeshRenderer[] meshs;//메시
    public NavMeshAgent nav;//길찾기 시스템
    public Animator anim;//에니매이션

    void Awake()//최초실행 코드
    {
        rigid = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        meshs = GetComponentsInChildren<MeshRenderer>();
        nav = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        
        if(enemyType != Type.D)//D몬스터가 아니면 2초뒤 실행
            Invoke("ChaseStart", 2);//플레이어를 쫒아감
    }

    void ChaseStart()//플레이어를 쫒아갈 준비
    {
        isChase = true;
        anim.SetBool("isWalk", true);
    }

    void Update()
    {
       if(nav.enabled && enemyType != Type.D)//Type D 몬스터 제외 타겟을 쫒아감
        {
            nav.SetDestination(Target.position);//목표지점
            nav.isStopped = !isChase;//쫒는 것이 끝나면 멈춤
        }
            
    }

    void FreezeVelocity()//불필요 관성 제서하여 튕겨나지 않게함
    {
        if (isChase)
        {
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
        
    }

    void Targerting()
    {
        if(!isDead && enemyType != Type.D)//Type D 제외 나머지 않 죽은 Type
        {
            float targetRadius = 0;//사거리
            float targetRange = 0;//범위

            switch (enemyType)//몬스터별 사거리와 감지 범위
            {
                case Type.A:
                    targetRadius = 1.5f;
                    targetRange = 3f;
                    break;
                case Type.B:
                    targetRadius = 1f;
                    targetRange = 12f;
                    break;
                case Type.C:
                    targetRadius = 0.5f;
                    targetRange = 25f;
                    break;
            }
        
            RaycastHit[] rayHits = Physics.SphereCastAll(transform.position, targetRadius, transform.forward, targetRange, LayerMask.GetMask("Player"));
            //공격범위안 플레이어 감지
            if(rayHits.Length > 0 && !isAttack)//타겟이 감지되면
            {
                StartCoroutine(Attack());//공격 호출
            }
        }
        
    }

    IEnumerator Attack()//공격패턴
    {
        isChase = false;//쫒지 않음
        isAttack = true;//공격허용
        anim.SetBool("isAttack", true);
        
        switch (enemyType)
        {
            case Type.A://에니매이션 재생후 끄기
                yield return new WaitForSeconds(0.2f);//0.2초 대기
                meleeArea.enabled = true;//공격허용

                yield return new WaitForSeconds(1f);//1초 대기
                meleeArea.enabled = false;//공격 중지

                yield return new WaitForSeconds(1f);//1초대기
                break;
            case Type.B://돌진후 끄기
                yield return new WaitForSeconds(0.1f);//0.1초 대기
                rigid.AddForce(transform.forward * 20, ForceMode.Impulse);//전방으로 돌진
                meleeArea.enabled = true;//공격허용

                yield return new WaitForSeconds(0.5f);//0.5초 대기
                rigid.linearVelocity = Vector3.zero;
                meleeArea.enabled = false;//공격 중지

                yield return new WaitForSeconds(2f);//2초 대기
                break;
            case Type.C://발사후 종료
                yield return new WaitForSeconds(0.5f);//0.5초 대기
                GameObject instantBullet = Instantiate(bullet, transform.position, transform.rotation);//총알생성
                Rigidbody rigidBullet = instantBullet.GetComponent<Rigidbody>();
                rigidBullet.linearVelocity = transform.forward * 20;

                yield return new WaitForSeconds(2f);//2초대기
                break;
        }

        

        isChase = true;//쫒아감
        isAttack = false;//공격중지
        anim.SetBool("isAttack", false);
    }

    void FixedUpdate()//매 프래임마다 실행
    {
        Targerting();//감지범위에서 목표 찾음
        FreezeVelocity();//물리버그 방지
    }

    void OnTriggerEnter(Collider other)//플레이어의 공격무기별 상호작용
    {
        if(other.tag == "Melee")
        {
            Weapon weapon = other.GetComponent<Weapon>();
            curHealth -= weapon.damage;//체력감소
            Vector3 reactVec = transform.position - other.transform.position;//너백
            StartCoroutine(onDamage(reactVec, false));
        }
        else if (other.tag == "Bullet")
        {
            Bullet bullet = other.GetComponent<Bullet>();
            curHealth -= bullet.damage;//체력감소
            Vector3 reactVec = transform.position - other.transform.position;//너백
            StartCoroutine(onDamage(reactVec, false));
        }
    }

    public void HitByGrenade(Vector3 explosionPos)//플레이어 공격무기 상호작용2
    {
        curHealth -= 100;//체력감소
        Vector3 reactVec = transform.position - explosionPos;//너백
        StartCoroutine(onDamage(reactVec, true));
    }

    IEnumerator onDamage(Vector3 reactVec, bool isGrenade)//대미지를 받음
    {
        foreach(MeshRenderer mesh in meshs)
            mesh.material.color = Color.red;//일시적으로 빨갛게 변화
        yield return new WaitForSeconds(0.1f);//0.1초 대기

        if(curHealth > 0)//살아있으면
        {
           foreach(MeshRenderer mesh in meshs)
                mesh.material.color = Color.white;//살아있으면 다시 원상태
        }
        else//죽으면
        {
            foreach(MeshRenderer mesh in meshs)
                mesh.material.color = Color.gray;//몸색깔 회색
            gameObject.layer = 14;
            //초기화↓
            isDead = true;
            isChase = false;
            nav.enabled = false;
            anim.SetTrigger("doDie");
            
            if (isGrenade)//원인이 수류탄이면 날려버림
            {
                reactVec = reactVec.normalized;
                reactVec += Vector3.up *3;

                rigid.freezeRotation = false;
                rigid.AddForce(reactVec * 5, ForceMode.Impulse);
                rigid.AddTorque(reactVec * 15, ForceMode.Impulse);
            }
            else//그외 원인 약하게 날림
            {
                reactVec = reactVec.normalized;
                reactVec += Vector3.up;

                rigid.AddForce(reactVec * 5, ForceMode.Impulse);
            }
            if(enemyType != Type.D)//보스가 아니면 사망 몬스터 제거
                Destroy(gameObject, 4);
        }
    }
}
