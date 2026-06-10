using System.Collections;
using UnityEngine;

// 몬스터 A/B/C 확률(불규칙) 스킬. Enemy 와 같은 오브젝트에 부착한다.
// Start()에서 Enemy.isAttack 을 고정해 내장 Attack() 을 억제하고,
// 이 스크립트가 패턴을 확률로 골라 대신 실행한다.
// 기존 스크립트(Enemy / PlayerControl / PlayerRotation / Attack_1 등)는 수정하지 않으며,
// 스턴·속박은 PlayerStatus, DOT탄은 Missile 의 기존 기능을 호출만 한다.
[RequireComponent(typeof(Enemy))]
public class EnemySkill : MonoBehaviour
{
        Enemy enemy;
    bool busy;

    [Header("A 회복 — healDuration초 동안 최대체력의 healPercent 만큼 점진 회복")]
    [Range(0f, 1f)] public float healPercent = 0.25f;  // 회복 비율 (0~1, 예: 0.25 → 25%)
    public float      healDuration = 3f;               // 회복 시간 T (초)
    public GameObject healCirclePrefab;                // Healing circle.prefab (비우면 에디터 자동 로드)

    [Header("탐지")]
    public LayerMask playerMask;                 // 비우면 자동으로 "Player" 레이어 사용
    public float meleeRange = 3f;            // A/B 근접 탐지 거리

    [Header("A 패턴 확률 (기본 / 2연타 / 회복)")]
    public float aBasic = 1f, aDouble = 1f, aHeal = 1f;

    [Header("B 패턴 확률 (기본 / 스턴 / 속박)")]
    public float bBasic = 1f, bStun = 1f, bBind = 1f;
    public float stunDuration = 1.5f;
    public float bindDuration = 2f;

    [Header("C 패턴 확률 (기본 / DOT탄 / 다중발사)")]
    public float cBasic = 1f, cDot = 1f, cMulti = 1f;
    public int   multiCount  = 3;     // 다중발사 투사체 개수
    public float multiSpread = 15f;   // 발사 부채꼴 각도(±도)
    public int   dotTicks    = 4;     // DOT 횟수
    public float dotInterval = 0.5f;  // DOT 간격(초)

    void Awake()
    {
        enemy = GetComponent<Enemy>();
#if UNITY_EDITOR
        if (healCirclePrefab == null)
            healCirclePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Hovl Studio/Magic effects pack/Prefabs/Magic circles/Healing circle.prefab");
#endif
    }

    void Start()
    {
        // 내장 Attack() 억제: 이 스크립트가 공격을 담당한다.
        if (enemy != null) enemy.isAttack = true;
    }

    // ── 탐지 → 스킬 시작 ───────────────────────────
    void FixedUpdate()
    {
        if (busy || enemy == null || enemy.isDead) return;
        if (enemy.enemyType == Enemy.Type.D) return;
        if (enemy.anim != null) enemy.anim.SetBool("IsWalk", enemy.isChase);
        if (enemy.Target == null) return;

        bool inRange;
        if (enemy.enemyType == Enemy.Type.C)
        {
            // C(원거리): 기존 전방 SphereCast 유지
            int mask = playerMask.value != 0 ? playerMask.value : LayerMask.GetMask("Player");
            inRange = Physics.SphereCastAll(
                transform.position, 0.5f, transform.forward, 25f, mask).Length > 0;
        }
        else
        {
            // A/B(근접): 거리 기반 탐지(방향·근거리 사각지대 의존 제거)
            inRange = Vector3.Distance(transform.position, enemy.Target.position) <= meleeRange;
        }

        if (inRange) StartCoroutine(DoSkill());
    }

    IEnumerator DoSkill()
    {
        busy = true;
        enemy.isChase = false;
        if (enemy.anim != null) enemy.anim.SetBool("IsAttack", true);

        switch (enemy.enemyType)
        {
            case Enemy.Type.A: yield return PatternA(); break;
            case Enemy.Type.B: yield return PatternB(); break;
            case Enemy.Type.C: yield return PatternC(); break;
        }

        if (enemy.anim != null) enemy.anim.SetBool("IsAttack", false);
        enemy.isChase = true;
        busy = false;
    }

    // ── A : 기본 / 2연타 / 회복(초록 깜빡임) ─────────
    IEnumerator PatternA()
    {
        switch (Pick(aBasic, aDouble, aHeal))
        {
            case 0: // 기본
                yield return new WaitForSeconds(0.2f);
                MeleeHit();
                break;
            case 1: // 2연타
                yield return new WaitForSeconds(0.2f);
                MeleeHit();
                yield return new WaitForSeconds(0.3f);
                MeleeHit();
                break;
            default: // 회복
                if (enemy.anim != null) enemy.anim.SetBool("IsAttack", false);
                yield return GreenBlinkHeal();
                break;
        }
        yield return new WaitForSeconds(2f);
    }

    // ── B : 기본 / 스턴 / 속박 (흰검 반짝은 PlayerStatus 담당) ──
    IEnumerator PatternB()
    {
        yield return new WaitForSeconds(0.2f);
        switch (Pick(bBasic, bStun, bBind))
        {
            case 0: // 기본
                MeleeHit();
                break;
            case 1: // 스턴 1.5초
                MeleeHit();
                { PlayerStatus ps = GetPlayerStatus(); if (ps != null) ps.ApplyStun(stunDuration); }
                break;
            default: // 속박 2초
                MeleeHit();
                { PlayerStatus ps = GetPlayerStatus(); if (ps != null) ps.ApplyBind(bindDuration); }
                break;
        }
        yield return new WaitForSeconds(2f);
    }

