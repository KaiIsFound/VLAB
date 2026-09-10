# Blueprint nâng cấp VLAB Chemistry Lab thành Windows PCVR OpenXR + Desktop

- **Plan version:** 1.4 (mutation trước P1.d sau adversarial review lifecycle/evidence).
- **Trạng thái:** Kế hoạch triển khai; chưa phải báo cáo hoàn thành.
- **Project:** `C:\Users\SchoolGiang\Documents\ChatGPT\VLAB\VLAB-Unity-ChemistryLab`
- **Target chính:** Windows PCVR qua OpenXR.
- **Target đồng tồn tại:** Desktop chuột/bàn phím; XR Device/Interaction Simulator để kiểm thử không có headset.
- **Unity:** 6000.5.6f1; Linear Color Space.
- **Quy tắc phát hành:** Không commit/push nếu người dùng chưa yêu cầu. Không sửa YAML scene thủ công. Mọi thay đổi scene sinh ra phải đi qua Builder/prefab nguồn và tăng migration marker.

## 1. Mục tiêu và định nghĩa “đạt”

Nâng project thành phòng lab giáo dục hóa học hoàn chỉnh, dùng chung một lõi trạng thái thí nghiệm cho ba presentation mode: `Desktop`, `XR Simulator`, `OpenXR Hardware`. Chỉ một rig/camera/audio listener được active. Ba thí nghiệm — chuẩn độ, Pin Daniell và điện phân CuSO4 — phải chạy end-to-end, từ chối trình tự sai, có reset/recovery và không phụ thuộc UI cụ thể.

Một hạng mục chỉ được ghi `Pass` khi có bằng chứng tương ứng:

| Nhãn | Ý nghĩa |
|---|---|
| Implemented | Đã có code/asset nhưng chưa đủ bằng chứng chạy |
| Structurally verified | Compile, validation tĩnh hoặc EditMode đã chạy và có artifact |
| Simulator verified | PlayMode/XR simulator đã chạy và có log/capture |
| Build verified | Windows player đã build, khởi động và có smoke-test artifact |
| Hardware verified | Đã kiểm tra trên headset/controller thật, ghi runtime, headset, GPU, resolution, refresh rate |
| Not tested | Chưa có phương tiện hoặc bằng chứng; không được đổi thành Pass |

Simulator không thay thế hardware verification. Nếu không có headset, stereo depth, tracking đầu/tay, floor calibration, comfort, haptic và frame timing headset phải ghi `Not tested — hardware required`.

Trạng thái kết thúc được tách rõ:

- `Software/Simulator Complete`: logic, Desktop và simulator đã đạt gate.
- `Windows Build Verified`: Windows x64 player đã build/launch và Desktop fallback đã smoke-test; OpenXR loader path được test khi runtime có sẵn.
- `PCVR Hardware Verified`: đã test headset/controller thật với runtime, tracking, stereo, calibration, haptic, comfort và frame timing.
- `Release Accepted`: người dùng chấp nhận release dựa trên ma trận bằng chứng. Không có headset thì trạng thái cao nhất về kỹ thuật là `Windows Build Verified`, không được gọi toàn bộ PCVR là hoàn tất.

### Acceptance catalog (ID bất biến)

| ID | Criterion | Owner/gate | Evidence bắt buộc | Terminal state tối đa nếu thiếu |
|---|---|---|---|---|
| AC-001 | Recovery/evidence có thể restore và không dùng stale artifact | P0.0, mọi `*.gate` | checkpoint + gate manifests có hash hợp lệ | Planned |
| AC-010 | OpenXR Windows config/validation và effective renderer đúng | P1.b/P1.gate, P4.gate | package/settings diff, Project Validation, renderer matrix | Software/Simulator Complete |
| AC-011 | Ba mode; một camera/listener; Desktop fallback giữ state | P1.c–P1.gate | Edit/PlayMode + player smoke | Software/Simulator Complete |
| AC-020 | Desktop/XR locomotion, standing/seated, bounds, comfort rules | P2.c/P2.gate | Edit/PlayMode/simulator logs | Software/Simulator Complete |
| AC-021 | Two-hand/direct/ray/grab/snap/physics/recovery | P2.b–P2.gate | stress + simulator results | Software/Simulator Complete |
| AC-030 | TMP SDF tiếng Việt; VR/Desktop UI readable/usable | P3.a–P3.gate | glyph/layout tests + visual checklist | Software/Simulator Complete |
| AC-040 | Models/PBR/glass/liquid/lighting không regression | P4.a–P4.gate | inventory, renderer matrix, captures, perf delta | Software/Simulator Complete |
| AC-050 | Chuẩn độ E2E, wrong-order, units, reset | P5.b/P5.gate | RED/GREEN EditMode + Desktop/simulator E2E | Software/Simulator Complete |
| AC-051 | Pin Daniell E2E và circuit error matrix | P5.c/P5.gate | RED/GREEN + Desktop/simulator E2E | Software/Simulator Complete |
| AC-052 | Điện phân CuSO4 đúng electrode model và E2E | P5.d/P5.gate | RED/GREEN + Desktop/simulator E2E | Software/Simulator Complete |
| AC-060 | Audio/haptic abstraction/accessibility/recovery UX tác động thật | P6.a–P6.gate | service/settings/recovery tests; mixer report | Software/Simulator Complete |
| AC-070 | Full EditMode/PlayMode/simulator regression nonzero | P7.b/P7.gate | XML/log + counts/hash | Software/Simulator Complete |
| AC-071 | Windows x64 build/launch/fallback | P1.gate, P7.d/P7.gate | build hash, player log/sentinel | Software/Simulator Complete |
| AC-072 | Deterministic visual + measured player performance | P7.a/P7.c/P7.gate | profiler captures + human checklist | Windows Build Verified |
| AC-080 | Headset/controller stereo/tracking/haptic/calibration/comfort/timing | P7.d/P7.gate | hardware matrix ghi thiết bị/runtime | Windows Build Verified |

Plan mutation phải nêu các AC-ID bị ảnh hưởng và invalidate artifact của dependency closure từ owner package đến mọi downstream gate.

## 2. Pre-audit đã xác minh ngày 2026-08-31

Đây là kiểm tra tĩnh/read-only để lập kế hoạch, không phải Phase 0 hoàn chỉnh.

