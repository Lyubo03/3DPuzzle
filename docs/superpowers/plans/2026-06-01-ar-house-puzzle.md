# AR House Puzzle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the existing Unity 3D house-assembly puzzle into a handheld AR app for **iOS and Android** that auto-places a tabletop-scale puzzle in front of the camera and lets the user drag pieces into place with touch.

**Architecture:** Add AR Foundation to the Unity project with both providers — ARKit (iOS) and ARCore (Android). AR Foundation is a provider-agnostic abstraction, so all C# (`SceneSetup`, `ARPuzzlePlacer`, `PuzzlePiece`) is identical across platforms; only the active XR provider and per-platform build settings differ. Replace the orbit camera with an AR camera rig (device pose = view). Parent the whole procedurally-built scene under a single `PuzzleRoot` transform, then position and scale that root ~0.5 m in front of the camera at ~0.1 scale on the first tracked frame. Rewrite `PuzzlePiece` mouse input as touch raycasting that drags pieces along the puzzle's base plane. All game logic (snap, progress, win, confetti, UI) is reused unchanged.

**Tech Stack:** Unity 2022.3.22f1 LTS, C#, AR Foundation, ARKit (`com.unity.xr.arkit`, iOS) + ARCore (`com.unity.xr.arcore`, Android). Xcode (Mac) for iOS deployment; Android SDK/NDK (bundled with Unity) for Android deployment.

---

## Important context for the implementer

- **Working directory:** repo root is `C:\repos\Uni\tmp\3DPuzzle` (Windows, bash shell). Use forward slashes in paths.
- **Two-machine workflow:** C# editing + Unity build happen on the Windows machine OR the Mac (Unity is cross-platform); the **final Xcode build + device deploy must happen on the Mac**. Tasks are ordered so all code/asset work finishes before the Mac-only build steps.
- **Testing reality:** ARKit cannot run in the Unity Editor's Play mode against a real camera, and there is no automated AR test harness. Pure-logic helpers are covered with Unity Edit Mode tests (Task 2). Everything device-dependent uses an explicit **manual on-device checklist** (Task 11). This is intentional, not a gap.
- **Source of truth:** the Unity C# project. Do **not** modify `index.html` (desktop fallback).
- **Piece count stays 7** (Floor, FrontWall, BackWall, LeftWall, RightWall, Roof, Door).
- **Branch:** if not already on a feature branch, create one before the first commit:
  `git checkout -b ar-house-puzzle`

---

## File Structure

**Create:**
- `Assets/Scripts/PuzzleScale.cs` — pure static helper: converts world snap/drag distances into `PuzzleRoot` local space given the root scale. Testable in Edit Mode.
- `Assets/Scripts/ARPuzzlePlacer.cs` — positions & scales `PuzzleRoot` in front of the AR camera once tracking is ready.
- `Assets/Tests/EditMode/PuzzleScaleTests.cs` — Edit Mode tests for `PuzzleScale`.
- `Assets/Tests/EditMode/Tests.asmdef` — assembly definition so tests compile against UnityEngine + NUnit.
- `Assets/Scripts/Game.asmdef` — assembly definition for runtime scripts so the test assembly can reference them.

**Modify:**
- `Packages/manifest.json` — add AR Foundation + ARKit packages.
- `Assets/Scripts/SceneSetup.cs` — build AR rig instead of orbit camera; parent everything under `PuzzleRoot`; attach `ARPuzzlePlacer`.
- `Assets/Scripts/PuzzlePiece.cs` — replace mouse input with touch raycasting on the base plane; use `PuzzleScale` for thresholds.
- `Assets/Scripts/CameraController.cs` — **delete** (orbit camera no longer used).
- `README.txt` — update controls/run instructions for AR.

**Unchanged:** `GameManager.cs`, `GhostTarget.cs`, `index.html`.

---

### Task 1: Create feature branch and assembly definitions

Assembly definitions are required so the Edit Mode test assembly (Task 2) can reference the runtime scripts. Without a runtime `.asmdef`, scripts live in the default `Assembly-CSharp` which test assemblies cannot reference cleanly.

**Files:**
- Create: `Assets/Scripts/Game.asmdef`

- [ ] **Step 1: Create the feature branch**

