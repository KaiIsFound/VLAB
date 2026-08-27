# Phòng Lab Vật lí - Thí nghiệm con lắc đơn

Thư mục này bổ sung phần **mô phỏng học tập** còn thiếu cho scene `Assets/PhysicsLab.unity`: đo chu kì con lắc đơn và suy ra gia tốc trọng trường. Menu `VLAB > Build Physics Lab` tạo phòng, các trạm thiết bị, bảng trạng thái VR và bảng điều khiển XR.

## Gắn vào scene

1. Mở `PhysicsLab.unity` và chạy `VLAB > Build Physics Lab`. Tool giữ lại XR Origin/Interaction Manager nhưng xóa các bàn demo của Starter Assets.
2. Nhấn Play. Dùng XR Device Simulator/VR controller chạm hoặc select các nút vật lí trên bàn: `L -`, `L +`, `BẮT ĐẦU`, `DỪNG/GHI`, `ĐẶT LẠI`.
3. Bảng trong VR hiển thị thời gian, số dao động, chiều dài và giá trị g của lần ghi gần nhất.
4. Nếu cần UI 2D cho điện thoại, tạo Canvas với Slider chiều dài (0.20-1.50), Slider số dao động (5-20), các nút và TMP Text; sau đó gắn `PendulumExperimentPanel.cs`.
5. Với ESP32/BLE, object gốc đã có `VLabControllerBridge`. Plugin Android BLE gọi `UnitySendMessage("__PhysicsLab_Generated__", "ReceiveCommand", "start")` (hoặc `stop`, `reset`, `length_plus`, `length_minus`) để dùng cùng luồng điều khiển với nút VR.

## Tiêu chí hoàn thành bài học

- Học sinh đặt L, chọn N = 10, khởi động phép đo và ghi được `t`.
- Hệ thống tính `T = t/N` và `g = 4π²L/T²`.
- Học sinh thực hiện ít nhất ba lần đo ở các chiều dài khác nhau, so sánh sai số với 9.81 m/s².

Các script không phụ thuộc vào plugin ngoài; dự án đang dùng Unity 6, URP và XR Interaction Toolkit 3.5.1.
