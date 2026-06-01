using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Once AR tracking is ready, positions the puzzle root a fixed distance in front of
/// the AR camera and scales it down to tabletop size. Runs once, then disables itself.
/// </summary>
public class ARPuzzlePlacer : MonoBehaviour
{
    [Tooltip("Root transform parenting the whole puzzle.")]
    public Transform puzzleRoot;

    [Tooltip("AR session, used to detect when tracking is ready.")]
    public ARSession arSession;

    [Tooltip("The AR camera transform (device pose).")]
    public Transform arCamera;

    [Tooltip("Metres in front of the camera to place the puzzle.")]
    public float distance = 0.5f;

    [Tooltip("Uniform scale applied to the puzzle root (4m house -> ~0.4m).")]
    public float rootScale = 0.1f;

    [Tooltip("Seconds to wait for tracking before placing anyway, so the puzzle never stays frozen at origin.")]
    public float trackingTimeout = 5f;

    private bool placed = false;
    private float elapsed = 0f;

    void Start()
    {
        // Scale down immediately so the puzzle is never a full-size (~4m) scene around
        // the camera while we wait for tracking to come up.
        if (puzzleRoot != null)
            puzzleRoot.localScale = Vector3.one * rootScale;
    }

    void Update()
    {
        if (placed) return;
        if (puzzleRoot == null || arCamera == null) return;

        elapsed += Time.deltaTime;

        // Place once tracking is ready, or after a timeout so a device that never
        // attains tracking (permission denied, unsupported) still shows the puzzle.
        bool tracking = ARSession.state == ARSessionState.SessionTracking;
        if (!tracking && elapsed < trackingTimeout) return;

        PlaceInFront();
        placed = true;
        enabled = false;
    }

    void PlaceInFront()
    {
        // Project the camera forward onto the horizontal plane so the puzzle sits level.
        Vector3 forward = arCamera.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 pos = arCamera.position + forward * distance;
        pos.y = arCamera.position.y - 0.3f; // place a little below eye/hand level

        puzzleRoot.position = pos;
        puzzleRoot.rotation = Quaternion.LookRotation(forward, Vector3.up);
        puzzleRoot.localScale = Vector3.one * rootScale;
    }
}
