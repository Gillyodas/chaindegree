# Product Backlog: Toàn bộ User Stories hệ thống Xác thực Văn bằng

## Cấu trúc Bảng Nhật ký hành vi chung (Hệ thống dùng chung cho tất cả US)
Mọi hành vi thay đổi dữ liệu hoặc tác động hệ thống từ các Actor đều phải được ghi nhận tự động vào bảng trung tâm: `BehaviorLogs(Id, ActorId, ActionType, ImpactedEntityId, Description, Timestamp)`.

---

## Nhóm 1: Vòng đời Văn bằng & Xác thực (Core Value)

### User Story ID: US-1: Cấp và xác thực bằng cấp cho sinh viên
* **As a:** Người quản lý hồ sơ (Registrar) của Cơ sở đào tạo (CSDT).
* **I want to:** Thêm một hoặc nhiều bằng cấp cho sinh viên cùng một lúc.
* **So that:** Thông tin chi tiết bằng cấp được ghi nhận vào hệ thống để sinh viên có thể xem ngay, đồng thời hệ thống tự động đưa vào hàng đợi để tiến hành xác thực lên mạng lưới liên minh mà không làm gián đoạn công việc của tôi.

#### Điều kiện nghiệm thu (Acceptance Criteria - AC)
* **AC1 (Pre-condition):**
    * Chỉ người dùng có vai trò `Registrar` thuộc CSDT đó mới có quyền thực hiện.
    * Yêu cầu nhập đầy đủ các thông tin bắt buộc của bằng cấp.
    * Yêu cầu cung cấp thông tin định danh chính xác của sinh viên (Số CCCD/Định danh).
* **AC2 (Luồng thành công - Lưu trữ và Đẩy vào hàng đợi):**
    * Khi nhấn nút "Cấp bằng", hệ thống tự động tạo một mã `Salt` ngẫu nhiên cho từng văn bằng, tính toán mã băm dữ liệu `DataHash = Hash(PlainData + Salt)`.
    * Hệ thống lưu thông tin bằng cấp, `Salt`, và `DataHash` vào CSDL truyền thống với trạng thái ban đầu là `Pending_Confirmation`.
    * Hệ thống phản hồi ngay cho người dùng là đã tiếp nhận thành công và chuyển tác vụ đẩy `DataHash` lên mạng lưới xác thực ngầm (Background Queue).
* **AC3 (Luồng thất bại ngay tại bước kiểm tra dữ liệu):**
    * Nếu sinh viên đã sở hữu một bằng cấp cùng loại được cấp bởi chính CSDT hiện tại $\rightarrow$ Hệ thống chặn lại, không lưu vào CSDL và trả về lý do thất bại cụ thể cho bằng cấp đó.
* **AC4 (Post-condition - Xử lý ngầm hoàn tất):**
    * Sau khi tiến trình xử lý ngầm đẩy `DataHash` lên mạng lưới thành công: Trạng thái cập nhật từ `Pending_Confirmation` sang `Confirmed`. Đồng thời liên kết mã định danh giao dịch (`TxHash`) vào bản ghi dữ liệu.
    * Nếu xử lý ngầm thất bại: Trạng thái chuyển sang `Confirmation_Error` để người quản lý có thể nhấn nút "Thử lại" (Retry).
* **AC5 (Behavior Audit):**
    * Hệ thống tự động ghi nhận một bản ghi vào bảng nhật ký chung: `ActionType = "CREATE_DEGREE"`.

#### Giao diện sơ bộ (UX/UI Mockup Flow)
* Màn hình thêm bằng cấp hiển thị thông tin định danh sơ bộ của sinh viên (CCCD, MSSV, Họ tên...). Form nhập có nút icon dấu `[ + ]` để tạo thêm form mới nhập nhiều bằng cấp cùng lúc.
* Khi nhấn `[Cấp bằng]`, hiển thị một Toast Notification: *"Đã tiếp nhận thành công X bằng cấp. Hệ thống đang tiến hành xác thực ngầm."*, làm sạch các ô nhập liệu thành công.
* Giữ lại thông tin của các bằng cấp bị trùng (theo AC3) trên form kèm dòng thông báo lỗi màu đỏ.
* Màn hình danh sách cấp bằng có cột "Trạng thái xác thực" hiển thị các Badge màu sắc tương ứng: 🟡 `Pending_Confirmation`, 🟢 `Confirmed`, 🔴 `Confirmation_Error` (Có nút `[Thử lại]` bên cạnh).

