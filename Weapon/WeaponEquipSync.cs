using UnityEngine;

/// <summary>
/// [기존 코드 무수정] HandGun(원거리 무기) 발사 안 되는 문제 해결용 브릿지 스크립트
/// 
/// 문제 원인:
///   ItemInteraction.cs 의 equipWeapon 은 private 필드이고,
///   Attack_1.cs 의 equipWeapon 은 별도 public 필드라 서로 동기화되지 않음.
///   → 무기 교체 후 Attack_1 은 equipWeapon == null 상태로 남아 발사 불가.
///
/// 해결 방법:
///   매 프레임 ItemInteraction.weapons[] 에서 활성화된 무기를 찾아
///   Attack_1.equipWeapon 에 자동으로 할당합니다.
///
/// 사용법:
///   1. Player 오브젝트에 이 컴포넌트를 추가하세요.
///   2. Inspector 에서 itemInteraction 과 attack1 을 연결하세요.
///      (같은 오브젝트에 있으면 자동 탐색됩니다)
/// </summary>
public class WeaponEquipSync : MonoBehaviour
{
    [Header("참조 연결 (비워두면 자동 탐색)")]
    public ItemInteraction itemInteraction;
    public Attack_1 attack1;

    void Awake()
    {
        // Inspector 에서 연결 안 됐으면 같은 오브젝트에서 자동 탐색
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

    /// <summary>
    /// ItemInteraction.weapons[] 에서 현재 활성화된 무기를 찾아 Attack_1.equipWeapon 에 동기화
    /// </summary>
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

        // 변경이 있을 때만 업데이트 (불필요한 할당 방지)
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
