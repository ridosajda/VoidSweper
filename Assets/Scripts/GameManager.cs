using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Won, Lost }

/// <summary>Mengatur state level, timer, total sampah, dan penilaian bintang.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level")]
    public float parTimeSeconds = 120f;      // target waktu untuk bintang ke-2
    public float playAreaRadius = 70f;       // batas area bermain (pusat di 0,0,0)

    public GameState State { get; private set; } = GameState.Playing;
    public float ElapsedTime { get; private set; }
    public int TotalTrash { get; private set; }
    public int CollectedTrash { get; private set; }
    public int TrashLeft => TotalTrash - CollectedTrash;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (State != GameState.Playing) return;
        ElapsedTime += Time.deltaTime;
        if (TotalTrash > 0 && CollectedTrash >= TotalTrash) Win();
    }

    public void RegisterTrash(int amount) { TotalTrash += amount; }
    public void Deposit(int amount) { CollectedTrash += amount; }

    public void Win()  { if (State != GameState.Playing) return; State = GameState.Won;  Time.timeScale = 0f; }
    public void Lose() { if (State != GameState.Playing) return; State = GameState.Lost; Time.timeScale = 0f; }

    /// <summary>1 bintang = selesai, +1 = di bawah par time, +1 = tanpa kehilangan HP.</summary>
    public int CalculateStars()
    {
        if (State != GameState.Won) return 0;
        int stars = 1;
        if (ElapsedTime <= parTimeSeconds) stars++;
        if (Mothership.Instance != null && Mothership.Instance.CurrentHP >= Mothership.Instance.maxHP - 0.01f) stars++;
        return stars;
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
