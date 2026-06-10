using System.Collections;
using UnityEngine;

// 플레이어 상태이상(스턴/속박/도트) 중앙 처리기.
// 기존 플레이어 스크립트(PlayerControl 등)는 수정하지 않고,
// 해당 컴포넌트를 일시적으로 비활성화하는 방식으로 제어한다.
public class PlayerStatus : MonoBehaviour
{
    Renderer[] renderers;
    Items items;
    Behaviour control;   // PlayerControl (이동/점프/회피)
    Behaviour rotation;  // PlayerRotation (회전)
    Behaviour attack;    // Attack_1 (공격)
    bool ccActive;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        items    = GetComponent<Items>();
        control  = GetComponent("PlayerControl") as Behaviour;
        rotation = GetComponent("PlayerRotation") as Behaviour;
        attack   = GetComponent("Attack_1") as Behaviour;
    }

    // 스턴: 이동/회전/공격 모두 잠금
    public void ApplyStun(float duration)
    {
        if (!ccActive) StartCoroutine(CrowdControl(duration, true));
    }

    // 속박: 이동/회전만 잠금(공격은 가능)
    public void ApplyBind(float duration)
    {
        if (!ccActive) StartCoroutine(CrowdControl(duration, false));
    }

    IEnumerator CrowdControl(float duration, bool isStun)
    {
        ccActive = true;
        if (control != null)  control.enabled = false;
        if (rotation != null) rotation.enabled = false;
        if (isStun && attack != null) attack.enabled = false;

        StartCoroutine(FlashWhiteBlack(duration));
        yield return new WaitForSeconds(duration);

        if (control != null)  control.enabled = true;
        if (rotation != null) rotation.enabled = true;
        if (attack != null)   attack.enabled = true;
        ccActive = false;
    }

    // 도트 데미지: interval 간격으로 ticks회 피해
    public void ApplyDot(int damagePerTick, int ticks, float interval)
    {
        StartCoroutine(DotRoutine(damagePerTick, ticks, interval));
    }

    IEnumerator DotRoutine(int dmg, int ticks, float interval)
    {
        for (int i = 0; i < ticks; i++)
        {
            if (items != null) items.TakeDamage(dmg);
            yield return new WaitForSeconds(interval);
        }
    }

    // 흰검 반짝
    IEnumerator FlashWhiteBlack(float duration)
    {
        float t = 0f; bool white = true;
        while (t < duration)
        {
            SetColor(white ? Color.white : Color.black);
            white = !white;
            yield return new WaitForSeconds(0.1f);
            t += 0.1f;
        }
        SetColor(Color.white);
    }

    void SetColor(Color c)
    {
        if (renderers == null) return;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            foreach (var m in r.materials)
                if (m != null) m.color = c;
        }
    }
}