---

### User Story ID: US-2: Cập nhật thông tin hoặc Thu hồi văn bằng đã cấp
* **As a:** Người quản lý hồ sơ (Registrar) của CSDT.
* **I want to:** Cập nhật lại thông tin sai sót hoặc Thu hồi hoàn toàn một văn bằng đã cấp trên hệ thống kèm theo lý do cụ thể.
* **So that:** Tôi có thể sửa chữa sai sót hành chính hoặc vô hiệu hóa các văn bằng không còn giá trị pháp lý, đảm bảo tính trung thực của dữ liệu.

#### Điều kiện nghiệm thu (Acceptance Criteria - AC)
* **AC1 (Pre-condition):**
    * Người dùng phải có quyền `Registrar` và văn bằng cần xử lý phải thuộc quyền quản lý của CSDT đó.
    * Văn bằng mục tiêu phải đang ở trạng thái `Confirmed` hoặc `Pending_Confirmation`.
    * Bắt buộc phải chọn/nhập **Lý do thay đổi** thuộc danh mục có sẵn.
* **AC2 (Luồng xử lý đối với Văn bằng đã ở trạng thái `Confirmed`):**
    * **Giai đoạn tạm thời:** Hệ thống lập tức chuyển trạng thái văn bằng trong CSDL truyền thống từ `Confirmed` sang `Pending_Revocation` (hoặc `Pending_Update`), giải phóng giao diện và đẩy lệnh vào hàng đợi xử lý ngầm.
    * **Trạng thái cuối:** Tiến trình chạy ngầm gửi một giao dịch mới đại diện cho trạng thái thay đổi của văn bằng lên mạng lưới xác thực bất biến. Khi mạng lưới xác nhận thành công, trạng thái trong CSDL truyền thống chính thức chuyển sang `Revoked` (Đã thu hồi) hoặc quay lại `Confirmed` (nếu là lệnh Cập nhật).
* **AC3 (Luồng xử lý nhanh đối với Văn bằng đang ở trạng thái `Pending_Confirmation`):**
    * Vì dữ liệu văn bằng chưa được xác thực chính thức trên chuỗi, hệ thống cho phép can thiệp xử lý nhanh (Shortcut):
        * **Nếu là lệnh Thu hồi:** Hệ thống cập nhật thẳng trạng thái văn bằng trong CSDL truyền thống thành `Revoked` và hủy tác vụ đẩy lên mạng lưới đang xếp hàng trong Queue (hoặc đẩy một lệnh hủy bỏ lên chuỗi nếu tác vụ cũ không thể rút lại).
        * **Nếu là lệnh Cập nhật:** Hệ thống cập nhật thông tin mới trực tiếp vào CSDL truyền thống và tính toán lại mã băm `DataHash` mới để tiến trình ngầm đẩy lên mạng lưới thay cho mã băm cũ.
* **AC4 (Tính toán uy tín ngầm - Phân định trách nhiệm):**
    * **Trường hợp miễn trừ đặc biệt:** Nếu văn bằng bị Thu hồi/Cập nhật khi đang ở trạng thái `Pending_Confirmation` $\rightarrow$ Hệ thống **giữ nguyên** Điểm uy tín (Reputation Score) của CSDT, không thực hiện phạt điểm với bất kỳ lý do nào.
    * **Trường hợp văn bằng đã ở trạng thái `Confirmed`:** Hệ thống tự động phân tích lý do: Nếu lỗi thuộc về CSDT (Sai sót hành chính, nhập nhầm), hệ thống kích hoạt luồng trừ điểm uy tín của trường. Nếu lý do xuất phát từ phía sinh viên (vi phạm kỷ luật, gian lận học tập), điểm uy tín của trường được giữ nguyên.
* **AC5 (Behavior Audit):**
    * Hệ thống tự động ghi một bản ghi vào nhật ký hành vi chung: `ActionType = "REVOKE_DEGREE"` hoặc `"UPDATE_DEGREE"` kèm lý do giải trình và ghi nhận trạng thái gốc của văn bằng lúc thực hiện thao tác.

