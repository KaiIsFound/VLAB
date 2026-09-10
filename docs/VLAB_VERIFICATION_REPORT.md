# VLAB Chemistry Lab — Verification Report

Latest update 2026-09-10: full PlayMode **19/19** at `DemoDay/20260910T054327Z-locomotion-contact-gate-PlayMode/results.xml`. Includes held-bottle tabletop collision, teleport ray select/release moving the rig, floor/furniture/bounds rejection and actual snap input rotating 30 degrees. FloorTileLine decorative colliders no longer block floor rays. Crouch checks exact capsule reduction and eye clearance bounded by its configured skinWidth. Grab uses bounded velocity-based Rigidbody tracking in saved scenes at runtime. UI readability and held-vessel HUD upgraded. See `DEMO_DAY_CHECKLIST.md` for the Windows artifact. This is not a full P2–P7 gate or hardware verification; remaining tasks are in `VLAB_TASK_DIFFICULTY.md`.

Everything below is historical evidence; earlier failures/counts do not override the latest dated gate above.

Current source EditMode: **60/60**, `DemoDay/20260910T054509Z-locomotion-final-EditMode/results.xml`. Current Windows release: `Builds/DemoDay-20260910-locomotion`, Succeeded/0 errors/1 warning, player smoke Pass/exit 0 and simulator absent. Warning is the TMP URP Lit shader stripped in the Built-in pipeline; retained in `build-messages.txt`. Startup screenshot inspected; not a full manual experiment or headset run.

- Report date: 2026-09-09
- **Demo Windows release delivered:** `Builds/DemoDay-20260909/VLAB-Demo-Windows.zip`; Build Succeeded (0 errors, 1 warning), player smoke Pass/exit 0, startup UI screenshot inspected. See JSON and PNG in the same build directory. Hardware VR, exhaustive manual simulator usability and performance on target hardware remain unverified; this is the scoped demo, not completion of the full product roadmap.
- **Latest demo gate (evening): 60/60 EditMode + 16/16 PlayMode passed.** Evidence: `DemoDay/20260909T160815Z-release-gate-EditMode/results.xml` and `DemoDay/20260909T160903Z-release-gate-PlayMode/results.xml` under `Artifacts/VLABUpgrade/plan-1.4/`. XR Device Simulator is now installed in the rebuilt scene; decorative Desktop hands removed. Trigger test failure was caused by injecting a button edge before a render-only input update; Dynamic-phase injection passes the real XRI activation path. Hardware remains not tested. See `DEMO_DAY_CHECKLIST.md` for current delivery scope; the entries below retain earlier phase history.
- Current Desktop grab verification: **3/3 tests passed**, including mouse click/pick/move/F/use/Escape, manager restoration on disable, and all four former shelf props (including upright pipette). Full PlayMode: **14/15 passed**, artifact `Artifacts/VLABUpgrade/plan-1.4/P2.hands-on/20260909T094849Z-grab-final-PlayMode/results.xml`.
- Final EditMode: **60/60 passed**, artifact `Artifacts/VLABUpgrade/plan-1.4/P2.hands-on/20260909T095059Z-desktop-grab-verification-EditMode/results.xml`; includes atomic Builder rollback and chemistry/liquid tests.
- Remaining failed test: `ControllerSignals_DriveGripTriggerAndTrackingLoss`. Grip selection and input reading succeed, but Trigger emits no activation event in this test. Disabled-manager and group registration defects were repaired; the Trigger path is still unverified. Do not claim the XR hands-on gate passed or hardware support tested.
- Scene upgraded via checkpoint `CP-P2-desktop-grab-20260909`; `desktop-shelf-build.log` records successful atomic build. Eleven liquid vessels/tools are wired. Desktop runtime adapter is added by `DesktopLabNavigator.Awake`, so no manual component wiring is required.
- P2.a historical input evidence: PlayMode 6/6 and EditMode 49/49; see `Docs/P2A_TDD_EVIDENCE.md`. Phase 1 fingerprints/builds below are historical, not verification of current P2 changes.
- Historical Phase 1 build evidence: `Artifacts/VLABUpgrade/plan-1.4/P1.gate/20260901T180000Z-windows-development-smoke`
- Historical Phase 1 source fingerprint (`Assets/`, `Packages/`, `ProjectSettings/`, 1,905 files): `db3c87a1ca99d832748055cd6bd7e3f13487599668db45533a25ff4eff0ee373`
- P1.d2 pre-integration checkpoint: `CP-P1.d2-pre-20260901T140000Z`; original scene SHA-256 `1ad6bcd2774b0ec8e63c2585091bc0ce704a362dbd854dd24d74fef2a8c01575`
- Historical full checkpoint fingerprint (1,862 files including docs/root files): `97d59be614b6daad11dea9cec047c60415c5ab76132c1aaed609d40d8ff10826`
- Current terminal status: **Phase 1 passed — Windows Build Verified for this phase; P2.a verified; P2.b in progress; hardware path remains Not tested**
- Full product status: **Not complete**

