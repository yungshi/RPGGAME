using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Enemy(Type.D) 전용 보스 컨트롤러.
// Enemy.cs / EnemySkill.cs 는 Type.D 를 처리하지 않으므로(둘 다 제외) 이 스크립트가 D의 행동을 담당한다.
// Enemy 와 같은 오브젝트에 부착한다.
//  1) Taunt(도발 점프) : 점프 시작 시 예상 착륙 지점(현재 플레이어 위치)에 Freeze circle(스케일 3,3,3) 생성 →
//                        지면 착륙 시 원 내부에 플레이어가 있으면 데미지
//  2) 기본 공격        : 보스 기준 전방에 Electro slash 생성 → 슬래시 범위에 플레이어가 있으면(맞으면) 체력 감소
// 플레이어 피해는 기존 방식(Items.TakeDamage)을 그대로 사용한다.
[RequireComponent(typeof(Enemy))]
public class EnemyD : MonoBehaviour
{
    Enemy enemy;
    NavMeshAgent nav;
    bool busy;

    [Header("프리팹 (비우면 에디터에서 자동 로드)")]
    public GameObject freezeCirclePrefab;   // Freeze circle.prefab
    public GameObject electroSlashPrefab;   // Electro slash.prefab

    [Header("탐지 / 이동")]
    public LayerMask playerMask;            // 비우면 자동으로 "Player" 레이어 사용
    
    public float moveSpeed   = 3.5f;        // 추격 이동 속도
public float detectRange = 18f;         // 플레이어 인식 거리
    public float meleeRange  = 3.5f;        // 기본 공격(슬래시) 사정거리
    public float tauntRange  = 12f;         // Taunt(점프) 가능 거리

    [Header("Taunt (도발 점프)")]
    public float tauntTelegraph = 1.5f;     // 점프 전 예상 착륙 원을 미리 보여주는 시간(초)
    public float freezeScale    = 3f;       // Freeze circle 스케일 (3,3,3)
    public float freezeRadius   = 3f;       // 착륙 데미지 판정 반경(스케일과 동일 기준)
    public float jumpDuration   = 1.0f;     // 점프 체공 시간
    public float jumpHeight     = 4f;       // 점프 포물선 최고 높이
    public int   tauntDamage    = 25;       // 착륙 시 데미지
    public float freezeLifetime = 2.5f;     // Freeze circle 잔존 시간

    [Header("기본 공격 (Electro slash)")]
    public Vector3 slashScale = new Vector3(6f, 6f, 6f);   // 전기 슬래시 이펙트 스케일
    public float slashForward   = 1.5f;     // 보스 앞 슬래시 생성 거리
    public float slashUp        = 1.0f;     // 슬래시 생성 높이
    public float slashRange     = 3.5f;     // 슬래시 피해 반경
    public float slashHalfAngle = 60f;      // 전방 부채꼴 반각(도)
    public int   slashDamage    = 15;       // 슬래시 데미지
    public float slashLifetime  = 1.5f;     // 슬래시 이펙트 잔존 시간

    [Header("쿨다운")]
    public float attackCooldown = 2.5f;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        nav   = GetComponent<NavMeshAgent>();

        // D 는 이동(위치)을 NavMeshAgent 에 맡기고, 회전은 FaceTarget 가 담당한다.
        if (nav != null)
        {
            nav.enabled          = true;
            nav.updatePosition   = true;
            nav.updateRotation   = false;
            nav.speed            = moveSpeed;
            nav.angularSpeed     = 360f;
            nav.acceleration     = 12f;
            nav.stoppingDistance = Mathf.Max(0f, meleeRange - 1f);

            // 시작 시 NavMesh 위가 아니면 가장 가까운 NavMesh 지점으로 스냅
            if (!nav.isOnNavMesh &&
                NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                nav.Warp(hit.position);
        }
#if UNITY_EDITOR
        if (freezeCirclePrefab == null)
            freezeCirclePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Hovl Studio/Magic effects pack/Prefabs/Magic circles/Freeze circle.prefab");
        if (electroSlashPrefab == null)
            electroSlashPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Hovl Studio/Magic effects pack/Prefabs/Slash effects/Electro slash.prefab");
#endif
    }

    int PlayerMaskValue => playerMask.value != 0 ? playerMask.value : LayerMask.GetMask("Player");

    void Update()
    {
        if (busy || enemy == null || enemy.isDead || enemy.Target == null) return;

        float dist = Vector3.Distance(transform.position, enemy.Target.position);

        // 추격 : NavMeshAgent 로 이동(위치), 회전은 FaceTarget
        if (dist > meleeRange && dist <= detectRange)
        {
            FaceTarget();
            if (nav != null && nav.enabled && nav.isOnNavMesh)
            {
                nav.isStopped = false;
                nav.SetDestination(enemy.Target.position);
            }
            SetBoolSafe("IsWalk", true);
        }
        else
        {
            if (nav != null && nav.enabled && nav.isOnNavMesh)
                nav.isStopped = true;
            SetBoolSafe("IsWalk", false);
        }

        // 공격 선택: 근접이면 기본 공격, Taunt 사거리 밖이면 점프, 그 외에는 추격
        if (dist <= meleeRange)
            StartCoroutine(BasicAttack());
        else if (dist > tauntRange && dist <= detectRange)
            StartCoroutine(TauntAttack());
    }

