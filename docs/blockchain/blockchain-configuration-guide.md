# Blockchain Configuration Guide

Tài liệu này hướng dẫn cách thiết lập cấu hình Blockchain cho Backend .NET, từ môi trường Development cục bộ đến các môi trường Staging/Production tương lai. 

Cấu hình blockchain được nạp từ file `appsettings.json` hoặc Environment Variables thông qua section `"Blockchain"`.

## Cấu trúc cấu hình (`appsettings.json`)

```json
{
  "Blockchain": {
    "RpcUrl": "http://localhost:8545",
    "ChainId": 1337,
    "ContractAddress": "0x42699A7612A82f1d9C36148af9C77354759b210b",
    "PrivateKey": "0x8f2a55949038a9610f50fb23b5883af3b4ca1366f1a09c2a114f10c14457e937",
    "ConfirmationCount": 1
  }
}
```

### Ý nghĩa các tham số

1. **RpcUrl**: Điểm truy cập JSON-RPC của Node.
   - *Local (Besu):* `http://localhost:8545`
   - *Production:* URL trỏ tới Node RPC riêng trong mạng (tuyệt đối không dùng RPC public free cho các giao dịch cấp bằng).
2. **ChainId**: Mã định danh mạng lưới để chống replay attack.
   - *Local (Besu):* `1337`
   - *Rinkeby/Goerli/Mainnet:* Tương ứng 4, 5, 1...
3. **ContractAddress**: Địa chỉ của hợp đồng `DegreeAnchor` đã được deploy thành công. (Xem file `deployed-address.json` trong project Hardhat).
4. **PrivateKey**: Khóa riêng tư của ví (account) thực hiện giao dịch.
   - Ví này **BẮT BUỘC** phải được cấp quyền trên Smart Contract thông qua hàm `addAnchorService` (nếu không phải là Owner).
   - Có thể có hoặc không có tiền tố `0x`.
5. **ConfirmationCount**: Số block cần chờ để xem như giao dịch đã Finalized.
   - *Besu QBFT (Local/Consortium):* Đặt là `1` (Vì QBFT có instant finality, receipt = final).
   - *Ethereum/Polygon:* Có thể cấu hình `3` hoặc `6` để đảm bảo an toàn khỏi Reorg.

## Cơ chế Fail Fast (An toàn khi khởi động)

Backend được trang bị hệ thống rào chắn **Fail Fast** ngay lúc khởi động (`BlockchainStartupValidatorService`). Nếu cấu hình sai, ứng dụng sẽ `Crash` (văng lỗi và dừng lại) thay vì chạy ngầm và tạo ra lỗi khi người dùng gửi yêu cầu.

Quy trình Startup kiểm tra 3 điều kiện:
1. **Network Mismatch:** Gọi `eth_chainId`. Nếu kết quả trả về từ node khác với `ChainId` khai báo -> Crash.
2. **Contract Missing:** Gọi `eth_getCode(ContractAddress)`. Nếu địa chỉ hợp đồng là rỗng (`0x`) -> Crash. (Tránh lỗi gửi giao dịch vào không khí).
3. **Signer Unauthorized:** Kéo địa chỉ sinh ra từ `PrivateKey` và gọi trực tiếp lên mạng đọc mapping `authorizedAnchors`. Nếu `false` -> Crash. (Tránh lỗi Contract Revert tốn phí gas vô ích sau này).

## Troubleshoot

- **Lỗi: "Signer ... is NOT authorized" lúc startup:**
  Bạn đang dùng một `PrivateKey` chưa được whitelist. Hãy lấy private key của `Owner` (tài khoản deploy contract) để gọi hàm `addAnchorService(ví_mới)` trên Hardhat, hoặc đơn giản là dán trực tiếp `PrivateKey` của `Owner` vào cấu hình.

- **Lỗi: "No contract found at address ..." lúc startup:**
  Bạn chưa chạy script deploy, hoặc node Besu đã bị clear data. Hãy chạy lại lệnh deploy trên Hardhat: `npx hardhat run scripts/deploy.ts --network besuLocal`.

- **Timeout kéo dài hoặc Unknown Outcome:**
  Worker có cơ chế Fallback Idempotency tự động. Hãy đảm bảo Besu Node đang đóng block bình thường. Kiểm tra qua log của Docker container: `docker compose logs -f besu-node`.
