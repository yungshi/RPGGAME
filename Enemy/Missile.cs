using UnityEngine;

/// <summary>
/// Enemy C가 발사하는 호밍 미사일
/// - Enemy.Attack() 에서 target / damage 를 주입받습니다.
/// - 플레이어를 추적(호밍)하며 충돌 시 Items.TakeDamage() 로 피해를 줍니다.
/// </summary>
public class Missile : MonoBehaviour
{
    [Header("기본 설정")]
    public int   damage        = 20;   // 플레이어에게 주는 데미지
    public float speed         = 12f;  // 이동 속도
    public float homingStrength = 3f;  // 호밍 강도 (클수록 급선회)
    
    public bool  homing        = true; // false 면 유도 없이 직진(부채꼴 다중발사용)
public float lifetime      = 8f;   // 최대 생존 시간 (초)

    [Header("목표")]
    public Transform target;           // Enemy.Attack() 에서 주입 (플레이어 Transform)
    [Header("도트(빨간 총알)")]
    public bool isDot = false;
    public int dotTicks = 4;
    public float dotInterval = 0.5f;

    Rigidbody rigid;
    bool debugRotationLogged = false;

    // ── 초기화 ─────────────────────────────────────
    void Awake()
    {
        rigid = GetComponent<Rigidbody>();

        // Rigidbody가 있으면 물리 간섭을 막기 위해 kinematic 처리
        if (rigid != null)
        {
            rigid.isKinematic = true;
            rigid.useGravity  = false;
        }
    }

    void Start()
    {
        // 미사일이 생성될 때 자식의 로컬 회전만 초기화하고, 루트 회전은 유지한다.
        Quaternion rootRotation = transform.localRotation;
        ResetChildRotationRecursive(transform);
        transform.localRotation = rootRotation;

        // 유도(homing) 미사일만 목표를 자동 탐색하고 목표를 향해 초기 정렬한다.
        // (homing=false 인 부채꼴 직진탄은 생성 시 부여된 부채꼴 방향을 그대로 유지)
        if (homing)
        {
            if (target == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
            }

            if (target != null)
            {
                Vector3 dirToTarget = (target.position - transform.position).normalized;
                if (dirToTarget.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(dirToTarget) * Quaternion.Euler(0, -90f, 0);
            }
        }

        // 수명 제한 (벽·바닥에 안 닿아도 자동 제거)
        if (isDot)
            foreach (var rend in GetComponentsInChildren<Renderer>())
                foreach (var mat in rend.materials)
                    mat.color = Color.red;
        Destroy(gameObject, lifetime);
    }

    void ResetChildRotationRecursive(Transform t)
    {
        foreach (Transform child in t)
        {
            child.localRotation = Quaternion.identity;
            ResetChildRotationRecursive(child);
        }
    }

    // ── 매 프레임 이동·호밍 ────────────────────────
    void Update()
    {
        // 호밍 : 목표 방향으로 서서히 회전 (homing=true 일 때만)
        if (homing && target != null)
        {
            Vector3    dir     = (target.position - transform.position).normalized;
            Quaternion lookRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, -90f, 0);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, lookRot, homingStrength * Time.deltaTime);
        }

        // 항상 X 축 방향(+right)으로 전진 (모델이 X축을 앞 방향으로 사용함)
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    // ── 충돌 감지 (Trigger) ────────────────────────
    void OnTriggerEnter(Collider other)
    {
        ProcessHit(other.gameObject);
    }

    // ── 충돌 감지 (Solid Collider) ─────────────────
    void OnCollisionEnter(Collision collision)
    {
        ProcessHit(collision.gameObject);
    }

    // ── 공통 충돌 처리 ─────────────────────────────
    void ProcessHit(GameObject other)
    {
        if (other.CompareTag("Player"))
        {
            // 플레이어의 Items 컴포넌트에 데미지 전달
            Items items = other.GetComponent<Items>();
            if (items == null)
                items = other.GetComponentInParent<Items>();

            if (isDot)
            {
                PlayerStatus ps = other.GetComponent<PlayerStatus>();
                if (ps == null) ps = other.GetComponentInParent<PlayerStatus>();
                if (ps != null) ps.ApplyDot(damage, dotTicks, dotInterval);
                else if (items != null) items.TakeDamage(damage);
            }
            else if (items != null)
                items.TakeDamage(damage);

            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Floor"))
        {
            Destroy(gameObject);
        }
        // Enemy 자신에게 맞는 것 방지 (같은 태그면 무시)
        // else : 그 외 오브젝트는 통과
    }
}
