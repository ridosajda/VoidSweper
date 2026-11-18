using TMPro;
using UnityEngine;

/// <summary>Tumpukan sampah. Butuh Collider agar bisa diklik.</summary>
public class DebrisCluster : MonoBehaviour
{
    [Header("Trash")]
    public int remaining = 30;
    public float approachRadius = 3f;        // jarak drone mulai mengambil

    [Header("Visual (opsional)")]
    public GameObject highlightIndicator;    // mis. ring/outline yang diaktifkan saat hover
    public TMP_Text amountLabel;             // TextMeshPro 3D di atas tumpukan

    int initial;
    int reserved;
    Vector3 baseScale;
    Renderer[] renderers;
    Collider col;
    Camera cam;

    public int Available => remaining - reserved;

    public void Setup(int amount)
    {
        remaining = amount;
        initial = amount;
    }

    void Start()
    {
        if (initial == 0) initial = remaining;
        baseScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>();
        col = GetComponent<Collider>();
        cam = Camera.main;
        if (highlightIndicator) highlightIndicator.SetActive(false);
        GameManager.Instance.RegisterTrash(remaining);
        Refresh();
    }

    void LateUpdate()
    {
        if (amountLabel && cam) amountLabel.transform.rotation = cam.transform.rotation;
    }

    public int Reserve(int amount)
    {
        int a = Mathf.Min(amount, Available);
        if (a <= 0) return 0;
        reserved += a;
        return a;
    }

    public void Release(int amount) { reserved = Mathf.Max(0, reserved - amount); }

    public int Take(int amount)
    {
        int t = Mathf.Min(amount, remaining);
        remaining -= t;
        reserved = Mathf.Max(0, reserved - amount);
        Refresh();
        return t;
    }

    public void Restore(int amount)
    {
        remaining += amount;
        Refresh();
    }

    public void SetHighlighted(bool on)
    {
        if (highlightIndicator) highlightIndicator.SetActive(on && remaining > 0);
    }

    void Refresh()
    {
        bool alive = remaining > 0;
        if (col) col.enabled = alive;
        foreach (var r in renderers) r.enabled = alive;
        if (amountLabel) { amountLabel.text = alive ? remaining.ToString() : ""; }

        float frac = initial > 0 ? (float)remaining / initial : 0f;
        transform.localScale = baseScale * Mathf.Lerp(0.5f, 1f, frac);
        if (!alive && highlightIndicator) highlightIndicator.SetActive(false);
    }
}
