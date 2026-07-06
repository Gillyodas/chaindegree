# 📝 ĐẶC TẢ CHI TIẾT ENDPOINT API (API SPECIFICATION)

Hệ thống `ChainDegree` tuân thủ nguyên tắc thiết kế **RESTful API**, tối ưu hóa trải nghiệm người dùng thông qua cơ chế xử lý bất đồng bộ kết hợp hàng đợi thông điệp (RabbitMQ), đảm bảo tháo khớp các Module nghiệp vụ độc lập.

## 1. DANH SÁCH MÃ PHẢN HỒI HTTP TIÊU CHUẨN (HTTP STATUS CODES)

* **`201 Created`**: Dữ liệu nghiệp vụ hợp lệ và được ghi nhận thành công, có hiệu lực tức thời trong hệ thống truyền thống (Áp dụng cho luồng lưu trực tiếp hoặc nộp cưỡng bức tại US-6, US-7).
* **`202 Accepted`**: Yêu cầu đã được thẩm định cú pháp hợp lệ và được đẩy vào hàng đợi xử lý ngầm (Mempool ứng dụng). Hệ thống trả về mã này kèm `batchId` để giải phóng UI ngay lập tức (Áp dụng cho US-1, US-2, US-5).
* **`422 Unprocessable Entity`**: Dữ liệu gửi lên đúng định dạng (Valid Syntax) nhưng vi phạm nghiêm trọng quy tắc nghiệp vụ hoặc kiểm tra mật mã thất bại (Mã băm không khớp, phát hiện gian lận dữ liệu) (Áp dụng cho US-3, US-7).

---

## 2. CHI TIẾT CÁC ENDPOINT THEO USER STORY

### 🌐 MODULE: EDUCATION INSTITUTION CONTEXT (Quản lý Bằng cấp & CSDT)

#### 📌 [US-1] Cấp phát văn bằng (Cá nhân hoặc Gom lô bất đồng bộ)

* **HTTP Method & URL:** `POST /api/v1/institutions/degrees`
* **Mô tả:** Tiếp nhận danh sách văn bằng mới từ CSDT. Hệ thống kiểm tra trùng lặp (AC3), tự động tính toán mã Salt/Hash, lưu DB trạng thái `Pending_Confirmation` và trả về kết quả tiếp nhận ngay lập tức.
* **Headers:** 
  * `Authorization: Bearer JWT` (Yêu cầu Role: `Registrar`).
  * `Idempotency-Key: <unique_key>` (Bắt buộc để tránh trùng lặp khi retry request).
* **Content-Type:** `application/json`
* **Request Payload:**

```json
{
  "degrees": [
    {
      "studentId": "550e8400-e29b-41d4-a716-446655440000",
      "major": "Software Engineering",
      "classification": "Giỏi",
      "issuedAt": "2026-06-15T08:00:00Z"
    }
  ]
}
```

**Response Payload (202 Accepted):**

```json
{
  "message": "Degree issuance request processed successfully.",
  "acceptedCount": 1,
  "degreeIds": [
    "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"
  ],
  "failures": []
}
```

---

#### 📌 [US-2] Thu hồi văn bằng

* **HTTP Method & URL:** `POST /api/v1/institutions/degrees/{id}/revoke`
* **Mô tả:** Thu hồi một văn bằng cụ thể dựa trên `id` (UUID). Hệ thống tự động phân tích trạng thái gốc của bằng để áp dụng luồng xử lý nhanh (Shortcut - AC3) hoặc luồng xử lý chuỗi bất đồng bộ kết hợp tính điểm uy tín (AC4).
* **Authentication:** `Bearer JWT` (Yêu cầu Role: `Registrar`).
* **Content-Type:** `application/json`

**Request Payload:**

```json
{
  "revocationReasonEnum": "Administrative_Error",
  "specificComment": "Cán bộ giáo vụ nhập nhầm xếp loại tốt nghiệp của sinh viên từ Khá thành Giỏi."
}

```

**Kịch bản 1: Thu hồi văn bằng ĐÃ LÊN CHUỖI (Trạng thái gốc là `Confirmed`) $\rightarrow$ Phản hồi `202 Accepted**`

```json
{
  "message": "Revocation request accepted for an active on-chain degree. Initiating asynchronous blockchain invalidation transaction.",
  "degreeId": "550e8400-e29b-41d4-a716-446655440000",
  "currentStatus": "Pending_Revocation",
  "reputationImpact": "Pending_Calculation",
  "trackingUrl": "/api/v1/institutions/degrees/revocation/status/91cfa82d-1123-441d-bca2-88219c0182bb"
}

```

**Kịch bản 2: Thu hồi văn bằng CHƯA LÊN CHUỖI (Trạng thái gốc là `Pending_Confirmation`) $\rightarrow$ Phản hồi `200 OK` (Xử lý nhanh, miễn phạt điểm)**

```json
{
  "message": "Degree revoked instantly via application shortcut. Since the record was not yet anchored to the blockchain, the operation is finalized and the institution is fully exempted from reputation penalties.",
  "degreeId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "currentStatus": "Revoked",
  "reputationImpact": "Exempted_No_Penalty"
}

```