Run:
```bash
cd /c/repos/Uni/tmp/3DPuzzle
git checkout -b ar-house-puzzle
```
Expected: `Switched to a new branch 'ar-house-puzzle'`

- [ ] **Step 2: Create the runtime assembly definition**

Create `Assets/Scripts/Game.asmdef`:
```json
{
    "name": "Game",
    "rootNamespace": "",
    "references": [
        "Unity.XR.ARFoundation",
        "Unity.XR.CoreUtils"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

> Note: the `references` entries resolve after Task 3 adds the AR packages. Unity will
> show an unresolved-reference warning until then; that is expected and clears once the
> packages import.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Game.asmdef
git commit -m "chore: add runtime assembly definition for AR puzzle"
```

---

### Task 2: PuzzleScale helper (TDD, Edit Mode)

The puzzle is built at ~4 m world size but displayed at ~0.1 scale (~40 cm). Snap
thresholds and drag heights are defined in the original world units. When the pieces
live under a scaled `PuzzleRoot`, distance comparisons must be done consistently. This
helper centralizes that conversion and is the one piece of pure logic we can unit-test.

**Files:**
- Create: `Assets/Scripts/PuzzleScale.cs`
- Create: `Assets/Tests/EditMode/Tests.asmdef`
- Test: `Assets/Tests/EditMode/PuzzleScaleTests.cs`

- [ ] **Step 1: Create the test assembly definition**

Create `Assets/Tests/EditMode/Tests.asmdef`:
```json
{
    "name": "Tests.EditMode",
    "rootNamespace": "",
    "references": [
        "Game",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Write the failing test**

Create `Assets/Tests/EditMode/PuzzleScaleTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

public class PuzzleScaleTests
{
    [Test]
    public void ScaledThreshold_HalvesWhenScaleIsHalf()
    {
        // A 1.0 world-unit threshold at 0.5 root scale should be 0.5 world units
        float result = PuzzleScale.ScaledDistance(1.0f, 0.5f);
        Assert.AreEqual(0.5f, result, 1e-5f);
    }

    [Test]
    public void ScaledThreshold_IdentityAtScaleOne()
    {
        float result = PuzzleScale.ScaledDistance(1.2f, 1.0f);
        Assert.AreEqual(1.2f, result, 1e-5f);
    }

