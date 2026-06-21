using System;
using System.Collections.Generic;
using System.Text;

namespace ChainDegree.Core.Domain.Jobs.Enums
{
    public enum DegreeTypeEnum
    {
        /// <summary>
        /// Hệ đào tạo Cử nhân (Thời gian đào tạo thường từ 3 - 4 năm)
        /// </summary>
        Cu_Nhan,

        /// <summary>
        /// Hệ đào tạo Kỹ sư (Thời gian đào tạo chuyên sâu kỹ thuật thường từ 4.5 - 5 năm)
        /// </summary>
        Ky_Su,

        /// <summary>
        /// Hệ đào tạo Thạc sĩ (Bậc đào tạo sau đại học)
        /// </summary>
        Thac_Si,

        /// <summary>
        /// Hệ đào tạo Tiến sĩ (Bậc học cao nhất trong hệ thống học vị)
        /// </summary>
        Tien_Si
    }
}
