using System;
using System.Collections.Generic;
using System.Text;

namespace ChainDegree.Core.Domain.Reports.Enums
{
    public enum ReportStatusEnum
    {
        /// <summary>
        /// Đơn khiếu nại mới được gửi từ Sinh viên hoặc Nhà tuyển dụng, đang chờ Admin hệ thống thẩm định (US-4)
        /// </summary>
        Pending_Review,

        /// <summary>
        /// Đơn khiếu nại chính xác và được duyệt. 
        /// Kích hoạt luồng cập nhật trạng thái văn bằng sang Revoked/Pending_Revocation 
        /// và bắn Event tính toán phạt điểm uy tín CSDT (US-5)
        /// </summary>
        Approved,

        /// <summary>
        /// Đơn khiếu nại bị từ chối do thông tin sai lệch, thiếu minh chứng hoặc spam dữ liệu.
        /// </summary>
        Rejected
    }
}
