# Thực hành bằng tay cầm và Desktop — 2026-09-09

Cập nhật buổi tối: đã tích hợp XR Device Simulator từ sample XRI, bỏ tay trang trí Desktop, thêm giữ chuột trên khóa burette để rót. Test Trigger đã đạt khi tín hiệu thử được gửi đúng pha Dynamic (không để pha BeforeRender tiêu thụ trước). Trạng thái kiểm thử/bản phát hành mới nhất ở `DEMO_DAY_CHECKLIST.md`; hướng dẫn thao tác ngắn ở `DEMO_QUICK_START.md`. Các số liệu nghiệm thu bên dưới ghi lại đợt trước đó.

## Phạm vi đã tích hợp

Scene `Assets/ChemistryLab.unity`, marker v10, có một bộ chuẩn độ thao tác bằng dụng cụ thật trong môi trường ảo. Bộ này dùng chung `TitrationLessonController` và kết quả ba lượt với giao diện Desktop. Chưa chuyển Pin Daniell và điện phân CuSO4 sang thao tác bằng dụng cụ; hai bài này vẫn dùng quy trình cũ.

Nghiệm thu ngày 2026-09-09: ba test cầm/thả Desktop đã pass. Full PlayMode 14/15; test phát sự kiện Trigger từ controller còn thất bại, nên phần XR chưa được nghiệm thu. Xem chi tiết tại `Docs/VLAB_VERIFICATION_REPORT.md`.

- Ba chai: NaOH 0.100 M (200 mL), giấm pha loãng (100 mL), chỉ thị (20 mL), có nắp bật bằng trigger.
- Pipette có bóng hút, định lượng 10 mL; bình tam giác 250 mL; burette 50 mL cố định trên giá và bình thải 500 mL.
- Mười dụng cụ di động dùng XRGrabInteractable và recovery; burette cố định. Ba chai và pipette trên kệ hóa chất cũ đã chuyển từ đồ trưng bày sang dụng cụ dùng được, tổng cộng 11 bình/dụng cụ chứa chất lỏng.
- Mesh thủy tinh rỗng tạo bằng Editor, 48 cạnh vòng, lưu trong `Assets/VLABChemistryLab/Models/HandsOn`; không cần tải model bên ngoài.
- Dòng rót xét góc nghiêng, miệng nhận mở và dựng đứng, khoảng cách tối đa 45 cm và vật cản. Mức chất lỏng và nhãn thể tích cập nhật sau mỗi lần chuyển.

## Cách thao tác

### Chuột và bàn phím

Mở lại `Assets/ChemistryLab.unity`, bấm Play và đi tới gần bàn/kệ (WASD). Click vào bình trong tầm 3 m để cầm, click lần nữa để thả. Di chuyển chuột để dịch chuyển bình; cuộn để chỉnh xa/gần. **F** mở/đóng nắp hoặc dùng pipette; giữ **R** để nghiêng rót, thả R để dựng thẳng; **Esc** thả vật. Hướng dẫn xuất hiện ở cuối màn hình. Cuộn khi cầm không đổi FOV. Click trên bảng giao diện không cầm/thả nhầm đồ.

Desktop có manager cầm riêng vì rig XR bị tắt ở chế độ này. Trạng thái giữ vẫn là XRI selection thật, được dùng chung với kiểm tra nắp và recovery; khi thả/chuyển mode, dụng cụ trả về manager trước đó. Hai bàn tay trang trí cũ không còn là cơ chế duy nhất của Desktop.

### XR Simulator / tay cầm

