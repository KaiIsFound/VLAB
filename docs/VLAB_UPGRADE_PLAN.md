# VLAB Chemistry Lab — Phased Upgrade Plan

- Plan version: 1.4 execution summary
- 2026-09-10 current increment: physical grab/tabletop fix, Desktop UI/HUD, safe floor teleport with ray select/release and actual snap 30° input. PlayMode 19/19 at `DemoDay/20260910T054327Z-locomotion-contact-gate-PlayMode/results.xml`. Full Phase 2 is still open (continuous/vignette, seated/calibration and remaining XR matrix); later phases are not implicitly waived by the demo deadline. `VLAB_TASK_DIFFICULTY.md` lists outstanding large tasks.
- Detailed cold-start blueprint: `plans/VLAB-PCVR-OPENXR-DESKTOP-BLUEPRINT.md`
- Current gate: **Phase 1 passed on 2026-09-01**; manifest independently verified against 1,905 source files
- Current Phase 1 status: Windows x64 release/development builds succeeded; no-runtime hardware request fell back safely to Desktop; final EditMode 46/46 and PlayMode 3/3
- Current work: P2.b hands-on titration integrated into v10; Desktop mouse grab/use/release and the old reagent shelf upgraded on 2026-09-09. Current verification details and controller limits: `Docs/VLAB_VERIFICATION_REPORT.md`; controls: `Docs/HANDS_ON_LAB.md`.
- Difficulty split and priority: `Docs/VLAB_TASK_DIFFICULTY.md`; hard tasks H1–H8 first, easy prerequisites included where necessary.
- Terminal claim hiện tại: **Windows Build Verified for Phase 1 only**; toàn bộ VLAB chưa hoàn tất và PCVR hardware vẫn `Not tested`

## Invariants

1. Scene layout chỉ thay đổi qua Builder/prefab/Editor API; không sửa YAML ChemistryLab thủ công.
2. Mỗi phase phải compile và đạt gate bằng artifact mới trước phase kế tiếp.
3. Chỉ một rig, Main Camera đang render và AudioListener active theo mode.
4. Desktop, XR Simulator và OpenXR Hardware dùng chung experiment state.
5. Không bật URP trước material/shader/glass/liquid/TMP audit ở Phase 4.
6. Không dùng simulator làm bằng chứng stereo/tracking/haptic/comfort phần cứng.
7. Không commit/push; workspace chưa có commit và toàn bộ project đang untracked.
8. Mọi run test phải có discovered/executed count > 0; exit code một mình không đủ.

## Dependency graph

`P0 Audit → P1 XR foundation → P2 interaction/locomotion → P3 UI/type → P4 art/render → P5 chemistry flow → P6 feedback/accessibility/recovery → P7 performance/build/regression`

Các phase tuần tự. Chỉ workstream sở hữu file độc lập bên trong một phase mới được song song; mọi Unity invocation trên checkout chính chạy tuần tự.

## Phase plan và quality gate

