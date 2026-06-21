using System;
using System.Collections.Generic;
using System.Text;

namespace ChainDegree.Core.Domain.Applications.Enums
{
    public enum ApplicationProcessStatusEnum
    {
        /// <summary>
        /// Đơn ứng tuyển mới gửi thành công, đang nằm trong hộp thư chờ của Doanh nghiệp (US-7)
        /// </summary>
        Submitted,

        /// <summary>
        /// Nhà tuyển dụng (RecruiterAgent) đã mở xem và đang trong quá trình đánh giá hồ sơ
        /// </summary>
        Reviewing,

        /// <summary>
        /// Hồ sơ đạt yêu cầu và được chấp nhận (Ví dụ: Qua vòng lọc hồ sơ để vào vòng Phỏng vấn)
        /// </summary>
        Accepted,

        /// <summary>
        /// Hồ sơ bị Nhà tuyển dụng từ chối thủ công sau khi xem xét
        /// </summary>
        Rejected
    }
}
