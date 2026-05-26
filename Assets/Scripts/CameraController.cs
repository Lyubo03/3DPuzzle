using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Transform target;
    public float rotationSpeed = 5f;
    public float zoomSpeed = 2f;
    public float minDistance = 5f;
    public float maxDistance = 20f;
    public float minVerticalAngle = 10f;
    public float maxVerticalAngle = 80f;

    [Header("Smoothing")]
    public float smoothTime = 0.1f;

    public static bool IsRotating { get; private set; }

    private float currentDistance = 12f;
    private float currentYaw = 45f;
    private float currentPitch = 30f;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (target == null)
        {
            GameObject pivot = new GameObject("CameraPivot");
            pivot.transform.position = Vector3.up * 1.5f;
            target = pivot.transform;
        }

        Vector3 dir = transform.position - target.position;
        currentDistance = dir.magnitude;
        currentYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        currentPitch = Mathf.Asin(dir.y / currentDistance) * Mathf.Rad2Deg;
    }

    void LateUpdate()
    {
        // Rotate with right mouse button
        if (Input.GetMouseButton(1))
        {
            IsRotating = true;
            currentYaw += Input.GetAxis("Mouse X") * rotationSpeed;
            currentPitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
            currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
        }
        else
        {
            IsRotating = false;
        }

        // Zoom with scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentDistance -= scroll * zoomSpeed;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        // Calculate position
        float yawRad = currentYaw * Mathf.Deg2Rad;
        float pitchRad = currentPitch * Mathf.Deg2Rad;

        Vector3 desiredPosition = target.position + new Vector3(
            Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
            Mathf.Sin(pitchRad),
            Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
        ) * currentDistance;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.LookAt(target.position);
    }
}
