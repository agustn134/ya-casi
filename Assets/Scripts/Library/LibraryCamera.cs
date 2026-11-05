using UnityEngine;

public sealed class LibraryCamera : MonoBehaviour
{
    public Transform target;
    Vector3 velocity;
    void LateUpdate()
    {
        if (target == null) return;
        var cameraComponent = GetComponent<Camera>();
        float halfHeight = cameraComponent.orthographicSize;
        float halfWidth = halfHeight * cameraComponent.aspect;
        float limitX = Mathf.Max(0, 15f - halfWidth);
        float limitY = Mathf.Max(0, 9f - halfHeight);
        var destination = new Vector3(Mathf.Clamp(target.position.x, -limitX, limitX), Mathf.Clamp(target.position.y + .7f, -limitY, limitY), -10);
        transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, .13f);
    }
}