#### Giao diện sơ bộ (UX/UI Mockup Flow)
* Tại màn hình chi tiết văn bằng đã cấp, hiển thị nút `[Cập nhật]` và `[Thu hồi bằng]` bất kể bằng đó đang ở trạng thái `Confirmed` hay `Pending_Confirmation`.
* **Phản hồi UI dựa theo trạng thái gốc:**
    * **Nếu bằng đang ở trạng thái `Pending_Confirmation`:** Khi bấm xác nhận Thu hồi, hệ thống hiển thị Toast: *"Đã thu hồi văn bằng thành công (Hệ thống miễn trừ đánh giá uy tín cho văn bằng chưa lên chuỗi)."*. Trạng thái chuyển lập tức sang màu đỏ hẳn 🔴 `Revoked`.
    * **Nếu bằng đang ở trạng thái `Confirmed`:** Khi nhấn xác nhận Thu hồi, màn hình trả về Toast: *"Yêu cầu thu hồi đã được tiếp nhận và đang đồng bộ ngầm."*. Trạng thái văn bằng chuyển sang màu vàng 🟡 `Pending_Revocation`, sau khi đồng bộ hoàn tất mới chuyển sang màu đỏ hẳn 🔴 `Revoked`.

---

### User Story ID: US-3: Tra cứu và đối chiếu xác thực văn bằng
* **As a:** Sinh viên (Người sở hữu bằng) hoặc Nhà tuyển dụng (Recruiter).
* **I want to:** Nhập mã định danh văn bằng hoặc quét mã QR trên bằng cấp để kiểm tra tính chính danh.
* **So that:** Tôi có thể biết được văn bằng này là thật hay giả, do trường nào cấp và trạng thái hiện tại của nó có hợp pháp hay không.

#### Điều kiện nghiệm thu (Acceptance Criteria - AC)
* **AC1 (Luồng tra cứu công khai):** Tính năng không yêu cầu đăng nhập đối với hành vi tra cứu cơ bản qua mã công khai hoặc mã QR.
* **AC2 (Cơ chế đối chiếu kép - Hybrid Verification):**
    * Hệ thống lấy dữ liệu chi tiết, `Salt` và `DataHash` từ CSDL truyền thống.
    * Hệ thống gọi lên mạng lưới xác thực bất biến bằng mã định danh, lấy về chuỗi Hash được lưu ở Block mới nhất.
    * Tiến hành so khớp: Tính toán lại `Hash(Dữ liệu từ DB + Salt từ DB)` rồi đối chiếu với chuỗi Hash lấy từ mạng lưới bất biến về.
* **AC3 (Kết quả xác thực):**
    * **Hợp lệ:** Nếu các chuỗi Hash trùng khớp hoàn toàn và trạng thái là `Confirmed` $\rightarrow$ Hiển thị trạng thái dữ liệu an toàn.
    * **Đã bị thu hồi:** Nếu kết quả đối chiếu khớp thông tin nhưng trạng thái cuối cùng được ghi nhận là `Revoked` $\rightarrow$ Hiển thị cảnh báo văn bằng đã bị cơ sở đào tạo thu hồi.
    * **Gian lận/Sai lệch:** Nếu chuỗi Hash tính toán từ dữ liệu CSDL tập trung không khớp với chuỗi Hash lưu trên mạng lưới bất biến $\rightarrow$ Hiển thị cảnh báo nghiêm trọng: Dữ liệu bị sai lệch, nghi vấn cơ sở dữ liệu nền tảng bị can thiệp trái phép.
* **AC4 (Behavior Audit):**
    * Hệ thống ghi nhận hành vi tra cứu ẩn danh hoặc định danh vào bảng nhật ký chung: `ActionType = "VERIFY_DEGREE"`.

#### Giao diện sơ bộ (UX/UI Mockup Flow)
* Một trang công khai `Verification Portal` chứa ô nhập mã số bằng. Trang kết quả trả về hiển thị đầy đủ thông tin bằng cấp kèm theo nhãn trạng thái trực quan: Khung viền xanh lá cây cho bằng hợp lệ, khung viền màu đỏ cho bằng đã bị `Revoked`, và khung viền màu cam nhấp nháy kèm cảnh báo nguy hiểm nếu dữ liệu bị sai lệch (Mất tính nhất quán dữ liệu).

---

## Nhóm 2: Cơ chế Điểm uy tín & Báo cáo (Reputation & Report)

