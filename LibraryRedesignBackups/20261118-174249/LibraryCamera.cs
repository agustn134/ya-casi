using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class LibraryCamera : MonoBehaviour
{
    public Transform target;

    Camera view;
    float normalSize;
    float impactUntil;

    void Awake()
    {
        view = GetComponent<Camera>();
        normalSize = view.orthographicSize;
    }

    public void Shake() => impactUntil = Time.unscaledTime + .18f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 center = target.position;
        transform.position = new Vector3(center.x, center.y, transform.position.z);

        // El golpe cambia el zoom sin descentrar al jugador.
        float impact = Mathf.Max(0, impactUntil - Time.unscaledTime) / .18f;
        view.orthographicSize = normalSize * (1f - .018f * impact);
    }
}