---

#### 📌 [US-2] Cập nhật thông tin văn bằng

* **HTTP Method & URL:** `PUT /api/v1/institutions/degrees/{id}`
* **Mô tả:** Chỉnh sửa thông tin sai sót của văn bằng. Nếu bằng chưa lên chuỗi thì ghi đè dữ liệu ngay, nếu bằng đã lên chuỗi thì kích hoạt luồng cập nhật bất đồng bộ và tạo giao dịch trạng thái mới trên Blockchain.
* **Authentication:** `Bearer JWT` (Yêu cầu Role: `Registrar`).
* **Content-Type:** `application/json`

**Request Payload:**

```json
{
  "updateReasonEnum": "Administrative_Error",
  "specificComment": "Sửa lại chính xác tên chuyên ngành đào tạo.",
  "updatedData": {
    "major": "Information Technology (Data Science Specialization)",
    "classification": "Giỏi"
  }
}

```

**Response Payload (202 Accepted):**

```json
{
  "message": "Degree update request submitted. Asynchronous ledger modification sequence has been queued.",
  "degreeId": "550e8400-e29b-41d4-a716-446655440000",
  "currentStatus": "Pending_Update"
}

```

---

#### 📌 [US-3] Xác thực & Đối chiếu văn bằng (Mật mã cục bộ & Blockchain)

* **HTTP Method & URL:** `POST /api/v1/institutions/degrees/verify`
* **Mô tả:** Tiếp nhận chuỗi JSON văn bằng gốc hoặc file thông tin từ bên ngoài. Hệ thống băm lại dữ liệu thực tế, đối chiếu với `data_hash_local` hoặc kiểm tra Merkle Proof trên Smart Contract mạng Hyperledger Besu.
* **Authentication:** Không yêu cầu (Public Endpoint).
* **Content-Type:** `application/json`

**Request Payload:**

```json
{
  "degreeCode": "DEG-2026-99102",
  "plainDataJson": "{\"degreeCode\":\"DEG-2026-99102\",\"studentCode\":\"STU-88291\",\"major\":\"Software Engineering\",\"classification\":\"Giỏi\"}",
  "salt": "a7d83bf92c81e3d0"
}

```

**Trường hợp 1: Xác thực hoàn toàn thành công (200 OK)**

```json
{
  "verified": true,
  "verificationSource": "Blockchain_Merkle_Root",
  "blockchainTxHash": "0x4f8e5bca12a83f92c81e3d0991a27b83c7d83bf92c81e3d066554400003a1c02",
  "blockNumber": 10432,
  "degreeDetails": {
    "institutionName": "Trường Đại học Công nghệ Thông tin",
    "major": "Software Engineering",
    "classification": "Giỏi",
    "status": "Confirmed"
  }
}

```

**Trường hợp 2: Phát hiện sửa đổi / Gian lận dữ liệu (422 Unprocessable Entity)**

```json
{
  "verified": false,
  "errorCode": "CRYPTO_HASH_MISMATCH",
  "message": "Verification failed. The calculated cryptographic hash does not match the local ledger or the anchored Merkle Root on the blockchain network."
}

```

---

#### 📌 [US-4] Gửi đơn khiếu nại / Báo cáo sai sót dữ liệu bằng cấp

* **HTTP Method & URL:** `POST /api/v1/institutions/degrees/reports`
* **Mô tả:** Sinh viên hoặc Doanh nghiệp gửi đơn phản ánh kèm theo bằng chứng vật lý dưới dạng File đính kèm.
* **Authentication:** `Bearer JWT` (Role: `Student` hoặc `Recruiter`).
* **Content-Type:** `multipart/form-data`

**Request Body (Multipart FormData):**

| Key | Type | Value | Description |
| --- | --- | --- | --- |
| `degreeId` | string (UUID) | `550e8400-e29b-41d4-a716-446655440000` | ID hệ thống của văn bằng bị khiếu nại |
| `reportType` | string | `Fraudulent_Data` | Phân loại báo cáo (`Administrative_Error` hoặc `Fraudulent_Data`) |
| `description` | string | "Phát hiện thông tin xếp loại trên văn bằng sai lệch với bảng điểm gốc." | Nội dung mô tả chi tiết |
| `evidenceFile` | File (Binary) | `transcript_evidence.pdf` | Tệp tin minh chứng đính kèm (PDF/PNG/JPG) |

**Response Payload (201 Created):**

```json
{
  "reportId": "88a7c211-12bc-4def-91a2-334455667788",
  "degreeId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Pending_Review",
  "evidenceUrl": "https://storage.chaindegree.io/evidences/88a7c211_evidence.pdf",
  "createdAt": "2026-06-17T06:30:00Z"
}

```

---

#### 📌 [US-5] Duyệt đơn khiếu nại & Kích hoạt luồng xử lý hệ thống