### User Story ID: US-4: Gửi báo cáo sai sót hoặc gian lận văn bằng
* **As a:** Sinh viên hoặc Nhà tuyển dụng (Recruiter).
* **I want to:** Gửi một yêu cầu báo cáo (Report/Khiếu nại) đối với một văn bằng cụ thể trên hệ thống kèm theo minh chứng.
* **So that:** Phản ánh các sai sót về mặt thông tin (đối với sinh viên) hoặc tố cáo hành vi cấp bằng gian lận, không đúng thực tế của CSDT (đối với nhà tuyển dụng).

#### Điều kiện nghiệm thu (Acceptance Criteria - AC)
* **AC1 (Phân quyền báo cáo):**
    * Sinh viên chỉ được quyền báo cáo đối với các văn bằng thuộc sở hữu của chính mình.
    * Nhà tuyển dụng được quyền báo cáo bất kỳ văn bằng nào mà họ phát hiện nghi vấn trong quá trình tuyển dụng.
* **AC2 (Thông tin báo cáo):**
    * Yêu cầu bắt buộc: Chọn Loại báo cáo, nhập nội dung mô tả chi tiết và tải lên file minh chứng.
* **AC3 (Xử lý sau báo cáo):**
    * Hệ thống lưu đơn báo cáo vào CSDL truyền thống với trạng thái `Pending_Review`.
    * Gửi thông báo đến tài khoản quản trị của CSDT bị báo cáo và cơ quan thẩm quyền tối cao để yêu cầu xét duyệt đơn khiếu nại.
* **AC4 (Behavior Audit):**
    * Hệ thống ghi nhận vào nhật ký chung: `ActionType = "SUBMIT_REPORT"`, lưu kèm thông tin ID của văn bằng bị khiếu nại.

#### Giao diện sơ bộ (UX/UI Mockup Flow)
* Bên cạnh màn hình xem chi tiết văn bằng hiển thị nút `[Báo cáo sai sót/Gian lận]`. Click vào hiển thị Form gồm: Loại báo cáo (Dropdown), Nội dung khiếu nại (Textarea), và vùng kéo thả file đính kèm. Bấm gửi sẽ hiển thị Toast báo cáo thành công.

---

### User Story ID: US-5: Tự động cập nhật và đóng băng Điểm uy tín của CSDT
* **As a:** Hệ thống (System Engine).
* **I want to:** Tự động tính toán lại Điểm uy tín (Reputation Score) của CSDT dựa trên các sự kiện thu hồi bằng hoặc báo cáo vi phạm được phê duyệt, sau đó ghi nhận lịch sử điểm số.
* **So that:** Đảm bảo điểm uy tín của các trường luôn minh bạch, chính xác theo thời gian thực và không một cá nhân nào có thể can thiệp sửa đổi tùy tiện.

#### Điều kiện nghiệm thu (Acceptance Criteria - AC)
* **AC1 (Trigger tính điểm - Trừ điểm):** Hệ thống tự động kích hoạt hàm tính toán lại điểm ngay khi:
    * Đơn báo cáo lỗi sai thông tin của Sinh viên được phê duyệt là đúng $\rightarrow$ Trừ điểm uy tín của trường do lỗi nhập liệu hành chính.
    * Nhà tuyển dụng báo cáo bằng cấp sai thực tế/cấp khống và được xác minh đúng $\rightarrow$ Trừ điểm uy tín nặng của trường do gian lận dữ liệu.
    * CSDT chủ động thực hiện US-2 (Thu hồi bằng) với lý do từ phía nhà trường (Sai sót nội bộ).
* **AC2 (Quy tắc miễn trừ trách nhiệm):** Nếu CSDT thực hiện Thu hồi bằng với lý do lỗi xuất phát hoàn toàn từ phía Sinh viên (Phát hiện sinh viên gian lận thi cử, đạo văn, vi phạm kỷ luật nghiêm trọng) $\rightarrow$ Hệ thống thực hiện vô hiệu hóa bằng cấp nhưng **giữ nguyên** điểm uy tín của CSDT.
* **AC3 (Lưu trữ đồng bộ):**
    * Cập nhật điểm số mới nhất vào bảng CSDT để phục vụ các câu lệnh sắp xếp tốc độ cao.
    * Gửi một giao dịch bất đồng bộ đẩy lịch sử thay đổi điểm số lên mạng lưới lưu trữ bất biến nhằm đóng băng lịch sử biến động điểm số.
* **AC4 (Behavior Audit):**
    * Hệ thống tự động ghi nhận vào nhật ký chung: `ActionType = "REPUTATION_CHANGED"`, lưu rõ điểm cũ, điểm mới và lý do thay đổi.