| Hạng mục | Trạng thái | Bằng chứng | Giới hạn còn lại |
|---|---|---|---|
| Recovery checkpoint | Structurally verified | Snapshot hash verified; independent project copy imported/compiled; restored fingerprint matched | Không phải VCS; phải tạo checkpoint mới trước package/settings/scene mutation |
| Unity compile | Structurally verified | `phase0-audit-gui.log`, GUI process exit 0; audit method executed | Batch mode Licensing Client bị timeout |
| Packages | Build verified | XR Management 4.5.4 + OpenXR 1.17.1; exact lock diff allowlisted; release/development Windows builds succeeded | Working OpenXR runtime/headset path chưa có |
| Render pipeline | Structurally verified | Graphics, all Quality overrides and effective pipeline đều Built-in | URP decision deferred to Phase 4 |
| Scene generation | Structurally verified | Atomic Builder generated marker v9 from verified checkpoint; target changed to `6c5491…5232`, checkpoint retained `1ad6bc…1575`; post-build 46/46 + 2/2 | Future Builder integrations must create a new checkpoint and marker |
| Missing Script | Structurally verified | 0 | Chỉ ChemistryLab scene baseline |
| Material/shader | Structurally verified + screenshot inspected | 0 missing slots, 0 unsupported shader; no pink in 11 captures | Chưa test player shader stripping |
| Desktop camera/listener | Build verified | Editor scene switch and Windows player sentinel each report exactly 1 active camera, 1 listener and 1 MainCamera tag | Headset stereo camera inspection remains hardware-only |
| XR rig components | Structurally verified | VLAB-owned XR Origin/simulator prefabs; scale 1; Floor preferred + Device offset; 2 Direct Interactors and 2 tracking-loss guards | Full two-hand/ray/locomotion behavior belongs to Phase 2 |
| Desktop mode | Build verified | Development player requested Hardware, loader failed, reason `LoaderInitializationFailed`, same player fell back to safe Desktop and exited 0 | Performance not profiled |
| XR Simulator | Presentation smoke verified | Canonical simulator object activates only in `XRSimulator`; scene switch 3/3; release build strips one simulator root | Head/hand action, ray/direct, teleport and grab matrix remains Phase 2; not hardware evidence |
| OpenXR hardware | Not tested — hardware required | Không có headset/runtime evidence | Stereo, pose, tracking, origin, haptic, comfort |
| EditMode tests | Structurally verified | Final Phase 1 regression 46/46; includes coordinator lifecycle, rig structure and atomic Builder failure paths | Phase 2–7 coverage not yet implemented |
| PlayMode tests | Presentation smoke verified | Final 3/3; loads ChemistryLab and switches Desktop → Simulator → Hardware → Desktop while checking camera/listener/tag | Does not simulate tracked hand actions yet |
| Existing scene validation | Structurally verified | Pass, không rebuild scene | Validator còn chứa legacy expectations phải thay dần theo phase |
| Visual baseline | Screenshot inspected | 11 PNG 1920×1080 | Không phải headset inspection; nhiều issue UX/art đã ghi trong audit |
| Windows build | Build verified | Release 130,359,794 bytes, Development 200,535,508 bytes, both 0 build errors; player exit 0 with fallback sentinel | Release player launch not separately profiled; hardware runtime unavailable |
| Performance | Not tested | Host/GPU đã ghi | Chưa có player/profiler frame time, draw calls, GC/frame |
| Audio/haptic | Not implemented | 0 AudioSource; haptic chỉ từ sample components | Chưa có mixer/service/hardware test |
| Experiments | Partial, structurally verified | Titration pure logic + 24 tests; legacy validation 3 concordant trials | Daniell/electrolysis mới là step counter, chưa E2E đúng hóa học |
| P1.a PlayMode bootstrap | Structurally verified | Final run: 2 discovered/executed/passed; EditMode regression 24/24; `activeInputHandler: 2` | Chỉ là test infrastructure/input freeze, chưa xác minh XR mode |
| P1.b packages/settings | Structurally verified | UPM recommended: XR Management 4.5.4, OpenXR 1.17.1; lock allowlist verified; Project Validation 0/0; EditMode 28/28, PlayMode 2/2 | Loader/runtime startup chưa được mode router hoặc player smoke xác minh |
| P1.c mode decision/coordinator | Structurally verified | 11/11 coordinator tests; latest EditMode 39/39, PlayMode 2/2; scene/package/renderer/input hashes protected | Pure logic only; chưa nối `ChemistryLabModeController`, rig hay loader runtime thật |
| P1.atomic-builder | Structurally verified | 44/44 EditMode; 5/5 injected preflight/build/validation/swap/missing-checkpoint tests; target scene hash, packages và renderer settings giữ nguyên; temp scene cleanup pass | Success-path replacement được hoãn cho P1.d cùng checkpoint integration mới |
| Windows OpenXR config | Implemented + structurally verified | OpenXR là loader duy nhất; auto-start false; Khronos Simple/Oculus Touch/Valve Index/Microsoft Motion enabled | Không có headset/runtime hardware evidence |
| P1.evidence-bootstrap | Structurally verified | Plan 1.4 checkpoint/source binding closed and independently verified before mutation | Immutable historical P1.a–atomic gates carried by hash |
| P1.d1 lifecycle/assets | Structurally verified | 46/46 EditMode, 2/2 PlayMode; loader cleanup, owned rig/simulator/input assets, tracking guard | No real controller tracking evidence |
| P1.d2 scene integration | Structurally verified | Atomic success build v9; pre/post regressions 46/46 and 2/2; Built-in sentinel unchanged | Visual XR inspection not performed |
| P1.d3 release/smoke | Build verified | Release processor stripped 1 simulator root; Development build kept it test-only; no-runtime fallback player sentinel Pass | Working runtime/hardware loader success path Not tested |
| P1.gate | Pass | Gate manifest/source inventory verified: `db3c87…e373`; blockers empty | Pass is limited to Phase 1 acceptance scope |