| Phase | Deliverable | Verification bắt buộc | Gate/rollback |
|---|---|---|---|
| 0 — Audit/baseline | Recovery checkpoint, renderer/XR/content audit, 11 captures, risk register | Restore probe, compile, pre/post renderer sentinel, SHA-256 inventories, 24 EditMode tests | **Passed structurally (plan 1.3)**; plan 1.2 evidence superseded |
| 1 — XR foundation | PlayMode assembly; XR Plug-in Management/OpenXR supported versions; Windows settings/profiles; three-mode state machine; canonical rig; fallback | OpenXR Project Validation, Edit/PlayMode, Desktop + simulator smoke, one camera/listener | **Passed — Windows build verified**; release strips simulator, no-runtime request falls back to Desktop, Built-in retained |
| 2 — Interaction/locomotion | Semantic input; Direct/Ray; teleport/snap default; optional continuous; standing/seated; grab/button/recovery | Two-hand simulator tests, debounce, bounds/floor, desktop UI/zoom/crouch conflicts | Disable new adapter/providers independently; preserve experiment state |
| 3 — Typography/UI | Licensed Vietnamese SDF TMP asset; tokens/styles; world/desktop UI; settings and collapse/reposition | Glyph scan, contrast/layout tests, fixed-distance screenshot inspection | Keep legacy labels available until new validator/captures pass |
| 4 — Art/render | Material/shader inventory; explicit Built-in-vs-URP decision; props/glass/liquid/lighting/probes | Renderer matrix, no pink, before/after captures, perf delta ≤ 20% | URP trial isolated and fully reversible; default remains Built-in until pass |
| 5 — Chemistry logic | Presentation-independent state machine; Titration, Daniell and CuSO4 electrolysis end-to-end | RED/GREEN EditMode + Desktop/simulator E2E, wrong-order and reset | Data definition and station integration separated; rollback per experiment |
| 6 — Audio/haptic/a11y/recovery | Mixer/services, pooled sources, haptic abstraction, persisted settings, checkpoint recovery | Settings effect tests, source pooling, recovery, simulator structural haptic | Hardware haptic remains Not tested without headset |
| 7 — Performance/final | Four quality presets, profiler suite, full regression, Windows x64 build/smoke, final captures | Edit/PlayMode/simulator nonzero; build launch; measured frame/GC/draw data | No release claim if required row lacks artifact |

## Phase 1 execution order

1. **Done — P1.a:** Add a PlayMode test assembly and a minimal boot smoke test while `activeInputHandler: Both` remains unchanged.
2. **Done — P1.b:** Query Unity Package Manager for versions supported/recommended by Unity 6000.5.6f1; pin XR Plug-in Management 4.5.4 and OpenXR 1.17.1 without changing XRI/Input System.
3. **Done — P1.b:** Configure Windows Standalone OpenXR loader, manual startup and four reviewed controller profiles; Project Validation 0 errors/0 warnings.
4. **Done — P1.c:** Introduce `Desktop`, `XRSimulator`, `OpenXRHardware` enum/state machine with explicit detection precedence, async init timeout/failure/cancellation reason and state-preserving Desktop fallback.
5. **Done — P1.atomic-builder:** checkpoint bắt buộc, full preflight, temporary scene/validate/swap và injected-failure tests giữ nguyên target scene hash.
6. **Done — P1.evidence-bootstrap:** schema v2, blocker rejection và checkpoint/source binding đã close + verify trước scene mutation.
7. **Done — P1.d1:** loader init/start/stop/deinitialize cleanup; VLAB-owned rig/simulator/input assets; Floor/Device fallback, Direct Interactor hai tay và tracking-loss guard.
8. **Done — P1.d2:** Builder/prefab integration nguyên tử từ checkpoint `CP-P1.d2-pre-20260901T140000Z`; marker v9; scene hash mới `6c5491…5232`; checkpoint giữ hash gốc `1ad6bc…1575`.
9. **Done — P1.d3:** scene mode-switch smoke 3/3; một camera/listener/MainCamera; release build strip simulator; Development player no-runtime fallback Pass.
10. **Done — P1.gate:** final EditMode 46/46, PlayMode 3/3, OpenXR validation 0 mandatory errors, Windows build/player smoke Pass; source manifest `db3c87…e373` verified.

## Evidence status vocabulary

- `Implemented`: code/asset exists, not yet run.
- `Structurally verified`: compile/validation/EditMode evidence exists.
- `Simulator verified`: PlayMode/XR simulator run exists.
- `Build verified`: Windows player built/launched and smoke-tested.
- `Hardware verified`: headset/controller/runtime measurement exists.
- `Not tested`: no valid evidence; never promoted by inference.

## Plan mutation protocol

Any split/reorder/skip must record affected acceptance IDs in the detailed blueprint, invalidate downstream artifacts whose source fingerprint changed, and retain a safe rollback. A failed method is logged and a safe alternative is attempted; the gate remains failed if a required evidence row is still absent.
