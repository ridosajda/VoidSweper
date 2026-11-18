using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public Slider hpSlider;
    public TMP_Text hpText;
    public TMP_Text timerText;
    public TMP_Text trashText;
    public TMP_Text dronesText;
    public TMP_Text toastText;

    [Header("End Screen")]
    public GameObject endPanel;
    public TMP_Text endTitleText;
    public TMP_Text endInfoText;

    float toastTimer;
    bool endShown;

    void Awake()
    {
        Instance = this;
        if (endPanel) endPanel.SetActive(false);
        if (toastText) toastText.text = "";
    }

    public void ShowToast(string msg)
    {
        if (!toastText) return;
        toastText.text = msg;
        toastTimer = 2f;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        var ship = Mothership.Instance;
        if (gm == null || ship == null) return;

        if (hpSlider) hpSlider.value = ship.CurrentHP / ship.maxHP;
        if (hpText) hpText.text = "HP " + Mathf.CeilToInt(ship.CurrentHP) + "/" + Mathf.CeilToInt(ship.maxHP);
        if (timerText) timerText.text = FormatTime(gm.ElapsedTime);
        if (trashText) trashText.text = "Sampah: " + gm.CollectedTrash + "/" + gm.TotalTrash;
        if (dronesText) dronesText.text = "Drone: " + ship.DronesReady + "/" + ship.totalDrones;

        if (toastText && toastTimer > 0f)
        {
            toastTimer -= Time.unscaledDeltaTime;
            if (toastTimer <= 0f) toastText.text = "";
        }

        if (gm.State != GameState.Playing && !endShown) ShowEnd(gm);
    }

    void ShowEnd(GameManager gm)
    {
        endShown = true;
        if (!endPanel) return;
        endPanel.SetActive(true);
        if (gm.State == GameState.Won)
        {
            endTitleText.text = "LEVEL BERSIH!";
            endInfoText.text = "Waktu: " + FormatTime(gm.ElapsedTime) + "\nBintang: " + gm.CalculateStars() + "/3";
        }
        else
        {
            endTitleText.text = "KAPAL HANCUR";
            endInfoText.text = "Sampah terkumpul: " + gm.CollectedTrash + "/" + gm.TotalTrash;
        }
    }

    static string FormatTime(float t)
    {
        int m = (int)(t / 60f);
        int s = (int)(t % 60f);
        return m.ToString("00") + ":" + s.ToString("00");
    }

    // Hubungkan ke OnClick tombol "Main Lagi"
    public void OnRestartClicked() { GameManager.Instance.Restart(); }
}
