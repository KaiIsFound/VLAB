# VLAB Chemistry Lab — Phase 0 Audit và Baseline

- Ngày audit: 2026-08-31 (Asia/Bangkok)
- Project: `VLAB-Unity-ChemistryLab`
- Unity: 6000.5.6f1 (revision `0e0577a1a2ac`)
- Target chính: Windows PCVR qua OpenXR; Desktop là fallback bắt buộc
- Trạng thái tài liệu: Phase 0 structurally verified; chưa phải báo cáo hoàn thành sản phẩm

## Nguồn bằng chứng

- Gate có hiệu lực: plan 1.3, run `Artifacts/VLABUpgrade/plan-1.3/P0/20260831T144944Z-p0audit/`.
- Audit Editor API đọc scene: `phase0-audit.json`; renderer sentinel được chụp trước và sau cùng run.
- 11 ảnh baseline 1920×1080: `baseline-screenshots/`.
- EditMode Test Runner XML/log: `editmode-results.xml`, `editmode-tests.log`.
- Inventory nguồn/checkpoint/artifact: `source-inventory.json`, `checkpoint-inventory.json`, `artifact-inventory.json`; từng file có size + SHA-256 và aggregate có thuật toán chuẩn hóa.
- Gate closure: `gate-manifest.json`; verifier độc lập đã chạy lại thành công.
- Run plan 1.2 tại `Artifacts/VLABUpgrade/plan-1.2/P0/20260831-baseline/` được giữ làm lịch sử nhưng đã superseded, không dùng để tuyên bố gate.
- Recovery checkpoint: `C:/Users/SchoolGiang/Documents/ChatGPT/VLAB/_VLAB_Backups/ChemistryLab/20260831-phase0-preflight`.

## Khác biệt so với mô tả ban đầu

| Hạng mục | Mô tả ban đầu | Project thực tế | Kết luận |
|---|---|---|---|
| Input System | 1.8.1 trong dependency graph | 1.20.0 resolved | Lấy 1.20.0 làm nguồn đúng; không downgrade |
| Test Framework | Đã có | 1.7.0 transitive | Xác nhận |
| Performance Test Framework | Chưa chắc | 3.5.0 transitive | Có dependency nhưng chưa có performance test VLAB |
| OpenXR | Chưa xác nhận | Không có trong manifest/lock/ProjectSettings | Chưa cấu hình |
| XR Plug-in Management | Chưa xác nhận | Không có trong manifest/lock/ProjectSettings | Chưa cấu hình |
| URP | 17.5.0, chưa active | 17.5.0 có asset/global settings nhưng mọi pipeline reference hiệu lực đều null | Built-in đang active |
| XR rig | Có sample/simulator | Scene có XRI Starter rig, ray/locomotion components; không có OpenXR loader | Chưa phải PCVR rig được xác minh |
| Font mới | Có TMP package/sample | Generated scene dùng 41 legacy TextMesh, 0 TMP | Font pipeline Phase 3 chưa tồn tại |

## Package baseline

| Package | Resolved | Loại | Trạng thái |
|---|---:|---|---|
| XR Interaction Toolkit | 3.5.1 | direct | Installed; Starter Assets, XR Device Simulator và XR Interaction Simulator đã import |
| Input System | 1.20.0 | transitive | Installed; PlayerSettings đang `Both` |
| XR Plug-in Management | — | — | Missing |
| OpenXR Plugin | — | — | Missing |
| Universal Render Pipeline | 17.5.0 | direct | Installed nhưng inactive |
| Test Framework | 1.7.0 | transitive | 24 EditMode tests được discover và chạy |
| Performance Test Framework | 3.5.0 | transitive | Installed; chưa có measurement suite VLAB |

Rủi ro tái lập: `com.coplaydev.unity-mcp` được khai báo Git `#main` trong manifest trong khi package hiện được embed trong project. Phải pin source/hash trước khi coi build có thể tái lập.

## Renderer và quality baseline

- `GraphicsSettings.defaultRenderPipeline`: null → Built-in Render Pipeline.
- `QualitySettings.renderPipeline`: null ở cả 6 quality levels → Built-in.
- `GraphicsSettings.currentRenderPipeline`: null → Built-in tại runtime Editor audit.
- Color Space: Linear.
- Active quality: Ultra.
- MSAA: 8x.
- Shadow distance: 80 m; resolution Very High.
- Scene shaders: Standard 152 slots, GUI/Text 41, particle legacy 4, XRI BiRP Fresnel 2, tunneling vignette 1.
- 0 material slot rỗng; 0 material có shader missing/unsupported; không thấy material hồng trong 11 ảnh baseline.