## Baseline environment

| Thuộc tính | Giá trị |
|---|---|
| OS | Windows 10 64-bit (10.0.19045) |
| CPU | Intel Xeon E5-2680 v4 @ 2.40 GHz |
| GPU | NVIDIA GeForce GTX 1050 Ti |
| VRAM | 4,004 MB |
| Unity | 6000.5.6f1 |
| Resolution capture | 1920×1080 |
| Color Space | Linear |
| Active quality | Ultra |
| MSAA | 8x |
| Shadow distance/resolution | 80 m / Very High |
| Refresh rate | Not measured |
| Render scale | Not applicable/not measured in Built-in baseline |
| Draw calls/batches | Not measured |
| CPU/GPU frame time | Not measured |
| GC allocation/frame | Not measured |

## Files changed through P1.gate

- `Assets/Editor/ChemistryLabSceneMigrator.cs`: auto-rebuild → detector-only.
- `Assets/Editor/ChemistryLabEditorValidation.cs`: fixed bottle-label false positive.
- `Assets/Editor/VLABUpgrade/VLABPhase0Audit.cs`: immutable unique-run output, read-only inventory, fixed captures, pre/post renderer sentinel và GUI verification entry points.
- `Assets/Tests/PlayMode/VLAB.ChemistryLab.Tests.PlayMode.asmdef`: PlayMode test assembly chuẩn với `UNITY_INCLUDE_TESTS`.
- `Assets/Tests/PlayMode/PlayModeBootstrapTests.cs`: frame-entry smoke và Input Handling `Both` freeze.
- `Assets/Editor/VLABUpgrade/VLABP1OpenXRConfigurator.cs`: checkpoint-gated Windows loader/settings/profile configuration và validation export.
- `Assets/Tests/EditMode/OpenXRConfigurationTests.cs`: loader/manual-start/profile/package/renderer invariants.
- `Assets/VLABChemistryLab/Scripts/Mode/VLabPresentationMode.cs`: resolver/session/coordinator cho Desktop, simulator và OpenXR hardware; fallback giữ cùng experiment-state reference.
- `Assets/VLABChemistryLab/Scripts/ChemistryLabModeController.cs`: live three-mode wiring and deterministic loader lifecycle cleanup.
- `Assets/VLABChemistryLab/Scripts/Mode/VLabPlayerSmokeProbe.cs`: opt-in player sentinel for camera/listener/fallback evidence.
- `Assets/VLABChemistryLab/Scripts/Interaction/XRTrackedControllerGuard.cs`: disables renderer/interactors on tracking loss without relocating hands.
- `Assets/VLABChemistryLab/Prefabs/XR/**`, `Assets/VLABChemistryLab/Input/**`: VLAB-owned rig, simulator and primary action assets.
- `Assets/Tests/EditMode/PresentationModeCoordinatorTests.cs`: 11 test precedence, guard, loader success/failure/timeout/cancellation và state preservation.
- `Assets/Editor/ChemistryLabBuilder.cs`: transaction checkpoint-gated; preflight renderer/type/serialized-field, temporary scene build/validation, file replacement/backup rollback và cleanup.
- `Assets/Editor/ChemistryLabEditorValidation.cs` / `Assets/Editor/ChemistryLabVisualCapture.cs`: validation/capture no longer invokes the checkpoint-gated atomic builder without explicit checkpoint arguments; atomic build safety guard remains enforced.
- `Assets/Editor/ChemistryLabEditorValidation.cs`: validation của scene đã mở để Builder có thể kiểm tra scene tạm mà không mở target scene.
- `Assets/Tests/EditMode/AtomicChemistryLabBuilderTests.cs`: 5 test injected failure và preservation của target scene hash.
- `Assets/Tests/EditMode/P1D1RigTests.cs`: owned asset structure and three-mode camera/audio/tag invariants.
- `Assets/Tests/PlayMode/PlayModeBootstrapTests.cs`: ChemistryLab scene mode-switch smoke added.
- `Assets/Editor/VLABUpgrade/VLABP1RigAssetBuilder.cs`: creates/validates VLAB-owned XR assets through Editor APIs.
- `Assets/Editor/VLABUpgrade/VLABP1BuildGuards.cs`: strips simulator from release scene copies and builds/launches Windows smoke players.
- `Assets/XR/**`: XR Management/OpenXR settings assets tạo bằng Editor API.
- `Packages/manifest.json`, `Packages/packages-lock.json`: XR Management 4.5.4, OpenXR 1.17.1, legacy input helpers 3.0.1 transitive.
- `Tools/VLABGateEvidence.ps1`: canonical per-file SHA-256 inventories, gate close và independent verifier.
- `Tools/VLABP1EvidenceBootstrap.ps1`, `Tools/VLABP1Gate.ps1`: plan 1.4 pre-mutation and final Phase 1 gate closure/verifiers.
- `Docs/VLAB_UPGRADE_AUDIT.md`.
- `Docs/VLAB_UPGRADE_PLAN.md`.
- `Docs/VLAB_VERIFICATION_REPORT.md`.
- `plans/VLAB-PCVR-OPENXR-DESKTOP-BLUEPRINT.md`: mutation 1.4; đưa evidence bootstrap lên trước P1.d và tách lifecycle/assets, scene integration, smoke.
- `Artifacts/VLABUpgrade/plan-1.3/P0/20260831T144944Z-p0audit/**`: immutable logs, JSON inventories, XML, captures và gate manifest. Run 1.2 được giữ historical/superseded.
- `Artifacts/VLABUpgrade/plan-1.3/P1.a/20260831T150620Z-playmode-bootstrap-final/**`: P1.a final XML/log/inventories/gate; hai run lỗi trước được giữ để không che lịch sử.
- `Artifacts/VLABUpgrade/plan-1.3/P1.b/20260831T155433Z-openxr-tests-retry/**`: consolidated UPM probe/install/config/renderer/test evidence và verified P1.b gate.
- `Artifacts/VLABUpgrade/plan-1.3/P1.c/20260831T160724Z-mode-router/**`: Edit/PlayMode XML/log, source/checkpoint inventories và verified P1.c gate.
- `Artifacts/VLABUpgrade/plan-1.3/P1.atomic-builder/20260901T103025Z-editmode-final/**`: 44/44 XML/log, inventories và verified P1.atomic-builder gate.
- `Artifacts/VLABUpgrade/plan-1.4/P1.d1/**`, `P1.d2/**`, `P1.d3/**`: lifecycle/asset, atomic scene and release/smoke evidence including retained failed attempts.
- `Artifacts/VLABUpgrade/plan-1.4/P1.gate/20260901T180000Z-windows-development-smoke/**`: Windows player, logs, smoke JSON, source inventory and verified gate manifest.