#### Giao diện sơ bộ (UX/UI Mockup Flow)
* Tính năng chạy ngầm hệ thống. Trên giao diện Dashboard của CSDT và trang công khai sẽ hiển thị một biểu đồ đường (Line Chart) thể hiện lịch sử biến động điểm uy tín của trường qua các giai đoạn kèm danh sách các lý do bị tăng/trừ điểm công khai để đảm bảo tính minh bạch.

---

## Nhóm 3: Tính năng Nhà tuyển dụng (Recruitment Features)

### User Story ID: US-6: Đăng bài tuyển dụng có bộ lọc cấu hình bằng cấp
* **As a:** Nhà tuyển dụng (Recruiter).
* **I want to:** Tạo một bài đăng tuyển dụng mới và thiết lập bộ lọc điều kiện bằng cấp bắt buộc.
* **So that:** Tôi có thể phân loại hồ sơ và tối ưu hóa việc tìm kiếm các ứng viên phù hợp với tiêu chí công việc của công ty.

#### Điều kiện nghiệm thu (Acceptance Criteria - AC)
* **AC1 (Pre-condition):** Người dùng phải có quyền `Recruiter` đã được xác thực tài khoản doanh nghiệp.
* **AC2 (Cấu hình bộ lọc bằng cấp):** Khi tạo bài đăng, hệ thống cung cấp các tùy chọn bộ lọc bao gồm: Loại bằng, Chuyên ngành bắt buộc, Xếp loại tối thiểu.
* **AC3 (Luồng lưu trữ):** Bài đăng và cấu hình bộ lọc được lưu trữ hoàn toàn tại CSDL truyền thống.
* **AC4 (Behavior Audit):**
    * Hệ thống ghi nhận vào nhật ký chung: `ActionType = "POST_JOB"`.

#### Giao diện sơ bộ (UX/UI Mockup Flow)
* Màn hình "Đăng tin tuyển dụng": Ngoài các thông tin cơ bản (Tiêu đề, Mức lương), có thêm một mục mang tên **"Yêu cầu văn bằng hệ thống"**. Nhà tuyển dụng bấm nút `[+ Thêm điều kiện bằng]`, chọn các giá trị tương ứng từ danh mục hệ thống.

---

### User Story ID: US-7: Ứng tuyển linh hoạt và Ưu tiên hiển thị bài đăng theo Điểm uy tín
* **As a:** Sinh viên (Ứng viên) và Nhà tuyển dụng (Doanh nghiệp).
* **I want to:** 1. *(Sinh viên)* Sử dụng mã văn bằng để ứng tuyển vào các bài đăng, hệ thống vẫn cho phép nộp hồ sơ dù không đủ điều kiện văn bằng nhưng chấp nhận bị xếp hạng thấp hơn.
    2. *(Nhà tuyển dụng)* Bài đăng của mình được ưu tiên hiển thị dựa trên Điểm uy tín cao của nhà trường mà doanh nghiệp có liên kết hoặc tuyển dụng nhiều.
* **So that:** Sinh viên không bị tước bỏ cơ hội ứng tuyển nếu có năng lực khác bù đắp, đồng thời khuyến khích các Doanh nghiệp ưu tiên tuyển dụng sinh viên từ các trường có điểm uy tín cao.

#### Điều kiện nghiệm thu (Acceptance Criteria - AC)
* **AC1 (Luồng Ứng tuyển và Kiểm tra điều kiện):**
    * Khi sinh viên nhấn "Ứng tuyển", hệ thống đối chiếu kho bằng cấp của sinh viên trong CSDL với bộ lọc yêu cầu của bài đăng (US-6).
    * **Trường hợp Khớp hoàn toàn:** Hồ sơ được chuyển đến Nhà tuyển dụng với nhãn trạng thái `Highly_Qualified` (Ưu tiên cao).
    * **Trường hợp Không khớp hoặc Thiếu bằng yêu cầu:** Hệ thống hiển thị một cảnh báo màu vàng trên màn hình. Nếu sinh viên vẫn xác nhận nộp, hệ thống chấp nhận cho nộp đơn nhưng hồ sơ được lưu vào CSDL với nhãn trạng thái `Under_Qualified` (Xếp hạng thấp hơn trong danh sách quản lý của Nhà tuyển dụng).
    * **Trường hợp Gian lận:** Bằng cấp đính kèm đang ở trạng thái `Revoked` hoặc `Pending_Revocation` $\rightarrow$ Hệ thống từ chối thẳng luồng ứng tuyển.