Quyết định hiện tại: giữ Built-in qua Phase 1–3. Không kích hoạt URP trước inventory/migration matrix ở Phase 4. Ultra/8x/80 m là baseline desktop hiện hữu, không phải preset PCVR được chấp nhận.

## XR và mode baseline

- Scene có `XROrigin`, `XRInteractionManager`, Input Action Manager, ray/near-far, teleportation, snap turn và continuous turn từ Starter Assets.
- Audit định danh 92 component có tên XR, 2 Ray Interactor, 7 Teleport và 17 Locomotion.
- Không tìm thấy component tên Direct Interactor; rig dùng Near/Far Interactor nên phải kiểm tra contract direct-grab bằng simulator ở Phase 1/2.
- Không có XR loader, OpenXR settings, interaction profile hoặc OpenXR Project Validation.
- `ChemistryLabModeController` hiện chỉ dùng `XRSettings.isDeviceActive` và boolean Desktop/XR; chưa có ba mode, loader lifecycle, reason code hoặc fallback message.
- Scene có 2 Camera và 2 AudioListener; tại baseline chỉ 1 camera và 1 listener active/enabled nhưng cả 2 camera đều mang tag MainCamera.
- Desktop camera là camera đang chạy. XR hardware chưa có camera path được xác minh.
- Simulated desktop hands được tạo dưới desktop camera; Phase 1 phải đảm bảo chúng không tồn tại trong hardware mode.

## Scene generation baseline

- Generated root: đúng 1 `__ChemistryLab_Generated__`.
- Marker: `__ChemistryLab_Generated_v8` tồn tại.
- Builder là nguồn layout chính; scene `Assets/ChemistryLab.unity` không được chỉnh YAML thủ công.
- Builder hiện xóa root cũ trước khi bản thay thế được validate và lưu; đây là rủi ro mất scene nếu build giữa chừng lỗi.
- Không gọi Builder trong Phase 0. Mọi entrypoint dựng scene bị khóa theo kế hoạch cho đến khi `P1.atomic-builder` chứng minh target scene không đổi trên các đường lỗi giả lập.
- Migrator trước audit tự chạy Builder khi mở scene/thoát Play Mode. Đã sửa thành detector-only: chỉ kiểm tra scene đang load và cảnh báo, không rebuild tự động.
- Validator nhãn chai có false positive vì yêu cầu text là con trực tiếp của root trong khi Builder tạo sticker/text cùng station parent. Đã sửa validator để kiểm tra cùng parent, khoảng cách ≤ 3 cm và cỡ chữ.

## Nội dung và scene metrics

| Metric | Baseline |
|---|---:|
| GameObjects | 285 |
| Renderers | 200 |
| Colliders | 140 |
| Rigidbodies | 0 |
| Missing Scripts | 0 |
| Missing material slots | 0 |
| Missing/unsupported shaders | 0 |
| Cameras / active | 2 / 1 |
| AudioListeners / active | 2 / 1 |
| Lights / realtime / shadow-casting | 21 / 21 / 6 |
| Reflection Probes / Light Probes | 0 / 0 |
| Canvases | 0 |
| TextMesh / TMP labels | 41 / 0 |
| AudioSources | 0 |
| Particle Systems | 0 |
| Objects outside 16×16 m floor bounds | 0 |

## Visual inspection baseline

Đây là screenshot inspection, không phải headset inspection.

- Entrance: tỷ lệ phòng đọc được nhưng hình học chủ yếu là primitive, nhiều cạnh trụ thô, trần sáng gắt và vật liệu phẳng.
- Titration standing: bàn/console chiếm nhiều tầm nhìn; chữ nút bị outline lệch và một số nhãn nằm sát mép/cắt khung.
- Titration seated: mặt bàn che phần lớn dụng cụ và nút; chưa đạt seated usability.
- Daniell/Electrolysis: chữ control trôi trên thiết bị, outline màu bị ghosting; vật dụng và glass/liquid chưa đủ tin cậy.
- Bottle labels: chữ đọc được ở close-up nhưng sticker lớn, không ôm thân; pipette chắn nhãn; glass thiếu thickness/Fresnel rõ.
- Instruction panel checkpoint: không có instruction panel thực tế; ảnh chỉ ghi không gian station.
- Bright/dark text: sign có backing plate và tương phản cơ bản, nhưng legacy raster TextMesh lộ blur/pixelation ở khoảng cách gần.
- Không thấy material hồng trong bộ ảnh.

## Console, compile và test baseline

