using System;
using System.Collections.Generic;
using System.Text;

namespace ChainDegree.Core.Domain.SharedKernel.Enums
{
    public enum ActionTypeEnum
    {
        /// <summary>
        /// Tạo mới văn bằng và đưa vào hàng đợi cấp phát ngầm (US-1)
        /// </summary>
        CREATE_DEGREE,

        /// <summary>
        /// Thay đổi/Cập nhật thông tin hoặc Thu hồi văn bằng (US-2)
        /// </summary>
        ALTER_DEGREE,

        /// <summary>
        /// Thay đổi điểm uy tín của Cơ sở đào tạo (Hệ thống tự động kích hoạt sau khi duyệt đơn US-5)
        /// </summary>
        REPUTATION_CHANGED,

        /// <summary>
        /// Doanh nghiệp đăng tin tuyển dụng mới kèm bộ lọc điều kiện (US-6)
        /// </summary>
        POST_JOB,

        /// <summary>
        /// Sinh viên nộp đơn ứng tuyển vào một bài đăng (US-7)
        /// </summary>
        APPLY_JOB,

        /// <summary>
        /// Thực hiện đối chiếu mật mã cục bộ hoặc kiểm tra chéo Merkle Root trên Blockchain (US-3)
        /// </summary>
        VERIFY_DEGREE
    }
}
