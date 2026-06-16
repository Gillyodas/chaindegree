# Bảng: **Degree** (Bằng cấp)
| Column Name | Type | Nullable | Description/Constraint |
| Id | Guid | NOT NULL | Primary Key |
| Title | NVarChar(150) | NOT NULL | Tên bằng cấp |
| Type | Guid | NOT NULL | Loại bằng cấp |
| Status | int | NOT NULL | Trạng thái xử lý Enum:  
| CreatedBy | Guid | NOT NULL | Tạo bởi ai |
| IssuedBy | Guid | NOT NULL | Tạo bởi cơ sở đào tạo nào |
| UpdatedBy | Guid | NULL | Cập nhật bởi ai |
| CreatedAt | Datetime | NOT NULL | Tạo lúc nào |
| UpdatedAt | Datetime | NOT NULL | Cập nhật lúc nào |
| DeletedAt | Datetime | NOT NULL | Xóa lúc nào |