- Unity Editor GUI automation compile và chạy audit thành công, process exit code 0.
- EditMode run 1.3 lúc 14:54:45Z: 24 discovered, 24 executed, 24 passed, 0 failed/skipped/inconclusive.
- Existing ChemistryLab Editor validation: pass sau khi sửa false positive, không rebuild scene; gồm desktop setup, hai configurable lesson và ba trial chuẩn độ concordant.
- Batch mode đã thử hai lần và bị Licensing Client timeout/mất kết nối; log được giữ, không tính pass. Editor GUI automation là đường xác minh thay thế đã chạy thật.
- Warning còn lại: Input Manager deprecation vì `Both`; giữ có chủ đích đến khi semantic Input Actions của Desktop được triển khai. Unity shutdown báo JobTemp allocation warning; cần tái đo trong player ở Phase 7, chưa quy kết cho VLAB runtime.

## Rủi ro migration ưu tiên

1. OpenXR/XR Plug-in Management vắng mặt; thêm package/settings có thể làm thay đổi input/build configuration.
2. Mode controller boolean có thể kích hoạt sai rig hoặc không fallback khi XR init fail.
3. Hai camera đều mang MainCamera tag; code dùng `Camera.main` có thể chọn sai.
4. Builder chưa atomic; lỗi giữa build có thể mất generated root.
5. 21 realtime lights, gồm glow label, trái ràng buộc hiệu năng/thiết kế và cần loại bỏ ở Phase 3/4.
6. Không Rigidbody/grab contract cho dụng cụ; scene hiện chưa phải lab tương tác vật lý.
7. Legacy TextMesh/GUI Text shader chưa đáp ứng SDF tiếng Việt hoặc missing-glyph test.
8. Daniell/điện phân hiện là configurable step counter, chưa kiểm tra wiring/electrode/chemistry state.
9. Không có audio, mixer, haptic abstraction, recovery service hoặc PlayMode test assembly.
10. GTX 1050 Ti 4 GB là máy baseline; chưa có headset/runtime nên không thể đo PCVR frame timing.

## Quality Gate Phase 0

Trạng thái: **PASS — Structurally verified**, với hardware/performance rows giữ `Not tested`.

- Renderer thực tế, OpenXR status, camera Desktop/XR, generated scene và package versions đã xác định trong run plan 1.3.
- Có recovery checkpoint đã restore/import/compile thử, before metrics, 11 ảnh baseline, risk register, compile log và EditMode XML nonzero.
- Verifier cuối: `PHASE0_GATE=PASS`; source `ea7ece13ecf4c702d3b584777d9f916a56bb74576515b6cf3097b4d463f22c2c`, required-artifact aggregate `3d56ab6c0175a51ac66874eab253036f45538043ba3d7647de49a7683d433f64`.
- Không thay đổi layout/scene và không bật URP.
- Không tuyên bố simulator/build/hardware/performance pass ở Phase 0.

## Post-audit delta — Phase 1 closed 2026-09-01

2026-09-07: P2.a review found and fixed 10× mouse sensitivity and leaked input clones with reproducing PlayMode tests. Desktop capsule now remains upright independently of camera pitch. Detailed evidence: `Docs/P2A_TDD_EVIDENCE.md`. Difficulty-prioritized backlog: `Docs/VLAB_TASK_DIFFICULTY.md`.

Phần trên giữ nguyên như baseline lịch sử. Trạng thái hiện tại sau Phase 1:

- XR Management 4.5.4 và OpenXR 1.17.1 đã được pin/configure cho Windows; initialize-on-startup tắt và router quản lý loader.
- Scene đã được atomic Builder nâng từ marker v8 lên v9 qua checkpoint `CP-P1.d2-pre-20260901T140000Z`; không sửa YAML thủ công.
- VLAB-owned XR Origin/simulator/input assets thay cho sample path làm nguồn tích hợp scene; rig có hai Direct Interactor và tracking-loss guards.
- Ba presentation mode dùng chung state; PlayMode scene smoke 3/3 xác nhận mỗi mode chỉ có một camera/listener/MainCamera tag và simulator chỉ active trong Simulator mode.
- Release Windows build đã strip đúng một simulator root khỏi build scene copy. Development player yêu cầu Hardware nhưng loader không khởi tạo được trên host, ghi `LoaderInitializationFailed`, fallback an toàn về Desktop và tự thoát 0.
- Renderer vẫn Built-in ở Graphics và mọi Quality override; Input Handling vẫn `Both` theo freeze của Phase 1.
- Final Phase 1 evidence: EditMode 46/46, PlayMode 3/3, release/development build 0 errors, gate/source fingerprint `db3c87a1ca99d832748055cd6bd7e3f13487599668db45533a25ff4eff0ee373`.
- Hardware stereo, tracking đầu/tay, floor calibration, haptic, comfort và headset frame timing vẫn `Not tested — hardware required`.
