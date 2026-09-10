# P2.a — Desktop Input Actions

## Hành vi đã chạy

- RED 2026-09-01: thiếu semantic input namespace; GREEN 3/3; regression EditMode 49/49, PlayMode 3/3.
- RED 2026-09-07 `20260907-input-behavior-red-host`: 2/2 test thất bại đúng lỗi nhìn nhanh gấp 10 và clone input không được hủy.
- Sửa: nhân mouse delta với hệ số 0,1 cũ, clone riêng và hủy action asset theo vòng đời; file component được đổi tên phù hợp Unity và giữ GUID.
- RED `20260907-escape-red`: thiếu API hiển thị menu. Sửa: Escape toggle panel và giải phóng chuột.
- Capsule Desktop được tách khỏi camera để giữ trục đứng khi camera pitch; collider được tắt cùng Desktop mode và hủy khi unload.
- GREEN `Artifacts/VLABUpgrade/plan-1.4/P2.a/20260907T080958Z-floor-contact-green-PlayMode/results.xml`: **6/6**, gồm input thiết bị ảo, sensitivity, action disposal, UI scroll/FOV clamp, crouch/stand và scene mode-switch.
- Crouch được đo bằng chênh lệch 0,62 m so với standing; camera giữ khoảng hở skin/ron sàn. Các lần thử trước đo world-Y tuyệt đối được giữ và không tính Pass.

## Giới hạn

- Unity trong sandbox ngày 7/9 trả license error 198. Phiên Windows ngoài sandbox chạy được; không thay đổi cấu hình license.
- Chưa đo coverage bằng công cụ; không tuyên bố đạt 80% chỉ từ test count.
- Windows build ngày 1/9 thuộc nguồn Phase 1. Nguồn P2 cần build mới sau phase integration.
- Không commit theo yêu cầu gốc; checkpoint trước P2.a giữ trong `_VLAB_Backups/ChemistryLab/CP-P2.a-pre-20260901T190000Z`.
