using System;
using System.Collections.Generic;
using System.Text;

namespace ChainDegree.Core.Domain.Applications.Enums
{
    public enum ApplicationRankStatusEnum
    {
        /// <summary>
        /// Hồ sơ hoàn toàn đáp ứng đầy đủ hoặc vượt bộ lọc tiêu chuẩn bằng cấp do doanh nghiệp cấu hình (US-7)
        /// </summary>
        Highly_Qualified,

        /// <summary>
        /// Hồ sơ bị thiếu chuẩn/lệch bộ lọc, nhưng được sinh viên chọn nộp cưỡng bức (ForceSubmit = true)
        /// Hệ thống đánh dấu phân hạng thấp để doanh nghiệp cân nhắc (US-7)
        /// </summary>
        Under_Qualified
    }
}
