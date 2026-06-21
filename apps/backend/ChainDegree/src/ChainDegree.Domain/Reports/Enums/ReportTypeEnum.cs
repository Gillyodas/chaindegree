using System;
using System.Collections.Generic;
using System.Text;

namespace ChainDegree.Core.Domain.Reports.Enums
{
    public enum ReportTypeEnum
    {
        /// <summary>
        /// Sai sót thông tin hành chính (Kịch bản S-01, S-02).
        /// Ví dụ: Sai lỗi chính tả tên sinh viên, ngày sinh, CCCD hoặc nhập nhầm điểm số/chuyên ngành.
        /// Hệ thống sẽ áp dụng mức phạt nhẹ (Minor Penalty: -20 điểm) đối với CSDT sau khi được duyệt.
        /// </summary>
        Administrative_Error,

        /// <summary>
        /// Gian lận dữ liệu hệ thống nghiêm trọng (Kịch bản R-01, R-02).
        /// Ví dụ: Cấp bằng khống, bán bằng cho người không đi học, hoặc cố ý làm sai lệch quy chế đào tạo.
        /// Hệ thống sẽ áp dụng mức phạt rất nặng (Critical Penalty: -400 điểm) và kích hoạt luồng đóng băng tài khoản trường.
        /// </summary>
        Fraudulent_Data
    }
}
