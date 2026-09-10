# Chemistry Lab — TDD evidence

## Nguồn yêu cầu

User journeys được rút ra từ kế hoạch đã duyệt trong phiên làm việc, không có tệp kế hoạch đầu vào:

- Học sinh thực hiện chuẩn độ theo đúng thứ tự an toàn và thao tác.
- Học sinh nhận biết điểm cuối, tránh quá chuẩn và ghi số liệu.
- Học sinh hoàn thành ba phép đo đồng quy và nhận kết quả nồng độ giấm.
- Cùng một bài học nhận lệnh từ XR, chuột, UI điện thoại hoặc bridge ESP32/BLE.

## RED

Các EditMode tests được tạo trước production code. Unity batch-mode không thể bắt đầu Test Runner vì máy không có entitlement `com.unity.editor.headless` (exit 198), nên RED compile được xác nhận bằng Roslyn và NUnit đi kèm Unity:

```text
CS0246: TitrationExperiment could not be found
CS0246: TitrationEndpointState could not be found
CS0103: TitrationEndpointState does not exist
CSC_EXIT=1
```

Lỗi chỉ ra đúng API Chemistry Lab chưa tồn tại, không phải lỗi cú pháp hoặc dependency ngoài phạm vi.

## GREEN

Sau khi thêm lõi C# thuần và mở rộng workflow, cùng bộ tests được biên dịch và chạy bằng harness phản chiếu NUnit:

```text
TOTAL=23 PASSED=23 FAILED=0
```

Unity Editor 6000.5.6f1 sau đó import project và biên dịch thành công các assembly:

```text
VLAB.ChemistryLab.dll
VLAB.ChemistryLab.Tests.EditMode.dll
Assembly-CSharp.dll
Assembly-CSharp-Editor.dll
Tundra build success
```

Builder đã chạy trong Unity GUI và ghi:

```text
[VLAB] ChemistryLab created and saved at Assets/ChemistryLab.unity
```

## Test specification

| Nhóm đảm bảo | Test/kiểm tra | Loại | Kết quả |
|---|---|---|---|
| Pha loãng giấm 10× và NaOH 0,1000 M cho nồng độ đúng | `ChemistryCalculationTests` | Unit | PASS |
| Quy đổi mol/L sang `% m/V` bằng 60,052 g/mol | `ChemistryCalculationTests` | Unit | PASS |
| Endpoint, trước endpoint và overshoot được phân loại | `ChemistryCalculationTests` | Unit | PASS |
| Ba titre trong 0,10 mL được coi là đồng quy | `ChemistryCalculationTests` | Unit | PASS |
| Thao tác sai thứ tự không làm tiến trạng thái | `TitrationExperimentTests` | Unit | PASS |
| Liều NaOH cộng dồn và cập nhật endpoint | `TitrationExperimentTests` | Unit | PASS |
| Reset lượt đo giữ kết quả trước; Clear xóa toàn bộ | `TitrationExperimentTests` | Unit | PASS |
| Ba kết quả hợp lệ tạo trung bình, mol/L và `% m/V` | `TitrationExperimentTests` | Unit | PASS |
| Runtime, test, navigation và editor builder biên dịch trong Unity | Unity GUI import/compile | Integration | PASS |
| Scene được sinh, có runtime components và nằm trong Build Settings | `ChemistryLabBuilder.BuildChemistryLabBatch` | Integration | PASS |

## Coverage và khoảng trống

Không có báo cáo phần trăm coverage vì Unity Test Runner batch-mode bị giới hạn license; không suy diễn một con số chưa đo. 23 test bao phủ toàn bộ nhánh chính của lõi tính toán/state machine. XR headset, Android và ESP32/BLE cần kiểm thử thủ công trên thiết bị thật; bridge hiện chỉ là contract trung lập, không phải plugin Bluetooth hoàn chỉnh.

Không tạo checkpoint commit vì người dùng yêu cầu rõ ràng chỉ lưu trên máy, chưa commit hoặc push Git.

