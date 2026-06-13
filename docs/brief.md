# Project Brief: Ecosystem & Blockchain Network

## 1. Actors & Roles
* **Cơ sở đào tạo (CSDT):** * Bên cấp phát dữ liệu. Sử dụng tài khoản quyền `Registrar` để nhập/tải văn bằng lên hệ thống.
    * Có quyền **Cập nhật** hoặc **Thu hồi** văn bằng đã cấp kèm theo lý do cụ thể.
    * Sở hữu thuộc tính **Điểm uy tín (Reputation Score)** - biến động dựa trên trách nhiệm quản lý dữ liệu.
* **Validator (Bộ GD&ĐT / Đại học lớn):** Vận hành các Node thuộc mạng lưới Blockchain nội bộ để tham gia đồng thuận, đóng block và bảo vệ tính toàn vẹn dữ liệu.
* **Sinh viên:** * Người thụ hưởng. Tra cứu, lưu trữ và chia sẻ văn bằng đã được xác thực để ứng tuyển việc làm.
    * Có quyền **Gửi báo cáo (Report/Khiếu nại)** các vấn đề sai sót của văn bằng bản thân lên CSDT.
* **Nhà tuyển dụng (Recruiter):** * Tra cứu và xác thực tính chính danh của văn bằng.
    * Báo cáo các trường hợp CSDT cấp phát bằng sai sót hoặc gian lận $\rightarrow$ Hệ thống tự động xử phạt và trừ Điểm uy tín của CSDT.
    * Đăng bài tuyển dụng (Job Postings) có cấu hình bộ lọc loại bằng cấp bắt buộc.
    * Ưu tiên hiển thị bài đăng tuyển dụng dựa trên Điểm uy tín của các CSDT liên kết.

---

## 2. Cơ chế biến động Điểm uy tín (Reputation Engine)
Điểm uy tín của CSDT được tính toán tự động dựa trên **Nguyên nhân gốc rễ (Root Cause)** của việc thay đổi trạng thái văn bằng:

* **Trường hợp lỗi từ phía CSDT (Bị trừ điểm):**
    * Nhà tuyển dụng báo cáo bằng cấp sai/giả mạo và được xác thực đúng.
    * Sinh viên báo cáo lỗi sai thông tin do CSDT nhập liệu.
    * CSDT chủ động cập nhật/thu hồi bằng cấp với lý do: *Sai sót hành chính, lỗi hệ thống, nhầm lẫn dữ liệu...*
    * **Hệ quả:** Điểm uy tín bị giảm (mức độ giảm tùy thuộc vào trọng số của lý do). Điểm thấp sẽ giảm hiển thị bài đăng tuyển dụng của các đối tác liên kết với trường.
* **Trường hợp lỗi từ phía Sinh viên (CSDT KHÔNG bị trừ điểm):**
    * CSDT chủ động thu hồi bằng cấp với lý do xuất phát từ sinh viên: *Phát hiện gian lận thi cử, đạo văn, vi phạm kỷ luật nghiêm trọng bị tước bằng...*
    * **Hệ quả:** Bằng cấp bị vô hiệu hóa nhưng điểm uy tín của CSDT được giữ nguyên để đảm bảo tính công bằng.

---

## 3. Blockchain Network Architecture

### Core Specifications
* **Network Type:** Private / Consortium Blockchain (Mạng liên minh nội bộ).
* **Consensus Mechanism:** Proof of Authority (PoA) hoặc IBFT 2.0 / QBFT (Tối ưu cho Hyperledger Besu).

### Key Characteristics
* **High Speed:** Tốc độ tạo khối nhanh (độ trễ vài giây).
* **Zero Gas Fee:** Không tốn chi phí giao dịch thực tế (Sử dụng cơ chế cấp phát Gas nội bộ).
* **Immediate Finality:** Khối sau khi ghi vào chuỗi được xác định là duy nhất, không thể đảo ngược hoặc xảy ra hiện tượng rẽ nhánh (fork).

---

## 4. Data Storage Strategy (Hybrid Approach)

### Centralized DB
* Lưu trữ thông tin chi tiết, toàn vẹn của văn bằng, chứng chỉ.
* Khi Cập nhật/Thu hồi: Chỉ thực hiện **Cập nhật trạng thái (State/Status Update)** trên bản ghi hiện tại (ví dụ: chuyển từ `Confirmed` sang `Revoked`).
* Lưu trữ thông tin bài đăng tuyển dụng, bộ lọc bằng cấp, báo cáo lỗi từ Sinh viên/Nhà tuyển dụng.
* Lưu trữ giá trị Điểm uy tín hiện tại của CSDT để tính toán thuật toán ưu tiên hiển thị bài đăng (Ranking).

### Consortium Blockchain
* Lưu trữ bằng chứng xác thực (Proof of Verification) phục vụ đối chiếu công khai và chống giả mạo.
* Khi Cập nhật/Thu hồi: Do đặc tính bất biến (Immutability), hệ thống sẽ **Tạo một Transaction mới (Mã Tx mới)** đại diện cho trạng thái mới nhất của bằng cấp đó (Append-only). Khi tra cứu, hệ thống sẽ đọc Block mới nhất để lấy trạng thái hiện tại.
* Lưu trữ và đóng băng lịch sử thay đổi Điểm uy tín của các CSDT (State of Reputation) để đảm bảo tính minh bạch tuyệt đối, không thể can thiệp thao túng điểm số.