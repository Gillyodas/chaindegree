# Kế hoạch Triển khai Phase 3: Key Management & Security (Design Only)

Tài liệu này đặc tả chi tiết kế hoạch triển khai cho **Phase 3: Key Management & Security** của hệ thống ChainDegree. Giai đoạn này tập trung hoàn toàn vào việc thiết kế và tài liệu hóa (Design Only) để chuẩn bị sẵn kiến trúc bảo mật cho việc scale lên môi trường Cloud/Enterprise. 

Tuân thủ nguyên tắc cốt lõi: **KHÔNG implement code lúc này** để tránh over-engineering; hệ thống giữ nguyên `LocalEnvSigner` trong codebase hiện tại cho đến khi thực sự cần triển khai lên Cloud.

---

## 1. Danh sách các Work Package (Work Packages)

### WP 3.1: KMS Signer Architecture Design
*   **Mục tiêu**: Thiết kế và tài liệu hóa kiến trúc nâng cấp abstraction `IBlockchainSigner` để tích hợp với các hệ thống quản lý khóa tập trung (KMS) như Azure Key Vault hoặc HashiCorp Vault.
*   **Ràng buộc (Constraints)**: 
    *   Chỉ dừng ở mức độ tài liệu, biểu đồ kiến trúc (Mermaid).
    *   Giữ sự độc lập của layer Application theo nguyên tắc Clean Architecture. `IBlockchainSigner` phải nằm ở Application hoặc Domain, implementation KMS nằm ở Infrastructure.
*   **Các quyết định kỹ thuật / nghiệp vụ**:
    *   **Lựa chọn mô hình tích hợp**: Quyết định thiết kế theo hướng External Signer, trong đó Nethereum gọi API của KMS (như Azure Key Vault API) để thực hiện ký transaction thay vì sử dụng Web3Signer độc lập. Điều này giúp dễ quản lý và tích hợp trực tiếp vào .NET Backend.
    *   **Xác thực với KMS**: Backend sẽ giao tiếp với KMS thông qua Cloud IAM (VD: Managed Identity trên Azure) hoặc Role-Based Access Control (RBAC), không dùng secret/password tĩnh để tránh lộ lọt cấu hình.
*   **Kế hoạch kiểm thử Unit Test (Dự kiến khi implement sau này)**:
    *   *KMS API Mocking*: Đảm bảo implementation mới có thể mock được KMS client để test luồng ký mà không cần kết nối internet.
    *   *Error Handling Test*: Viết test cho các trường hợp KMS bị timeout, trả về lỗi 503, hoặc unauthorized để đảm bảo Backend áp dụng Retry Policy đúng đắn (Result Pattern).
*   **Done Criteria**: Có tài liệu mô tả chi tiết kiến trúc KMS Signer và biểu đồ tương tác sequence diagram.

### WP 3.2: Key Lifecycle Management Procedure
*   **Mục tiêu**: Tài liệu hóa quy trình chuẩn (SOP - Standard Operating Procedure) cho việc quản lý vòng đời của private key blockchain.
*   **Ràng buộc (Constraints)**:
    *   Quy trình hướng tới vận hành thủ công (Manual) kết hợp với các tool có sẵn của Cloud. Không viết CLI automation scripts hay custom tool.
*   **Các quyết định kỹ thuật / nghiệp vụ**:
    *   **Generate (Tạo khóa)**: Khóa phải được sinh trực tiếp bên trong Hardware Security Module (HSM) của KMS và thiết lập thuộc tính *non-exportable* (không thể trích xuất ra ngoài).
    *   **Activate (Kích hoạt)**: Đăng ký địa chỉ public address tương ứng vào whitelist của smart contract hoặc hệ thống backend.
    *   **Rotate (Luân chuyển)**: Đặc tả quy trình chuyển đổi key định kỳ hoặc khẩn cấp: (1) Generate key mới -> (2) Activate key mới -> (3) Đổi cấu hình backend dùng key mới -> (4) Disable key cũ.
    *   **Disable/Destroy (Thu hồi/Hủy)**: Cập nhật state trên contract để gỡ quyền của key cũ, soft-delete trên KMS.
*   **Kế hoạch kiểm thử (Tabletop Exercise)**:
    *   Kiểm thử bằng hình thức mô phỏng trên giấy (Tabletop): Đội Security và DevOps cùng duyệt qua quy trình giả lập sự cố lộ key và xác minh quy trình Rotate & Disable đáp ứng đủ tiêu chuẩn (không downtime, không lộ key mới).
*   **Done Criteria**: Tài liệu hóa chi tiết các bước quản lý vòng đời khóa dưới dạng Markdown (SOP).

### WP 3.3: Integration & Security Review
*   **Mục tiêu**: Đánh giá tính nhất quán giữa kiến trúc KMS và quy trình vòng đời khóa, đảm bảo chúng tích hợp trơn tru với topology Phase 2 hiện tại.
*   **Kế hoạch kiểm thử tích hợp (Mô phỏng)**:
    *   **Security Threat Modeling**: Mô phỏng kịch bản attacker chiếm quyền đọc Database (SQL Server). Kết quả kỳ vọng: Attacker không thể thực hiện giao dịch blockchain giả mạo vì Private Key nằm trong KMS và chỉ Application được cấp quyền IAM mới gọi được KMS.
    *   **Latency Impact Analysis**: Đánh giá lý thuyết về độ trễ (latency) khi gọi KMS API trước mỗi giao dịch. Cập nhật `BatchingDegreeWorker` timeout config document nếu cần thiết để bù đắp cho độ trễ này.
*   **Done Criteria**: Hoàn thành đánh giá bảo mật (Security Review) và tích hợp lý thuyết. Tài liệu được phê duyệt bởi Tech Lead/Architect.

---

## 2. Cấu trúc thư mục bàn giao (Deliverables Structure)

Khi hoàn thành Phase 3, thư mục tài liệu sẽ được bổ sung các cấu trúc sau:

```
docs/blockchain/
└── security/
    ├── kms-signer-architecture.md  # Tài liệu kiến trúc và biểu đồ (Mermaid) tích hợp KMS
    └── key-lifecycle-procedure.md  # Tài liệu quy trình (SOP) vận hành vòng đời khóa
```

## 3. Mục tiêu chung & Expected Outcomes
*   **Mục tiêu chung**: Cung cấp bức tranh toàn cảnh rõ ràng và kế hoạch bảo mật sẵn sàng cho Enterprise/Cloud deployment mà không làm chậm tiến độ phát triển hệ thống (KISS/MVP).
*   **Expected Outcomes**:
    *   Team phát triển nắm rõ giới hạn của `LocalEnvSigner` hiện tại.
    *   Có bản thiết kế chuẩn xác định hướng cho việc nâng cấp ở Phase sau hoặc khi có yêu cầu security audit.
    *   Mọi thay đổi bảo mật trong tương lai đã được lập kế hoạch và có hướng dẫn tích hợp chi tiết.
