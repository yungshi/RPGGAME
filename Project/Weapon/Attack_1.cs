using UnityEngine;
using System.Collections;
using System.Data.SqlTypes;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
public class Attack_1 : MonoBehaviour
{
    public bool fdown;
    public bool fhold;
    public bool IsFireDown => fdown;
    public bool IsFireHeld => fhold;
    public bool rDown;
    public bool isFireReady;
    public bool auto = true; // true=연발(꾹 누르면 자동), false=단발(한 번씩)
    float fireDelay;
    bool canAttack = true;
    bool isReload;
    public float attackDelay = 0.2f;
    public string attackStateName = "Attack";
    
    public ItemInteraction itemInteraction;
    
    public Weapon equipWeapon;
    public PlayerControl playerControl;
    public Items items;

    Animator anim;

void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (playerControl == null)
            playerControl = GetComponent<PlayerControl>();
        if (itemInteraction == null)
            itemInteraction = GetComponent<ItemInteraction>();
        if (items == null)
            items = GetComponent<Items>();
    }

    void Update()
    {
        GetInput();
        Attack();
        Reload();
    }
     
     
     
     
    
    
    void GetInput()
    {
        bool canUseInput = equipWeapon != null && equipWeapon.gameObject.activeInHierarchy && canAttack;
        fdown = canUseInput && (Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0));
        fhold = canUseInput && (Input.GetButton("Fire1") || Input.GetMouseButton(0));

        // 재장전: R키(Reload 축) 사용, 공격 쿨타임(canAttack)과 무관하게 동작
        bool hasWeapon = equipWeapon != null && equipWeapon.gameObject.activeInHierarchy;
        rDown = hasWeapon && (Input.GetButtonDown("Reload") || Input.GetKeyDown(KeyCode.R));
    }

void Attack()
    {
        if (equipWeapon == null || !equipWeapon.gameObject.activeInHierarchy)
            return;

        if (equipWeapon.type == Weapon.Type.Range)
        {
            // auto=true 면 누르고 있는 동안 연발(fhold), auto=false 면 누를 때마다 단발(fdown)
            bool fireInput = auto ? fhold : fdown;
            if (fireInput && canAttack && equipWeapon.curAmmo > 0 && !playerControl.isDodge && !itemInteraction.isSwap)
            {
                equipWeapon.Use();
                if (anim != null)
                    anim.SetTrigger("doShot");
                float cooldown = equipWeapon.rate > 0f ? equipWeapon.rate : attackDelay;
                StartCoroutine(FireRateCooldown(cooldown));
            }
        }
        else
        {
            // 근접: 누를 때마다 1회
            if (fdown && canAttack && !playerControl.isDodge && !itemInteraction.isSwap)
            {
                equipWeapon.Use();
                if (anim != null)
                    anim.SetTrigger("doSwing");
                float cooldown = equipWeapon.rate > 0f ? equipWeapon.rate : attackDelay;
                StartCoroutine(AttackCooldown(cooldown));
            }
        }
    }

IEnumerator FireRateCooldown(float delay)
    {
        canAttack = false;
        yield return new WaitForSeconds(delay);
        canAttack = true;
    }


void Reload()
    {
        if (equipWeapon == null)
            return;

        if (equipWeapon.type == Weapon.Type.Melee)
            return;

        if (items == null || items.ammo == 0)
            return;

        if (playerControl == null || itemInteraction == null)
            return;

        if (rDown && !isReload && !playerControl.isJump && !playerControl.isDodge && !itemInteraction.isSwap)
        {
            anim.SetTrigger("doReload");
            isReload = true;
            playerControl.isReload = true;
            Invoke("ReloadOut", 2f);
        }
    }

void ReloadOut()
    {
        int reAmmo = items.ammo < equipWeapon.maxAmmo ? items.ammo : equipWeapon.maxAmmo;
        equipWeapon.curAmmo = reAmmo;
        items.ammo -= reAmmo;
        isReload = false;
        if (playerControl != null)
            playerControl.isReload = false;
    }

    IEnumerator AttackCooldown(float delay)
    {
        canAttack = false;
        yield return new WaitForSeconds(delay);

        if (anim != null && !string.IsNullOrEmpty(attackStateName))
        {
            while (anim.IsInTransition(0) || (anim.GetCurrentAnimatorStateInfo(0).IsName(attackStateName) && anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f))
            {
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.5f);
        canAttack = true;
    }
}