* **AC2 (Thuật toán hiển thị ưu tiên - Ranking Algorithm):**
    * Khi Sinh viên tìm kiếm việc làm, các bài đăng tuyển dụng được sắp xếp thứ tự hiển thị dựa trên trọng số **Điểm uy tín (Reputation Score)** của các CSDT có liên kết đào tạo hoặc liên kết tuyển dụng với doanh nghiệp đó. Doanh nghiệp duy trì mối quan hệ với các trường có điểm uy tín cao sẽ được đẩy bài đăng lên đầu trang (`Top Featured Jobs`).
* **AC3 (Behavior Audit):**
    * Hệ thống ghi nhận hành vi ứng tuyển vào bảng nhật ký chung: `ActionType = "APPLY_JOB"`, ghi nhận rõ trạng thái hồ sơ lúc nộp là `Highly_Qualified` hay `Under_Qualified`.

#### Giao diện sơ bộ (UX/UI Mockup Flow)
* **Giao diện Sinh viên ứng tuyển:** Khi bấm ứng tuyển nhanh, nếu hệ thống quét thấy bằng cấp của ứng viên không khớp bộ lọc bài đăng, một Popup Modal cảnh báo màu vàng ⚠️ sẽ hiện lên: *"Bạn chưa có bằng cấp phù hợp với yêu cầu bắt buộc của nhà tuyển dụng. Bạn vẫn muốn nộp hồ sơ chứ? (Hồ sơ của bạn sẽ xếp ở nhóm ưu tiên thấp hơn)"*. Nếu bấm [Vẫn nộp], hệ thống xử lý thành công.
* **Giao diện Nhà tuyển dụng duyệt hồ sơ:** Danh sách ứng viên được chia làm 2 tab rõ rệt hoặc hiển thị từ trên xuống dưới theo thứ tự ưu tiên: Nhóm văn bằng đạt chuẩn xếp lên trên, nhóm văn bằng chưa đạt chuẩn (nhãn vàng) xếp xuống dưới.

---

Dưới đây là cẩm nang chi tiết phân tích tất cả các kịch bản Cơ sở đào tạo (CSDT) có thể bị báo cáo bởi Sinh viên và Nhà tuyển dụng (Recruiter), đi kèm với phương pháp phát hiện thực tế để bạn lưu lại làm tài liệu đặc tả nghiệp vụ cho dự án.

---

# CẨM NANG NGHIỆP VỤ: DANH SÁCH KỊCH BẢN BÁO CÁO VI PHẠM CỦA CSDT

*Tài liệu hỗ trợ xây dựng luồng logic cho US-4 và US-5 trong Hệ thống Xác thực Văn bằng Số*

---

## I. GÓC NHÌN TỪ SINH VIÊN (STUDENT)

*Sinh viên chỉ có quyền báo cáo đối với các văn bằng thuộc sở hữu của chính mình (AC1 - US-4). Mục tiêu của sinh viên là bảo vệ quyền lợi học tập cá nhân và tính chính xác của hồ sơ.*

### 1. Sai sót thông tin định danh cá nhân (Lỗi Hành chính)

* **Mô tả hành vi:** Văn bằng được cấp trên hệ thống chứa thông tin cá nhân không trùng khớp với giấy tờ pháp lý (CCCD/Hộ chiếu).
* **Cách sinh viên phát hiện:**
* **Phát hiện trực quan trên UI:** Khi sinh viên đăng nhập vào hệ thống, truy cập màn hình "Văn bằng của tôi", họ nhìn thấy họ tên bị sai chính tả (ví dụ: `Nguyễn Văn An` thành `Nguyễn Văn Anh`), sai ngày tháng năm sinh, hoặc sai số CCCD định danh.
* **Khi quét mã QR:** Sinh viên thử dùng tính năng quét mã QR in trên phôi bằng giấy, trang kết quả trả về hiển thị thông tin bị lệch so với thực tế.



### 2. Sai lệch kết quả học tập / Nội dung chuyên môn

