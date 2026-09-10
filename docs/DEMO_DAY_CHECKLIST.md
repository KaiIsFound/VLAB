# Demo trong một ngày — phạm vi chốt

## Bổ sung reset lượng chất và căn rót — 10/09

- Nút reset cố định dưới tab chuẩn độ: nạp lại chai, làm rỗng dụng cụ, dừng dòng burette, reset lượt, giữ kết quả và vị trí vật đang cầm.
- Đường/vòng căn rót dùng cùng ReceiverBelow với rót thật; xanh báo trúng miệng bình mở, cam báo trượt/vật cản. HUD ghi tên bình nhận. Đây là trợ giúp hình học, không cho phép bỏ qua thứ tự hóa học hay mô phỏng CFD.
- Dòng thực hiển thị điểm rơi thẳng xuống thay vì kéo chéo về tâm bình; giữ hiển thị giọt 0.10 giây để dễ thấy.
- PlayMode **21/21**: `DemoDay/20260910T061025Z-pour-guide-reset-PlayMode/results.xml`; EditMode **60/60**: `DemoDay/20260910T061227Z-pour-guide-final-EditMode/results.xml`. Test hành vi theo skill csharp-testing; vẫn dùng NUnit Unity sẵn có.
- Windows mới: `Builds/DemoDay-20260910-pour-guide/VLAB-Demo-Windows.zip`. Build Succeeded, 0 errors/1 warning TMP URP như trước; player smoke Pass/exit 0, ảnh startup đã xem và thấy nút reset. Dấu căn có test hình học/hiển thị bật-tắt nhưng chưa kiểm tra bằng kính VR thật.

## Cập nhật 10/09 — sửa xuyên bàn và UI

- Gate PlayMode mới nhất **19/19**: `DemoDay/20260910T054327Z-locomotion-contact-gate-PlayMode/results.xml`. Teleport qua chọn/thả tia đã chạy, layer Teleport khớp hai tay, sàn kiểm tra khoảng trống/biên, snap input xoay rig 30°. Đã bỏ collider đường kẻ sàn chặn tia; collider sàn/bàn/tường giữ nguyên. Đây không phải kiểm thử thao tác thumbstick của người dùng trên kính thật.
- Gate EditMode mới nhất **60/60**: `DemoDay/20260910T054509Z-locomotion-final-EditMode/results.xml`.
- Rà soát mới nhất: P2–P7 vẫn còn yêu cầu mở; xem `VLAB_TASK_DIFFICULTY.md`. Không coi các tab Daniell/điện phân hiện có là mô phỏng vật lý hoàn chỉnh.

- Chuyển grab của dụng cụ sang Velocity Tracking + Continuous Dynamic ngay trong Awake, nên scene đã lưu cũng nhận sửa khi Play. Giới hạn vận tốc bám tay 3 m/s và xoay 6 rad/s; không dùng dịch chuyển Transform xuyên collider. Builder cũng lưu cấu hình mới cho lần tái tạo sau.
- Test ép bình đang cầm xuống dưới mặt bàn qua 40 bước vật lý và thả ra đã đạt. Full PlayMode **17/17**: `DemoDay/20260909T235825Z-collision-release-gate-PlayMode/results.xml` trong artifact root. Test pipette được tách vật thử sau mỗi lượt để tránh các chai trước đó cản pipette; không bỏ tiêu chí dựng thẳng. Cấu hình quán tính pipette thử nghiệm đã bỏ vì không phải nguyên nhân cuối cùng.
- UI: tăng cỡ chữ, nút phẳng và sáng hơn, có trạng thái hover; khung bảng chừa chỗ cho thanh điều khiển; đổi tab đưa cuộn về đầu; thanh đang cầm hiện tên, thể tích và trạng thái nắp.
- Bản mới nhất: `Builds/DemoDay-20260910-locomotion/WindowsPlayer/VLAB Chemistry Lab.exe`. Build Succeeded, 0 errors, 1 warning; player smoke Pass/exit 0, simulator đã loại khỏi release. Ảnh player 1280×720 đã kiểm tra: bảng và HUD không chồng nhau, chữ Việt Desktop đọc được; nhãn 3D ở xa vẫn nhỏ và chưa qua SDF upgrade. Bản `DemoDay-20260910` giữ làm dự phòng trước teleport; bản 09/09 chưa có sửa xuyên bàn.
- Warning còn lại: shader `TextMeshPro/SRP/TMP_SDF-URP Lit` không khớp Built-in nên bị strip. Chi tiết ở `build-messages.txt`; không đổi renderer sát giờ. Không xác nhận mọi material đã audit chỉ từ ảnh khởi động.
- Gói mới đã tạo và kiểm tra 205 entry: `Builds/DemoDay-20260910-locomotion/VLAB-Demo-Windows.zip`, có exe và README; Burst symbols giữ riêng ở `BurstDebugSymbols`, không xóa. Giải nén toàn bộ rồi chạy exe, không lấy riêng exe.

Các mục 09/09 bên dưới là bằng chứng lịch sử, không thay thế gate của bản 10/09.

## Đã tích hợp và kiểm thử Unity