* **HTTP Method & URL:** `POST /api/v1/institutions/reports/{id}/approve`
* **Mô tả:** Admin phê duyệt đơn khiếu nại chính xác. Tác vụ đổi trạng thái đơn, hạ cấp trạng thái văn bằng, bắn `FraudulentDataDetectedEvent` sang RabbitMQ để kích hoạt Module Uy tín tính toán phạt điểm ngầm.
* **Authentication:** `Bearer JWT` (Yêu cầu Role: `Admin`).
* **Content-Type:** `application/json`

**Response Payload (202 Accepted):**

```json
{
  "message": "Report approved successfully. Asynchronous revocation and reputation penalty processes have been initiated.",
  "reportId": "88a7c211-12bc-4def-91a2-334455667788",
  "initiatedProcesses": [
    "DegreeRevocationChainTransaction",
    "ReputationScoreRecalculationEvent"
  ],
  "timestamp": "2026-06-17T06:35:00Z"
}

```

---

### 💼 MODULE: RECRUITMENT CONTEXT (Quản lý Tuyển dụng & Ứng tuyển)

#### 📌 [US-6] Đăng bài tuyển dụng kèm Bộ lọc tiêu chuẩn bằng cấp (Khớp tức thời)

* **HTTP Method & URL:** `POST /api/v1/recruitment/jobs`
* **Mô tả:** Doanh nghiệp đăng tin tuyển dụng mới và thiết lập cứng các điều kiện lọc văn bằng tự động tại Database truyền thống.
* **Authentication:** `Bearer JWT` (Yêu cầu Role: `Recruiter`).
* **Content-Type:** `application/json`

**Request Payload:**

```json
{
  "title": "Senior .NET Backend Engineer",
  "salaryMin": 30000000,
  "salaryMax": 50000000,
  "description": "We are looking for a C# expert with clean architecture skills...",
  "degreeFilters": [
    {
      "degreeType": "Ky_Su",
      "requiredMajor": "Information Technology",
      "minClassification": "Khá"
    },
    {
      "degreeType": "Cu_Nhan",
      "requiredMajor": "Software Engineering",
      "minClassification": "Giỏi"
    }
  ]
}

```

**Response Payload (201 Created):**

```json
{
  "jobId": "f29da229-3b1a-4c2d-9441-2a1c899d14a2",
  "status": "Active",
  "createdAt": "2026-06-17T06:40:00Z",
  "filtersAppliedCount": 2
}

```

---

#### 📌 [US-7] Nộp đơn ứng tuyển (Kiểm tra điều kiện & Cho phép nộp cưỡng bức)

* **HTTP Method & URL:** `POST /api/v1/recruitment/applications`
* **Mô tả:** Sinh viên đính kèm văn bằng để nộp đơn. Hệ thống tự động so khớp cấu trúc văn bằng với bộ lọc điều kiện của Job. Nếu vi phạm bộ lọc nhưng sinh viên chọn cờ `forceSubmit = true`, hệ thống vẫn nhận đơn và đánh dấu phân hạng thấp.
* **Authentication:** `Bearer JWT` (Yêu cầu Role: `Student`).
* **Content-Type:** `application/json`

**Kịch bản 1: Nộp đơn hợp lệ và đủ tiêu chuẩn (201 Created)**

* *Request Payload:*

```json
{
  "jobId": "f29da229-3b1a-4c2d-9441-2a1c899d14a2",
  "degreeId": "550e8400-e29b-41d4-a716-446655440000",
  "forceSubmit": false
}

```

* *Response Payload:*

```json
{
  "applicationId": "91cfa82d-1123-441d-bca2-88219c0182bb",
  "processStatus": "Submitted",
  "rankStatus": "Highly_Qualified",
  "message": "Application submitted successfully. Your degree satisfies all job filter prerequisites.",
  "createdAt": "2026-06-17T06:45:00Z"
}

```

**Kịch bản 2: Bằng cấp không đủ chuẩn - Bị từ chối tự động (422 Unprocessable Entity)**

* *Request Payload (với `forceSubmit: false`, hệ thống quét thấy xếp loại hoặc chuyên ngành không khớp):*

```json
{
  "errorCode": "FILTER_CRITERIA_NOT_SATISFIED",
  "message": "Your degree does not satisfy the recruiter's minimum classification or major criteria. Submission rejected.",
  "details": {
    "required": "Degree Classification: Giỏi",
    "provided": "Degree Classification: Khá"
  },
  "remediation": "You can still enforce submission if desired by setting 'forceSubmit' parameter to true."
}

```

**Kịch bản 3: Thực hiện nộp cưỡng bức thành công khi chấp nhận xếp hạng thấp (201 Created)**

* *Request Payload:*

```json
{
  "jobId": "f29da229-3b1a-4c2d-9441-2a1c899d14a2",
  "degreeId": "550e8400-e29b-41d4-a716-446655440000",
  "forceSubmit": true
}

```

* *Response Payload:*

```json
{
  "applicationId": "91cfa82d-1123-441d-bca2-88219c0182bb",
  "processStatus": "Submitted",
  "rankStatus": "Under_Qualified",
  "message": "Application forced successfully. Note that your status is flagged as Under_Qualified due to filter mismatches.",
  "createdAt": "2026-06-17T06:47:00Z"
}

```