1. Mở scene ChemistryLab. Trong Inspector của `__ChemistryLab_Generated__`, tại `ChemistryLabModeController / Startup / Default Request`, chọn `XRSimulator` để thử trong Editor hoặc `OpenXRHardware` khi có kính/runtime. Chọn trước khi Play; cấu hình dòng lệnh hoặc lựa chọn đã lưu có ưu tiên cao hơn Inspector. Hoàn thành PPE trên bảng điều khiển.
2. Đưa tay gần chai và giữ **Grip** để cầm. Bóp **Trigger** để mở/đóng nắp. Nghiêng chai trên miệng burette; giữ miệng chai gần để rót đúng chỗ.
3. Rót 5 mL NaOH để tráng. Cầm bình thải đặt dưới vòi burette. Chọn khóa bằng Grip và giữ Trigger để xả; hết lượng tráng mới được nạp tiếp tới 50 mL.
4. Mở chai giấm. Cầm pipette, đưa đầu dưới vào miệng chai, bóp Trigger để hút 10 mL. Đưa đầu pipette phía trên miệng bình tam giác và bóp Trigger để nhả mẫu.
5. Mở chai chỉ thị và nghiêng trên miệng bình: 0.05 mL mỗi giọt, cách nhau 0.4 giây; đủ hai giọt sẽ chuyển sang chuẩn độ.
6. Đặt bình mẫu dưới vòi burette. Chọn khóa để thêm 0.01 mL; giữ Trigger để chảy chậm 0.5 mL/s. Thả Trigger hoặc Grip để dừng. Theo dõi hồng nhạt và nhấn **GHI** khi đạt điểm cuối.
7. Ghi xong sẽ làm sạch/nạp lại tồn kho cho lượt kế tiếp; vị trí dụng cụ đang cầm được giữ nguyên. **RESET** hủy lượt hiện tại. **CLEAR** xóa kết quả.

Khi đã chuyển hóa chất bằng dụng cụ, các nút tắt chuẩn bị/lấy mẫu/thêm chỉ thị/thêm NaOH bị khóa cho lượt đó. RESET cho phép quay lại luồng hướng dẫn Desktop bằng nút.

## Phản hồi và giới hạn

- Sai hóa chất/thứ tự hoặc nắp đóng: từ chối chuyển và báo hướng dẫn. Đây là chế độ thực hành có hướng dẫn, chưa mô phỏng mọi phản ứng khi trộn sai.
- Rót trượt/vướng vật cản: trừ hóa chất đã đổ; mất mẫu trong pipette/bình/burette sẽ chặn ghi kết quả và yêu cầu RESET.
- Giới hạn dung tích chặn lượng vượt quá; chưa mô phỏng chất lỏng chảy tràn trên mặt bàn.
- Vật ngoài vùng được đưa về vị trí xuất phát khi không đang cầm. Khóa burette ngừng khi nhả tay hoặc vô hiệu hóa.
- Dòng rót/mức chất lỏng là mô phỏng thể tích, không phải mô phỏng chất lỏng động lực học; chưa mô phỏng meniscus quang học, xoáy trộn hay vỡ kính.
- PPE còn là bước xác nhận trên bảng. Chưa có thao tác mặc găng/kính lên cơ thể.
- Chưa xác minh cảm giác cầm, độ thuận tay, tracking, haptic hoặc hiệu năng trên kính thật. Không dùng kết quả Unity để thay cho kiểm thử phần cứng.
- Builder nối Select/Activate của cả hai direct interactor từ Input Actions do project sở hữu. Tracking guard đọc trạng thái Input System để nhận được cả controller giả lập và controller OpenXR; vẫn vô hiệu hóa tay khi mất tracking.

## Tái tạo và bằng chứng

Builder nguồn: `Assets/Editor/ChemistryLabHandsOnBuilder.cs`, được gọi trong atomic `ChemistryLabBuilder`. Không sửa YAML scene.

Menu **VLAB / Build Chemistry Lab** hiện mở hộp chọn thư mục checkpoint. Chọn checkpoint có scene hash khớp scene đang lưu; batch build vẫn cần cả `-vlabCheckpointId` và `-vlabCheckpointRoot`.

Checkpoint trước tích hợp: `../_VLAB_Backups/ChemistryLab/CP-P2-hands-on-pre-20260908`. Các checkpoint review/final bảo vệ các lần chỉnh hình. Artifact, XML kiểm thử và ảnh nằm dưới `Artifacts/VLABUpgrade/plan-1.4/P2.hands-on/`.

P2.b trước tích hợp: 7/7 PlayMode. Kiểm thử mới bao gồm bảo toàn thể tích, giá trị không hợp lệ, nắp đóng, hóa chất sai, vật cản, rót theo góc nghiêng, thao tác pipette, ba lượt đồng quy, chặn bỏ qua bước và reset sau mất mẫu. Kết quả cuối ghi tại báo cáo xác minh.
