# Quy Trình Phục Hồi Sau Sự Cố (Disaster Recovery SOP)

Tài liệu này đặc tả quy trình vận hành tiêu chuẩn (Standard Operating Procedure - SOP) cho việc backup và khôi phục hệ thống khi xảy ra thảm họa dữ liệu hoặc sự cố mạng lưới.

---

## 1. Phân Loại Tầm Quan Trọng Của Dữ Liệu

| Loại Dữ Liệu | Nơi Lưu Trữ | Mức Độ | Tác Động Khi Mất | Phương Án Phục Hồi |
|---|---|---|---|---|
| `genesis.json` | Local File / Volume | **CRITICAL** | Không thể kết nối hoặc phục hồi mạng blockchain | Restore từ backup an toàn (khuyến nghị 2 địa điểm khác nhau) |
| Validator Key Pair | KMS / Secret File | **CRITICAL** | Validator node không thể tham gia đồng thuận QBFT | Restore key material từ KMS Backup |
| SQL Server DB | Container Volume | **CRITICAL** | Mất toàn bộ PlainData, Merkle Proofs và trạng thái | Restore từ file `.bak` mới nhất |
| Smart Contract Address / ABI | Project Repo / Config | **HIGH** | Backend không biết contract address | Re-deploy contract hoặc đọc từ config backup |
| Besu Data Volume | Container Volume | **MEDIUM** | Mất dữ liệu ledger local trên 1 node | Tự động đồng bộ (sync) lại từ các Validator peer khác |

---

## 2. Quy Trình Backup Định Kỳ

### 2.1. Backup Cơ Sở Dữ Liệu SQL Server
- Sử dụng script `apps/blockchain/scripts/backup-db.sh`.
- Thiết lập Cron job chạy hàng ngày lúc 00:00:
  ```bash
  0 0 * * * /path/to/apps/blockchain/scripts/backup-db.sh >> /var/log/chaindegree-backup.log 2>&1
  ```
- Chính sách lưu trữ (Retention): Giữ lại bản backup trong 30 ngày.

### 2.2. Backup Cấu Hình Mạng Blockchain Immutable
- Thực hiện backup ngay sau mỗi lần cập nhật mạng:
  - `apps/blockchain/genesis/genesis.json`
  - `apps/blockchain/configs/static-nodes.json`
  - `apps/blockchain/contracts/` (ABI & bytecode)

---

## 3. Quy Trình Khôi Phục (Restoration Workflow)

### Kịch Bản 1: Khôi Phục Cơ Sở Dữ Liệu SQL Server
1. Dừng dịch vụ Backend API & Worker để ngắt ghi dữ liệu:
   ```bash
   docker stop chaindegree-api chaindegree-worker
   ```
2. Thực thi lệnh Restore DB từ file `.bak`:
   ```bash
   docker exec chaindegree-sqlserver /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P "YourStrongPass123!" -C \
     -Q "RESTORE DATABASE [ChainDegree] FROM DISK = N'/var/opt/mssql/backup/ChainDegree_Backup_Latest.bak' WITH REPLACE"
   ```
3. Khởi động lại dịch vụ Backend:
   ```bash
   docker start chaindegree-api chaindegree-worker
   ```

### Kịch Bản 2: Mất Volume Dữ Liệu Trên 1 Validator Node
1. Xóa volume lỗi và khởi động lại container validator:
   ```bash
   docker compose restart besu-validator1
   ```
2. Node sẽ tự động đọc `static-nodes.json`, kết nối P2P với các validator khác và thực hiện Fast Sync toàn bộ block height về thời điểm hiện tại.

### Kịch Bản 3: Mất File Genesis
1. Đọc file `genesis.json` từ kho lưu trữ backup an toàn.
2. Kiểm tra checksum hash SHA-256 của file `genesis.json` đảm bảo không bị sai lệch.
3. Thay thế file `genesis.json` vào thư mục `apps/blockchain/genesis/` và khởi động lại cluster Besu.
