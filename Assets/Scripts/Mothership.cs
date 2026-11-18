using System.Collections;
using UnityEngine;

/// <summary>Kapal induk: bergerak (WASD/Panah), punya HP, dan meluncurkan drone ke tumpukan sampah.</summary>
[RequireComponent(typeof(Rigidbody))]
public class Mothership : MonoBehaviour
{
    public static Mothership Instance { get; private set; }

    [Header("Movement")]
    public float maxSpeed = 10f;
    public float acceleration = 8f;
    public float rotationSpeed = 6f;

    [Header("Health")]
    public float maxHP = 100f;
    public float asteroidDamage = 15f;
    public float droneLostDamage = 5f;
    public float invulnerableTime = 1f;

    [Header("Drones")]
    public DroneController dronePrefab;
    public Transform dockPoint;              // opsional, titik keluar/masuk drone
    public int totalDrones = 6;
    public int dronesPerLaunch = 3;
    public float launchRange = 25f;          // jarak maksimal klik ke tumpukan sampah
    public float droneRespawnTime = 6f;

    public float CurrentHP { get; private set; }
    public int DronesReady { get; private set; }

    Vector3 velocity;
    float invTimer;

    void Awake()
    {
        Instance = this;
        CurrentHP = maxHP;
        DronesReady = totalDrones;
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        invTimer -= Time.deltaTime;

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();

        velocity = Vector3.MoveTowards(velocity, input * maxSpeed, acceleration * Time.deltaTime);
        Vector3 pos = transform.position + velocity * Time.deltaTime;
        pos.y = 0f;
        pos = Vector3.ClampMagnitude(pos, GameManager.Instance.playAreaRadius);
        transform.position = pos;

        if (velocity.sqrMagnitude > 0.2f)
        {
            Quaternion target = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }

    public bool LaunchDrones(DebrisCluster cluster)
    {
        if (cluster == null || cluster.Available <= 0) return false;

        Vector3 flat = cluster.transform.position - transform.position; flat.y = 0;
        if (flat.magnitude > launchRange)
        {
            if (UIManager.Instance) UIManager.Instance.ShowToast("Terlalu jauh! Dekati tumpukan sampah.");
            return false;
        }
        if (DronesReady <= 0)
        {
            if (UIManager.Instance) UIManager.Instance.ShowToast("Semua drone sedang bertugas!");
            return false;
        }

        int launched = 0;
        while (launched < dronesPerLaunch && DronesReady > 0)
        {
            int amount = cluster.Reserve(dronePrefab.capacity);
            if (amount <= 0) break;

            Vector3 spawn = (dockPoint ? dockPoint.position : transform.position);
            Vector2 jitter = Random.insideUnitCircle * 1.5f;
            spawn += new Vector3(jitter.x, 0, jitter.y);
            spawn.y = 0f;

            DroneController d = Instantiate(dronePrefab, spawn, transform.rotation);
            d.Init(this, cluster, amount);
            DronesReady--;
            launched++;
        }
        return launched > 0;
    }

    public void ReceiveDrone(int cargo)
    {
        GameManager.Instance.Deposit(cargo);
        DronesReady++;
    }

    public void OnDroneLost()
    {
        TakeDamage(droneLostDamage, true);
        StartCoroutine(RebuildDrone());
    }

    IEnumerator RebuildDrone()
    {
        yield return new WaitForSeconds(droneRespawnTime);
        DronesReady++;
    }

    public void TakeDamage(float amount, bool ignoreInvulnerability = false)
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (!ignoreInvulnerability && invTimer > 0f) return;

        CurrentHP = Mathf.Max(0f, CurrentHP - amount);
        if (!ignoreInvulnerability) invTimer = invulnerableTime;
        if (CurrentHP <= 0f) GameManager.Instance.Lose();
    }

    void OnTriggerEnter(Collider other)
    {
        Asteroid a = other.GetComponentInParent<Asteroid>();
        if (a == null) return;

        TakeDamage(asteroidDamage);
        // knockback kecil menjauhi asteroid
        Vector3 away = transform.position - a.transform.position; away.y = 0;
        velocity = away.normalized * maxSpeed;
    }
}
