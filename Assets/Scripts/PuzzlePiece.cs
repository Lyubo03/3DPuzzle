using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    [Header("Target")]
    public Transform targetPosition;
    public float snapThreshold = 0.5f;

    [Header("Drag Settings")]
    public float dragHeight = 2f;

    [HideInInspector] public bool isPlaced = false;

    private Camera mainCam;
    private bool isDragging = false;
    private Vector3 offset;
    private float zDistance;
    private Renderer pieceRenderer;
    private Color originalColor;

    void Start()
    {
        mainCam = Camera.main;
        pieceRenderer = GetComponent<Renderer>();
        if (pieceRenderer != null)
            originalColor = pieceRenderer.material.color;
    }

    void OnMouseDown()
    {
        if (isPlaced) return;
        if (CameraController.IsRotating) return;

        isDragging = true;
        Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position);
        zDistance = screenPos.z;
        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));
        offset = transform.position - mouseWorld;

        if (pieceRenderer != null)
            pieceRenderer.material.color = originalColor * 1.3f;
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 screenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance);
        Vector3 worldPos = mainCam.ScreenToWorldPoint(screenPos) + offset;
        worldPos.y = Mathf.Max(worldPos.y, dragHeight);
        transform.position = worldPos;
    }

    void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        if (pieceRenderer != null)
            pieceRenderer.material.color = originalColor;

        if (targetPosition == null) return;

        float dist = Vector3.Distance(transform.position, targetPosition.position);
        if (dist < snapThreshold)
        {
            Snap();
        }
    }

    void Snap()
    {
        isPlaced = true;
        transform.position = targetPosition.position;
        transform.rotation = targetPosition.rotation;

        // Disable further interaction
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Hide ghost
        GhostTarget ghost = targetPosition.GetComponent<GhostTarget>();
        if (ghost != null) ghost.Hide();

        // Notify GameManager
        GameManager.Instance.PiecePlaced();
    }
}
