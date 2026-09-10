# VLAB — hướng dẫn demo nhanh

## Setup Play trong Unity (không cần dựng lại phòng)

1. Unity Hub > Open: chọn thư mục `VLAB-Unity-ChemistryLab`, dùng Unity 6000.5.6f1. Chờ Unity biên dịch xong; Console không có lỗi đỏ.
2. Trong Project mở `Assets/ChemistryLab.unity`. Không kéo thêm XR Origin, camera hoặc simulator vào Hierarchy: scene đã có đủ.
3. Chọn `__ChemistryLab_Generated__` trong Hierarchy, tìm component `ChemistryLabModeController` ở Inspector.
4. Chọn `Startup > Default Request = Desktop` để dùng chuột/phím thường, hoặc `XRSimulator` để dùng simulator. Không chọn OpenXRHardware khi không có kính.
5. Nhấn Play rồi click vào **Game**, không phải Scene. Desktop: Esc ẩn bảng trước khi cầm bình. Simulator: xem bảng điều khiển sample và các phím bên dưới.
6. Nếu đổi mode, Stop rồi Play lại. Không dùng menu Build Chemistry Lab chỉ để Play; menu đó tái tạo scene và yêu cầu checkpoint.

Bản sửa xuyên bàn nằm trong script: khi Play, bình dùng Velocity Tracking và Continuous Dynamic. Không cần sửa từng bình trong Inspector. File Windows zip ngày 09/09 là bản cũ, chưa chứa sửa này; dùng gói mới nhất được bàn giao sau kiểm thử.

## Windows / chuột bàn phím

Chạy `VLAB Chemistry Lab.exe` cùng toàn bộ thư mục đi kèm, không chép riêng file exe. Không cần mở Unity. Bản Windows release dùng Desktop khi không có kính; simulator dành cho Unity Editor.

Nút **THIẾT LẬP** trên đầu bảng mở bốn mức đồ họa (Nhẹ/Cân bằng/Cao/Rất cao), độ nhạy chuột, âm lượng và bật/tắt căn rót. Thay đổi được lưu trên máy và không reset bài. **VỀ BÀI LAB** trở lại thí nghiệm; **KHÔI PHỤC THIẾT LẬP** trả về mức Cân bằng. Âm thanh bấm nút dùng tối đa hai nguồn âm thanh. Các preset là cấu hình đồ họa, chưa phải cam kết FPS trên kính VR.

- WASD di chuyển, giữ chuột phải để nhìn, Shift chạy, C cúi.
- Click bình trong tầm 3 m để cầm, click lần nữa để thả. Cuộn chỉnh xa/gần khi cầm.
- F mở/đóng nắp hoặc hút/nhả pipette. Giữ R nghiêng bình để rót; thả R dựng thẳng.
- Vòi burette: click 0.01 mL; giữ chuột trái quá 0.25 giây để chảy liên tục, nhả để dừng.
- Esc thả vật và ẩn/mở bảng. Không click qua bảng UI để cầm dụng cụ.
- Nút **RESET LƯỢNG CHẤT — GIỮ KẾT QUẢ** ngay dưới các tab: nạp lại chai gốc, làm rỗng dụng cụ, dừng khóa burette và reset lượt hiện tại. Không di chuyển vật đang cầm, không xóa kết quả đã ghi.
- Khi cầm dụng cụ có chất và đã mở nắp, đường căn rót cùng vòng đánh dấu cho biết điểm rơi theo dòng thẳng xuống. Xanh = trúng miệng bình mở trong tầm; cam = không có bình nhận hợp lệ về vị trí hoặc bị vật cản. Xanh chỉ xác nhận căn miệng, vẫn phải dùng đúng hóa chất/thứ tự. Burette có chất luôn hiện dấu để căn bình bên dưới. Dấu xem trước không tiêu hao chất; nghiêng chai/nhấn vòi mới rót.

## Kịch bản chuẩn độ

1. Nhấn PPE. Rót 5 mL NaOH vào burette. Đưa bình thải sát dưới vòi, không để bình mẫu chắn ở giữa, rồi xả hết.
2. Nạp burette 50 mL NaOH; dựng chai thẳng và đóng nắp.
3. Đưa đầu pipette vào chai giấm đã mở, F hút 10 mL; đưa trên miệng bình tam giác, F nhả mẫu.
4. Nghiêng chai chỉ thị trên miệng bình để nhỏ hai giọt.
5. Đặt bình mẫu dưới vòi, dời bình thải ra. Giữ vòi cho dòng chậm; gần 8.33 mL thì click từng giọt, quan sát hồng nhạt rồi GHI.
6. Lặp ba lượt đồng quy. RESET làm lại lượt; XÓA xóa kết quả. Nếu mất mẫu, RESET trước khi tiếp tục.

Có thể demo quy trình bằng nút trên bảng từ đầu. Sau khi đã chuyển hóa chất bằng dụng cụ, nút tắt chuẩn bị/thêm hóa chất bị khóa cho lượt đó; RESET để quay về cách dùng nút. Pin Daniell và điện phân là bài hướng dẫn bằng nút, chưa phải thao tác vật lý đầy đủ.

## XR Device Simulator trong Unity

Sample XR Device Simulator của XRI 3.5.1 đã được tích hợp vào scene. Wrapper vẫn mang tên `VLAB XR Interaction Simulator` để giữ liên kết; component bên trong là XRDeviceSimulator.

Mở `Assets/ChemistryLab.unity`. Chọn `__ChemistryLab_Generated__` > ChemistryLabModeController > Default Request = XRSimulator, rồi Play và focus Game view. Nếu có lựa chọn mode đã lưu hoặc tham số dòng lệnh, chúng có thể ghi đè Inspector.

Bindings lấy từ sample đã cài: giữ Shift trái chọn tay trái, Space chọn tay phải; G = Grip, chuột trái = Trigger. Chuột phải điều khiển đầu; WASD/QE dịch chuyển thiết bị đang chọn; Ctrl đổi sang xoay; Tab chuyển thiết bị, V reset. Đây là điều khiển simulator, khác phím Desktop.

Không chạy `Create Owned XR Rig Assets` để thử thông thường: đây là lệnh tái tạo asset, không phải nút Play. Không cần rebuild scene mỗi lần mở project.

Khi Play, rig dùng snap-turn 30° và vùng teleport chỉ trên sàn. Điểm sát mép phòng hoặc không đủ chỗ đứng dưới bàn/cạnh tường bị từ chối; không teleport lên bàn. Đường kẻ sàn chỉ trang trí, không chặn tia. Đây là cấu hình runtime nên scene đã lưu nhận sửa mà không cần tái tạo. Điều khiển thumbstick/teleport phải dùng binding của sample; WASD trong Device Simulator dịch chuyển thiết bị mô phỏng, không tự chứng minh teleport.

## Trước buổi demo

- Dùng đúng máy, chuột và độ phân giải sẽ trình chiếu; chạy một lượt đầy đủ trước giờ demo.
- Giữ một bản sao cả thư mục WindowsPlayer dự phòng; không cập nhật package/renderer sát giờ.
- Kính VR thật chưa được kiểm thử. Kết quả test simulator không xác nhận độ ổn định trên kính.
