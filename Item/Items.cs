using UnityEngine;

public class Items : MonoBehaviour
{
    public PlayerControl playerControl;
    public PlayerHealth playerHealth;
    public bool[] hasWeapons;
    public GameObject[] grenades;
    public int ammo;
    public int coin;
    public int health;
    public int hasGrenades;

    public int maxAmmo;
    public int maxCoin;
    public int maxHealth;
    public int maxHasGrenades;

    void Awake()
    {
        if (playerControl == null)
            playerControl = GetComponent<PlayerControl>();

        if (playerControl == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                playerControl = player.GetComponent<PlayerControl>();
        }

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Item") == false)
            return;
        // 공전 디스플레이용 수류탄(부모에 Orbit이 있는)은 픽업 대상이 아니므로 무시 (공전 중 재수집/삭제 방지)
        if (other.GetComponentInParent<Orbit>() != null)
            return;


        if (playerControl != null && (playerControl.isJump || playerControl.isDodge))
            return;

        Item item = other.GetComponent<Item>();
        if (item == null)
            return;

        switch (item.type)
        {
            case Item.Type.Ammo:
                ammo += item.value;
                if (ammo > maxAmmo)
                    ammo = maxAmmo;
                break;
            case Item.Type.Coin:
                coin += item.value;
                if (coin > maxCoin)
                    coin = maxCoin;
                break;
            case Item.Type.Heart:
                health += item.value;
                if (health > maxHealth)
                    health = maxHealth;
                break;
            case Item.Type.Grenade:
                // value가 0이어도 1개씩 증가 + grenades 배열 범위 보호
                if (hasGrenades < grenades.Length)
                    grenades[hasGrenades].SetActive(true);
                hasGrenades++;
                if (hasGrenades > maxHasGrenades)
                    hasGrenades = maxHasGrenades;
                break;
        }

        // 아이템을 먹으면 월드 픽업 오브젝트 제거 (수류탄이 삭제되지 않던 문제 수정)
        Destroy(other.gameObject);
    }

    // ── 플레이어 피격 처리 ─────────────────────────
    /// <summary>Enemy 또는 Missile이 호출 - 플레이어 체력 감소</summary>
    public void TakeDamage(int dmg)
    {
        health -= dmg;
        if (health < 0) health = 0;
        if (playerHealth != null)
            playerHealth.OnDamaged();
        // TODO : health <= 0 일 때 게임오버 처리 추가 가능
    }
}
