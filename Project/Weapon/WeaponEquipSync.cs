using UnityEngine;


public class WeaponEquipSync : MonoBehaviour
{
    [Header("참조 연결 (비워두면 자동 탐색)")]
    public ItemInteraction itemInteraction;
    public Attack_1 attack1;

    void Awake()//자동연결
    {
        
        if (itemInteraction == null)
            itemInteraction = GetComponent<ItemInteraction>();
        if (attack1 == null)
            attack1 = GetComponent<Attack_1>();

        if (itemInteraction == null)
            Debug.LogWarning("[WeaponEquipSync] ItemInteraction 을 찾지 못했습니다. Inspector 에서 직접 연결하세요.");
        if (attack1 == null)
            Debug.LogWarning("[WeaponEquipSync] Attack_1 을 찾지 못했습니다. Inspector 에서 직접 연결하세요.");
    }

    void Update()
    {
        if (itemInteraction == null || attack1 == null)
            return;

        SyncEquippedWeapon();
    }

    
    void SyncEquippedWeapon()
    {
        if (itemInteraction.weapons == null)
            return;

        Weapon activeWeapon = null;

        foreach (var weaponObj in itemInteraction.weapons)
        {
            if (weaponObj != null && weaponObj.activeInHierarchy)
            {
                Weapon w = weaponObj.GetComponent<Weapon>();
                if (w != null)
                {
                    activeWeapon = w;
                    break;
                }
            }
        }

        
        if (attack1.equipWeapon != activeWeapon)
        {
            attack1.equipWeapon = activeWeapon;

            if (activeWeapon != null)
                Debug.Log($"[WeaponEquipSync] 장착 무기 동기화: {activeWeapon.gameObject.name} (타입: {activeWeapon.type}, 탄약: {activeWeapon.curAmmo}/{activeWeapon.maxAmmo})");
            else
                Debug.Log("[WeaponEquipSync] 장착 무기 없음 (비장착 상태)");
        }
    }
}
