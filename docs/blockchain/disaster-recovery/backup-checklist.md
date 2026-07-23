# Danh Sách Kiểm Tra Backup Cấu Hình (Configuration Backup Checklist)

Danh sách các hạng mục bắt buộc phải thực hiện kiểm tra và lưu trữ backup độc lập để đảm bảo tính sẵn sàng của hệ thống ChainDegree.

---

## Danh Sách Hạng Mục Bắt Buộc

- [ ] **1. File Genesis Khởi Tạo (`genesis.json`)**
  - Path: `apps/blockchain/genesis/genesis.json`
  - Mô tả: Chứa ChainID (`2026`), tham số QBFT, và danh sách 4 validator gốc.
  - Vị trí lưu trữ backup: Lưu ở 2 kênh lưu trữ độc lập (Cold storage / Offsite Cloud Storage).

- [ ] **2. Khóa Node Validator (`key`)**
  - Paths:
    - `apps/blockchain/configs/validator1/key`
    - `apps/blockchain/configs/validator2/key`
    - `apps/blockchain/configs/validator3/key`
    - `apps/blockchain/configs/validator4/key`
    - `apps/blockchain/configs/rpc/key`
  - Mô tả: Khóa riêng tư P2P định danh cho từng Node.

- [ ] **3. Danh Sách Node Cố Định (`static-nodes.json`)**
  - Path: `apps/blockchain/configs/static-nodes.json`
  - Mô tả: Chứa chuỗi `enode://` kèm IP tĩnh và port P2P của 5 nodes.

- [ ] **4. Địa Chỉ Smart Contract & ABI**
  - Paths:
    - `apps/blockchain/contracts/DegreeAnchor.sol`
    - `deployed-address.json` (Contract address đã deploy)

- [ ] **5. File Backup Cơ Sở Dữ Liệu SQL Server (`.bak`)**
  - Path: `/var/opt/mssql/backup/ChainDegree_Backup_*.bak`
  - Tần suất: Backup hàng ngày (Daily Cron job).