- EditMode **60/60**: `Artifacts/VLABUpgrade/plan-1.4/DemoDay/20260909T160815Z-release-gate-EditMode/results.xml`.
- PlayMode **16/16**: `Artifacts/VLABUpgrade/plan-1.4/DemoDay/20260909T160903Z-release-gate-PlayMode/results.xml`.
- Scene đã dùng XR Device Simulator chính thức; atomic build thành công tại `DemoDay/simulator-build.log`, checkpoint `CP-DemoDay-20260909` có hash gốc `89BC4D4E53351A472FED9EE9ED163FC61C765B2E0C5B2BC6DAC50A5D491E22B2`.
- Test Trigger trước đây gửi tín hiệu trước pha BeforeRender nên cạnh bấm bị tiêu thụ trước Dynamic. Đã sửa cách đưa tín hiệu thử, không bỏ assertion hoặc gọi mở nắp trực tiếp: Grip, Trigger mở nắp và mất tracking đều đạt.

- Bỏ hai tay trang trí Desktop; giữ script tương thích để scene cũ không mất reference. Builder mới không gắn script này.
- Vòi burette Desktop: click một giọt, giữ hơn 0.25 giây để rót liên tục; nhả, Esc, mất focus hoặc tắt navigator sẽ dừng.
- UI: nền tối phẳng, thanh tiến độ, khung trạng thái tự giãn, sửa kết quả đè lên nút và vùng cuộn; phản hồi dụng cụ khi ẩn bảng. Nút mở bảng không còn click xuyên vào vật phía sau.
- Thêm kiểm thử chuột giữ/nhả vòi, mất focus, bảo toàn thể tích và không sinh tay trang trí. Theo skill csharp-testing, kiểm tra hành vi và kết quả bằng bộ Unity NUnit sẵn có, không đổi framework.
- Thêm entry point build release + player smoke: `VLAB.Editor.Upgrade.VLABP1WindowsBuildRunner.BuildReleaseAndSmokeWindows`.

## XR Device Simulator theo yêu cầu mới

1. Dừng Play. Window > Package Manager > XR Interaction Toolkit > Samples > **XR Device Simulator** > Import.
2. Chạy **VLAB > Setup > Use Package XR Device Simulator**. Menu sao lưu prefab cũ trước khi thay bằng sample chính thức. Không chạy lại Create Owned XR Rig Assets sau đó vì lệnh cũ tạo XR Interaction Simulator.
3. Tạo checkpoint Assets/Packages/ProjectSettings có ChemistryLab.unity khớp scene hiện tại; dùng **VLAB > Build Chemistry Lab**, chọn checkpoint đó để tái tạo scene bằng atomic builder.
4. Trên `__ChemistryLab_Generated__`, ChemistryLabModeController > Default Request: XRSimulator. Play rồi focus Game view; xem hướng dẫn của simulator. Lựa chọn dòng lệnh/đã lưu có thể ghi đè Inspector.

Tên wrapper `VLAB XR Interaction Simulator` giữ nguyên để không làm hỏng liên kết scene; component bên trong sau cài đặt là XRDeviceSimulator. XRI 3.5.1 đánh dấu đây là sample classic/legacy; không phải package độc lập. Release loại simulator; thử simulator trong Editor.

## Gate bản Windows

Windows release **Succeeded**, 0 errors, 1 warning theo BuildReport; smoke **Pass**, player exit 0. Evidence: `Builds/DemoDay-20260909/windows-build-evidence.json`, `player-smoke.json`, `player-smoke.png`. Ảnh player 1280×720 đã xem: UI tiếng Việt hiển thị, không còn tay trang trí, không thấy shader hồng; nội dung cuối bảng cuộn được. Đây là kiểm tra màn hình khởi động, không thay thế chạy toàn bộ bài bằng người dùng.

Gói bàn giao: `Builds/DemoDay-20260909/VLAB-Demo-Windows.zip`; giải nén rồi chạy `WindowsPlayer/VLAB Chemistry Lab.exe`. Có README trong thư mục chạy. Burst debug symbols giữ riêng ở `BurstDebugSymbols`, không xóa.

Smoke chủ động yêu cầu hardware khi không có runtime, kiểm tra fallback Desktop: một camera, một AudioListener, một MainCamera tag; simulator đã được loại khỏi release. Log có XR runtime unavailable và chuyển software video decoder của môi trường thử. BuildReport ghi 1 warning nhưng log không nêu rõ warning tương ứng; không tuyên bố warning-free.

Scene SHA-256: `AA440AD6D2A083AB9B4CE6621DCF7FEADB96989C695287672045550AF73B4FC4`. Exe SHA-256: `E0F2357D8E9DB65FC9F144E51124E3B366089DE2D8496979070510E2282C8596` (không phải fingerprint toàn bộ player).

VR thật chưa thử; chưa nghiệm thu hiệu năng/haptic/tracking trên kính. Không coi phạm vi demo là hoàn tất toàn bộ roadmap P2–P7.

## Hoãn để giữ hạn một ngày

Không đổi renderer, không thêm mô phỏng chất lỏng động lực học, haptic, model lớn hoặc mở rộng hai bài còn lại thành thao tác vật lý. Chuẩn độ là bài thao tác dụng cụ chính; Pin Daniell và điện phân vẫn là bài hướng dẫn bằng nút. Không xóa mã hoặc bài học cũ.