    [Test]
    public void ScaledThreshold_ClampsNonPositiveScaleToIdentity()
    {
        // Guard against divide-by-zero / nonsense scale: fall back to the world value
        Assert.AreEqual(1.2f, PuzzleScale.ScaledDistance(1.2f, 0f), 1e-5f);
        Assert.AreEqual(1.2f, PuzzleScale.ScaledDistance(1.2f, -3f), 1e-5f);
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

In Unity: **Window → General → Test Runner → EditMode → Run All**.
Expected: compile error / FAIL — `PuzzleScale` does not exist.

- [ ] **Step 4: Write the minimal implementation**

Create `Assets/Scripts/PuzzleScale.cs`:
```csharp
using UnityEngine;

/// <summary>
/// Pure helpers for interpreting world-unit distances when the puzzle is displayed
/// under a uniformly-scaled root transform (AR tabletop scale).
/// </summary>
public static class PuzzleScale
{
    /// <summary>
    /// Convert a distance defined in original world units into the equivalent distance
    /// after the puzzle root has been uniformly scaled by <paramref name="rootScale"/>.
    /// Non-positive scales fall back to the original value (identity).
    /// </summary>
    public static float ScaledDistance(float worldDistance, float rootScale)
    {
        if (rootScale <= 0f) return worldDistance;
        return worldDistance * rootScale;
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

In Unity: **Test Runner → EditMode → Run All**.
Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/PuzzleScale.cs Assets/Tests/EditMode/Tests.asmdef Assets/Tests/EditMode/PuzzleScaleTests.cs
git commit -m "feat: add PuzzleScale helper with edit-mode tests"
```

---

### Task 3: Add AR Foundation + ARKit + ARCore packages

**Files:**
- Modify: `Packages/manifest.json`

- [ ] **Step 1: Add the AR packages to the dependencies block**

In `Packages/manifest.json`, add these lines inside `"dependencies"` (alphabetical
order near the other `com.unity.*` entries is fine; exact placement does not matter):
```json
    "com.unity.xr.arcore": "5.1.5",
    "com.unity.xr.arfoundation": "5.1.5",
    "com.unity.xr.arkit": "5.1.5",
    "com.unity.xr.legacyinputhelpers": "2.1.10",
    "com.unity.xr.management": "4.4.1",
```

> `com.unity.xr.arcore` is the Android provider, `com.unity.xr.arkit` the iOS provider —
> both plug into the same AR Foundation API, so no C# differs by platform.
> `com.unity.xr.legacyinputhelpers` provides `UnityEngine.SpatialTracking.TrackedPoseDriver`
> used by the AR camera rig. These versions are the AR Foundation 5.x line compatible
> with Unity 2022.3 LTS. If Unity's Package Manager reports a newer compatible patch,
> accept it — keep `arfoundation` / `arkit` / `arcore` on the **same** minor version.

- [ ] **Step 2: Let Unity resolve packages**

Open the project in Unity (or focus it if open). Unity auto-resolves on manifest change.
Expected: Package Manager imports AR Foundation, ARKit, ARCore, XR Management, Legacy
Input Helpers. The `Game.asmdef` unresolved-reference warning from Task 1 clears.

- [ ] **Step 3: Verify no compile errors**

In Unity, check the Console.
Expected: no compile errors. (Warnings about deprecated APIs are acceptable.)

- [ ] **Step 4: Commit**

```bash
git add Packages/manifest.json Packages/packages-lock.json
git commit -m "feat: add AR Foundation and ARKit packages"
```

---

### Task 4: ARPuzzlePlacer — position & scale the puzzle root

This component runs every frame until AR tracking is ready, then positions `PuzzleRoot`
in front of the camera and scales it to tabletop size, exactly once.

**Files:**
- Create: `Assets/Scripts/ARPuzzlePlacer.cs`

- [ ] **Step 1: Create ARPuzzlePlacer.cs**

Create `Assets/Scripts/ARPuzzlePlacer.cs`:
```csharp
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

    private bool placed = false;

    void Update()
    {
        if (placed) return;
        if (puzzleRoot == null || arCamera == null) return;

        // Wait until AR tracking reports a usable session state.
        if (ARSession.state != ARSessionState.SessionTracking) return;

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
```

- [ ] **Step 2: Verify it compiles**

In Unity, check the Console after the script imports.
Expected: no compile errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/ARPuzzlePlacer.cs
git commit -m "feat: add ARPuzzlePlacer to auto-place puzzle in front of camera"
```

---

### Task 5: Delete the orbit CameraController

The AR camera is driven by device pose; the orbit controller is dead code and its
`IsRotating` static is referenced by `PuzzlePiece` (removed in Task 7).

**Files:**
- Delete: `Assets/Scripts/CameraController.cs` (and its `.meta`)

- [ ] **Step 1: Delete the files**

Run:
```bash
cd /c/repos/Uni/tmp/3DPuzzle
git rm Assets/Scripts/CameraController.cs Assets/Scripts/CameraController.cs.meta
```
Expected: both files removed.

> Note: this will leave a temporary compile error in `PuzzlePiece.cs`
> (`CameraController.IsRotating`) and `SceneSetup.cs` (`AddComponent<CameraController>`).
> Tasks 6 and 7 fix those. If you are committing per-task, that is fine — the project
> need not compile mid-refactor as long as it compiles by Task 7. To keep each commit
> green instead, do Tasks 5–7 together and commit once at the end of Task 7.

- [ ] **Step 2: Commit (or defer to Task 7 — see note)**

```bash
git commit -m "refactor: remove orbit CameraController (replaced by AR camera)"
```

---

### Task 6: Rebuild SceneSetup for AR

Replace the orbit-camera setup with an AR rig, parent everything under `PuzzleRoot`, and
wire up `ARPuzzlePlacer`. The piece/ghost/confetti/UI building logic is reused; only the
camera/rig and parenting change.

**Files:**
- Modify: `Assets/Scripts/SceneSetup.cs`

- [ ] **Step 1: Replace the camera/lighting/root section of BuildScene**

In `Assets/Scripts/SceneSetup.cs`, add these `using` directives at the top (after the
existing ones):
```csharp
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;
```

- [ ] **Step 2: Add a PuzzleRoot field and AR rig builder**

Replace the **Camera** and **GameManager** portions of `BuildScene()` (the lines from
the `// Camera` comment through the GameManager creation) with the following. Keep the
Ground and Lighting blocks, but parent the Ground under the new root (see Step 3).

Add this field at the top of the class (above `Awake`):
```csharp
    private Transform puzzleRoot;
```

Insert this at the very start of `BuildScene()` (before Ground):
```csharp
        // Root that parents the entire puzzle so AR placement can move/scale it as one.
        GameObject rootObj = new GameObject("PuzzleRoot");
        puzzleRoot = rootObj.transform;

        // --- AR rig ---
        GameObject sessionObj = new GameObject("AR Session");
        ARSession arSession = sessionObj.AddComponent<ARSession>();
        sessionObj.AddComponent<ARInputManager>();

        GameObject originObj = new GameObject("XR Origin");
        XROrigin xrOrigin = originObj.AddComponent<XROrigin>();

        GameObject camOffset = new GameObject("Camera Offset");
        camOffset.transform.SetParent(originObj.transform, false);
        xrOrigin.CameraFloorOffsetObject = camOffset;

        GameObject camObj = new GameObject("AR Camera");
        camObj.transform.SetParent(camOffset.transform, false);
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 20f;
        camObj.AddComponent<ARCameraManager>();
        camObj.AddComponent<ARCameraBackground>();
        camObj.AddComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
        xrOrigin.Camera = cam;
```

> Note: deleting the old `Camera.main...` lines also removes the
> `AddComponent<CameraController>()` call — that is required (CameraController is gone).

- [ ] **Step 3: Parent Ground, GameManager, and the placer correctly**

Find the Ground block. After it is created, parent it under the root:
```csharp
        ground.transform.SetParent(puzzleRoot, true);
```

Keep the Lighting block as-is (a directional light need not be parented).

For the **GameManager** block, leave it as a top-level object (UI/logic, not spatial).
Keep the existing:
```csharp
        GameObject gm = new GameObject("GameManager");
        GameManager manager = gm.AddComponent<GameManager>();
        manager.totalPieces = 7;
        SetupUI(manager);
```

- [ ] **Step 4: Parent pieces, ghosts, and confetti under the root**

In `CreatePiece(PieceData data)`, after the ghost and piece are created, parent both
under the root. Add at the end of `CreatePiece`:
```csharp
        ghost.transform.SetParent(puzzleRoot, true);
        piece.transform.SetParent(puzzleRoot, true);
```
> `CreatePiece` is an instance method on `SceneSetup`, so it can read `puzzleRoot`
> directly — no signature change needed.

In `BuildScene`, find the Confetti block and parent it too. After
`confettiObj.transform.position = ...`:
```csharp
        confettiObj.transform.SetParent(puzzleRoot, true);
```

- [ ] **Step 5: Attach ARPuzzlePlacer and wire references**

At the end of `BuildScene()` (after the confetti / manager wiring), add:
```csharp
        // Auto-place the whole puzzle in front of the AR camera once tracking is ready.
        ARPuzzlePlacer placer = rootObj.AddComponent<ARPuzzlePlacer>();
        placer.puzzleRoot = puzzleRoot;
        placer.arSession = arSession;
        placer.arCamera = camObj.transform;
        placer.distance = 0.5f;
        placer.rootScale = 0.1f;
```

- [ ] **Step 6: Verify SceneSetup compiles**

In Unity Console (after Tasks 5 and 7 are also applied, since they are interdependent).
Expected: no compile errors once Task 7 is done.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/SceneSetup.cs
git commit -m "feat: build AR rig and parent puzzle under scalable root"
```

---

### Task 7: Rewrite PuzzlePiece touch input

Replace mouse events with touch raycasting. The held piece is dragged along the
horizontal plane at the puzzle base. Snap logic is preserved but the threshold is
interpreted via `PuzzleScale` so it works at tabletop scale.

**Files:**
- Modify: `Assets/Scripts/PuzzlePiece.cs`

- [ ] **Step 1: Replace the entire PuzzlePiece.cs**

Replace the full contents of `Assets/Scripts/PuzzlePiece.cs` with:
```csharp
using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    [Header("Target")]
    public Transform targetPosition;
    public float snapThreshold = 0.5f; // world units (pre-scale)

    [Header("Drag Settings")]
    public float dragHeight = 2f; // world units (pre-scale), height above base while dragging

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
        if (Input.GetMouseButtonDown(0)) TryBeginDrag(Input.mousePosition, -1);
        else if (isDragging && Input.GetMouseButton(0)) UpdateDrag(Input.mousePosition);
        else if (isDragging && Input.GetMouseButtonUp(0)) EndDrag();
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

        float dist = Vector3.Distance(transform.position, targetPosition.position);
        if (dist < effectiveThreshold)
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
```

- [ ] **Step 2: Verify the full project compiles**

In Unity Console (with Tasks 5 and 6 applied).
Expected: no compile errors anywhere. `CameraController.IsRotating` references are gone.

- [ ] **Step 3: Run Edit Mode tests again (regression)**

In Unity: **Test Runner → EditMode → Run All**.
Expected: the 3 `PuzzleScale` tests still PASS.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/PuzzlePiece.cs
git commit -m "feat: rewrite PuzzlePiece input for AR touch dragging"
```

---

### Task 8: Configure iOS + XR project settings (Unity editor)

These are Unity Editor settings, not code. Perform them in the Unity Editor; they are
saved into `ProjectSettings/` and committed.

**Files:**
- Modify (via editor): `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/XRSettings` assets

- [ ] **Step 1: Switch platform to iOS**

In Unity: **File → Build Settings → iOS → Switch Platform**.
Expected: the iOS logo highlights; Unity reimports assets.

- [ ] **Step 2: Enable ARKit in XR Plug-in Management**

**Edit → Project Settings → XR Plug-in Management** → (install if prompted) → on the
**iOS** tab, check **ARKit**.
Expected: ARKit listed as the active iOS XR provider.

- [ ] **Step 3: Set the camera usage description**

**Project Settings → Player → iOS → Other Settings → Camera Usage Description**:
```
Camera is used to display the AR puzzle.
```

- [ ] **Step 4: Set minimum iOS / architecture**

In the same **Player → iOS → Other Settings**:
- **Target minimum iOS Version:** `13.0` (ARKit/AR Foundation 5.x requirement).
- **Architecture:** `ARM64`.
- **Requires ARKit support** (if shown under AR settings): enable.

- [ ] **Step 5: Commit the project settings**

```bash
git add ProjectSettings
git commit -m "chore: configure iOS build and ARKit XR settings"
```

---

### Task 8b: Configure Android + ARCore project settings (Unity editor)

Android-side equivalent of Task 8. No code changes — AR Foundation uses the same C#;
only the provider and Player settings differ. ARCore has stricter requirements than
ARKit (no Vulkan, IL2CPP, ARM64, min API 24). These are Editor settings saved into
`ProjectSettings/`.

**Files:**
- Modify (via editor): `ProjectSettings/ProjectSettings.asset`, XR settings assets

- [ ] **Step 1: Switch platform to Android**

In Unity: **File → Build Settings → Android → Switch Platform**.
Expected: the Android logo highlights; Unity reimports assets.

- [ ] **Step 2: Enable ARCore in XR Plug-in Management**

**Edit → Project Settings → XR Plug-in Management** → on the **Android** tab, check
**Google ARCore**.
Expected: ARCore listed as the active Android XR provider.

- [ ] **Step 3: Remove Vulkan (ARCore requires OpenGLES3)**

**Project Settings → Player → Android → Other Settings → Rendering**:
- Uncheck **Auto Graphics API**.
- In the **Graphics APIs** list, remove **Vulkan**, leave only **OpenGLES3**.
> AR Foundation's ARCore background renderer does not support Vulkan; leaving it in
> causes a black/garbled camera feed or a crash on launch.

- [ ] **Step 4: Set scripting backend, architecture, min API**

In the same **Player → Android → Other Settings**:
- **Scripting Backend:** `IL2CPP` (required for ARM64).
- **Target Architectures:** check **ARM64**, uncheck ARMv7 (ARCore + Play Store require 64-bit).
- **Minimum API Level:** `24` (Android 7.0) or higher — ARCore requirement.
- The camera usage permission is added automatically by the ARCore plugin's manifest;
  no manual Info.plist-style string is needed on Android.

- [ ] **Step 5: Commit the project settings**

```bash
git add ProjectSettings
git commit -m "chore: configure Android build and ARCore XR settings"
```

> Note: iOS and Android share the same `ProjectSettings.asset`. Switching platforms in
> Unity does not lose the other platform's settings — both ARKit (iOS tab) and ARCore
> (Android tab) stay enabled, and each platform reads its own Player settings block.

---

### Task 9: Verify scene contains the SceneSetup bootstrapper

The whole scene is built procedurally by `SceneSetup` on `Awake`. Confirm `MainScene`
has a GameObject carrying `SceneSetup` so the AR build actually bootstraps.

**Files:**
- Possibly modify (via editor): `Assets/Scenes/MainScene.unity`

- [ ] **Step 1: Open MainScene and check for SceneSetup**

In Unity, open `Assets/Scenes/MainScene.unity`. Look in the Hierarchy for any GameObject
with the `SceneSetup` component.

- [ ] **Step 2: If missing, add it**

If no object has `SceneSetup`: create an empty GameObject (**GameObject → Create Empty**),
name it `Bootstrap`, and **Add Component → SceneSetup**. Remove any leftover default
`Main Camera` object in the scene (the AR rig creates its own camera) to avoid two
cameras tagged `MainCamera`.

- [ ] **Step 3: Save and commit (if changed)**

```bash
git add Assets/Scenes/MainScene.unity
git commit -m "chore: ensure MainScene bootstraps SceneSetup for AR"
```
> If the scene was already correct and unchanged, skip the commit.

---

### Task 10: Build to Xcode and deploy to iPhone (Mac only)

**Prerequisite:** perform on the Mac with Unity + Xcode installed, iPhone connected, and
a free personal Apple ID added to Xcode (**Xcode → Settings → Accounts**).

- [ ] **Step 1: Pull the branch onto the Mac**

On the Mac, in the repo:
```bash
git fetch origin
git checkout ar-house-puzzle
```
(or pull if the branch is already checked out)

- [ ] **Step 2: Build the Xcode project from Unity**

In Unity on the Mac: **File → Build Settings → Add Open Scenes** (ensure `MainScene` is
checked) → **Build** → choose an output folder (e.g. `build/ios`).
Expected: Unity produces an Xcode project (`Unity-iPhone.xcodeproj`).

- [ ] **Step 3: Open in Xcode and set signing**

Open `Unity-iPhone.xcodeproj` → select the **Unity-iPhone** target → **Signing &
Capabilities** → check **Automatically manage signing** → select your personal Team →
set a unique **Bundle Identifier** (e.g. `com.<yourname>.arhousepuzzle`).

- [ ] **Step 4: Run on device**

Select your connected iPhone as the run target → press **Run (▶)**.
Expected: app installs. On first launch, iOS prompts to **trust the developer**
(Settings → General → VPN & Device Management) — trust it, then relaunch.

- [ ] **Step 5: Grant camera permission**

On launch, accept the camera permission prompt.
Expected: live camera feed appears as the background.

> No commit in this task — it produces a device build, not source changes. The `build/`
> output should not be committed (add to `.gitignore` if Unity created it inside the repo;
> see Task 11 Step 2).

---

### Task 10b: Build and deploy to Android (any OS)

Android builds work from Windows, Mac, or Linux — no Mac required. Unity ships its own
Android SDK/NDK/JDK (install via Unity Hub → Add Modules → Android Build Support if not
present).

**Prerequisite:** an ARCore-supported Android device with **USB debugging** enabled
(Settings → Developer options), connected via USB. Confirm "Google Play Services for AR"
will install from the Play Store on first launch (Unity handles this automatically when
ARCore is set to "Required").

- [ ] **Step 1: Confirm platform is Android**

Ensure Task 8b was done (platform switched to Android, ARCore enabled, Vulkan removed,
IL2CPP/ARM64/min API 24 set).

- [ ] **Step 2: Build and run**

In Unity: **File → Build Settings → Add Open Scenes** (ensure `MainScene` is checked) →
select your connected device in the **Run Device** dropdown → **Build And Run** → choose
an output `.apk` path (e.g. `build/android/ARHousePuzzle.apk`).
Expected: Unity compiles, installs the APK, and launches it on the device.

- [ ] **Step 3: Grant camera permission**

On launch, accept the camera permission prompt (and let "Google Play Services for AR"
install/update if prompted).
Expected: live camera feed appears as the background.

> No commit — produces a device build, not source. Same `build/` ignore note as Task 10.

---

### Task 11: Manual on-device acceptance test + docs

**Files:**
- Modify: `README.txt`
- Possibly modify: `.gitignore`

- [ ] **Step 1: Run the acceptance checklist on the iPhone**

Walk through each item; all must pass:

1. App launches; live camera (passthrough) is visible.
2. Within ~1–2 s, the puzzle (green ground, white pulsing ghost outlines, coloured
   scattered pieces) appears ~0.5 m in front of the phone at tabletop (~40 cm) scale.
3. Moving the phone lets you view the puzzle from different angles; it stays anchored in
   place (does not follow the phone).
4. Touching a scattered piece highlights it; dragging slides it across the base plane.
5. Releasing a piece near its ghost snaps it into place; that ghost disappears.
6. The "Placed: N/7" counter increments on each successful placement.
7. After the 7th piece: the win panel ("Congratulations! Puzzle Complete!") shows and
   confetti plays.
8. Tapping **Reset** restarts the puzzle (pieces scattered again, counter back to 0/7).

If any step fails, file the symptom and revisit the relevant task (placement → Task 4;
touch/snap → Task 7; rig/scene → Tasks 6/9; build/permission → Tasks 8/10).

- [ ] **Step 2: Ignore the iOS build output (if inside the repo)**

If Unity wrote the build inside the repo, create/append `.gitignore`:
```
# Unity iOS build output
/build/
```
Then:
```bash
git add .gitignore
git commit -m "chore: ignore iOS build output"
```

- [ ] **Step 3: Update README for AR**

Replace the **Управление** (Controls) and **Стартиране** (Run) sections of `README.txt`
to describe AR. Set the Controls section to:
```
Управление (AR)
---------------
- Докосване и плъзгане: хващане и местене на парче върху контура
- Движение на телефона: разглеждане на пъзела от различни ъгли
- Бутон "Reset": започва играта отначало
```
And set the Run section to:
```
Стартиране (iOS AR)
-------------------
1. Отворете проекта в Unity 2022.3, превключете платформата на iOS
   (File -> Build Settings -> iOS -> Switch Platform).
2. Build -> отворете генерирания Xcode проект на Mac.
3. Настройте подписване (Signing) с Apple ID и стартирайте на iPhone.
4. Разрешете достъп до камерата при първото стартиране.
```
Also update the Технологии section to add: `- AR Foundation + ARKit (iOS)`.

- [ ] **Step 4: Commit the docs**

```bash
git add README.txt
git commit -m "docs: update README for AR controls and iOS build"
```

---

## Self-Review

**Spec coverage:**
- AR-not-VR decision & rationale → captured in plan intro + spec (no task needed; it's a decision).
- Packages (arfoundation, arkit) → Task 3. ✓
- iOS settings / camera usage / ARKit XR → Task 8. ✓
- AR rig replaces orbit camera → Tasks 5 (delete) + 6 (build rig). ✓
- PuzzleRoot parenting everything → Task 6 (ground, pieces, ghosts, confetti). ✓
- Auto-placement ~0.5 m / ~0.1 scale → Task 4 (`ARPuzzlePlacer`) + wired in Task 6. ✓
- Touch interaction rewrite, drag on base plane → Task 7. ✓
- Snap threshold in scaled space → Task 2 (`PuzzleScale`) + used in Task 7. ✓
- Unchanged GameManager/GhostTarget/UI/index.html → not touched by any task. ✓
- Build & deploy loop → Task 10. ✓
- Manual test checklist → Task 11 Step 1 (mirrors spec testing strategy). ✓
- Piece count stays 7 → Task 6 keeps `totalPieces = 7`. ✓

**Placeholder scan:** No TBD/TODO; every code step shows complete code; commands have
expected output. ✓

**Type consistency:** `PuzzleScale.ScaledDistance(float, float)` defined in Task 2 and
called with the same signature in Task 7. `ARPuzzlePlacer` public fields (`puzzleRoot`,
`arSession`, `arCamera`, `distance`, `rootScale`) defined in Task 4 and all set in Task 6
Step 5. `XROrigin.Camera` / `CameraFloorOffsetObject` are AR Foundation 5.x API names. ✓

**Known accepted limitation:** AR behavior is verified manually on-device (Task 11), not
by automated tests — inherent to ARKit. Only `PuzzleScale` is unit-testable and is tested.
