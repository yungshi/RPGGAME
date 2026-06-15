using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Enemy : MonoBehaviour
{
    
    public enum Type { A, B, C, D }; // 몬스터 타입
    public Type enemyType;

    public int maxHealth;           // 최대 체력
    public int curHealth;           // 현재 체력
    public int damage = 10;         // 데미지

    public Transform Target;        // 터겟 위치
    public BoxCollider meleeArea;   // 공격판정 Trigger
    public GameObject bullet;       // C 몬스터 미사일

    public bool isChase;//추격
    public bool isAttack;/공격
    public bool isDead;//사망

    public Rigidbody rigid;
    public BoxCollider boxCollider;
    public MeshRenderer[] meshs;
    public NavMeshAgent nav;
    public Animator anim;

    
    void Awake()//최초실행
    {
        rigid        = GetComponent<Rigidbody>();
        boxCollider  = GetComponent<BoxCollider>();
        meshs        = GetComponentsInChildren<MeshRenderer>();
        nav          = GetComponent<NavMeshAgent>();
        anim         = GetComponentInChildren<Animator>();

        if (enemyType != Type.D)
            Invoke("ChaseStart", 2f);
    }

    void ChaseStart()//플레이어 추격
    {
        isChase = true;
        if (anim != null) anim.SetBool("IsWalk", true);
    }

    
    void Update()//프레이마다 업데이트
    {
        if (nav.enabled && nav.isOnNavMesh && enemyType != Type.D)
        {
            nav.SetDestination(Target.position);
            nav.isStopped = !isChase;
        }
    }

    void FixedUpdate()//프레임 마다 업데이트
    {
        Targerting();
        FreezeVelocity();
    }

    void FreezeVelocity()//백터 고정
    {
        if (isChase)
        {
            rigid.linearVelocity    = Vector3.zero;
            rigid.angularVelocity   = Vector3.zero;
        }
    }

   
    void Targerting()//감지
    {
        if (isDead || enemyType == Type.D) return;

        float targetRadius = 0f;
        float targetRange  = 0f;

        switch (enemyType)//감지범위 
        {
            case Type.A: targetRadius = 1.5f; targetRange =  3f; break;
            case Type.B: targetRadius = 1.5f; targetRange =  3f; break; 
            case Type.C: targetRadius = 0.5f; targetRange = 25f; break;
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position, targetRadius, transform.forward,
            targetRange, LayerMask.GetMask("Player"));//감지 되었을때

        if (hits.Length > 0 && !isAttack)
            StartCoroutine(Attack());
    }

    
    IEnumerator Attack()//공격패턴
    {
        isChase = false;
        isAttack = true;
        if (anim != null) anim.SetBool("IsAttack", true);

        switch (enemyType)
        {
           
            case Type.A:
            case Type.B:
                yield return new WaitForSeconds(0.2f);

               
                if (Target != null)
                {
                    Items playerItems = Target.GetComponent<Items>();
                    if (playerItems == null)
                        playerItems = Target.GetComponentInParent<Items>();
                    if (playerItems != null &&
                        Vector3.Distance(transform.position, Target.position) <= 3f)
                        playerItems.TakeDamage(damage);
                }

                yield return new WaitForSeconds(2f);
                break;

           
            case Type.C:
                yield return new WaitForSeconds(0.5f);

                if (bullet != null)
                {
                   
                    Vector3 spawnPos = transform.position + Vector3.up * 1.0f + transform.forward * 0.8f;
                    Quaternion spawnRot = Quaternion.identity;
                    if (Target != null)
                    {
                        Vector3 dirToTarget = (Target.position - spawnPos).normalized;
                        if (dirToTarget.sqrMagnitude > 0.0001f)
                            spawnRot = Quaternion.LookRotation(dirToTarget) * Quaternion.Euler(0, -90f, 0);
                    }
                    else
                    {
                        spawnRot = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, -90f, 0);
                    }

                    Debug.Log($"[Enemy] Bullet prefab '{bullet.name}' prefabRot={bullet.transform.rotation.eulerAngles} prefabLocalEuler={bullet.transform.localEulerAngles}");
                    GameObject inst = Instantiate(bullet, spawnPos, spawnRot);
                    inst.transform.SetParent(null);
                    Debug.Log($"[Enemy] Spawned missile {inst.name} parent={(inst.transform.parent!=null?inst.transform.parent.name:"null")} spawnPos={spawnPos} rot={inst.transform.rotation.eulerAngles} localEuler={inst.transform.localEulerAngles}");
                    Missile missile = inst.GetComponent<Missile>();

                    if (missile != null)
                    {
                        missile.target = Target;
                        missile.damage = damage;
                    }
                    else
                    {
                        Rigidbody rb = inst.GetComponent<Rigidbody>();
                        if (rb != null) rb.linearVelocity = transform.forward * 20f;
                    }
                }

                yield return new WaitForSeconds(2f);
                break;
        }

        isChase  = true;
        isAttack = false;
        if (anim != null) anim.SetBool("IsAttack", false);
    }

   
    void OnTriggerEnter(Collider other)//피격감지
    {
        
        if (other.CompareTag("Melee"))//근접무기
        {
            Weapon weapon = other.GetComponent<Weapon>();
            if (weapon == null) return;
            TakeDamage(weapon.damage, transform.position - other.transform.position);
        }
        
        else if (other.CompareTag("Bullet"))//원거리 무기
        {
            Bullet bulletComp = other.GetComponent<Bullet>();
            if (bulletComp == null) return;
            TakeDamage(bulletComp.damage, transform.position - other.transform.position);
            Destroy(other.gameObject);
        }
        
        else if (other.CompareTag("Player"))
        {
            Items playerItems = other.GetComponent<Items>();
            if (playerItems == null)
                playerItems = other.GetComponentInParent<Items>();
            if (playerItems != null)
                playerItems.TakeDamage(damage);
        }
    }

  
    void OnCollisionEnter(Collision collision)//총알 피격및 총알 제거
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Bullet bulletComp = collision.gameObject.GetComponent<Bullet>();
            if (bulletComp == null || bulletComp.isMelee) return;
            TakeDamage(bulletComp.damage, transform.position - collision.transform.position);
            Destroy(collision.gameObject);
        }
    }

  
    public void TakeDamage(int dmg, Vector3 reactVec)//데미지 개산
    {
        if (isDead) return;
        curHealth -= dmg;
        StartCoroutine(onDamage(reactVec, false));
    }

    public void HitByGrenade(Vector3 explosionPos)//수류탄에 맞으면
    {
        if (isDead) return;
        curHealth -= 100;
        StartCoroutine(onDamage(transform.position - explosionPos, true));
    }

    IEnumerator onDamage(Vector3 reactVec, bool isGrenade)//공격을 받았을때 변화
    {
        foreach (MeshRenderer mesh in meshs)
            mesh.material.color = Color.red;

        yield return new WaitForSeconds(0.1f);

        if (curHealth > 0)
        {
            foreach (MeshRenderer mesh in meshs)
                mesh.material.color = Color.white;
        }
        else
        {
            foreach (MeshRenderer mesh in meshs)
                mesh.material.color = Color.gray;

            gameObject.layer = 14;
            isDead   = true;
            isChase  = false;
            if (nav != null) nav.enabled = false;
            if (anim != null) anim.SetTrigger("doDie");

            reactVec = reactVec.normalized;
            if (isGrenade)
            {
                reactVec += Vector3.up * 3f;
                rigid.freezeRotation = false;
                rigid.AddForce(reactVec * 5f, ForceMode.Impulse);
                rigid.AddTorque(reactVec * 15f, ForceMode.Impulse);
            }
            else
            {
                reactVec += Vector3.up;
                rigid.AddForce(reactVec * 5f, ForceMode.Impulse);
            }

            if (enemyType != Type.D)
                Destroy(gameObject, 4f);
        }
    }
}