    // ── C : 기본 미사일 / 빨간 DOT탄 / 다중발사 ──────
    IEnumerator PatternC()
    {
        yield return new WaitForSeconds(0.5f);
        switch (Pick(cBasic, cDot, cMulti))
        {
            case 0: // 기본 1발
                SpawnMissile(0f, false);
                break;
            case 1: // 빨간 DOT탄 1발
                SpawnMissile(0f, true);
                break;
            default: // 다중발사
                int n = Mathf.Max(1, multiCount);
                for (int i = 0; i < n; i++)
                {
                    float t = n == 1 ? 0f : (i / (float)(n - 1) - 0.5f) * 2f; // -1..1
                    SpawnMissile(t * multiSpread, false, false); // 부채꼴 직진(비유도)
                }
                break;
        }
        yield return new WaitForSeconds(2f);
    }

    // ── 근접 타격 (Enemy 와 동일 기준: 거리 3 이내) ──
    void MeleeHit()
    {
        if (enemy.Target == null) return;
        if (Vector3.Distance(transform.position, enemy.Target.position) > 3f) return;

        Items items = enemy.Target.GetComponent<Items>();
        if (items == null) items = enemy.Target.GetComponentInParent<Items>();
        if (items != null) items.TakeDamage(enemy.damage);
    }

    // ── 미사일 발사 (Enemy 와 동일 생성 로직, bullet 프리팹 재사용) ──
    void SpawnMissile(float yawOffset, bool isDot, bool homing = true)
    {
        if (enemy.bullet == null) return;

        Vector3 spawnPos = transform.position + Vector3.up * 1.0f + transform.forward * 0.8f;

        Vector3 dir = enemy.Target != null
            ? (enemy.Target.position - spawnPos).normalized
            : transform.forward;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir = Quaternion.Euler(0f, yawOffset, 0f) * dir;

        Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, -90f, 0f);

        GameObject inst = Instantiate(enemy.bullet, spawnPos, rot);
        inst.transform.SetParent(null);

        Missile m = inst.GetComponent<Missile>();
        if (m != null)
        {
            m.target = enemy.Target;
            m.damage = enemy.damage;
            m.isDot  = isDot;
            m.homing = homing;     // 다중발사(부채꼴)는 false → 유도 없이 직진
            if (isDot) { m.dotTicks = dotTicks; m.dotInterval = dotInterval; }
        }
        else
        {
            Rigidbody rb = inst.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = dir * 20f;
        }
    }

    // ── 회복 + 몸 초록 깜빡임 ───────────────────────
    // ── 회복 : 바닥에 힐 이펙트 생성 + Heal Percent 만큼 회복될 때까지 점진 회복(그동안 이동 정지) ──
    IEnumerator GreenBlinkHeal()
    {
        int healAmount   = Mathf.RoundToInt(enemy.maxHealth * healPercent);
        int startHealth  = enemy.curHealth;
        int targetHealth = Mathf.Min(enemy.maxHealth, startHealth + healAmount);

        // 바닥(발밑)에 힐 이펙트 생성 — 스케일 (1,1,1)
        GameObject fx = null;
        if (healCirclePrefab != null)
        {
            float groundY = enemy.boxCollider != null ? enemy.boxCollider.bounds.min.y : transform.position.y;
            Vector3 groundPos = new Vector3(transform.position.x, groundY + 0.05f, transform.position.z);
            fx = Instantiate(healCirclePrefab, groundPos, Quaternion.identity);
            fx.transform.localScale = Vector3.one;
        }

        // DoSkill 이 isChase=false 를 유지하므로 이동은 정지된 상태. 목표치까지 점진 회복
        float t = 0f;
        float dur = Mathf.Max(0.01f, healDuration);
        while (enemy.curHealth < targetHealth)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            enemy.curHealth = Mathf.Min(targetHealth, startHealth + Mathf.RoundToInt(healAmount * k));
            SetMeshColor(Color.Lerp(Color.white, Color.green, Mathf.PingPong(t * 4f, 1f)));
            if (k >= 1f) break;
            yield return null;
        }
        enemy.curHealth = targetHealth;
        SetMeshColor(Color.white);

        if (fx != null) Destroy(fx);
    }

    void SetMeshColor(Color c)
    {
        if (enemy.meshs == null) return;
        foreach (MeshRenderer mesh in enemy.meshs)
            if (mesh != null) mesh.material.color = c;
    }

    // ── 플레이어 상태이상 허브 찾기 ─────────────────
    PlayerStatus GetPlayerStatus()
    {
        if (enemy.Target == null) return null;
        PlayerStatus ps = enemy.Target.GetComponent<PlayerStatus>();
        if (ps == null) ps = enemy.Target.GetComponentInParent<PlayerStatus>();
        if (ps == null) ps = enemy.Target.GetComponentInChildren<PlayerStatus>();
        return ps;
    }

    // ── 가중치 확률 선택 (0/1/2) ────────────────────
    int Pick(float w0, float w1, float w2)
    {
        float sum = w0 + w1 + w2;
        if (sum <= 0f) return 0;
        float r = Random.value * sum;
        if (r < w0) return 0;
        if (r < w0 + w1) return 1;
        return 2;
    }
}