    // ── Taunt : 예상 착륙 지점으로 점프 → 착륙 시 Freeze circle 내부 플레이어에게 데미지 ──
    IEnumerator TauntAttack()
    {
        busy = true;
        if (enemy == null || enemy.isDead || enemy.Target == null)
        {
            busy = false;
            yield break;
        }

        FaceTarget();
        SetBoolSafe("IsWalk", false);
        if (nav != null && nav.enabled && nav.isOnNavMesh) nav.isStopped = true; // 예고 동안 정지

        // 예상 착륙 지점 = 예고 시작 시점의 플레이어 위치(이후 고정 → 플레이어가 피할 수 있음)
        Vector3 landing = enemy.Target.position;
        landing.y = transform.position.y;
        Vector3 markCenter = GroundPoint(landing);   // 예고 원 = 착륙 데미지 판정 중심(동일)

        // 1) 예고: 점프 전에 미리 착륙 예상 지점에 Freeze circle 표시
        GameObject circle = null;
        if (freezeCirclePrefab != null)
        {
            circle = Instantiate(freezeCirclePrefab, markCenter, Quaternion.identity);
            circle.transform.localScale = Vector3.one * freezeScale;
        }

        // 2) 예고 시간 대기(이 동안 플레이어가 원 밖으로 회피 가능)
        yield return new WaitForSeconds(tauntTelegraph);
        if (enemy == null || enemy.isDead || enemy.Target == null)
        {
            if (circle != null) Destroy(circle, freezeLifetime);
            busy = false;
            yield break;
        }

        // 3) 점프 — 물리/NavMesh 간섭 방지
        bool wasKinematic = enemy.rigid != null && enemy.rigid.isKinematic;
        if (enemy.rigid != null) enemy.rigid.isKinematic = true;
        bool navWasEnabled = nav != null && nav.enabled;
        if (navWasEnabled) nav.enabled = false;

        Vector3 start = transform.position;
        float t = 0f;
        float dur = Mathf.Max(0.1f, jumpDuration);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            Vector3 pos = Vector3.Lerp(start, landing, k);
            pos.y += jumpHeight * 4f * k * (1f - k);   // 0→1→0 포물선
            transform.position = pos;
            yield return null;
        }

        // 4) 착륙 지점을 NavMesh 위로 보정 후 Agent 복귀
        Vector3 landPos = landing;
        if (NavMesh.SamplePosition(landing, out NavMeshHit lh, 10f, NavMesh.AllAreas))
            landPos = lh.position;
        transform.position = landPos;
        if (navWasEnabled && nav != null)
        {
            nav.enabled = true;
            nav.Warp(landPos);
        }
        if (enemy.rigid != null) enemy.rigid.isKinematic = wasKinematic;

        // 5) 착륙 판정: 예고된 원(markCenter) 안에 아직 플레이어가 있으면 데미지(피했으면 안 맞음)
        if (enemy != null && !enemy.isDead && enemy.Target != null)
        {
            Collider[] hits = Physics.OverlapSphere(markCenter, freezeRadius, PlayerMaskValue);
            if (hits.Length > 0)
                DamagePlayer(tauntDamage);
        }

        if (circle != null) Destroy(circle, freezeLifetime);

        yield return new WaitForSeconds(attackCooldown);
        busy = false;
    }

    // ── 기본 공격 : 보스 기준 Electro slash 생성 → 슬래시 범위 플레이어에게 데미지 ──
    IEnumerator BasicAttack()
    {
        busy = true;
        if (nav != null && nav.enabled && nav.isOnNavMesh) nav.isStopped = true; // 공격 중 정지
        FaceTarget();
        SetBoolSafe("IsAttack", true);

        yield return new WaitForSeconds(0.25f);
        if (enemy == null || enemy.isDead || enemy.Target == null)
        {
            SetBoolSafe("IsAttack", false);
            busy = false;
            yield break;
        }

        // 보스 기준 전방에 슬래시 이펙트 생성
        if (electroSlashPrefab != null)
        {
            Vector3 pos = transform.position + Vector3.up * slashUp + transform.forward * slashForward;
            GameObject fx = Instantiate(electroSlashPrefab, pos, Quaternion.LookRotation(transform.forward));
            fx.transform.localScale = slashScale;   // 전기 슬래시 스케일(기본 6,6,6)
            Destroy(fx, slashLifetime);
        }

        // 슬래시 범위(전방 부채꼴) 안에 플레이어가 있으면(맞으면) 체력 감소
        if (enemy.Target != null)
        {
            Vector3 to = enemy.Target.position - transform.position;
            to.y = 0f;
            if (to.magnitude <= slashRange && Vector3.Angle(transform.forward, to) <= slashHalfAngle)
                DamagePlayer(slashDamage);
        }

        SetBoolSafe("IsAttack", false);
        yield return new WaitForSeconds(attackCooldown);
        busy = false;
    }

    // ── 유틸 ──────────────────────────────────────────
    void FaceTarget()
    {
        if (enemy.Target == null) return;
        Vector3 dir = enemy.Target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    // 애니메이터에 해당 Bool 파라미터가 있을 때만 설정 (없는 컨트롤러에서 경고 방지)
    void SetBoolSafe(string n, bool v)
    {
        if (enemy.anim == null) return;
        foreach (var p in enemy.anim.parameters)
            if (p.type == AnimatorControllerParameterType.Bool && p.name == n)
            {
                enemy.anim.SetBool(n, v);
                return;
            }
    }


    void DamagePlayer(int dmg)
    {
        if (enemy.Target == null) return;
        Items items = enemy.Target.GetComponent<Items>();
        if (items == null) items = enemy.Target.GetComponentInParent<Items>();
        if (items != null) items.TakeDamage(dmg);
    }

    // 착륙 지점의 지면 높이 보정(플레이어 레이어 제외 후 아래로 레이캐스트)
    Vector3 GroundPoint(Vector3 p)
    {
        if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out RaycastHit h, 12f,
                            ~PlayerMaskValue, QueryTriggerInteraction.Ignore))
            return h.point + Vector3.up * 0.05f;
        return p;
    }
}
