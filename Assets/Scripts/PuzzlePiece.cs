using UnityEngine;
using UnityEngine.EventSystems;

public class PuzzlePiece : MonoBehaviour
{
    [Header("Target")]
    public Transform targetPosition;
    public float snapThreshold = 0.5f; // world units (pre-scale)

    [HideInInspector] public bool isPlaced = false;

    private Camera mainCam;
    private bool isDragging = false;
    private Plane dragPlane;
    private Vector3 dragOffset;
    private Renderer pieceRenderer;
    private Color originalColor;
    private int activeFingerId = -1;

    void Start()
    {
        mainCam = Camera.main;
        pieceRenderer = GetComponent<Renderer>();
        if (pieceRenderer != null)
            originalColor = pieceRenderer.material.color;
    }

    void Update()
    {
        if (isPlaced || mainCam == null) return;

        if (Input.touchCount == 0)
        {
            // Mouse fallback for in-editor testing (no real drag plane in editor AR).
            HandleMouseFallback();
            return;
        }

        foreach (Touch touch in Input.touches)
        {
            if (!isDragging && touch.phase == TouchPhase.Began)
            {
                // Ignore touches that land on UI (e.g. the Reset button).
                if (IsPointerOverUI(touch.fingerId)) continue;
                TryBeginDrag(touch.position, touch.fingerId);
            }
            else if (isDragging && touch.fingerId == activeFingerId)
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    UpdateDrag(touch.position);
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    EndDrag();
            }
        }
    }

    void HandleMouseFallback()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI(-1)) return;
            TryBeginDrag(Input.mousePosition, -1);
        }
        else if (isDragging && Input.GetMouseButton(0)) UpdateDrag(Input.mousePosition);
        else if (isDragging && Input.GetMouseButtonUp(0)) EndDrag();
    }

    bool IsPointerOverUI(int pointerId)
    {
        if (EventSystem.current == null) return false;
        return pointerId < 0
            ? EventSystem.current.IsPointerOverGameObject()
            : EventSystem.current.IsPointerOverGameObject(pointerId);
    }

    void TryBeginDrag(Vector2 screenPos, int fingerId)
    {
        Ray ray = mainCam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
        {
            isDragging = true;
            activeFingerId = fingerId;

            // Drag plane is horizontal, at the piece's current height.
            dragPlane = new Plane(Vector3.up, transform.position);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                dragOffset = transform.position - hitPoint;
            }
            else
            {
                dragOffset = Vector3.zero;
            }

            if (pieceRenderer != null)
                pieceRenderer.material.color = originalColor * 1.3f;
        }
    }

    void UpdateDrag(Vector2 screenPos)
    {
        Ray ray = mainCam.ScreenPointToRay(screenPos);
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            transform.position = hitPoint + dragOffset;
        }
    }

    void EndDrag()
    {
        isDragging = false;
        activeFingerId = -1;

        if (pieceRenderer != null)
            pieceRenderer.material.color = originalColor;

        if (targetPosition == null) return;

        // Interpret the snap threshold in the puzzle's scaled space.
        float rootScale = transform.parent != null ? transform.parent.lossyScale.x : 1f;
        float effectiveThreshold = PuzzleScale.ScaledDistance(snapThreshold, rootScale);

        // Snap on horizontal (XZ) distance only: pieces are dragged on a fixed-height
        // plane, so comparing full 3D distance would make pieces whose target height
        // differs from their grab height (e.g. the roof) impossible to place. Snap()
        // restores the correct target height.
        Vector3 pos = transform.position;
        Vector3 target = targetPosition.position;
        float dx = pos.x - target.x;
        float dz = pos.z - target.z;
        float horizontalDist = Mathf.Sqrt(dx * dx + dz * dz);

        if (horizontalDist < effectiveThreshold)
        {
            Snap();
        }
    }

    void Snap()
    {
        isPlaced = true;
        transform.position = targetPosition.position;
        transform.rotation = targetPosition.rotation;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        GhostTarget ghost = targetPosition.GetComponent<GhostTarget>();
        if (ghost != null) ghost.Hide();

        GameManager.Instance.PiecePlaced();
    }
}