| Hạng mục | Bằng chứng từ project | Kết luận hiện tại |
|---|---|---|
| Unity | `ProjectSettings/ProjectVersion.txt` | 6000.5.6f1 xác nhận |
| XRI | manifest/lock | 3.5.1 direct dependency; Starter Assets, XR Device Simulator và XR Interaction Simulator đã import |
| Input System | lock | 1.20.0 transitive, khác mô tả 1.8.1 |
| XR Plug-in Management | manifest/lock | Vắng mặt |
| OpenXR | manifest/lock và ProjectSettings | Vắng mặt; chưa có loader/profile/validation settings |
| URP | manifest/lock | URP 17.5.0 đã cài |
| Pipeline active | `GraphicsSettings.asset` và mọi quality entry | `m_CustomRenderPipeline: fileID 0`; Built-in đang active. Không được bật URP trước material/shader audit |
| Tests | lock và `Assets/Tests/EditMode` | Test Framework 1.7.0 và Performance Test Framework 3.5.0 là transitive; có 2 fixture EditMode cho chemistry/titration; chưa thấy PlayMode assembly |
| Scene generation | Builder/migrator/scene | `__ChemistryLab_Generated__`, marker `__ChemistryLab_Generated_v8`; migrator tự rebuild sau EditMode nếu stale |
| Mode routing | `ChemistryLabModeController.cs` | Chỉ có bool Desktop/XR qua `XRSettings.isDeviceActive`; chưa có 3 mode, loader lifecycle hoặc fallback rõ ràng |
| Rig trong scene | `ChemistryLab.unity` | Có XR Origin và XR Interaction Manager từ sample nhưng Builder tắt chúng ở desktop; OpenXR chưa tồn tại nên chưa phải PCVR rig đã xác minh |
| Camera | Builder/scene | Có `ChemistryLab Desktop Camera` với MainCamera + AudioListener; cần audit bằng Editor API để đếm camera/listener active theo từng mode |
| UI/font | Builder/validation | World labels đang dùng legacy `TextMesh`; validation hiện còn cấm TMP trong generated root. Điều này xung đột trực tiếp với yêu cầu TMP SDF và phải được thay bằng validation mới |
| Render quality | Builder/QualitySettings | Builder ép quality cuối, 8x MSAA, shadow Very High; có 6 quality entries mặc định. Chưa phù hợp preset PCVR/desktop theo ngân sách |
| Experiment logic | runtime scripts/assets | Chuẩn độ có lõi C# thuần và TDD hiện hữu. Pin Daniell/điện phân dùng `ConfigurableExperimentController` chỉ tăng bộ đếm bước, chưa kiểm tra mạch, cực, cầu muối, loại điện cực hoặc kết quả động |
| Material/shader sở hữu bởi VLAB | source scan + Builder | Chưa thấy estate custom shader/material VLAB đáng kể; vấn đề chính là material được Builder tạo/serialize embedded cùng TMP shaders, không phải một thư viện custom shader đã chuẩn hóa |
| Audio/haptic | asset/code scan | Không thấy audio/mixer tùy biến hoặc abstraction haptic trong code VLAB |
| Build settings | `EditorBuildSettings.asset` | `Assets/ChemistryLab.unity` đã được include |
| Compile evidence | `Logs/Editor.log` | Lần import gần nhất không có compiler error; đây không phải một Test Runner run mới |
| Batch evidence | log 2026-08-31 | Hai lần full verification gần nhất dừng ở licensing initialization; log visual capture thoát code 1. Không được coi là pass |
| Git/GitHub | workspace preflight | Git root ở thư mục cha, chưa có commit, toàn bộ workspace untracked; `gh` không cài. Kế hoạch dùng direct mode, không branch/PR/commit |
| Reproducibility package | manifest/Packages/lock | `com.coplaydev.unity-mcp` được khai báo Git `#main` trong manifest trong khi package source hiện diện trong project; phải chốt source/hash trước khi dùng checkpoint làm bằng chứng tái lập |

Tài liệu cũ có câu “Unity/URP” và một số log PASS lịch sử; nguồn đúng cho renderer hiện tại vẫn là ProjectSettings: Built-in. Các log lịch sử được giữ làm tham khảo, không thay thế baseline mới của Phase 0.

## 3. Invariant xuyên suốt

1. Không mở Unity, sửa source hoặc chạy Builder trước checkpoint `P0.0` ở §6. Checkpoint là phương án khôi phục bắt buộc vì project chưa có commit.
2. Không qua phase kế tiếp nếu phase hiện tại chưa compile và chưa đạt gate bằng artifact mới khớp source fingerprint/plan version/checkpoint ID.
3. Không sửa `Assets/ChemistryLab.unity` bằng text/YAML. Sửa `ChemistryLabBuilder`, prefab/source asset hoặc Editor API; chạy Builder; tăng marker `v8 -> v9...`; cập nhật migrator và validation cùng một thay đổi.
4. Migrator không được tự rebuild scene từ `[InitializeOnLoad]`. Nó chỉ được phát hiện stale và báo lỗi/hướng dẫn. Migration phải là lệnh explicit, có preflight và checkpoint.
5. Builder phải preflight type/asset/serialized-field trước khi xóa generated root, dựng + validate vào temporary scene/path và chỉ thay target scene sau khi toàn bộ validation pass. Không xóa target trước khi bản thay thế hợp lệ tồn tại.
6. Không bật URP chỉ vì package đã cài. Phase 4 phải inventory toàn bộ material/shader/glass/liquid/TMP trước khi ra quyết định.
7. Renderer sentinel phải chạy đầu/cuối mọi gate P0–P3 và trước P4 audit: kiểm tra Graphics default, override của mọi Quality level và `GraphicsSettings.currentRenderPipeline`/runtime effective pipeline. Bất kỳ SRP non-null trước decision gate đều Fail, kể cả Graphics default null.
8. Không đổi package version tùy tiện. OpenXR/XR Plug-in Management phải dùng exact released version mà UPM của Unity 6000.5.6f1 đánh dấu supported/recommended; dependency diff phải nằm trong allowlist và không được downgrade XRI/Input System.
9. Giữ `activeInputHandler: Both` suốt P1; cấm Project Validation autofix đổi input backend. Chỉ cân nhắc New-only sau khi P2 đã chuyển Desktop sang Input Actions và regression pass.
10. Desktop và XR chỉ khác presentation/input/feedback; không nhân đôi experiment state.
11. XR hardware không dùng camera desktop, không dùng tay giả gắn camera, không ghi đè headset pose/FOV trong `Update`.
12. Không `Find*`, LINQ, `new Material`, Instantiate/Destroy lặp trong hot path; pool audio/droplet/particle.
13. Không dùng asset ZIP/RAR/model chưa xác minh license; không mua/tải asset ngoài nếu chưa được người dùng cho phép.
14. Không che chất lượng bằng bloom; VR gameplay cấm motion blur, DOF, chromatic aberration và fixed cinematic vignette.
15. Mọi số liệu performance phải kèm máy, GPU, runtime, headset (nếu có), resolution, refresh rate, quality preset và build type.
16. Mọi logic có thể kiểm thử phải lưu bằng chứng RED → GREEN tối thiểu → REFACTOR/PASS. Pure chemistry/domain nằm trong assembly không phụ thuộc UnityEngine; presentation không được leak vào chemistry truth.

## 4. Dependency graph và khả năng song song

```text
P0 Audit + baseline
  -> P1 OpenXR foundation + 3-mode separation
    -> P2 Two-hand interaction + locomotion + physics + recovery
      -> P3 TMP SDF + VR/Desktop UI
        -> P4 Render decision + models/PBR/glass/liquid/lighting
          -> P5 Experiment core + 3 end-to-end experiments (TDD)
            -> P6 Audio + haptic + accessibility + recovery UX
              -> P7 Performance + full regression + simulator + build + visual + hardware matrix
```

Các phase là tuần tự đúng yêu cầu gate. Chỉ song song bên trong phase và chỉ khi không dùng chung file:

- P0: package/settings audit, scene/content audit và baseline capture có thể thu thập song song rồi hợp nhất vào cùng report.
- P4: sau khi chốt pipeline, prop modeling, material/glass/liquid và lighting có thể làm song song nếu mỗi luồng sở hữu prefab/source riêng; Builder integration là điểm hội tụ duy nhất.
- P5: sau khi contract state machine dùng chung đã pass, chuẩn độ, Daniell và điện phân có thể phát triển test-first ở file riêng; integration với station/Builder làm tuần tự.
- P7: trong checkout chính, mọi job chạy Unity phải tuần tự vì dùng chung project/Library. Chỉ phân tích read-only được song song; job Unity chỉ được chạy song song trên các bản project restore tách biệt, Library/output riêng và cùng checkpoint hash.

## 5. Protocol compile/verify sau mỗi phase

Mỗi phase tạo thư mục artifact duy nhất, ví dụ `Artifacts/VLABUpgrade/plan-1.3/P1/<UTC-run-id>/`, và thêm một hàng vào `Docs/VLAB_VERIFICATION_REPORT.md`. Mỗi run bắt buộc có `gate-manifest.json` chứa:

- plan version + hash, phase/work-package ID, recovery-checkpoint ID;
- SHA-256 source fingerprint của `Assets/`, `Packages/`, `ProjectSettings/` (không dùng timestamp thay hash), hash riêng manifest/lock;
- Unity full version/revision, Builder version + generated marker, scene GUID/hash;
- Graphics default, mọi Quality render-pipeline override và runtime-effective renderer;
- command/menu chính xác, UTC start/end, host/GPU/runtime, exit code;
- test discovery/executed/passed/failed/skipped counts; hashes của log/XML/screenshot/build/player log;
- evidence class (`required` hoặc một trong các `hardware_optional` AC-080 rows được liệt kê), accepted warnings, blockers và status terminal tối đa.

Gate tự động Fail nếu artifact thiếu, source fingerprint lệch, plan version lệch, output không tồn tại, exit code khác 0, test discovered/executed = 0, failed > 0, hoặc renderer sentinel sai. Bất kỳ row `required` bị skipped/blocked đều Fail dù có giải thích. Chỉ các row AC-080 đã liệt kê `hardware_optional` mới được `Not tested — hardware required`, đồng thời cap terminal state theo catalog. Screenshot đơn lẻ không phải evidence compile/test.

