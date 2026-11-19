using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class LibraryCamera : MonoBehaviour
{
    public Transform target;
    Camera view;
    float normalSize, impactUntil;

    void Awake()
    {
        view = GetComponent<Camera>();
        normalSize = Mathf.Max(5.4f, view.orthographicSize);
        view.orthographicSize = normalSize;
    }
    public void Shake() => impactUntil = Time.unscaledTime + .18f;
    void LateUpdate()
    {
        if (target == null) return;
        float impact = Mathf.Max(0, impactUntil - Time.unscaledTime) / .18f;
        view.orthographicSize = normalSize * (1f - .018f * impact);
        // El exterior cubre el campo de visión: el jugador permanece en el centro.
        transform.position = new Vector3(target.position.x, target.position.y, -10);
    }
}
