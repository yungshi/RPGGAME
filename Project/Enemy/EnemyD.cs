using System.Collections;
using UnityEngine;
using UnityEngine.AI;
//보스 스킬 스크립트

[RequireComponent(typeof(Enemy))]
public class EnemyD : MonoBehaviour
{
    Enemy enemy;
    NavMeshAgent nav;
    bool busy;

    [Header("프리팹 (비우면 에디터에서 자동 로드)")]
    public GameObject freezeCirclePrefab;   // Freeze circle.prefab(빙결이펙트)
    public GameObject electroSlashPrefab;   // Electro slash.prefab(전기이펙트0

    [Header("탐지 / 이동")]
    public LayerMask playerMask;            //플레이어 탐지
    
    public float moveSpeed   = 3.5f;        // 이동 속도
    public float detectRange = 18f;         // 플레이어 탐지 범위
    public float meleeRange  = 3.5f;        // 기본 공격 범위
    public float tauntRange  = 12f;         //도발 거리

    [Header("Taunt (도발 점프)")]
    public float tauntTelegraph = 1.5f;     // 에상 착륙지점 표시
    public float freezeScale    = 3f;       // Freeze circle 스케일 (3,3,3)
    public float freezeRadius   = 3f;       // 착륙 범위
    public float jumpDuration   = 1.0f;     // 점프 총 시간
    public float jumpHeight     = 4f;       // 점프 최고 높이
    public int   tauntDamage    = 25;       // 착륙 시 타겟에게 주는 데미지
    public float freezeLifetime = 2.5f;     // Freeze circle 유지 시간

    [Header("기본 공격 (Electro slash)")]
    public Vector3 slashScale = new Vector3(6f, 6f, 6f);   // 전기 슬래시 이펙트 스케일
    public float slashForward   = 1.5f;     // 보스 이팩트 생성 거리
    public float slashUp        = 1.0f;     // 슬래시 높이
    public float slashRange     = 3.5f;     // 슬래시 범위
    public float slashHalfAngle = 60f;      // 부채꼴 각도
    public int   slashDamage    = 15;       // 슬래시 데미지
    public float slashLifetime  = 1.5f;     // 슬래시 이펙트 유지 시간

    [Header("쿨다운")]
    public float attackCooldown = 2.5f;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        nav   = GetComponent<NavMeshAgent>();

      
        if (nav != null)
        {
            nav.enabled          = true;
            nav.updatePosition   = true;
            nav.updateRotation   = false;
            nav.speed            = moveSpeed;
            nav.angularSpeed     = 360f;
            nav.acceleration     = 12f;
            nav.stoppingDistance = Mathf.Max(0f, meleeRange - 1f);

            
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

       
        if (dist <= meleeRange)
            StartCoroutine(BasicAttack());
        else if (dist > tauntRange && dist <= detectRange)
            StartCoroutine(TauntAttack());
    }

   
    IEnumerator TauntAttack()//도발 공격 및 데미지
    {
        busy = true;
        if (enemy == null || enemy.isDead || enemy.Target == null)
        {
            busy = false;
            yield break;
        }

        FaceTarget();
        SetBoolSafe("IsWalk", false);
        if (nav != null && nav.enabled && nav.isOnNavMesh) nav.isStopped = true; 

       
        Vector3 landing = enemy.Target.position;
        landing.y = transform.position.y;
        Vector3 markCenter = GroundPoint(landing);  

       
        GameObject circle = null;
        if (freezeCirclePrefab != null)
        {
            circle = Instantiate(freezeCirclePrefab, markCenter, Quaternion.identity);
            circle.transform.localScale = Vector3.one * freezeScale;
        }

      
        yield return new WaitForSeconds(tauntTelegraph);
        if (enemy == null || enemy.isDead || enemy.Target == null)
        {
            if (circle != null) Destroy(circle, freezeLifetime);
            busy = false;
            yield break;
        }

       
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
            pos.y += jumpHeight * 4f * k * (1f - k); 
            transform.position = pos;
            yield return null;
        }

       
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

 
    IEnumerator BasicAttack()
    {
        busy = true;
        if (nav != null && nav.enabled && nav.isOnNavMesh) nav.isStopped = true; 
        FaceTarget();
        SetBoolSafe("IsAttack", true);

        yield return new WaitForSeconds(0.25f);
        if (enemy == null || enemy.isDead || enemy.Target == null)
        {
            SetBoolSafe("IsAttack", false);
            busy = false;
            yield break;
        }


        if (electroSlashPrefab != null)
        {
            Vector3 pos = transform.position + Vector3.up * slashUp + transform.forward * slashForward;
            GameObject fx = Instantiate(electroSlashPrefab, pos, Quaternion.LookRotation(transform.forward));
            fx.transform.localScale = slashScale;  
            Destroy(fx, slashLifetime);
        }

       
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

   
    void FaceTarget()
    {
        if (enemy.Target == null) return;
        Vector3 dir = enemy.Target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    
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

 
    Vector3 GroundPoint(Vector3 p)
    {
        if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out RaycastHit h, 12f,
                            ~PlayerMaskValue, QueryTriggerInteraction.Ignore))
            return h.point + Vector3.up * 0.05f;
        return p;
    }
}