Thứ tự tối thiểu:

1. Xác nhận checkpoint/source fingerprint rồi mới mở/import bằng đúng Unity 6000.5.6f1; chờ domain reload; xác nhận Console không có compile error.
2. Chỉ package có Builder integration mới chạy Builder qua menu/Editor method, sau khi `P1.atomic-builder` Pass; xác nhận marker mới và scene chỉ có một generated root. P0 audit tuyệt đối không gọi Builder.
3. Chạy Editor validation của phase.
4. Chạy EditMode tests liên quan; từ P2 trở đi chạy PlayMode smoke liên quan.
5. Từ P1 chạy Desktop và XR Simulator riêng; không suy diễn hardware.
6. Lưu XML test result, log, JSON inventory/metrics và ảnh checkpoint vào artifact; ghi content hash của scene/marker/build, không dùng timestamp làm định danh.

Batch command mẫu, chỉ dùng sau khi đóng Unity GUI đang giữ project:

```powershell
& 'D:\unity download\6000.5.6f1\Editor\Unity.exe' -batchmode -quit `
  -projectPath 'C:\Users\SchoolGiang\Documents\ChatGPT\VLAB\VLAB-Unity-ChemistryLab' `
  -runTests -testPlatform EditMode `
  -testResults 'Artifacts\VLABUpgrade\P1\EditMode.xml' `
  -logFile 'Artifacts\VLABUpgrade\P1\EditMode.log'
```

Nếu Licensing Client chặn batch: ghi `Blocked — Licensing Client`, không ghi Pass; thử GUI Test Runner/Editor menu validation và export result. GUI evidence vẫn phải có discovery/executed counts và source fingerprint. Nếu không chạy được phương án thứ hai, gate vẫn chưa pass.

## 6. Construction steps

Phase là gate container, không phải một change set duy nhất. Mỗi hàng dưới đây là một work package có thể thực thi/verify độc lập; chỉ package `*.gate` mới được đánh dấu phase Pass. `Checkpoint` nghĩa là snapshot mới kế thừa từ P0.0 và có SHA-256 manifest.

