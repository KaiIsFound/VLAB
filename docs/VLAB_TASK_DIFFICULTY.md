# Phân loại công việc — rà soát yêu cầu lớn 2026-09-10

Yêu cầu mới nhất khôi phục việc rà soát toàn bộ task lớn, không tự động bỏ P2–P7. Chuẩn độ bằng dụng cụ, XR Device Simulator chính thức, UI Desktop và bản Windows đã có; điều đó không đồng nghĩa toàn bộ blueprint hoàn tất. Các mục còn thiếu bên dưới vẫn mở. Xem `DEMO_QUICK_START.md` cho cách Play, `DEMO_DAY_CHECKLIST.md` cho bằng chứng hiện tại.

Theo yêu cầu mới: hoàn thiện phần đang dở, ưu tiên nhóm khó. Độ khó xét theo rủi ro tích hợp và bằng chứng cần có; vẫn giữ quality gate tuần tự P2 → P7. Việc dễ là prerequisite của việc khó được làm kèm.

## Nhóm khó — ưu tiên thực hiện

| ID | Công việc | Trạng thái / điều kiện đạt |
|---|---|---|
| H1 / P2.a | Hoàn thiện Desktop Input Actions: vòng đời, sensitivity, UI scroll, crouch, Escape, cô lập XR | **Đã verify**: EditMode 49/49, PlayMode 6/6; hardware XR vẫn Not tested |
| H2 / P2.b–d | Direct/ray hai tay, grab/transfer, physics, button debounce, recovery | Đã có chuẩn độ vật lý, Desktop click cầm/thả/F/R, Trigger giả lập mở nắp, mất tracking, chống xuyên bàn. Hồi quy PlayMode 19/19 ngày 10/09; thao tác chuyển tay và hardware vẫn cần kiểm tra riêng |
| H3 / P2.c | Teleport/snap mặc định, continuous tùy chọn, standing/seated, floor/bounds | Đã thêm vùng sàn an toàn, kiểm tra khoảng trống tránh bàn/tường, nối layer Teleport và snap 30°; cả chọn/thả tia qua interactable lẫn snap input đạt test. Đã sửa đường kẻ sàn chặn tia. Còn continuous/vignette tùy chọn, seated/calibration, đầy đủ tracking-origin và hardware comfort |
| H4 / P3 | Font SDF tiếng Việt + UI Desktop/VR, ray/direct, layout/contrast | UI Desktop đã tăng cỡ chữ/contrast, hover, progress, status, sửa cuộn/chồng nội dung và HUD vật đang cầm; ảnh Windows 1280×720 đã xem. Còn thay TextMesh bằng SDF tiếng Việt, glyph audit, world-space settings và kiểm tra VR |
| H5 / P4 | Glass/liquid/material/lighting, quyết định renderer, chất lượng hình và hiệu năng | Chưa đạt; cần audit và ảnh so sánh; Built-in hiện tại |
| H6 / P5 | Domain hóa học ba thí nghiệm, wrong-order, units, reset và E2E chung Desktop/XR | Chưa đạt; cần test domain và flow tích hợp |
| H7 / P6 | Audio pooling, haptic capability, accessibility persistence và recovery checkpoint | Chưa đạt; haptic thật cần thiết bị |
| H8 / P7 | Performance player, full regression/simulator, Windows build cuối và visual acceptance | Chưa đạt; cần đo trên build nguồn cuối |

## Nhóm dễ — tách riêng, làm khi cần cho gate

- E1: cập nhật trạng thái, chỉ mục bằng chứng, hướng dẫn điều khiển và báo cáo.
- E2: chuẩn hóa tên asset, tooltip, nhãn, nội dung hướng dẫn và đơn vị đã được domain xác nhận.
- E3: gom cấu hình mặc định/quality preset sau khi H3/H5/H8 chốt giá trị đo được.
- E4: đóng gói artifact, kiểm tra đường dẫn/build scene, tổng hợp warning và checklist phần cứng.

Không coi việc phân loại là hoàn thành implementation. Test phần cứng chưa chạy luôn giữ `Not tested — hardware required`.

## Cập nhật nhánh Git — thiết lập và phản hồi

- H7: đã có lưu/khôi phục thiết lập cục bộ, kiểm tra dữ liệu lỗi, âm lượng master và pool âm thanh nút bấm tối đa hai nguồn. Haptic và checkpoint tiến trình bền vững vẫn mở.
- H8/E3: bốn preset áp dụng MSAA, bóng, khoảng cách bóng và số đèn; mặc định Cân bằng. Chưa có benchmark FPS/CPU/GPU trên máy demo hoặc kính, không đánh dấu toàn bộ H8 hoàn tất.
- H4: có bảng thiết lập Desktop, cô lập chuột trong cùng cửa sổ dashboard; world-space settings/SDF/VR vẫn mở.
- Kiểm thử: 65/65 EditMode, 22/22 PlayMode. Chi tiết bàn giao Git: `GIT_RELEASE_20260910.md`.
