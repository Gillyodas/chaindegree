# ChainDegree Blockchain Dev Environment

Môi trường phát triển blockchain cho dự án ChainDegree, sử dụng Hyperledger Besu chạy ở chế độ dev.

## Hướng dẫn chạy node Besu local

1. Khởi chạy Docker Compose:
   ```bash
   docker compose up -d
   ```

2. Kiểm tra node có phản hồi RPC không:
   ```bash
   curl -X POST --header "Content-Type: application/json" --data '{"jsonrpc":"2.0","method":"eth_chainId","params":[],"id":1}' http://localhost:8545
   ```
   Kết quả kỳ vọng trả về ChainId `0x539` (1337 ở hệ thập phân).
