using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 34f, -20f);
    public float smooth = 5f;

    void LateUpdate()
    {
        if (!target) return;
        transform.position = Vector3.Lerp(transform.position, target.position + offset, smooth * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
    }
}
