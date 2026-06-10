using UnityEngine;
using UnityEngine.InputSystem;
public class ItemInteraction : MonoBehaviour
{
    public PlayerControl playerControl;
    public GameObject[] weapons;
    public bool[] hasWeapons;
    public Transform weaponHand; // 손 위치 기준점

    private Vector3[] weaponLocalPositions;
    private Quaternion[] weaponLocalRotations;
    Animator anim;
    bool iDown;
    bool sDown1;
    bool sDown2;
    bool sDown3;
    public bool isSwap;
    GameObject nearObject;
    Weapon equipWeapon;
    int equipWeaponIndex = -1;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (anim == null)
            Debug.LogWarning("Animator가 Player 오브젝트에 없습니다. 무기 교체 애니메이션이 재생되지 않습니다.");

        InitializeWeaponPositions();
    }

    void Update()
    {
        GetInput();
        Interation();
        Swap();
        UpdateWeaponPosition(); // 매 프레임 무기 위치 보정
    }

    void GetInput()
    {
        iDown = Input.GetButtonDown("Interation") || Input.GetKeyDown(KeyCode.E);
        sDown1 = Input.GetButtonDown("Swap1") || Input.GetKeyDown(KeyCode.Alpha1);
        sDown2 = Input.GetButtonDown("Swap2") || Input.GetKeyDown(KeyCode.Alpha2);
        sDown3 = Input.GetButtonDown("Swap3") || Input.GetKeyDown(KeyCode.Alpha3);
    }

    void Swap()
    {
        if (sDown1 && (!hasWeapons[0] || equipWeaponIndex == 0))
            return;
        if (sDown2 && (!hasWeapons[1] || equipWeaponIndex == 1))
            return;
        if (sDown3 && (!hasWeapons[2] || equipWeaponIndex == 2))
            return;

        int weaponIndex = -1;
        if (sDown1) weaponIndex = 0;
        if (sDown2) weaponIndex = 1;
        if (sDown3) weaponIndex = 2;

        if ((sDown1 || sDown2 || sDown3) && !playerControl.isJump && !playerControl.isDodge)
        {
            if (equipWeapon != null)
            {
                equipWeapon.gameObject.SetActive(false);
                equipWeapon.transform.SetParent(null); // // 이전 무기 부모 해제
            }

            equipWeaponIndex = weaponIndex;
            equipWeapon = weapons[weaponIndex].GetComponent<Weapon>();
            equipWeapon.gameObject.SetActive(true);
            if (weaponHand != null)
            {
                equipWeapon.transform.SetParent(weaponHand);
                equipWeapon.transform.localPosition = weaponLocalPositions[weaponIndex];
                equipWeapon.transform.localRotation = weaponLocalRotations[weaponIndex];
            }

            if (anim != null)
                anim.SetTrigger("doSwap");

            isSwap = true;
            Invoke("SwapOut", 0.4f);
        }
    }

    void SwapOut()
    {
        isSwap = false;
    }

    void Interation()
    {
        if (iDown && nearObject != null && !playerControl.isJump && !playerControl.isDodge)
        {
            if (nearObject.CompareTag("Weapon"))
            {
                Item item = nearObject.GetComponent<Item>();
                int weaponIndex = item.value;
                hasWeapons[weaponIndex] = true;
                Destroy(nearObject);
            }
        }
    }

    void UpdateWeaponPosition()
    {
        if (equipWeapon != null && weaponHand != null && equipWeapon.transform.parent == weaponHand)
        {
            // 부모가 손이면 로컬 좌표로 자동 위치 보정
            equipWeapon.transform.localPosition = weaponLocalPositions[equipWeaponIndex];
            equipWeapon.transform.localRotation = weaponLocalRotations[equipWeaponIndex];
        }
    }

    void InitializeWeaponPositions()
    {
        if (weapons == null)
            return;

        weaponLocalPositions = new Vector3[weapons.Length];
        weaponLocalRotations = new Quaternion[weapons.Length];

        if (weaponHand == null)
        {
            Debug.LogWarning("weaponHand가 설정되지 않았습니다. 무기 손 위치를 할당하세요.");
        }

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == null)
                continue;

            if (weaponHand != null)
            {
                // 초기 위치를 기준으로 로컬 위치/회전 저장
                weaponLocalPositions[i] = weaponHand.InverseTransformPoint(weapons[i].transform.position);
                weaponLocalRotations[i] = Quaternion.Inverse(weaponHand.rotation) * weapons[i].transform.rotation;
            }
            else
            {
                weaponLocalPositions[i] = weapons[i].transform.localPosition;
                weaponLocalRotations[i] = weapons[i].transform.localRotation;
            }

            weapons[i].SetActive(false);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Weapon"))
        {
            nearObject = other.gameObject;
            Debug.Log(nearObject.name);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Weapon"))
            nearObject = null;
    }
}
