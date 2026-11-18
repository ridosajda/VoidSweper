using System.Collections.Generic;
using UnityEngine;

/// <summary>Menyebar tumpukan sampah & asteroid secara acak saat level dimulai.</summary>
public class LevelSpawner : MonoBehaviour
{
    [Header("Prefabs (.fbx yang sudah dijadikan prefab)")]
    public DebrisCluster[] clusterPrefabs;
    public Asteroid[] asteroidPrefabs;

    [Header("Jumlah")]
    public int clusterCount = 8;
    public int asteroidCount = 25;
    public Vector2Int trashPerCluster = new Vector2Int(15, 40);

    [Header("Penempatan")]
    public float safeRadiusAroundStart = 14f;
    public float minClusterSpacing = 12f;
    public int seed = 0;                     // 0 = acak tiap main

    void Start()
    {
        if (seed != 0) Random.InitState(seed);
        float R = GameManager.Instance.playAreaRadius * 0.9f;
        var placed = new List<Vector3>();

        for (int i = 0; i < clusterCount && clusterPrefabs.Length > 0; i++)
        {
            Vector3 p = Vector3.zero;
            for (int tries = 0; tries < 30; tries++)
            {
                Vector2 r = Random.insideUnitCircle * R;
                p = new Vector3(r.x, 0f, r.y);
                if (p.magnitude < safeRadiusAroundStart) continue;
                bool ok = true;
                foreach (var q in placed) if ((q - p).magnitude < minClusterSpacing) { ok = false; break; }
                if (ok) break;
            }
            placed.Add(p);
            var prefab = clusterPrefabs[Random.Range(0, clusterPrefabs.Length)];
            var c = Instantiate(prefab, p, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            c.Setup(Random.Range(trashPerCluster.x, trashPerCluster.y + 1));
        }

        for (int i = 0; i < asteroidCount && asteroidPrefabs.Length > 0; i++)
        {
            Vector2 r = Random.insideUnitCircle * R;
            Vector3 p = new Vector3(r.x, 0f, r.y);
            if (p.magnitude < safeRadiusAroundStart * 0.7f) p = p.normalized * safeRadiusAroundStart;
            var prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
            Instantiate(prefab, p, Random.rotation);
        }
    }
}
