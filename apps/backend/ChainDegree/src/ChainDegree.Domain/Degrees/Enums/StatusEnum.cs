using System;
using System.Collections.Generic;
using System.Text;

namespace ChainDegree.Core.Domain.Degrees.Enums
{
    public enum StatusEnum
    {
        Pending_Confirmation, // Văn bằng mới tiếp nhận, đang đợi gom lô xử lý ngầm
        Confirmed,            // Đã tính toán Merkle Root và neo chặn thành công lên Blockchain
        Confirmation_Error,   // Quá trình xử lý ngầm lên chuỗi gặp lỗi (Sẵn sàng để Retry)
        Pending_Update,       // Văn bằng đang trong quá trình cập nhật bất đồng bộ dữ liệu chuỗi
        Pending_Revocation,   // Văn bằng đang trong quá trình thu hồi bất đồng bộ trên chuỗi
        Revoked               // Văn bằng đã bị hủy bỏ/thu hồi hoàn toàn hiệu lực pháp lý
    }
}
