using System.Collections.Generic;
using UnityEngine;

/// <summary>Rintangan yang melayang. Prefab butuh Collider (Is Trigger).</summary>
public class Asteroid : MonoBehaviour
{
    public static readonly List<Asteroid> All = new List<Asteroid>();

    public float radius = 2f;                // dipakai drone untuk menghindar
    public float minSpeed = 1f;
    public float maxSpeed = 4f;
    public float spinSpeed = 20f;

    Vector3 velocity;
    Vector3 spinAxis;

    void OnEnable()  { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        Vector2 d = Random.insideUnitCircle.normalized;
        velocity = new Vector3(d.x, 0f, d.y) * Random.Range(minSpeed, maxSpeed);
        spinAxis = Random.onUnitSphere;
    }

    void Update()
    {
        transform.position += velocity * Time.deltaTime;
        transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);

        float R = GameManager.Instance.playAreaRadius;
        Vector3 p = transform.position;
        if (p.magnitude > R)
        {
            if (Vector3.Dot(velocity, p) > 0f) velocity = Vector3.Reflect(velocity, -p.normalized);
            transform.position = p.normalized * R;
        }
    }
}