Không có prefab/material/shader/package/XR setting/input binding nào được tạo hoặc thay đổi trong Phase 0. Scene `Assets/ChemistryLab.unity` không bị rebuild hoặc sửa.

## Issues tự phát hiện và xử lý

1. Ngăn migrator tự động ghi đè scene khi mở project/thoát Play Mode.
2. Sửa validation nhãn chai mâu thuẫn với output của chính Builder.
3. Batch licensing bị treo: dừng đúng Unity process, giữ log, chuyển sang Editor GUI automation ẩn và thu được artifact hợp lệ.
4. Lần chạy Test Runner đầu có `-quit` nên thoát trước khi tạo XML; không tính pass, chạy lại không có `-quit`, nhận XML 24/24 Passed.
5. Adversarial review phát hiện gate 1.2 trộn evidence thủ công và đặt atomic Builder sai scope. Đã mutate plan 1.3, rerun audit/test mới, thêm unique output + independent hash verifier; gate 1.3 đã xác minh lại PASS.
6. PlayMode bootstrap lần đầu compile fail vì Unity 6000.5 bỏ API `PlayerSettings.activeInputHandler`; đổi test sang đọc serialized source-of-truth. Retry kế tiếp compile nhưng filter trả 0 test; sửa asmdef theo PlayMode chuẩn và chạy không filter, nhận 2/2 pass. Cả hai run lỗi được giữ, không tính PASS.
7. P1.b install đầu vượt allowlist do dependency `legacyinputhelpers`; đã selective rollback đúng 12 file và xác minh 1.861 file khớp checkpoint. Retry dùng allowlist đã review; diff cuối chỉ Management/OpenXR/legacy helper. Nhận định tạm thời về core-utils được sửa sau khi checkpoint chứng minh 2.6.0 đã tồn tại trước P1.b.
8. P1.d1 asset test đầu tham chiếu Editor-only builder từ test assembly và không compile; chuyển test sang kiểm tra asset structure trực tiếp. Lần kế tiếp phát hiện assertion đếm camera ngoài ownership; thu hẹp đúng controller-owned cameras. Cả hai run lỗi được giữ.
9. P1.d3 scene processor ban đầu strip simulator khi Test Runner gọi với `BuildReport == null`; PlayMode 2/3 bắt lỗi. Guard được giới hạn cho build thật, retry 3/3 pass. Release build sau đó chứng minh strip đúng một root.
10. P1 gate close đầu dùng regex renderer có backtracking sai và từ chối Quality hợp lệ; sửa parser đọc từng `fileID`, sau đó close + independent verify pass. Không có manifest Pass giả được tạo ở lần lỗi.

## Accepted warnings và blockers

- Accepted tạm thời: Input Manager deprecation vì Phase 1/2 cần duy trì `Both` để không làm hỏng Desktop trước khi Input Actions migration pass.
- Accepted để theo dõi: JobTemp allocation warning khi Editor shutdown; phải tái đo player ở Phase 7.
- Blocker cho hardware rows: không có headset/controller hoặc OpenXR runtime hoạt động; loader success/stereo/tracking/haptic/comfort vẫn `Not tested`.
- Blocker cho performance rows: Windows player đã build/launch nhưng chưa chạy profiler/frame-time/draw-call/GC matrix của Phase 7.

## Bước tiếp theo có thể thực thi

P2.a đã có bằng chứng; Desktop cầm/thả đã được tích hợp và kiểm thử. Bước tiếp theo: giải quyết test Trigger controller còn lại, rồi hoàn tất gate P2 và locomotion P2.c. Pin Daniell/điện phân vẫn chưa chuyển sang thao tác dụng cụ như bộ chuẩn độ. Windows player mới và phần cứng VR chưa được kiểm thử trên nguồn hiện tại.
