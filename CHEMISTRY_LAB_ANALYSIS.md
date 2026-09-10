# Chemistry Lab — phạm vi hoàn thiện

## Thành phần VLAB được đáp ứng

| Thành phần | Triển khai |
|---|---|
| Unity/URP | Scene `Assets/ChemistryLab.unity`, dựng bằng primitive nhẹ |
| VR/XR | Nút `XRSimpleInteractable`, giữ XR Origin và XR Interaction Manager |
| Desktop | Cùng nút hỗ trợ `OnMouseDown` để chơi trong Unity Editor |
| Điện thoại | Adapter uGUI/TMP tùy chọn, không bắt buộc scene phải có Canvas |
| ESP32/BLE | Bridge lệnh trung lập, sẵn cho plugin Android; chưa bao gồm plugin BLE thật |
| Bài học | Mục tiêu, quy trình an toàn, hướng dẫn theo trạng thái và phản hồi lỗi |
| Mô phỏng | Hóa lượng chuẩn độ axit axetic–NaOH, endpoint và overshoot |
| Đo lường | Thể tích NaOH, nồng độ mol, g/L và `% m/V` |
| Đánh giá | Ba phép đo đồng quy trong 0,10 mL và kết quả định lượng |
| Khả năng mở rộng | Lõi C# thuần tách khỏi Unity; UI/XR/BLE dùng chung command path |

## Lựa chọn thiết kế

Lab dùng giấm pha loãng 10 lần, aliquot 10,00 mL, NaOH 0,1000 mol/L và phenolphthalein. Giá trị mô phỏng mặc định tương ứng giấm khoảng 5% m/V, điểm cuối gần 8,33 mL. Logic tính toán xác định, không dùng ngẫu nhiên nên có thể kiểm thử và so sánh kết quả học sinh.

Các tệp ZIP mô hình hóa học trong `Assets/Layer3_Dung cu` không được giải nén hoặc đưa vào scene vì chưa có bằng chứng giấy phép riêng. Primitive giúp prototype chạy ngay, nhẹ cho điện thoại và tránh rủi ro phân phối asset.

## Giới hạn kiểm chứng tại máy hiện tại

Unity Editor có thể mở và Play tương tác bằng giao diện. Batch-mode Test Runner yêu cầu entitlement headless mà cài đặt hiện tại chưa cung cấp, nên các EditMode test lõi được biên dịch và thực thi bằng Roslyn/NUnit đi kèm Unity. Kiểm thử kính VR, Android và ESP32/BLE vẫn cần thiết bị thật.

