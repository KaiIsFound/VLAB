# Đánh giá VLAB và phần hoàn thiện Physics Lab

Đánh giá tại commit `6944bd3` của nhánh `physicslab` (Unity `6000.5.6f1`).

## Hiện có trong Unity

| Hạng mục | Trạng thái | Bằng chứng |
|---|---|---|
| Khung Unity/URP | Có | `Assets`, `Packages`, `ProjectSettings`; Unity 6 và URP 17.5.0 |
| Trang chủ và điều hướng | Có | `Home.unity`, `UIManager.cs`, `SceneLoader.cs` |
| Scene Phòng Vật lí | Có | `Assets/PhysicsLab.unity`, đã nằm trong Build Settings |
| XR Interaction Toolkit + mô phỏng controller | Có | package `com.unity.xr.interaction.toolkit` 3.5.1 và Starter Assets |
| Không gian/phương tiện prototype | Có một phần | builder tạo bàn, tủ, con lắc, quả cân, lò xo, thước, dụng cụ; nhiều asset còn mang tính Hóa/Sinh |
| Nhặt/thả desktop prototype | Có | `PlayerInteraction.cs` dùng raycast, E và Q |
| Kết nối ESP32/BLE | Chưa có | README mô tả kiến trúc nhưng source không có adapter BLE/ESP32 |

## Khoảng thiếu trước khi bổ sung

1. **Bài học có thể hoàn thành:** không có mục tiêu, các bước, giới hạn thao tác hay tiêu chí hoàn thành.
2. **Mô hình và số liệu vật lí:** không tính chu kì/số đo/kết quả/đồ thị/sai số cho bất kỳ thí nghiệm nào.
3. **Tích hợp XR thật:** scene có sample XR nhưng thiết bị dựng bằng `PhysicsLabBuilder` lại dùng `Rigidbody` + layer 7, chưa được cấu hình `XRGrabInteractable`/UI XR.
4. **Kiểm tra/đánh giá:** không lưu bảng số liệu, tính toán hay phản hồi học tập.
5. **BLE:** UI mới chỉ mô phỏng thông báo “kết nối thành công”.

## Phần đã bổ sung

- `Assets/VLABPhysicsLab/Scripts/PendulumExperiment.cs`: mô phỏng con lắc đơn góc nhỏ, điều chỉnh chiều dài/số dao động, animation, ghi số đo và tính `g = 4π²L/T²`; builder tự gắn script này cho con lắc được tạo.
- `Assets/VLABPhysicsLab/Scripts/PendulumExperimentPanel.cs`: liên kết UI Unity (Slider, TMP Text, Button), hiển thị timer và bảng số liệu.
- `Assets/VLABPhysicsLab/Documentation/SETUP.md`: các bước gắn script vào scene cùng tiêu chí bài học.
- `../physics-lab-html-demo/index.html`: bản demo chạy độc lập trên trình duyệt, có điều khiển, animation, bảng số đo và sai số.

## Việc cần làm để thành bản phát hành

- Tạo prefab đúng tỉ lệ cho giá, dây, quả lắc và bảng UI; nối tham chiếu theo `SETUP.md`.
- Bổ sung XR Grab/Poke và kiểm thử Android/VR thực tế.
- Viết adapter BLE được kiểm thử cho ESP32/MPU9250, cơ chế ngắt kết nối và calibration.
- Thêm lưu kết quả theo học sinh, rubric, âm thanh/hướng dẫn và kiểm thử usability với giáo viên/học sinh.