* **Mô tả hành vi:** Thông tin về ngành đào tạo, chuyên ngành, xếp loại tốt nghiệp (Xuất sắc/Giỏi/Khá) hoặc điểm số GPA tích lũy trên văn bằng số bị ghi nhận thấp hơn hoặc sai lệch so với kết quả gốc trong sổ điểm của sinh viên.
* **Cách sinh viên phát hiện:**
* **Đối chiếu dữ liệu (Cross-checking):** Sinh viên đối chiếu thông tin trên cổng xác thực với **Bảng điểm học tập (Transcript)** được xuất từ phòng Khảo thí của trường hoặc đối chiếu với điểm số hiển thị trên Portal Quản lý đào tạo nội bộ.
* *Ví dụ:* Điểm tích lũy thực tế là $3.6/4.0$ (Xếp loại Giỏi) nhưng văn bằng số hệ thống trả về là $3.1/4.0$ (Xếp loại Khá).



### 3. Nghi vấn tài khoản giáo vụ bị xâm nhập (Hacker can thiệp)

* **Mô tả hành vi:** Xuất hiện một văn bằng "lạ" được gán cho tài khoản/số định danh của sinh viên mà sinh viên chưa từng học hoặc chưa đủ điều kiện tốt nghiệp, hoặc văn bằng cũ đột ngột bị sửa thông tin bất thường.
* **Cách sinh viên phát hiện:**
* **Thông báo hệ thống (System Notification):** Hệ thống bắn thông báo email/toast thông báo *"Văn bằng của bạn đã được cập nhật"* hoặc *"Bạn được cấp một văn bằng mới"*. Sinh viên vào kiểm tra phát hiện ngành học lạ hoắc (ví dụ: Sinh viên học CNTT nhưng nhận được bằng Ngôn ngữ Anh) hoặc thời gian cấp bằng bất hợp lý. Điều này báo hiệu có thể database tập trung của trường đã bị SQL Injection hoặc tài khoản cán bộ Registrar bị lộ, dẫn đến dữ liệu đầu vào bị phá hoại.



---

## II. GÓC NHÌN TỪ NHÀ TUYỂN DỤNG (RECRUITER)

*Nhà tuyển dụng được quyền báo cáo bất kỳ văn bằng nào họ tiếp cận để bảo vệ chất lượng nguồn nhân lực và phát hiện các CSDT thiếu uy tín (AC1 - US-4).*

### 1. Gian lận cấp bằng khống / Bán bằng (Credential Fraud)

* **Mô tả hành vi:** CSDT cố ý cấp bằng hợp pháp (có ký số, có băm đẩy lên Blockchain) cho một cá nhân không hề trải qua quá trình đào tạo, thi cử tại trường để trục lợi.
* **Cách Recruiter phát hiện:**
* **Kiểm tra lý lịch ngầm (Reference Check):** Khi ứng viên nộp hồ sơ, Recruiter quét mã QR, hệ thống trả về kết quả **Hợp lệ 🟢 Confirmed** (vì trường chủ động làm đúng quy trình kỹ thuật). Tuy nhiên, khi Recruiter thực hiện liên hệ với các bên liên quan để xác minh:
* Gọi điện/email cho Giảng viên chủ nhiệm hoặc Trưởng bộ môn của ngành đó tại trường để hỏi về sinh viên này, giảng viên xác nhận *"Khóa này không có sinh viên nào tên như vậy"*.
* Kiểm tra danh sách sinh viên thực tập, danh sách đóng học phí lịch sử (nếu có thể yêu cầu ứng viên cung cấp bổ sung) nhưng hoàn toàn trống không.
* Phỏng vấn trực tiếp chuyên môn (Technical Interview), ứng viên có bằng Xuất sắc nhưng không trả lời được các câu hỏi căn bản nhất, bộc lộ hành vi mua bằng.





### 2. Sự bất nhất giữa Bảng điểm gốc và Văn bằng trên Hệ thống

* **Mô tả hành vi:** CSDT chỉnh sửa trạng thái xét duyệt tốt nghiệp bằng tay (Override logic) để cấp bằng cho sinh viên chưa đủ điều kiện tốt nghiệp (Nợ môn, nợ chứng chỉ chuẩn đầu ra như VSTEP, Tin học).
* **Cách Recruiter phát hiện:**
* **Đối chiếu tài liệu cứng:** Khâu tuyển dụng yêu cầu ứng viên nộp cả **Văn bằng số** và **Bảng điểm gốc (Hardcopy có mộc đỏ)**. Recruiter dùng cổng tra cứu xác thực văn bằng số trả về kết quả khớp, nhưng khi cộng tổng số tín chỉ trên bảng điểm mộc đỏ thì phát hiện ứng viên mới tích lũy được 110/130 tín chỉ, hoặc có 2 môn chuyên ngành bị điểm $F$ chưa học cải thiện.
* Điều này chứng tỏ khâu duyệt cấp bằng của CSDT có sự gian dối, "châm chước" bất hợp pháp cho sinh viên.



