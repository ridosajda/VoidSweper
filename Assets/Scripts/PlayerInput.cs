using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Hover + klik pada tumpukan sampah untuk meluncurkan drone.</summary>
public class PlayerInput : MonoBehaviour
{
    public Camera cam;
    public Mothership mothership;

    DebrisCluster hovered;

    void Start()
    {
        if (!cam) cam = Camera.main;
        if (!mothership) mothership = Mothership.Instance;
    }

    void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        DebrisCluster found = null;
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (!overUI)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
            float best = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var c = hits[i].collider.GetComponentInParent<DebrisCluster>();
                if (c != null && hits[i].distance < best) { best = hits[i].distance; found = c; }
            }
        }

        if (found != hovered)
        {
            if (hovered != null) hovered.SetHighlighted(false);
            hovered = found;
            if (hovered != null) hovered.SetHighlighted(true);
        }

        if (hovered != null && Input.GetMouseButtonDown(0))
            mothership.LaunchDrones(hovered);
    }
}
