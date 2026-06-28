using System;
using System.Collections.Generic;
using System.Text;

namespace ChainDegree.Core.Infrastructure.Configurations
{
    public class JwtOptions
    {
        // Tên của Section trong file appsettings.json để map cho đúng
        public const string SectionName = "Jwt";

        // Khai báo các thuộc tính trùng khớp 1:1 với JSON/ENV
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
    }
}
