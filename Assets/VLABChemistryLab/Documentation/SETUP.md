# Phòng Lab Hóa học — Chuẩn độ axit–bazơ

Lab mô phỏng xác định hàm lượng axit axetic trong giấm bằng dung dịch NaOH 0,1000 mol/L. Mẫu giấm được pha loãng chính xác 10 lần; lấy 10,00 mL mẫu và dùng phenolphthalein để nhận biết điểm cuối.

## Mục tiêu học tập

- Thực hiện đúng thứ tự an toàn và thao tác chuẩn độ.
- Nhận biết điểm cuối hồng nhạt, phân biệt với quá chuẩn hồng đậm.
- Ghi thể tích NaOH và tính nồng độ axit axetic của giấm ban đầu.
- Thu được ba giá trị chuẩn độ đồng quy, có độ chênh không quá 0,10 mL.

Phản ứng có tỉ lệ mol 1:1:

```text
CH3COOH + NaOH -> CH3COONa + H2O
```

```text
C(CH3COOH, ban đầu) = C(NaOH) * V(NaOH) / V(mẫu pha loãng) * 10
% m/V = C(CH3COOH, ban đầu) * 60,052 / 10
```

## Tạo và chạy scene

1. Mở project `VLAB-Unity-ChemistryLab` bằng Unity 6000.5.6f1.
2. Chọn menu `VLAB > Build Chemistry Lab`.
3. Mở `Assets/ChemistryLab.unity` nếu Unity chưa tự mở scene.
4. Nhấn Play.
5. Trong Game View, dùng bảng **Desktop Lab** ở góc trái và bấm các nút bằng chuột. Nút **ẨN** thu gọn bảng để quan sát toàn bộ phòng lab.

Không có headset: Desktop Lab là chế độ mặc định. Khi Unity nhận headset XR thật, project tự tắt camera/bảng Desktop và bật XR Origin cùng XR Interaction Manager. XR Device Simulator UI được tắt để không chồng lên giao diện bài học.

### Bài học và yêu cầu có thể thay đổi

Ngoài chuẩn độ axit-bazơ, scene có **Pin Daniell Zn-Cu** và **Điện phân dung dịch CuSO₄**. Mở các asset trong `Assets/VLABChemistryLab/Experiments/` để giáo viên thay đổi mục tiêu, ghi chú an toàn, yêu cầu, bước tiến hành và kết quả mong đợi mà không sửa mã nguồn. Trong Game View, chọn tab bài học ở đầu bảng; kéo thanh tiêu đề để di chuyển bảng và kéo góc dưới-phải để đổi kích thước. Khi Game View nhỏ, nội dung tự cuộn thay vì bị cắt.

Builder chỉ dùng primitive của Unity để tránh phụ thuộc vào các gói mô hình chưa xác minh giấy phép. Đây là project độc lập, không cần Physics Lab. Có thể chạy builder nhiều lần; nhóm `__ChemistryLab_Generated__` cũ được thay thế thay vì nhân đôi.

## Quy trình trong bài học

1. Mang kính, găng tay và áo choàng.
2. Tráng rồi nạp burette bằng NaOH; đuổi bọt khí và tháo phễu.
3. Dùng pipette lấy 10,00 mL mẫu giấm pha loãng vào bình tam giác.
4. Thêm 2–3 giọt phenolphthalein.
5. Thêm NaOH nhanh khi còn xa điểm cuối, sau đó chuyển sang từng giọt.
6. Khi dung dịch hồng nhạt bền, ghi kết quả. Hồng đậm là quá chuẩn và phải làm lại lần đó.
7. Lặp lại đến khi có ba kết quả đồng quy.

### Lộ trình thao tác nhanh trong Desktop Lab

Mỗi lượt: **Mang PPE → Chuẩn bị burette → Lấy mẫu → Thêm chỉ thị**, sau đó thêm `+1.00 mL` tám lần, `+0.10 mL` ba lần và `+0.01 mL` ba lần. Thể tích sẽ là **8,33 mL**, dung dịch chuyển hồng nhạt và có thể bấm **GHI KẾT QUẢ**. Lặp lại đủ ba lượt để nhận % m/V. Dùng **LÀM LẠI** cho lượt đang dở hoặc **XÓA KẾT QUẢ** để bắt đầu lại toàn bộ.

## Điều khiển phần cứng trung lập

`ChemistryControllerBridge.ReceiveCommand(string)` là biên tích hợp cho plugin Android BLE/ESP32. Các lệnh dùng chung luồng với XR và chuột:

```text
safety
prepare
sample
indicator
dose_fast
dose_drop
record
reset
clear
```

Bridge không tự triển khai Bluetooth. Plugin Android có thể gọi `UnitySendMessage` vào object gốc do builder tạo. Lab vẫn hoạt động đầy đủ trong Editor khi không có plugin BLE.

## An toàn và giới hạn

- NaOH gây ăn mòn; axit axetic gây kích ứng; phenolphthalein thường dùng dung môi có khả năng cháy.
- Không hút pipette bằng miệng. Khi hóa chất bắn vào mắt/da, rửa ngay và báo giáo viên.
- Mô phỏng loại bỏ phơi nhiễm trong VR nhưng không thay thế hướng dẫn, SDS và giám sát khi làm thí nghiệm thật.
- Mô hình tập trung vào hóa lượng, quy trình và đánh giá học tập; không phải mô phỏng động lực học chất lỏng chính xác.
