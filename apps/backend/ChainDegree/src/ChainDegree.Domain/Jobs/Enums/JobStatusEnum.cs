using System;
using System.Collections.Generic;
using System.Text;

namespace ChainDegree.Core.Domain.Jobs.Enums
{
    public enum JobStatusEnum
    {
        Draft,
        /// <summary>
        /// Bài đăng tuyển dụng mới được tạo và đang hiển thị công khai trên hệ thống (US-6)
        /// </summary>
        Active,

        /// <summary>
        /// Bài đăng tạm ẩn/tạm dừng nhận hồ sơ ứng tuyển từ Sinh viên
        /// </summary>
        Paused,

        /// <summary>
        /// Bài đăng đã đóng (Hết hạn tuyển dụng hoặc đã tuyển đủ nhân sự).
        /// Sinh viên không thể nộp đơn ứng tuyển vào bài đăng này nữa (US-7)
        /// </summary>
        Closed,
        CompanyArchived,
        Deleted
    }
}
