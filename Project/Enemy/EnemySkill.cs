using System.Collections;
using UnityEngine;


[RequireComponent(typeof(Enemy))]
public class EnemySkill : MonoBehaviour
{
        Enemy enemy;
    bool busy;

    [Header("A 회복 — healDuration초 동안 최대체력의 healPercent 만큼 점진 회복")]
    [Range(0f, 1f)] public float healPercent = 0.25f; 
    public float      healDuration = 3f;              
    public GameObject healCirclePrefab;               

    [Header("탐지")]
    public LayerMask playerMask;                
    public float meleeRange = 3f;           

    [Header("A 패턴 확률 (기본 / 2연타 / 회복)")]
    public float aBasic = 1f, aDouble = 1f, aHeal = 1f;

    [Header("B 패턴 확률 (기본 / 스턴 / 속박)")]
    public float bBasic = 1f, bStun = 1f, bBind = 1f;
    public float stunDuration = 1.5f;
    public float bindDuration = 2f;

    [Header("C 패턴 확률 (기본 / DOT탄 / 다중발사)")]
    public float cBasic = 1f, cDot = 1f, cMulti = 1f;
    public int   multiCount  = 3;     
    public float multiSpread = 15f;  
    public int   dotTicks    = 4;    
    public float dotInterval = 0.5f; 

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
       
        if (enemy != null) enemy.isAttack = true;
    }

    
    void FixedUpdate()
    {
        if (busy || enemy == null || enemy.isDead) return;
        if (enemy.enemyType == Enemy.Type.D) return;
        if (enemy.anim != null) enemy.anim.SetBool("IsWalk", enemy.isChase);
        if (enemy.Target == null) return;

        bool inRange;
        if (enemy.enemyType == Enemy.Type.C)
        {
            
            int mask = playerMask.value != 0 ? playerMask.value : LayerMask.GetMask("Player");
            inRange = Physics.SphereCastAll(
                transform.position, 0.5f, transform.forward, 25f, mask).Length > 0;
        }
        else
        {
           
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

   
    IEnumerator PatternA()
    {
        switch (Pick(aBasic, aDouble, aHeal))
        {
            case 0:
                yield return new WaitForSeconds(0.2f);
                MeleeHit();
                break;
            case 1: 
                yield return new WaitForSeconds(0.2f);
                MeleeHit();
                yield return new WaitForSeconds(0.3f);
                MeleeHit();
                break;
            default: 
                if (enemy.anim != null) enemy.anim.SetBool("IsAttack", false);
                yield return GreenBlinkHeal();
                break;
        }
        yield return new WaitForSeconds(2f);
    }


    IEnumerator PatternB()
    {
        yield return new WaitForSeconds(0.2f);
        switch (Pick(bBasic, bStun, bBind))
        {
            case 0: 
                MeleeHit();
                break;
            case 1: 
                MeleeHit();
                { PlayerStatus ps = GetPlayerStatus(); if (ps != null) ps.ApplyStun(stunDuration); }
                break;
            default:
                MeleeHit();
                { PlayerStatus ps = GetPlayerStatus(); if (ps != null) ps.ApplyBind(bindDuration); }
                break;
        }
        yield return new WaitForSeconds(2f);
    }

    
    IEnumerator PatternC()
    {
        yield return new WaitForSeconds(0.5f);
        switch (Pick(cBasic, cDot, cMulti))
        {
            case 0: 
                SpawnMissile(0f, false);
                break;
            case 1: 
                SpawnMissile(0f, true);
                break;
            default:
                int n = Mathf.Max(1, multiCount);
                for (int i = 0; i < n; i++)
                {
                    float t = n == 1 ? 0f : (i / (float)(n - 1) - 0.5f) * 2f;
                    SpawnMissile(t * multiSpread, false, false);
                }
                break;
        }
        yield return new WaitForSeconds(2f);
    }

   
    void MeleeHit()
    {
        if (enemy.Target == null) return;
        if (Vector3.Distance(transform.position, enemy.Target.position) > 3f) return;

        Items items = enemy.Target.GetComponent<Items>();
        if (items == null) items = enemy.Target.GetComponentInParent<Items>();
        if (items != null) items.TakeDamage(enemy.damage);
    }

    
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
            m.homing = homing;  
            if (isDot) { m.dotTicks = dotTicks; m.dotInterval = dotInterval; }
        }
        else
        {
            Rigidbody rb = inst.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = dir * 20f;
        }
    }

   
    IEnumerator GreenBlinkHeal()
    {
        int healAmount   = Mathf.RoundToInt(enemy.maxHealth * healPercent);
        int startHealth  = enemy.curHealth;
        int targetHealth = Mathf.Min(enemy.maxHealth, startHealth + healAmount);

        
        GameObject fx = null;
        if (healCirclePrefab != null)
        {
            float groundY = enemy.boxCollider != null ? enemy.boxCollider.bounds.min.y : transform.position.y;
            Vector3 groundPos = new Vector3(transform.position.x, groundY + 0.05f, transform.position.z);
            fx = Instantiate(healCirclePrefab, groundPos, Quaternion.identity);
            fx.transform.localScale = Vector3.one;
        }

       
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

   
    PlayerStatus GetPlayerStatus()
    {
        if (enemy.Target == null) return null;
        PlayerStatus ps = enemy.Target.GetComponent<PlayerStatus>();
        if (ps == null) ps = enemy.Target.GetComponentInParent<PlayerStatus>();
        if (ps == null) ps = enemy.Target.GetComponentInChildren<PlayerStatus>();
        return ps;
    }

   
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
