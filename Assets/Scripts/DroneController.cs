using UnityEngine;

/// <summary>
/// Drone kecil: menuju sampah -> menghindari rintangan -> mengambil sampah -> pulang ke kapal induk.
/// Prefab drone butuh Collider (Is Trigger) + Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    enum Phase { ToTarget, Collecting, Returning }

    [Header("Stats")]
    public int capacity = 5;
    public float speed = 14f;
    public float turnSpeed = 10f;
    public float collectTime = 0.8f;
    public float dockDistance = 2.5f;

    [Header("Obstacle Avoidance")]
    public float avoidDistance = 5f;
    public float avoidStrength = 2.5f;

    [Header("FX (opsional)")]
    public GameObject destroyEffect;

    Mothership home;
    DebrisCluster target;
    int reserved;
    int cargo;
    bool hasCargo;
    Phase phase = Phase.ToTarget;
    float timer;

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void Init(Mothership ship, DebrisCluster cluster, int reservedAmount)
    {
        home = ship;
        target = cluster;
        reserved = reservedAmount;
    }

    void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        switch (phase)
        {
            case Phase.ToTarget:
                MoveToward(target.transform.position);
                if (FlatDistance(target.transform.position) <= target.approachRadius)
                {
                    phase = Phase.Collecting;
                    timer = collectTime;
                }
                break;

            case Phase.Collecting:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    cargo = target.Take(reserved);
                    reserved = 0;
                    hasCargo = cargo > 0;
                    phase = Phase.Returning;
                }
                break;

            case Phase.Returning:
                MoveToward(home.transform.position);   // kapal induk boleh bergerak, drone tetap mengejar
                if (FlatDistance(home.transform.position) <= dockDistance)
                {
                    home.ReceiveDrone(cargo);
                    Destroy(gameObject);
                }
                break;
        }
    }

    float FlatDistance(Vector3 p)
    {
        Vector3 d = p - transform.position; d.y = 0;
        return d.magnitude;
    }

    void MoveToward(Vector3 destination)
    {
        Vector3 pos = transform.position;
        Vector3 toDest = destination - pos; toDest.y = 0;
        Vector3 dir = toDest.normalized;

        Vector3 avoid = Vector3.zero;
        var all = Asteroid.All;
        for (int i = 0; i < all.Count; i++)
        {
            Asteroid a = all[i];
            Vector3 off = pos - a.transform.position; off.y = 0;
            float dist = off.magnitude - a.radius;
            if (dist >= avoidDistance) continue;

            float weight = 1f - Mathf.Max(dist, 0f) / avoidDistance;
            Vector3 away = off.normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, away);
            if (Vector3.Dot(tangent, dir) < 0f) tangent = -tangent;     // selalu belok ke sisi yang searah tujuan
            avoid += (away * 0.6f + tangent * 0.8f) * weight;
        }

        Vector3 steer = (dir + avoid * avoidStrength).normalized;
        if (steer.sqrMagnitude < 0.001f) steer = dir;

        pos += steer * speed * Time.deltaTime;
        pos.y = 0f;
        transform.position = pos;

        Quaternion look = Quaternion.LookRotation(steer, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Asteroid>() != null) Die();
    }

    void Die()
    {
        if (hasCargo) target.Restore(cargo);       // sampah jatuh kembali ke tumpukan
        else if (reserved > 0) target.Release(reserved);

        home.OnDroneLost();
        if (destroyEffect) Instantiate(destroyEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
