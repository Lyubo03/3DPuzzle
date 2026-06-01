# AR House Puzzle — Design Spec

**Date:** 2026-06-01
**Status:** Approved, ready for implementation planning

## Decision: AR (not VR)

The project must be either VR or AR, "whichever is easier." Given the available
hardware, **AR wins decisively**:

- **Hardware:** AR-capable **iPhone (iOS)** + a **Mac with Xcode**. No VR headset.
- VR is ruled out: no headset to test on, and the only browser VR API (WebXR) is
  unsupported by iOS Safari. Building VR that cannot be tested is not "easier."
- WebXR AR in the browser (building on the existing `index.html`) is also ruled out:
  iOS Safari does not support WebXR.
- **Chosen path:** Unity + AR Foundation (ARKit), native iOS app. It is the only
  route that delivers a *real, interactive* AR puzzle on the iPhone, and it reuses
  the existing Unity scripts.

## Overview & Scope

Convert the existing Unity 2022.3 puzzle into a handheld AR app for iPhone. On launch,
the entire puzzle (ground + ghost outlines + scattered pieces) spawns as a small
(~40 cm) model floating a fixed distance in front of the camera. The user moves the
phone to view it from different angles — the device pose *is* the camera, so there is
no orbit camera. The user drags scattered pieces with touch onto the glowing ghost
targets. Snapping, the progress counter, the win screen, and the confetti all carry
over from the existing implementation.

**Interaction model:** Auto-place in front of the camera (no plane detection). This is
the most demo-reliable choice — it removes the flakiest part of AR.

**Source of truth:** The Unity C# version. `index.html` (Three.js) is left untouched
as a desktop fallback. Piece count stays at Unity's **7** (Floor, FrontWall, BackWall,
LeftWall, RightWall, Roof, Door).

## Components

### 1. Packages & project settings
- Add to `Packages/manifest.json`:
  - `com.unity.xr.arfoundation`
  - `com.unity.xr.arkit`
- iOS build target and settings (Mac-side editor checklist, included in plan):
  - Switch platform to iOS.
  - Camera usage description string in Player Settings (Info.plist
    `NSCameraUsageDescription`), e.g. "Camera is used to display the AR puzzle."
  - Enable ARKit in XR Plug-in Management.
  - Set ARKit as a required capability / minimum iOS version per AR Foundation docs.

### 2. AR rig (replaces orbit camera)
- New `ARSetup.cs` (or extension of `SceneSetup`) builds:
  - **AR Session** GameObject.
  - **XR Origin** with an **AR Camera** (Camera + ARCameraManager + ARCameraBackground +
    TrackedPoseDriver).
- The old `CameraController` orbit logic is **removed**. The phone's physical movement
  controls the view.
- A `PuzzleRoot` empty GameObject parents everything `SceneSetup` creates (ground,
  ghosts, pieces, confetti) so the whole puzzle can be positioned and scaled as one unit.

### 3. Auto-placement
- New `ARPuzzlePlacer.cs`: on the first frame after AR tracking is ready, position
  `PuzzleRoot` ~0.5 m in front of the AR camera along its forward vector, and set
  `PuzzleRoot.localScale` to ~0.1 so the ~4 m house becomes ~40 cm. After placement,
  the puzzle stays world-anchored (it does not follow the phone).

### 4. Touch interaction (rewrite of PuzzlePiece input)
- Replace `OnMouseDown` / `OnMouseDrag` / `OnMouseUp` (mouse-plane logic) with touch
  raycasting:
  - A touch raycasts against piece colliders.
  - The held piece is dragged along the **horizontal plane of the puzzle base**, so
    pieces slide on the "table" surface.
  - Snap threshold logic and `Snap()` are unchanged in behavior, scaled to the new size.
- The `CameraController.IsRotating` guard is dropped (no orbit).

### 5. Unchanged
- `GameManager` — progress / win / confetti.
- `GhostTarget` — pulsing target outline.
- Win panel and Reset button UI (Screen Space Overlay works on mobile).
- The piece / ghost definitions in `SceneSetup`.

## Data flow

1. App launches → `ARSetup` creates AR Session + XR Origin + AR Camera.
2. `SceneSetup` builds the puzzle under `PuzzleRoot` (ground, ghosts, scattered pieces,
   confetti, UI).
3. AR tracking becomes ready → `ARPuzzlePlacer` positions and scales `PuzzleRoot` in
   front of the camera, once.
4. User touches a piece → `PuzzlePiece` raycast hit → drag on base plane.
5. Release within snap threshold → `Snap()` → ghost hides → `GameManager.PiecePlaced()`.
6. 7/7 placed → `GameManager.Win()` → win panel + confetti.
7. Reset button → reload scene (existing behavior).

## Error / edge handling
- If AR tracking is not yet ready, `ARPuzzlePlacer` waits (does not place) until the
  camera pose is valid.
- Touch input ignores already-placed pieces (existing `isPlaced` guard kept).
- Multi-touch: only the first active touch drags a piece; additional touches ignored.
- Scale: snap threshold, drag height, and any world distances must be interpreted in
  `PuzzleRoot` local space (scaled by ~0.1) so snapping still works at tabletop size.

## Build & test loop
- Build from Unity → generates an Xcode project.
- Open in Xcode on the Mac, sign with a free personal Apple ID, deploy to the iPhone.
- The plan will include exact menu steps and the Info.plist camera-permission string.

## Testing strategy
AR cannot be unit-tested in-editor (it needs a real device). Manual test checklist:
1. App launches; camera feed (passthrough) is visible.
2. Puzzle appears ~0.5 m in front of the phone at tabletop scale.
3. Moving the phone lets you view the puzzle from different angles; it stays anchored.
4. Each piece can be touched and dragged.
5. A piece released near its ghost snaps into place; ghost disappears.
6. Progress counter increments per placement.
7. At 7/7: win panel shows and confetti plays.
8. Reset button restarts the puzzle.

## Notes / flags
- Unity `totalPieces = 7` matches its 7 `PieceData` entries — consistent. The web
  version's 6 is intentionally left as-is (separate fallback).
- The XR/VR modules already present in `manifest.json` are Unity built-in modules, not
  AR Foundation; AR Foundation + ARKit packages still need to be added.