| ID | Deliverable hẹp | Owner files/directories dự kiến | RED/GREEN + exit |
|---|---|---|---|
| P0.0 | Recovery checkpoint trước khi mở Unity | Ngoài project: `C:\Users\SchoolGiang\Documents\ChatGPT\VLAB\_VLAB_Backups\ChemistryLab\<checkpoint-id>\` | Snapshot/restore-probe hash bằng nhau; mở được project copy; checkpoint manifest Pass |
| P0.safe-migrator | External source edit trước first main Unity open: auto-migration -> detector-only | `Assets/Editor/ChemistryLabSceneMigrator.cs`, static check artifact | Restore probe compile; stale detector không gọi Builder; checkpoint hash giữ nguyên target scene |
| P0.a | Immutable evidence inventory + read-only audit | `Assets/Editor/VLABUpgrade/**`, `Tools/VLABGateEvidence.ps1`, `Docs/VLAB_UPGRADE_*.md` | Unique run; canonical per-file SHA-256 inventory verifies; audit pre/post renderer sentinel Pass |
| P0.gate | Baseline/capture/risk register | `Artifacts/VLABUpgrade/plan-1.3/P0/**`, 3 Docs files | Restore probe + fresh compile/tests/captures + independently verified gate manifest Pass |
| P1.a | PlayMode bootstrap + input freeze | `Assets/Tests/PlayMode/**`, test asmdef, PlayerSettings evidence | >=1 PlayMode test discovered/executed; `Both` unchanged |
| P1.b | Deterministic XR packages/settings | `Packages/manifest.json`, lock, `ProjectSettings/XR*`, OpenXR settings | Allowlisted dependency diff; exact requested/resolved released versions; Project Validation mandatory errors = 0 |
| P1.c | Boot/mode state machine | `Assets/VLABChemistryLab/Scripts/Mode/**`, EditMode tests | RED timeout/failure/precedence → GREEN; reason codes + state preservation |
| P1.atomic-builder | Atomic Builder prerequisite | `Assets/Editor/ChemistryLabBuilder.cs`, `ChemistryLabEditorValidation.cs`, Editor tests | RED injected preflight/temp-build/validation failure → GREEN; target scene hash unchanged on every failure path; explicit checkpoint required |
| P1.evidence-bootstrap | Reusable gate runner/schema trước mọi scene mutation | `Assets/Editor/VLABUpgrade/**`, `Tools/**`, Editor tests | Schema self-tests; dependency/AC validation; blocker rejection; P1.atomic evidence reclosed or explicitly carried under source hash |
| P1.d1 | Loader lifecycle + project-owned rig/simulator assets, chưa sửa scene | `Assets/VLABChemistryLab/Scripts/Mode/**`, `Scripts/ChemistryLabModeController.cs`, `Prefabs/XR/**`, EditMode tests | Init/start/stop/deinit/late-timeout cleanup; rig structure; Floor/Device fallback; tracking-loss contract Pass |
| P1.d2 | Atomic Builder/marker/migrator/validator integration | Builder, migrator, validation, canonical rig assets, scene generated by Editor API | Fresh checkpoint; injected failure tests; v9 marker; unique active camera/listener/MainCamera tag; no manual YAML |
| P1.d3 | Desktop/simulator/no-runtime smoke and release guards | PlayMode/simulator tests, build verifier, evidence artifacts | State preserved; simulator excluded from release path; readable fallback; Desktop and simulator smoke Pass |
| P1.gate | Windows build smoke + phase evidence | Build verifier/editor tests + `Artifacts/**/P1/**` | Desktop no-runtime launch mandatory; runtime OpenXR path when available; gate manifest Pass |
| P2.a | Semantic Input Actions adapters | `Scripts/Input/**`, inputactions, Desktop tests | Desktop regression RED/GREEN; legacy polling removed from VLAB hot path |
| P2.b | Grab/two-hand/button contracts | `Scripts/Interaction/**`, `Prefabs/Interactions/**`, Edit/PlayMode tests | Grab/transfer/debounce/held-state tests Pass |
| P2.c | Locomotion + collision layers | `Scripts/Locomotion/**`, XR rig prefab, ProjectSettings layers via Editor API, tests | Teleport/snap/bounds/standing-seated tests Pass |
| P2.d | Physics/recovery integration | `Scripts/Recovery/**`, prop prefabs, Builder integration, tests | 50-cycle stress + no lost/held reset + gate manifest Pass |
| P2.gate | Interaction/locomotion phase evidence | `Artifacts/**/P2/**`, verification report | Required suites nonzero + renderer sentinel + AC-020/021 Pass |
| P3.a | Vietnamese TMP font/corpus | `Assets/VLABChemistryLab/Typography/**`, Editor tests | Missing-glyph corpus RED → atlas GREEN; license recorded |
| P3.b | VR UI prefab/ViewModel | `Prefabs/UI/VR/**`, `Scripts/UI/**`, tests | Ray/direct/layout/read-distance structural tests + screenshots Pass |
| P3.c | Desktop responsive UI | `Prefabs/UI/Desktop/**`, shared ViewModel, tests | 1280x720/1920x1080/ultrawide + scroll-vs-zoom tests Pass |
| P3.gate | Builder/migrator TMP integration | Builder/migrator/validation + captures | New marker, no legacy TextMesh in owned UI, glyph/layout gate Pass |
| P4.a | Renderer/material audit only | `Assets/Editor/VLABUpgrade/Rendering/**`, audit docs/artifacts | Sentinel confirms Built-in; 100% generated/embedded material references classified |
| P4.b | Isolated pipeline decision | Physical project copy from checkpoint; decision record only returns to main after review | Built-in vs URP evidence; no main-project SRP activation during prototype |
| P4.c | Props + reusable PBR sources | `Models/**`, `Materials/**`, prop prefabs; no Builder integration until review | Scale/pivot/collider/material validation per prop family Pass |
| P4.d | Glass + liquid systems | `Shaders-or-Materials/Glass/**`, `Liquid/**`, tests/captures | Sorting/fill/meniscus/pooling tests and visual checklist Pass |
| P4.e | Lighting/quality + Builder integration | lighting prefabs/settings via Editor API, Builder, validation | Before/after + <=20% interim perf delta + effective renderer report for all qualities |
| P4.gate | Render/art phase evidence | `Artifacts/**/P4/**`, decision record, verification report | AC-040 + renderer matrix + required build-if-pipeline-changed Pass |
| P5.a | Unity-free experiment domain contract | `Scripts/Domain/**`, no-engine asmdef, EditMode tests | RED state/rule/reset/unit tests → GREEN; no UnityEngine reference |
| P5.b | Titration engine/data | `Domain/Titration/**`, definition asset, tests | Full correct/wrong/overshoot/trial/reset matrix Pass |
| P5.c | Daniell engine/data | `Domain/Daniell/**`, definition asset, tests | Circuit/electrode/salt-bridge/polarity matrix Pass |
| P5.d | Electrolysis engine/data | `Domain/Electrolysis/**`, definition asset, tests | Inert-vs-copper/power/polarity/circuit matrix Pass |
| P5.e | Station adapters + E2E integration | `Scripts/Presentation/Experiments/**`, station prefabs, Builder, PlayMode tests | Desktop + simulator E2E for all 3; same state fingerprint; gate Pass |
| P5.gate | Chemistry phase evidence | `Artifacts/**/P5/**`, verification report | AC-050/051/052 EditMode + Desktop/simulator E2E Pass |
| P6.a | Mixer/pool/audio | `Audio/**`, `Scripts/Feedback/Audio/**`, tests | Source pool/loop/clipping checks Pass; license manifest complete |
| P6.b | Haptic abstraction | `Scripts/Feedback/Haptics/**`, tests | Fake-recorder/capability tests Pass; hardware remains separate |
| P6.c | Accessibility/settings | `Scripts/Settings/**`, UI settings prefabs, tests | Persistence/migration + real visual/control effects Pass |
| P6.d | Recovery UX/checkpoints | `Scripts/Recovery/**`, UI confirmation, tests | Held/tracking-loss/checkpoint/reset matrix + phase gate Pass |
| P6.gate | Feedback/accessibility phase evidence | `Artifacts/**/P6/**`, verification report | AC-060 required rows Pass; hardware haptic optional row remains separate |
| P7.a | Four quality profiles + profiler fixes | Quality/editor config tool, performance tests | No hot-path violations; measured player metrics attached |
| P7.b | Full automated regression | `Assets/Tests/EditMode/**`, `PlayMode/**`, simulator harness | Nonzero discovered/executed; all required suites Pass |
| P7.c | Deterministic visual regression | capture definitions/tool, `Artifacts/**/P7/Visual/**` | Seed/time/quality/cameras fixed; metric diff + human checklist Pass |
| P7.d | Final Windows build/report/hardware matrix | build verifier, 3 Docs files, artifacts | Build/launch Pass; terminal status bounded by actual hardware evidence |
| P7.gate | Final evidence closure | `Artifacts/**/P7/**`, 3 Docs files | AC-001…080 matrix resolved; terminal state assigned without overclaim |

**Dependency package-level:**

```text
P0.0 -> P0.safe-migrator -> [first main-project Unity open] -> P0.a -> P0.gate
P0.gate -> P1.a -> P1.b -> P1.c -> P1.atomic-builder -> P1.evidence-bootstrap -> P1.d1 -> P1.d2 -> P1.d3 -> P1.gate
P1.gate -> P2.a -> P2.b -> P2.c -> P2.d -> P2.gate
P2.gate -> P3.a -> P3.b -> P3.c -> P3.gate
P3.gate -> P4.a -> P4.b -> P4.c -> P4.d -> P4.e -> P4.gate
P4.gate -> P5.a -> {P5.b, P5.c, P5.d} -> P5.e -> P5.gate
P5.gate -> P6.a -> P6.b -> P6.c -> P6.d -> P6.gate
P6.gate -> P7.a -> {P7.b, P7.c} -> P7.d -> P7.gate
```

Các nhánh trong `{}` chỉ chạy song song ở project copies tách biệt; trong main checkout, Unity jobs vẫn tuần tự.

**Execution contract cho mọi package:**

1. Trước mọi package mutating, tạo và verify checkpoint `CP-<package-id>-pre-<UTC>` theo P0.0; package read-only ghi checkpoint nguồn đang dùng. Không có checkpoint/patch bytes đã verify thì không được sửa file.
2. Trước khi chạy, lưu `commands.txt` với exact Unity path, project copy, test filter/build method và output paths dưới `Artifacts/VLABUpgrade/plan-1.3/<package-id>/<run-id>/`.
3. Package testable dùng namespace/filter `VLAB.ChemistryLab.Tests.<PackageToken>` và chạy RED, GREEN, REFACTOR/PASS bằng Test Runner. P0.safe-migrator chạy static assertion trong source copy rồi compile restore probe; P0.a tạo inventory/verifier tối thiểu cho audit-only gate. Shared Editor runner/schema, blocker rejection và `CloseGate` đầy đủ là deliverable `P1.evidence-bootstrap` trước mọi mutation P1.d.
4. Package Builder integration chạy atomic failure test, explicit build/migration command, Editor validation và marker/source hash. Package settings/build chạy dedicated Editor method, Windows player smoke và player sentinel nếu bảng yêu cầu.
5. Exit chỉ khi row evidence + dependency gate manifests có fingerprint khớp và tất cả evidence `required` Pass.
6. Rollback cho mọi package: đóng Unity, restore đúng `CP-<package-id>-pre-<UTC>` (hoặc apply verified reverse patch bundle lưu cùng checkpoint), verify SHA-256, import/compile và rerun gate của dependency trực tiếp. Không dùng rollback mô tả khái niệm.

Command contract sau khi `P1.evidence-bootstrap` tạo `VLABUpgradeRunner` (thay `<id>`, `<token>`, `<artifact>` bằng row hiện tại và lưu lệnh đã expand vào `commands.txt`; P0 dùng audit method + verifier độc lập đã ghi trong artifact):

```powershell
& 'D:\unity download\6000.5.6f1\Editor\Unity.exe' -batchmode -quit `
  -projectPath '<verified-project-copy>' -executeMethod VLABUpgradeRunner.VerifyPackage `
  -vlabPackage '<id>' -vlabArtifactRoot '<artifact>' -logFile '<artifact>\compile-verify.log'

& 'D:\unity download\6000.5.6f1\Editor\Unity.exe' -batchmode -quit `
  -projectPath '<verified-project-copy>' -runTests -testPlatform EditMode `
  -testFilter 'VLAB.ChemistryLab.Tests.<token>' `
  -testResults '<artifact>\EditMode.xml' -logFile '<artifact>\EditMode.log'

& 'D:\unity download\6000.5.6f1\Editor\Unity.exe' -batchmode -quit `
  -projectPath '<verified-project-copy>' -runTests -testPlatform PlayMode `
  -testFilter 'VLAB.ChemistryLab.Tests.<token>' `
  -testResults '<artifact>\PlayMode.xml' -logFile '<artifact>\PlayMode.log'
```

Chỉ chạy platform phù hợp bảng row; package không có testable logic vẫn phải chạy compile/VerifyPackage và structural validator. Từ P1.gate, `*.gate` chạy thêm `VLABUpgradeRunner.CloseGate -vlabPackage <id>` để validate toàn bộ manifests/dependency/AC rows. P1.gate/P4.gate khi pipeline đổi/P7.d dùng dedicated `VLABUpgradeRunner.BuildAndSmokeWindows`; P7.c dùng `VLABUpgradeRunner.CaptureVisualCheckpoints`. Bất kỳ runner method chưa tồn tại là deliverable bắt buộc của P1.evidence/P7.c tương ứng, không được thay bằng việc bỏ command.

Owner rule: một package không sửa owner của package khác khi package kia đang active. Builder/migrator/central settings chỉ được sửa ở package integration đã nêu. Sau mỗi package, giữ checkpoint; không xóa trong epic.

### P0.0 — Recovery checkpoint bắt buộc

**Trước mọi hành động Unity:** đóng Unity/Hub process đang giữ project; snapshot nguyên vẹn `Assets/`, `Packages/`, `ProjectSettings/` và toàn bộ `.meta` sang đường dẫn backup ngoài project ở bảng trên. Ghi manifest SHA-256 theo relative path, size và hash; ghi hash của plan đang có hiệu lực. Không dùng `Library/` làm nguồn khôi phục.

Tạo một restore probe ở thư mục sibling riêng, copy snapshot vào đó, xác minh manifest byte-for-byte, rồi mở/import probe bằng đúng Unity 6000.5.6f1 và compile. Không được coi backup là hợp lệ nếu chỉ copy thành công nhưng chưa verify restore. Không xóa backup/probe trong quá trình epic.

Checkpoint ID phải xuất hiện trong mọi gate manifest. Trước package có thay đổi package, scene generation hoặc render pipeline, tạo checkpoint kế tiếp theo cùng protocol.

### P0 — Audit và baseline

**Phụ thuộc:** P0.0 recovery checkpoint Pass.  
**Model tier:** Strongest; cần quyết định kiến trúc và render migration.  
**Context cold-start:** Project đang Built-in, URP chỉ installed; OpenXR/XR Management absent; scene do Builder sinh marker v8; Git chưa có commit nên không có rollback bằng VCS.

**Task:**

- Tạo/cập nhật `Docs/VLAB_UPGRADE_AUDIT.md`, `Docs/VLAB_UPGRADE_PLAN.md`, `Docs/VLAB_VERIFICATION_REPORT.md`; audit report phân biệt `Observed`, `Measured`, `Historical evidence`, `Not tested`.
- Trước lần mở Unity chính đầu tiên, thay hành vi migrator tự rebuild bằng detector-only. P0 chỉ inventory Builder và khóa mọi entrypoint `BuildChemistryLab*`/`BuildAndValidate`; atomic preflight/temp-scene/validate/swap được triển khai ở `P1.atomic-builder` trước lần Builder integration đầu tiên.
- Viết Editor audit tool chỉ đọc để inventory package/version/source, Graphics/Quality/Player settings, loader/profile, scene objects/components, cameras/listeners, materials/shaders/textures/fonts, colliders/rigidbodies/interactables, lights/probes/canvases/audio/particles và Missing Script.
- Ghi rõ Input System 1.20.0 và test packages thực tế; xác minh dependency nào direct/transitive trước khi đề xuất thay đổi manifest. Chốt rủi ro `com.coplaydev.unity-mcp` mutable `#main` so với source hiện tại bằng URL/revision/source/hash; chưa rõ thì checkpoint không được gọi reproducible.
- Chạy renderer sentinel ở đầu/cuối: global default, từng Quality override và runtime-effective pipeline đều phải Built-in/null SRP trong P0–P3.
- Inventory Builder/migrator/marker/validation; phát hiện object nào sẽ bị Builder xóa/sinh lại bằng source/scene snapshot. Không rebuild scene trong P0; idempotence và injected-failure thuộc `P1.atomic-builder`.
- Chạy baseline compile, EditMode tests hiện có và validation read-only. Không gọi `BuildAndValidate` trong baseline vì method đó rebuild scene. Không tái sử dụng chữ PASS trong log cũ làm kết quả mới.
- Capture 11 checkpoint cố định: entrance, titration đứng/ngồi, Daniell, electrolysis, reagent, bottle label, button label, VR-distance panel, bright/dark text. Giữ cùng camera, resolution và color space cho ảnh sau.
- Ghi baseline CPU/GPU/GC/draw calls/batches nếu Profiler khả dụng; nếu không, ghi Not measured. Ghi active quality, MSAA, shadows, realtime/shadow lights.
- Lập risk register: không có VCS rollback, package source mutable, package install, mode router cũ, Builder phá thay đổi scene, validation cấm TMP, generic experiment controllers, transparency/URP, quality defaults chưa theo target, batch licensing.

**Verify:** restore-probe; compile/import log; EditMode XML; audit JSON/Markdown; screenshot set; pre/post renderer sentinel; canonical source/artifact SHA-256 inventory; Console error/warning list.  
**Exit:** renderer, OpenXR, camera Desktop/XR, marker, package versions, baseline metrics và migration risks đều có bằng chứng mới.  
**Rollback:** restore toàn bộ source snapshot P0.0 với Unity đóng, xác minh SHA-256, import/compile restore probe, rồi mới thay main project. Không rollback từng file đoán mò; P0 chỉ thay detector/audit/validation false-positive và không chạy Builder hay sửa scene/package/settings.

### P1 — OpenXR foundation, XR rig và mode separation

**Phụ thuộc:** P0 Pass.  
**Context cold-start:** XR Management 4.5.4 và OpenXR 1.17.1 đã được cấu hình cho Windows, auto-start tắt. Pure coordinator ba mode đã có, nhưng `ChemistryLabModeController` live vẫn dùng `XRSettings.isDeviceActive`; Builder tắt XR objects và scene còn dùng rig mẫu trực tiếp. Mục tiêu là ba mode nhưng một simulation state.

**Task:**

- Tạo `Assets/Tests/PlayMode/VLAB.ChemistryLab.Tests.PlayMode.asmdef` và bootstrap smoke; gate yêu cầu >=1 test được discover và execute trước khi suite PlayMode khác được tin cậy.
- TDD contract thuần C# cho `VLabPresentationMode`, mode request/decision, loader success/failure, Desktop fallback và invariant một active camera/listener. Lifecycle phải có init/start/stop/deinitialize idempotent và cleanup cả khi init hoàn tất muộn sau timeout/cancel.
- Cài XR Plug-in Management + OpenXR bằng UPM trong Unity bằng exact released version được Editor này đánh dấu supported/recommended. Trước install ghi requested version, dependency allowlist và lock hash; abort nếu có prerelease không được duyệt, dependency ngoài allowlist, hoặc downgrade XRI/Input System. Bật OpenXR cho Windows Standalone.
- Giữ Input Handling = Both trong P1; review từng Project Validation fix, không dùng auto-fix mù và không cho đổi input backend.
- Tắt `Initialize XR on Startup`; router tự quản lý loader. Boot precedence: command line `-vlabMode desktop|hardware|simulator` (simulator chỉ Editor/Development), rồi explicit persisted user choice Desktop/Hardware, rồi launch-menu choice, cuối cùng `Auto`. Auto luôn boot presentation Desktop trước, thử OpenXR async với timeout cấu hình; thành công mới chuyển Hardware, thất bại giữ Desktop và reason code (`NoRuntime`, `LoaderInitFailed`, `Timeout`, `ValidationBlocked`). Experiment state object không được recreate.
- Cấu hình tối thiểu Khronos Simple Controller và các profile Oculus Touch/Valve Index/Microsoft Motion Controller chỉ khi package version cung cấp và Project Validation chấp nhận; không bật feature thừa.
- Trước mọi Builder integration, triển khai `P1.atomic-builder`: entrypoint explicit yêu cầu checkpoint ID hợp lệ; preflight toàn bộ type/asset/serialized field; dựng và validate vào scene/path tạm; chỉ swap target sau khi hợp lệ. Inject lỗi tại preflight/build/validation/swap và chứng minh hash scene đích không đổi trên mọi failure path. Cấm gọi `BuildChemistryLab*`/`BuildAndValidate` trước khi package này Pass.
- Sau `P1.evidence-bootstrap`, tạo prefab nguồn `VLAB XR Origin`: scale 1, parent uniform, Floor ưu tiên, Device fallback + camera offset; Main Camera dùng tracked pose; left/right action riêng; Direct + Ray (hoặc Near/Far chỉ khi có test chứng minh direct-grab tương đương); Input Action Manager; XR Interaction Manager; Locomotion Mediator/providers; tracking-lost state ẩn renderer và tắt interaction thay vì nhảy về world origin.
- Chọn simulator canonical theo asset thực có của XRI 3.5.1 và tạo wrapper/prefab VLAB-owned; không tham chiếu đường dẫn sample như source phát hành duy nhất. Simulator chỉ Editor/test/Development theo define và bị loại khỏi hardware release player; validation kiểm tra asset/component simulator không nằm trong hardware build.
- Builder chỉ instantiate/configure prefab nguồn và set marker mới; không serialize component XR bằng sửa YAML.
- Thêm validation: đúng một Main Camera, đúng một AudioListener, desktop script không active trên XR camera, hardware mode không có fake hands, experiment state object không bị thay khi presentation đổi.
- Chạy OpenXR Project Validation; sửa mọi error bắt buộc, ghi warning chấp nhận và lý do.

**Verify:** compile; nonzero PlayMode bootstrap; EditMode mode-router tests; PlayMode Desktop, canonical Simulator, loader-timeout/failure fallback; Project Validation artifact; camera/listener counts theo mode; renderer sentinel; mandatory Windows x64 Development Build + launch không runtime phải ghi player smoke sentinel và vào Desktop. Nếu máy có OpenXR runtime, test thêm loader startup; không có runtime thì dòng này Not tested, không giả lập.  
**Exit:** Desktop/simulator vào được; OpenXR Windows config tồn tại; mandatory validation error = 0; no-runtime Windows player fallback pass; runtime loader path pass khi runtime có sẵn; hardware-only vẫn Not tested nếu thiếu headset.  
**Rollback:** dùng Execution contract §6, restore exact `CP-<failed-P1-package>-pre-<UTC>` trọn bộ bằng hash, không chỉ gỡ package; reimport/compile, rerun dependency gate và xác nhận marker/scene hash.

### P2 — Two-hand interaction, locomotion, physics và recovery foundation

**Phụ thuộc:** P1 Pass.  
**Context cold-start:** XRI sample assets có sẵn nhưng không đồng nghĩa station props đã có Rigidbody/collider/grab/attach hay locomotion đúng. Desktop navigator hiện dùng legacy Input API và quét simulator behaviours trong `Awake`.

**Task:**

- Tách input adapter khỏi experiment commands: Desktop mouse/keyboard, simulator và OpenXR cùng phát semantic commands/interactions vào một application layer. Chuyển Desktop từ legacy `Input` sang explicit Input Actions, thêm asmdef references và regression trước khi cân nhắc New-only; mặc định vẫn giữ Both nếu chưa có lợi ích/coverage rõ.
- Desktop: WASD, Shift, RMB-look, scroll zoom clamp, C hold/toggle, LMB interaction, Escape; chặn zoom khi pointer trên scroll view. Không chạy camera/FOV desktop trong hardware XR.
- XR: teleport mặc định, snap turn 30°, continuous move/smooth turn tùy chọn; vignette chỉ cho continuous; cấm forced head motion/camera bob/FOV change. Teleport area/reticle hợp lệ, chặn mặt bàn/vật dụng/vùng ngoài floor.
- Hai tay độc lập: pose/action left-right riêng; Direct + Ray/UI; tracking lost ẩn renderer và tắt interactor thay vì nhảy về origin; collision layer tách Hand/Interactable/Environment/UI/Player.
- Tạo prefab contract cho vật cầm: simple collider, Rigidbody, interpolation, collision mode, attach transform, mass/drag, throw clamp, interaction layer, optional socket/two-hand transformer.
- TDD EditMode cho button debounce, recovery eligibility, spawn snapshot, reset scope và state transfer giữa hai tay. PlayMode cho grab/release, grab hai vật, transfer, snap, mất vật, reset không tác động vật đang cầm.
- Burette/pipette ưu tiên constrained grab/two-hand support và valve control; recovery service trả object về spawn/socket sau delay hoặc nút Recover, không xuyên tay và không reset object đang held.
- Xóa việc dò toàn scene trong `DesktopLabNavigator.Awake`; router quản lý simulator references đã serialize.

**Verify:** compile; EditMode/PlayMode nonzero; Desktop controls/Input Actions; simulator direct/ray/teleport/snap/two-hand; physics stress 50 grab/drop; no exception và no permanent loss; renderer sentinel.  
**Exit:** gate Phase 2 trong yêu cầu gốc đạt; hardware haptic/tracking chưa được suy diễn.  
**Rollback:** dùng Execution contract §6, restore exact `CP-<failed-P2-package>-pre-<UTC>`, verify/import/compile và rerun dependency gate. Không “trả prefab” nếu không có prior bytes trong checkpoint.

### P3 — TMP SDF tiếng Việt, readable VR UI và Desktop UI

**Phụ thuộc:** P2 Pass.  
**Context cold-start:** generated world labels hiện là `TextMesh`; validation còn bắt TMP count = 0. Liberation Sans source có trong TMP package assets nhưng chưa có bằng chứng atlas/corpus tiếng Việt đầy đủ.

**Task:**

- Trích toàn bộ corpus runtime: tiếng Việt có dấu, công thức/đơn vị, số, dấu toán học và ký hiệu safety. Audit license + glyph coverage của source font hiện có; chỉ dùng font ngoài sau khi có quyền tải.
- Tạo TMP SDF font asset tĩnh cho corpus chính, fallback asset nếu cần; atlas đủ resolution, padding và material outline vừa phải; không Dynamic atlas trong build nếu không có lý do đo được.
- TDD/editor validation cho missing glyph, overflow, backward/180° text, non-uniform inherited scale, bottle-label offset, button-label orientation và duplicate label.
- Thay legacy TextMesh bằng TMP qua Builder/prefab nguồn; cập nhật validation cũ đang cấm TMP; tăng marker/migrator; không sửa scene YAML.
- VR UI world-space: khoảng đọc 0.75–1.0 m, panel không chắn tầm nhìn, có collapse, ray/direct interaction, target 4–5 cm, feedback hover/pressed/completed/disabled; không chỉ dùng màu.
- Desktop UI chuyển khỏi IMGUI prototype nếu profiling/UX cho thấy cần; responsive cho 1280x720, 1920x1080 và tỉ lệ rộng; scroll không điều khiển zoom camera; cùng ViewModel/state với XR.
- Thêm snapshot/measurement checkpoints cho text sáng/tối, đứng/ngồi, label chai khi cầm; manual screenshot inspection bắt buộc.

**Verify:** compile; glyph corpus test; UI EditMode/PlayMode; deterministic screenshot inspection cùng camera; simulator ray/direct UI; no missing glyph/overflow/stretch; renderer sentinel.  
**Exit:** UI VR/Desktop đọc được và hoạt động thật; headset readability vẫn Not tested nếu thiếu hardware.  
**Rollback:** dùng Execution contract §6, restore exact `CP-<failed-P3-package>-pre-<UTC>`, verify/import/compile và rerun dependency gate; font/prefab/source/validation phải rollback cùng checkpoint.

### P4 — Render decision, model/PBR/glass/liquid/lighting

**Phụ thuộc:** P3 Pass.  
**Context cold-start:** URP 17.5 installed nhưng inactive; Built-in active; VLAB chưa có estate custom shader/material source đáng kể, còn Builder tạo/serialize nhiều material embedded; TMP/sample package chứa phần lớn shader source nhìn thấy. Các archive model chưa có license evidence.

**Task A — audit trước quyết định:**

- Inventory từng material/shader/source/keyword/render queue/transparency/double-sided/instancing và nơi Builder tạo material. Kiểm tra Standard, custom, TMP, glass, liquid, particle và sample dependencies.
- Tạo 11 ảnh baseline cố định và performance baseline cùng quality/resolution. Lập mapping Built-in -> URP hoặc replacement manual cho custom shader.
- Chạy renderer sentinel trước/sau Task A; không kích hoạt URP trong main project.

**Decision gate:**

- **Giữ Built-in** nếu material/shader ổn định, OpenXR build hoạt động và URP không chứng minh lợi ích quality/performance đủ lớn.
- **Chuyển URP** chỉ khi 100% inventory có conversion/replacement plan, checkpoint `Assets/Packages/ProjectSettings` đã kiểm tra restore, prototype trong physical project copy riêng pass, và người thực hiện ghi rõ quyết định trong audit/plan. Không prototype URP trong main checkout.
- Nếu chuyển: thay đổi Graphics/Quality pipeline asset trong một change set cô lập bằng Editor/Project Settings; converter chỉ áp dụng material hỗ trợ; custom glass/liquid/TMP xử lý riêng. Unity cảnh báo material có thể hồng và converter không tự xử lý custom shader; vì vậy rollback phải có trước khi convert.
- Giữ URP chỉ khi no pink/missing shader, transparency/TMP/XR/build pass và performance không tệ hơn baseline quá 20%; nếu không, main project tiếp tục Built-in từ checkpoint đã xác minh. Khi adopt URP, report effective renderer của global default và từng quality preset; không dùng mỗi Graphics default làm bằng chứng.

**Task B — art/technical art:**

- Chuẩn hóa scale mét, pivot, bevel/silhouette và collider riêng cho chai, beaker/flask, burette, pipette, stand/clamp, electrodes, salt bridge, power supply, voltmeter, buttons, sink, PPE, benches/cabinets. Ưu tiên prop cầm gần; không dùng archive chưa có license.
- Chuyển material runtime/embedded không cần thiết thành source assets tái sử dụng; kim loại/nhựa/cao su/laminate/tường/sàn có PBR khác nhau; không tạo material mỗi frame.
- Glass có rim/Fresnel/thickness cảm nhận và low-quality fallback. Liquid dùng fill mesh + meniscus + optional lightweight tilt/wobble, pooled stream/droplet, không CFD, không z-fighting/xuyên thành.
- Lighting 4000–5000 K, baked/mixed khi lợi, ít realtime shadow light, reflection/light probes không update mỗi frame. Cấm label point-light glow; dùng TMP/material affordance.
- Post FX nhẹ; VR cấm motion blur/DOF/chromatic aberration. MSAA mặc định VR 4x; 8x chỉ sau profile.

**Verify:** compile; material/shader audit rerun; pink/missing shader = 0; glass/liquid/TMP screenshots; before/after inspection; simulator; Windows build nếu pipeline đổi; performance delta <= 20% trước final optimization; effective renderer cho mọi quality preset.  
**Exit:** render decision có bằng chứng; art gate đạt; tên renderer chỉ lấy từ runtime-effective pipeline kèm Graphics default và mọi Quality override. Không suy luận Built-in/URP từ một ProjectSettings pointer đơn lẻ.  
**Rollback:** dùng Execution contract §6, restore exact `CP-<failed-P4-package>-pre-<UTC>` với Unity đóng, verify SHA-256, reimport; chỉ rebuild scene nếu checkpoint manifest/Builder version yêu cầu, sau đó rerun dependency gate. Không convert ngược material thủ công.

### P5 — Logic end-to-end: chuẩn độ, Pin Daniell, điện phân

**Phụ thuộc:** P4 Pass.  
**Context cold-start:** chuẩn độ có pure C# model và tests; Daniell/điện phân hiện chỉ Advance/Record/Reset theo list, nên đang có thể “đúng” dù lắp sai. Presentation phải không sở hữu chemistry truth.

**Task core — TDD trước:**

- Thiết kế pure C# experiment engine với stable ID, trạng thái `NotStarted/SafetyCheck/Preparing/Running/AwaitingObservation/NeedsCorrection/Completed`, typed equipment/chemical/connection states, units, validation rule, feedback, observation, checkpoint và reset scope.
- Mở rộng ScriptableObject definition cho metadata, objective, safety, equipment, chemicals, steps, conditions, feedback, highlight/audio cue/localization; runtime copy không mutate asset.
- Viết RED tests cho order rejection, unit/significant figures, reset scope, shared state across Desktop/XR adapter và no-result khi apparatus sai; sau đó mới production code.

**Chuẩn độ:** PPE; rinse/fill burette; aliquot; indicator; coarse/fine/drop; finite reagent; smooth endpoint; overshoot; trials; significant figures/units; reset current trial giữ data hợp lệ. Giữ và mở rộng tests hiện có thay vì rewrite mù.

**Pin Daniell:** typed Zn/Zn2+ và Cu/Cu2+ placement; salt bridge; lead polarity; circuit completeness; electron Zn -> Cu; ~1.10 V chỉ khi đúng điều kiện mô hình; feedback riêng cho thiếu cầu muối, đảo dây, sai electrode; không coi standard voltage tuyệt đối khi concentration thay đổi.

**Điện phân CuSO4:** definition bắt buộc chọn inert hoặc copper electrodes và logic không trộn hai model; source off khi assembly; polarity/circuit checks; progressive cathode/anode/solution state; observation/result recording; pooled visual events.

**Integration:** semantic interaction events từ P2 cập nhật engine; UI P3 chỉ render ViewModel; Builder gắn station presenters/adapters và tăng marker.

**Verify:** compile; EditMode test-first suite cho ba bài; PlayMode E2E Desktop và simulator; wrong-order/wrong-apparatus matrix; reset/checkpoint; no exception; result units present.  
**Exit:** cả ba bài end-to-end; same experiment state cho Desktop/XR; không có UI-specific truth.  
**Rollback:** dùng Execution contract §6, restore exact `CP-<failed-P5-package>-pre-<UTC>`, verify/import/compile và rerun dependency gate. Một bài fail làm P5.gate Fail; không hạ chuẩn bằng generic step counter.

### P6 — Audio, haptic, accessibility và recovery UX

**Phụ thuộc:** P5 Pass.  
**Context cold-start:** chưa thấy custom AudioMixer/audio/haptic abstraction. Recovery logic nền từ P2 cần được nối vào UX/settings.

**Task:**

- Tạo mixer groups Master/Environment/Interaction/UI/Voice; pooled AudioSource; volume settings; room tone nhỏ và one-shots glass/button/pour/drop/switch/success/warning/error. Audit license mọi clip; có thể dùng deterministic editor-generated source tự sở hữu thay vì tải ngoài.
- Haptic service có capability check, intensity master và pattern ngắn khác nhau cho grab/button/snap/error; fake recorder cho EditMode/PlayMode. Hardware result chỉ Pass trên controller thật.
- Settings persistent hợp lý: text/UI scale, high contrast, color-blind presets, subtitles, volume/haptic, bloom off, vignette, snap angle, locomotion toggles/speed, dominant hand, standing/seated, recenter/height và crouch hold/toggle.
- Feedback đúng/sai dùng icon/text/audio cùng màu; seated offset không ghi đè HMD pose.
- Recovery menu: recover object, station reset và full reset có confirmation; không reset held object; checkpoint theo step; tracking loss không reset experiment.
- TDD settings serialization/migration, haptic capability, recovery exclusion, confirmation và checkpoint restore.

**Verify:** compile; EditMode/PlayMode; mixer no clipping; no leaked loop/source; settings tác động thật trong Desktop + simulator; recovery stress; hardware haptic Not tested nếu thiếu device.  
**Exit:** không mất vật quan trọng vĩnh viễn; accessibility/recovery không chỉ là UI placeholder.  
**Rollback:** dùng Execution contract §6, restore exact `CP-<failed-P6-package>-pre-<UTC>`, verify/import/compile và rerun dependency gate. No-op service chỉ là runtime fallback đã test, không thay thế source rollback.

### P7 — Performance, tests, simulator, Windows build và visual regression

**Phụ thuộc:** P6 Pass.  
**Context cold-start:** baseline P0 là điểm so sánh; quality hiện có 6 preset mặc định chưa ánh xạ theo target VLAB; batch licensing từng chặn; hardware có thể không có.

**Task:**

- Tạo 4 quality presets: VR Performance, VR Quality, Desktop Medium, Desktop High; mapping Build/Mode rõ ràng. VR Performance 4x MSAA; VR Quality 4x và chỉ cho 8x nếu profile pass.
- Static scan + profiler: cached refs, no `Find`/LINQ/new material trong hot path, pooling, transparency overdraw, batching/instancing/LOD, no per-frame probe/canvas/text churn, no GC spike grab/pour/UI.
- EditMode suite: marker/migrator, missing script/shader/glyph, text transforms, bottles/buttons/floor/paths, grab contract, rig/camera/listener, definitions/units, floor bounds.
- PlayMode suite: Desktop spawn/floor/bounds/zoom/crouch/UI scroll/debounce; grab/two-hand/snap/recovery/reset/mode switching; three E2E flows; wrong-order; UI progress; no exception.
- Simulator suite: head pose simulation, left/right actions, direct/ray UI, teleport/snap, grab two objects, transfer, buttons, supported tracking-loss simulation. Ghi đúng `Simulator verified`.
- Visual regression tại 11 checkpoint: deterministic seed, fixed simulation state, fixed quality/effective pipeline, fixed frame/time, exposure, camera, resolution và color space; lưu masks + metric thresholds. Automated structural/pixel diff phát hiện thay đổi lớn, nhưng Pass cuối cần human checklist ký trong gate manifest; headset inspection là dòng riêng.
- Windows x64 Development Build; smoke test launch, scene/input/font/shader inclusion, Desktop fallback không runtime, XR startup có runtime nếu máy có; capture player log.
- Profile player build, không chỉ Editor. Target 90 Hz 11.1 ms, CPU main ~7–8 ms, GPU ~10–10.5 ms; fallback 72 Hz 13.9 ms; Desktop >=60 FPS trên máy ghi rõ. Nếu không có headset/runtime phù hợp, không tuyên bố PCVR performance chung.
- Hoàn thiện `Docs/VLAB_VERIFICATION_REPORT.md` với bảng hạng mục/trạng thái/bằng chứng/giới hạn và inventory files/prefabs/materials/packages/settings/tests/build/metrics/screenshots/issues.

**Verify:** clean compile; all EditMode/PlayMode/simulator results mới; Windows build + launch; player logs; performance captures; visual inspection; hardware matrix.  
**Exit:** gán đúng một terminal state ở §1. Nếu hardware-only còn `Not tested — hardware required`, tối đa là `Windows Build Verified`; không tuyên bố `PCVR Hardware Verified` hay toàn bộ PCVR hoàn tất.  
**Rollback:** dùng Execution contract §6, restore exact `CP-<failed-P7-package>-pre-<UTC>`, verify/import/compile và rerun dependency gate + AC dependency closure; không nới threshold để giữ optimization lỗi.

## 7. Phase gate matrix

| Phase | Compile | EditMode | PlayMode | Simulator | Build | Visual inspection | Hardware |
|---|---:|---:|---:|---:|---:|---:|---:|
| P0 | Required | Existing suite | Baseline smoke nếu khả dụng | Baseline only | No | Baseline captures | Not required |
| P1 | Required | Mode/router | Desktop + fallback | Required | Windows x64 build + no-runtime launch mandatory | Camera/rig | Runtime/headset path ghi riêng |
| P2 | Required | Physics/recovery | Required | Required | No | Interaction/reticle | Hardware tracking deferred được phép |
| P3 | Required | Glyph/layout | Required | Required | No | Required | Headset readability riêng |
| P4 | Required | Material structure | Required | Required | Nếu đổi pipeline | Required before/after | Stereo transparency riêng |
| P5 | Required | 3 experiment engines | 3 E2E + errors | 3 E2E | No | Reaction feedback | No |
| P6 | Required | Settings/services | Required | Required | No | Accessibility states | Haptic only on hardware |
| P7 | Required | Full | Full | Full | Required cho `Windows Build Verified`; thiếu module = Blocked, không Pass | Full | Required để tuyên bố `PCVR Hardware Verified` |

## 8. Plan mutation protocol

- Mọi mutation tăng `Plan version` và append một hàng vào Change log ở cuối mục này: UTC date, author/approver, reason, acceptance IDs bị ảnh hưởng, dependency cũ/mới, checkpoint/artifact bị invalidate và gates phải rerun.
- Gate manifest phải tham chiếu đúng plan version/hash. Khi plan đổi, mọi artifact downstream của dependency bị đổi tự động invalid; không được giữ nhãn Pass chỉ vì test cũ từng xanh.
- **Insert:** bước mới phải có ID `P<n>.<letter>`, dependency, owner files, verify, exit và rollback; cập nhật graph + Change log trước khi code.
- **Split:** nếu một step vượt một phiên/không thể verify an toàn, tách theo contract trước, integration sau; phase gate vẫn nằm ở step cuối.
- **Reorder:** chỉ được đổi task nội bộ không phá dependency hoặc gate tuần tự P0→P7.
- **Skip:** chỉ khi acceptance criterion không còn thuộc scope do người dùng quyết định; ghi quyết định, người phê duyệt, ảnh hưởng và test thay thế. Không skip vì test khó chạy.
- **Abandon/rollback:** ghi artifact lỗi, restore theo rollback của step, compile lại state cũ và cập nhật verification report; không để project compile lỗi.
- **End-of-session:** hoàn thành phase hiện tại hoặc quay về state compile được; ghi `Last verified marker`, tests thật sự chạy, blockers và exact next executable step. Không bắt đầu refactor lớn cuối phiên.

### Change log (append-only)

| UTC date | Version | Author/approver | Reason | Invalidated evidence | Required rerun |
|---|---|---|---|---|---|
| 2026-08-31 | 1.1 | Blueprint author; adversarial review incorporated | Bổ sung executable recovery, renderer sentinel, evidence binding, work packages, PlayMode/build gates và terminal states | Mọi artifact giả định plan 1.0; chưa có implementation evidence mới | Bắt đầu P0.0; không kế thừa PASS |
| 2026-08-31 | 1.2 | Blueprint author; second adversarial review incorporated | Khóa package dependency/commands/gates/checkpoints/rollback, effective renderer exit, required-vs-hardware evidence và AC-ID invalidation | AC-001, AC-010, AC-011, AC-020, AC-021, AC-030, AC-040, AC-050, AC-051, AC-052, AC-060, AC-070, AC-071, AC-072, AC-080; mọi artifact plan 1.1 | Bắt đầu P0.0 trên plan hash 1.2; rerun toàn bộ dependency closure, không kế thừa PASS |
| 2026-08-31 | 1.3 | Blueprint author; Phase 0 adversarial evidence review incorporated | Sửa scope gate: P0 là audit-only; chuyển atomic Builder thành prerequisite trước P1.d; thêm unique immutable evidence run, pre/post renderer sentinel và verifier hash độc lập | AC-001, AC-010, AC-011; toàn bộ `Artifacts/VLABUpgrade/plan-1.2/P0/**` bị hạ thành historical/superseded, mọi downstream gate chưa được tạo | Rerun P0.a/P0.gate dưới plan 1.3; không gọi Builder cho đến `P1.atomic-builder` Pass |
| 2026-09-01 | 1.4 | Blueprint author; P1.d adversarial review incorporated | Đưa evidence bootstrap lên trước scene mutation; tách lifecycle/assets, Builder integration và smoke; bổ sung cleanup, tracking, tag và release-simulator gates; sửa thứ tự P7 | AC-001, AC-011, AC-070, AC-071; artifacts plan 1.3 là prerequisite lịch sử, phải được reclose/carry bằng source hash trước P1.d2 | Verify checkpoint P1.d; chạy P1.evidence-bootstrap; rerun lifecycle tests; chỉ sau đó mới P1.d2 scene swap |

## 9. Official references dùng khi triển khai

- Unity XR Plug-in Management có nhiệm vụ quản lý loader/init/settings/build; version phải lấy từ Package Manager của Editor đang dùng: https://docs.unity3d.com/current/Manual/com.unity.xr.management.html
- Unity XR Origin: chỉ một XR Origin active và tracked poses phải đi qua tracking origin: https://docs.unity3d.com/6000.0/Documentation/Manual/xr-origin.html
- Unity cảnh báo chuyển Built-in sang URP có thể làm material hồng và custom shader không được converter xử lý tự động: https://docs.unity3d.com/current/Manual/upgrade-material.html
- Unity Render Pipeline Converter và yêu cầu backup trước conversion: https://docs.unity3d.com/6000.0/Documentation/Manual/urp/features/rp-converter.html
- Sau P1.b, API/source of truth phải là documentation nằm trong local Package Manager cache đúng exact OpenXR/XR Management/XRI versions resolved; các link `current` trên chỉ là background, không được dùng để đoán API/version.

## 10. Bước thực thi đầu tiên của phiên triển khai

Bắt đầu từ `P1.evidence-bootstrap` trên checkpoint `CP-P1.d-pre-20260901T120000Z`, sau đó `P1.d1`. Không chạy Builder hoặc thay scene trước khi checkpoint/evidence schema và lifecycle cleanup đã pass. Không activate URP.
