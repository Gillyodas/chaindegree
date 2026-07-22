# Quy trình Quản lý Vòng đời Khóa Blockchain (Key Lifecycle Management SOP) & Security Review

Tài liệu này đặc tả Quy trình thao tác chuẩn (Standard Operating Procedure - SOP) quản lý vòng đời của Khóa riêng tư (Private Key) Blockchain trong hệ thống ChainDegree, đồng thời kết hợp kết quả Đánh giá Mô hình Đe dọa Bảo mật (Security Threat Modeling & Integration Review) thuộc Phase 3.

---

## Part I: Quy trình Vòng đời Khóa (Key Lifecycle SOP)

Vòng đời của Private Key được quản lý nghiêm ngặt qua 5 giai đoạn:

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  1. Generate │ ──> │  2. Activate │ ──> │  3. Rotation │ ──> │  4. Disable  │ ──> │ 5. Shredding │
│ (HSM/KMS)    │     │ & Whitelist  │     │ (Routine/Emer)     │ (Revoke Lock)│     │(Crypto Shred)│
└──────────────┘     └──────────────┘     └──────────────┘     └──────────────┘     └──────────────┘
```

### 1. Giai đoạn 1: Khởi tạo Khóa (Key Generation)
*   **Môi trường thực hiện**: Trực tiếp bên trong Phần cứng Bảo mật (HSM) của Cloud KMS (Azure Key Vault Premium / HashiCorp Vault HSM module).
*   **Thuật toán khóa**: Elliptic Curve Cryptography (`secp256k1` / `ECDSA`).
*   **Cấu hình bắt buộc**:
    *   `Exportable`: `False` (Tuyệt đối không cho phép xuất raw private key ra khỏi KMS dưới bất kỳ hình thức nào).
    *   `Key Operations`: Chỉ bật `Sign`, `Verify`. Tắt `Encrypt`, `Decrypt`, `WrapKey`, `UnwrapKey`.
*   **Người thực hiện**: Security Admin (Yêu cầu Dual-Control / 4-Eyes Principle nếu thực hiện trên Production).

### 2. Giai đoạn 2: Kích hoạt & Whitelist (Activation & Whitelisting)
*   **Trích xuất Public Address**: Xuất địa chỉ ví công khai (`0x...`) từ Public Key tương ứng trong KMS.
*   **Đăng ký Cấu hình**:
    *   Cập nhật Key Identifier (Key URI / Key Name) vào hệ thống Quản lý Cấu hình Backend (Azure App Configuration / Secret Manager).
    *   Cấp quyền Cloud IAM cho Managed Identity của `ChainDegree.API` / `Worker` được truy cập Key Identifier mới.
*   **Phân quyền Blockchain (Nếu có)**:
    *   Nạp số dư ban đầu (nếu chạy Public/Testnet chain) hoặc Whitelist ví mới vào danh sách cho phép giao dịch Zero-Gas (trên mạng Besu Consortium).

### 3. Giai đoạn 3: Luân chuyển Khóa Định kỳ & Khẩn cấp (Key Rotation)

#### 3.1. Luân chuyển Định kỳ (Routine Rotation - 90/180 ngày)
Để đảm bảo nguyên tắc Zero-Downtime, quy trình áp dụng cơ chế **Dual-Key Window**:
1.  **Bước 1**: Tạo Key mới (`Key_V2`) trên KMS theo Giai đoạn 1.
2.  **Bước 2**: Đăng ký `Key_V2` vào cấu hình Backend song song với `Key_V1`.
3.  **Bước 3**: Chuyển giao diện `IBlockchainSigner` trỏ sang dùng `Key_V2` cho các giao dịch cấp bằng mới.
4.  **Bước 4**: Theo dõi `BatchingDegreeWorker` trong 24h để đảm bảo các giao dịch pending dùng `Key_V1` đã hoàn tất (Receipt Success).
5.  **Bước 5**: Chuyển sang Giai đoạn 4 để gỡ bỏ `Key_V1`.

#### 3.2. Luân chuyển Khẩn cấp (Emergency Rotation - Khi nghi ngờ sự cố)
1.  Kích hoạt quy trình ứng phó sự cố (Incident Response Plan).
2.  Ngừng tạm thời `BatchingDegreeWorker` thông qua Feature Flag / Administrative API (`PAUSE_WORKER`).
3.  Tạo và kích hoạt `Key_V_NEW` lập tức.
4.  Thu hồi ngay lập tức IAM access của `Key_V_OLD`.
5.  Khởi động lại Worker với cấu hình Key mới.

### 4. Giai đoạn 4: Vô hiệu hóa (Disable & Revocation)
*   Chuyển trạng thái khóa trên KMS sang `Disabled`.
*   Gỡ bỏ Managed Identity Role Assignment truy cập khóa đó.
*   Ghi nhận sự kiện vô hiệu hóa khóa vào Audit Log vận hành hệ thống.

### 5. Giai đoạn 5: Hủy Khóa (Decommissioning & Crypto-Shredding)
*   **Soft-Delete Period**: Khóa được giữ ở trạng thái Soft-Delete trong 90 ngày (cho phép khôi phục nếu có nhầm lẫn).
*   **Purge / Crypto-Shredding**: Sau thời hạn Soft-Delete, thực hiện lệnh `Purge` để xóa vĩnh viễn khóa khỏi HSM. Không thể khôi phục dữ liệu sau bước này.

---

## Part II: Kế hoạch Kiểm thử Mô phỏng (Tabletop Exercise Plan)

Vì Phase 3 tuân thủ nguyên tắc **Design Only**, kiểm thử được thực hiện dưới dạng buổi **Tabletop Exercise** giữa Đội Security, DevOps và Lead Developer:

*   **Kịch bản kiểm thử 1 (Giả lập rò rỉ cấu hình Backend)**:
    *   *Thao tác*: Giả định attacker lấy được toàn bộ file cấu hình `appsettings.json` và môi trường của Backend container.
    *   *Kết quả kỳ vọng*: Attacker **KHÔNG** tìm thấy Private Key (vì cấu hình chỉ chứa Key Vault URI). Attacker không thể ký transaction từ bên ngoài vì không có Token Managed Identity của Azure Node.
*   **Kịch bản kiểm thử 2 (Giả lập xoay khóa khẩn cấp)**:
    *   *Thao tác*: Mô phỏng từng bước xoay khóa khẩn cấp trên sơ đồ.
    *   *Kết quả kỳ vọng*: Thời gian gián đoạn giao dịch (Downtime) < 5 phút; Không có giao dịch bị treo vĩnh viễn hoặc sai lệch dữ liệu Merkle Root.

---

## Part III: Đánh giá Tích hợp & Mô hình Đe dọa Bảo mật (Security Review & Threat Modeling)

### 1. Mô hình Đe dọa (Threat Modeling Analysis)

| Đối tượng tấn công / Kịch bản | Mức độ rủi ro | Cơ chế bảo vệ của Hệ thống |
| :--- | :--- | :--- |
| **Attacker chiếm quyền SQL Server DB** | Cao | Attacker chỉ sửa được dữ liệu SQL offline. Không thể tạo giao dịch Blockchain hợp lệ do không nắm Private Key và không gọi được KMS. Sự bất nhất giữa SQL và On-chain State sẽ bị phát hiện khi verify. |
| **Attacker chiếm quyền Backend App Container** | Trung bình | Attacker có thể tạm thời gọi KMS qua Managed Identity để ký transaction rác. Tuy nhiên, KMS Audit Log sẽ ghi lại toàn bộ IP/Activity, và Security Admin có thể thu hồi IAM Role của Container chỉ trong vài giây. |
| **Tấn công Man-in-the-Middle (MitM) RPC** | Thấp | Kết nối giữa Backend và Besu RPC Node chạy trên Mạng nội bộ cô lập (Docker Network/VPC) với mTLS. RPC không expose ra Internet. |

### 2. Đánh giá Ảnh hưởng Độ trễ (Latency Impact Analysis)

Khi chuyển từ `LocalEnvSigner` sang `KmsSigner`:

*   **Độ trễ ký local**: ~1ms - 2ms.
*   **Độ trễ ký qua Cloud KMS (HTTPS API)**: ~40ms - 120ms (tùy thuộc vị trí địa lý của Data Center).

**Điều chỉnh Cấu hình Backend**:
*   `BatchingDegreeWorker`: Tăng `Timeout` cấu hình cho thao tác ký giao dịch từ 5 giây lên 10 giây.
*   Không ảnh hưởng tới tổng thể throughput cấp bằng vì hệ thống sử dụng cơ chế gom Batch (Merkle Tree): **N bằng cấp chỉ tốn đúng 1 thao tác ký giao dịch on-chain**.
