using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [Header("References")]
    public Items items;
    public PlayerControl playerControl;

    [Header("Damage Flash")]
    public Color hitColor = Color.red;
    public float flashDuration = 0.2f;

    Renderer[] renderers;
    Color[] originalColors;
    Coroutine flashCoroutine;

    void Awake()
    {
        if (items == null)
            items = GetComponent<Items>();

        if (playerControl == null)
            playerControl = GetComponent<PlayerControl>();

        InitializeRenderers();
    }

    void InitializeRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        List<Color> colors = new List<Color>(renderers.Length);

        foreach (var renderer in renderers)
        {
            Material[] mats = renderer.materials;
            if (mats == null || mats.Length == 0)
            {
                colors.Add(Color.white);
                continue;
            }

            foreach (var mat in mats)
            {
                if (mat.HasProperty("_Color"))
                    colors.Add(mat.color);
                else
                    colors.Add(Color.white);
            }
        }

        originalColors = colors.ToArray();
    }

    public int CurrentHealth
    {
        get => items != null ? items.health : 0;
    }

    public int MaxHealth
    {
        get => items != null ? items.maxHealth : 0;
    }

    public void OnDamaged()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(DamageFlash());
    }

    IEnumerator DamageFlash()
    {
        SetRendererColor(hitColor);
        yield return new WaitForSeconds(flashDuration);
        RestoreRendererColors();
        flashCoroutine = null;
    }

    void SetRendererColor(Color color)
    {
        int colorIndex = 0;

        foreach (var renderer in renderers)
        {
            Material[] mats = renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i].HasProperty("_Color"))
                    mats[i].color = color;
                colorIndex++;
            }
        }
    }

    void RestoreRendererColors()
    {
        int colorIndex = 0;

        foreach (var renderer in renderers)
        {
            Material[] mats = renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i].HasProperty("_Color") && colorIndex < originalColors.Length)
                    mats[i].color = originalColors[colorIndex];
                colorIndex++;
            }
        }
    }
}
