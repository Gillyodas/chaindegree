using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Domain.Degrees.Interfaces
{
    public interface IJsonCanonicalizer
    {
        /// <summary>
        /// Chuyển đổi một đối tượng dữ liệu văn bằng thành chuỗi JSON chuẩn hóa (Deterministic/Canonical string)
        /// Các thuộc tính phải được sắp xếp theo bảng chữ cái và loại bỏ toàn bộ khoảng trắng thừa.
        /// </summary>
        Result<string> Canonicalize<T>(T data) where T : class;
    }
}
