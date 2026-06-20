Khi bạn đã hoàn thành đầy đủ bộ tài liệu phân tích nghiệp vụ cốt lõi bao gồm: **User Story (US)**, **Activity Diagram (Luồng công việc)**, **Sequence Diagram (Luồng tuần tự khớp trạng thái Hybrid/Pending)** và **Use Case Diagram (UC)**, hệ thống của bạn đã có một nền tảng logic cực kỳ vững chắc.

Để chuyển giao từ giai đoạn **Phân tích thiết kế (BA/System Analyst)** sang giai đoạn **Phát triển phần mềm (Developer/DevOps/Tester)** một cách mượt mà, bạn cần triển khai tiếp các tài liệu và cấu phần kỹ thuật dưới đây, chia theo từng mục tiêu cụ thể:

---

### 1. Thiết kế Mô hình Dữ liệu (Data Architecture & Model)

Vì hệ thống của bạn sử dụng mô hình lưu trữ lai **Hybrid Storage** (CSDL truyền thống + Blockchain), đây là phần tối quan trọng để định hình cách lưu trữ dữ liệu:

* **Database Schema Design (ERD - Entity Relationship Diagram):** Thiết kế các bảng trong CSDL truyền thống (SQL/NoSQL). Bảng này phải thể hiện rõ các trường trạng thái trung gian (`Status` kiểu Enum: `Pending_Confirmation`, `Confirmed`, `Pending_Revocation`, `Revoked`, `Pending_Freeze`, `Frozen`...) và các trường lưu vết chuỗi như `TxHash`, `BlockNumber`, `DataHash`, `Salt`.
* **Smart Contract State Design:** Xác định cấu trúc dữ liệu (`struct`, `mapping`) sẽ được lưu trực tiếp trên Blockchain (ví dụ: Mapping từ `DegreeID` sang chuỗi `DataHash` bất biến, bảng lưu lịch sử `ReputationScore`).

### 2. Thiết kế Kiến trúc Kỹ thuật (Technical & System Architecture)

Tài liệu này giúp lập trình viên hiểu được cách các thành phần giao tiếp với nhau (đặc biệt là luồng bất đồng bộ xử lý qua Queue):

* **System Architecture Diagram:** Sơ đồ tổng thể thể hiện mối quan hệ giữa Frontend, Web API Server, Message Queue (RabbitMQ), Background Worker, CSDL quan hệ và các Node Blockchain (Hyperledger Besu).
* **API Specification (Tài liệu đặc tả API):** Thiết kế chi tiết các Endpoint. Do hệ thống có nhiều tác vụ bất đồng bộ, tài liệu API cần định nghĩa rõ:
* Mã phản hồi **HTTP 202 Accepted** cho các tác vụ `Pending` (US-1, US-2, US-5).
* Mã phản hồi **HTTP 201 Created** cho luồng lưu trực tiếp hoặc nộp cưỡng bức (US-6, US-7).
* Mã phản hồi **HTTP 422 Unprocessable Entity** khi phát hiện mã băm không khớp/gian lận (US-3, US-7).
* Cấu trúc Request/Response Payload (JSON hoặc Multipart FormData cho US-4).



### 3. Đặc tả Logic Nghiệp vụ Chi tiết (Detail Business Logic & Rules)

Dù User Story đã có AC (Acceptance Criteria), Dev vẫn cần các thuật toán hoặc công thức tường minh:

* **Thuật toán Mã hóa và Kiểm tra chéo (Hybrid Verification Algorithm):** Quy định thuật toán băm cụ thể (SHA-256), cách thức cộng chuỗi `PlainData + Salt` trước khi băm ở US-1 và US-3.
* **Thuật toán Tính điểm uy tín (Reputation Engine Rules - US-5):** Định nghĩa rõ các biến số `MinorPoints` (trừ bao nhiêu điểm cho lỗi hành chính), `MajorPoints` (trừ bao nhiêu điểm cho gian lận/cấp bằng khống) và cơ chế kích hoạt tự động từ US-2/US-4.
* **Thuật toán Xếp hạng Bài đăng (Ranking Algorithm - US-7):** Công thức toán học toán cụ thể để tính trọng số hiển thị `Top Featured Jobs` dựa trên `Reputation Score` của trường liên kết.

### 4. Thiết kế Chi tiết Giao diện (UI/UX Mockup & Wireframe)

Chuyển các mô tả "UX/UI Mockup Flow" trong tài liệu của bạn thành giao diện trực quan:

* **Wireframes / Figma Design:** Thiết kế chi tiết từng màn hình:
* Màn hình quản lý văn bằng với các nhãn trạng thái trực quan bằng màu sắc (🔴 Đỏ, 🟡 Vàng, 🟢 Xanh).
* Popup Modal ⚠️ cảnh báo màu vàng khi ứng tuyển thiếu bằng cấp (US-7).
* Biểu đồ đường (Line Chart) thể hiện biến động điểm uy tín trên Dashboard (US-5).



### 5. Kế hoạch và Kịch bản Kiểm thử (Test Plan & Test Cases)

Dựa trên bộ điều kiện nghiệm thu AC rất chặt chẽ của bạn, QA/Tester cần cụ thể hóa thành các kịch bản test:

* **Happy Path Test Cases:** Test luồng chạy chuẩn (Cấp bằng thành công -> Lên chuỗi -> Đổi sang Confirmed).
* **Edge Case & Exception Test Cases:** Test các luồng rẽ nhánh, ví dụ:
* Khi mạng lưới Blockchain gặp sự cố hoặc từ chối giao dịch, hệ thống có thực hiện Rollback trạng thái từ `Pending_...` về trạng thái cũ thành công không?
* Khi cố tình sửa đổi database truyền thống (giả lập tấn công), tính năng tra cứu US-3 có bật khung cam nhấp nháy báo động không?
* Kiểm tra phân quyền (Registrar, Recruiter, Student) xem hệ thống có chặn đúng như AC quy định không.



### 6. Cấu hình Hạ tầng và Triển khai (DevOps & Deployment Setup)

* **Docker Compose / Kubernetes Config:** Cấu hình môi trường chạy cho Web API, Database, RabbitMQ và đặc biệt là script khởi tạo mạng lưới Blockchain cục bộ (ví dụ: các file cấu hình Genesis, Validator Node cho Hyperledger Besu mà nhóm bạn đang nghiên cứu).

---

### Tóm tắt các bước bạn nên làm ngay tiếp theo:

1. **Vẽ sơ đồ ERD** để định hình các bảng dữ liệu nền tảng.
2. **Viết tài liệu API Specification (Swagger/Postman)** dựa theo các luồng Sequence thiết lập trạng thái `Pending`.
3. **Thiết kế giao diện trên Figma** bám sát các cảnh báo UI (Toast, Popup, Màu sắc nhãn trạng thái) để làm việc với Frontend Dev.