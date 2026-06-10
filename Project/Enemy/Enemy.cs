using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Enemy : MonoBehaviour
{
    // ── 기본 변수 ──────────────────────────────────
    public enum Type { A, B, C, D }; // 몬스터 종류
    public Type enemyType;

    public int maxHealth;           // 최대 체력
    public int curHealth;           // 현재 체력
    public int damage = 10;         // 근접 공격 데미지

    public Transform Target;        // 플레이어 Transform
    public BoxCollider meleeArea;   // 근접 공격 판정 콜라이더(Trigger)
    public GameObject bullet;       // Type C : Missile 프리팹

    public bool isChase;
    public bool isAttack;
    public bool isDead;

    public Rigidbody rigid;
    public BoxCollider boxCollider;
    public MeshRenderer[] meshs;
    public NavMeshAgent nav;
    public Animator anim;

    // ── 초기화 ─────────────────────────────────────
    void Awake()
    {
        rigid        = GetComponent<Rigidbody>();
        boxCollider  = GetComponent<BoxCollider>();
        meshs        = GetComponentsInChildren<MeshRenderer>();
        nav          = GetComponent<NavMeshAgent>();
        anim         = GetComponentInChildren<Animator>();

        if (enemyType != Type.D)
            Invoke("ChaseStart", 2f);
    }

    void ChaseStart()
    {
        isChase = true;
        if (anim != null) anim.SetBool("IsWalk", true);
    }

    // ── 매 프레임 ──────────────────────────────────
    void Update()
    {
        if (nav.enabled && nav.isOnNavMesh && enemyType != Type.D)
        {
            nav.SetDestination(Target.position);
            nav.isStopped = !isChase;
        }
    }

    void FixedUpdate()
    {
        Targerting();
        FreezeVelocity();
    }

    void FreezeVelocity()
    {
        if (isChase)
        {
            rigid.linearVelocity    = Vector3.zero;
            rigid.angularVelocity   = Vector3.zero;
        }
    }

    // ── 공격 감지 ──────────────────────────────────
    void Targerting()
    {
        if (isDead || enemyType == Type.D) return;

        float targetRadius = 0f;
        float targetRange  = 0f;

        switch (enemyType)
        {
            case Type.A: targetRadius = 1.5f; targetRange =  3f; break;
            case Type.B: targetRadius = 1.5f; targetRange =  3f; break; // A와 동일
            case Type.C: targetRadius = 0.5f; targetRange = 25f; break;
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position, targetRadius, transform.forward,
            targetRange, LayerMask.GetMask("Player"));

        if (hits.Length > 0 && !isAttack)
            StartCoroutine(Attack());
    }

    // ── 공격 패턴 ──────────────────────────────────
    IEnumerator Attack()
    {
        isChase = false;
        isAttack = true;
        if (anim != null) anim.SetBool("IsAttack", true);

        switch (enemyType)
        {
            // ── Type A / B : 근접 공격 ──────
            case Type.A:
            case Type.B:
                yield return new WaitForSeconds(0.2f);

                // 근접 타격: 공격 순간 사정거리 안 플레이어에게 직접 피해
                // (meleeArea 콜라이더 토글 제거 - B/C의 meleeArea가 본체 콜라이더라
                //  공격 시 본체가 꺼져 더 이상 피격되지 않던 문제 방지)
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

            // ── Type C : 미사일 발사 ────────────
            case Type.C:
                yield return new WaitForSeconds(0.5f);

                if (bullet != null)
                {
                    // 바닥에 바로 닿아 즉시 파괴되지 않도록 위/앞으로 띄워서 생성
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

    // ── 피격 감지 : Trigger ────────────────────────
    void OnTriggerEnter(Collider other)
    {
        // 플레이어 무기(근접)에 맞을 때
        if (other.CompareTag("Melee"))
        {
            Weapon weapon = other.GetComponent<Weapon>();
            if (weapon == null) return;
            TakeDamage(weapon.damage, transform.position - other.transform.position);
        }
        // 플레이어 무기(총알 Trigger)에 맞을 때
        else if (other.CompareTag("Bullet"))
        {
            Bullet bulletComp = other.GetComponent<Bullet>();
            if (bulletComp == null) return;
            TakeDamage(bulletComp.damage, transform.position - other.transform.position);
            Destroy(other.gameObject);
        }
        // meleeArea가 플레이어를 타격할 때
        else if (other.CompareTag("Player"))
        {
            Items playerItems = other.GetComponent<Items>();
            if (playerItems == null)
                playerItems = other.GetComponentInParent<Items>();
            if (playerItems != null)
                playerItems.TakeDamage(damage);
        }
    }

    // ── 피격 감지 : Solid Collider (HandGun 등) ────
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Bullet bulletComp = collision.gameObject.GetComponent<Bullet>();
            if (bulletComp == null || bulletComp.isMelee) return;
            TakeDamage(bulletComp.damage, transform.position - collision.transform.position);
            Destroy(collision.gameObject);
        }
    }

    // ── 데미지 공통 처리 ──────────────────────────
    public void TakeDamage(int dmg, Vector3 reactVec)
    {
        if (isDead) return;
        curHealth -= dmg;
        StartCoroutine(onDamage(reactVec, false));
    }

    public void HitByGrenade(Vector3 explosionPos)
    {
        if (isDead) return;
        curHealth -= 100;
        StartCoroutine(onDamage(transform.position - explosionPos, true));
    }

    IEnumerator onDamage(Vector3 reactVec, bool isGrenade)
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