### 3. Gian lận thay đổi thông tin sau khi phát hành (Post-issuance Manipulation)

* **Mô tả hành vi:** CSDT âm thầm sửa đổi dữ liệu dưới CSDL tập trung để nâng điểm hoặc đổi loại bằng cho sinh viên sau khi đã bị Recruiter phát hiện nghi vấn trước đó.
* **Cách Recruiter phát hiện:**
* **Lịch sử đối chiếu (Snapshot Mismatch):** Vòng phỏng vấn 1, Recruiter tra cứu mã số bằng hiển thị loại Khá (Dữ liệu chưa lên chuỗi hoặc đã lên chuỗi nhưng Recruiter lưu lại ảnh chụp). Vòng phỏng vấn 2 (sau 1 tuần), Recruiter tra cứu lại thì thấy bằng đổi thành loại Giỏi.
* Nếu hệ thống kích hoạt **Cơ chế đối chiếu kép (AC2 - US-3)** và phát hiện mã băm dưới DB lệch với Blockchain, hệ thống sẽ tự nhấp nháy khung cam cảnh báo `🟠 ĐƠN VỊ BỊ CAN THIỆP`. Nhưng nếu CSDT cố tình chạy luồng `US-2` để hợp thức hóa việc sửa trên chuỗi, Recruiter sẽ phát hiện ra sự bất hợp lý về mặt thời gian (Tại sao bằng tốt nghiệp năm 2024 lại vừa có giao dịch cập nhật trạng thái mới tinh trên chuỗi vào năm 2026?).



### 4. CSDT hoạt động "chui" / Bị tước giấy phép đào tạo

* **Mô tả hành vi:** CSDT đã bị Cơ quan Thẩm quyền tối cao (Bộ GD&ĐT) đình chỉ tuyển sinh hoặc tước giấy phép cấp văn bằng do vi phạm pháp luật, nhưng hệ thống cục bộ của trường vẫn cố tình phát hành lệnh cấp bằng mới.
* **Cách Recruiter phát hiện:**
* **Cập nhật danh mục Blacklist:** Nhà tuyển dụng theo dõi thông tin đại chúng hoặc danh mục quản lý các trường của Bộ. Khi họ nhận được hồ sơ của một ứng viên tốt nghiệp năm 2026 tại một trường đã bị đóng cửa từ năm 2025, dù hệ thống của trường đó vẫn báo `Confirmed`, Recruiter lập tức gửi báo cáo vi phạm để ghim vết điều tra.



---

## III. MA TRẬN XỬ LÝ SỰ KIỆN KHI CÓ BÁO CÁO (KẾT NỐI US-4 ĐẾN US-5)

Để bạn dễ thiết kế Database và viết Code cho `System Engine` (US-5), dưới đây là bảng ma trận quy định hành vi tự động phạt điểm uy tín:

| ID Kịch bản | Bên báo cáo | Loại vi phạm | Trạng thái xác minh | Tác động Điểm uy tín CSDT (`US-5`) |
| --- | --- | --- | --- | --- |
| **S-01** | Sinh viên | Sai thông tin định danh | Đúng (Approved) | **Trừ điểm nhẹ** (Lỗi hành chính, cẩu thả nhập liệu) |
| **S-02** | Sinh viên | Sai kết quả học tập | Đúng (Approved) | **Trừ điểm nhẹ** (Sai sót nội bộ khâu quản lý đào tạo) |
| **R-01** | Recruiter | Bằng khống / Bán bằng | Đúng (Approved) | **TRỪ ĐIỂM NẶNG + ĐÓNG BĂNG** (Gian lận hệ thống nghiêm trọng) |
| **R-02** | Recruiter | Bằng cấp sai tiêu chuẩn | Đúng (Approved) | **TRỪ ĐIỂM NẶNG** (Cố ý làm sai lệch quy chế đào tạo) |
| **H-01** | Cả hai | CSDL nền tảng bị hack | Đúng (Approved) | **Đóng băng điểm uy tín tạm thời** để tiến hành kiểm toán bảo mật |