using System;
using System.Security.Cryptography;
using System.Text;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Infrastructure.Cryptography.Services;

public class Sha256HashService : IHashService
{
    private const int SaltByteLength = 32; // 256 bits

    /// <summary>
    /// Tạo ra một chuỗi Salt ngẫu nhiên bảo mật cao bằng CSPRNG (Cryptographically Secure Pseudo-Random Number Generator)
    /// </summary>
    public Result<string> GenerateSalt()
    {
        try
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltByteLength);
            string saltString = Convert.ToBase64String(saltBytes);

            return Result<string>.Success(saltString);
        }
        catch (Exception ex)
        {
            // Lỗi kỹ thuật hạ tầng được phép float lên hoặc gói gọn tùy hệ thống
            return Result<string>.Failure(CryptoErrors.SaltGenerationFailed);
        }
    }

    /// <summary>
    /// Thực hiện băm chuỗi dữ liệu gốc kèm Salt theo thuật toán SHA-256
    /// </summary>
    public Result<string> HashData(string plainText, string salt)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return Result<string>.Failure(CryptoErrors.EmptyPlainText);

        if (string.IsNullOrWhiteSpace(salt))
            return Result<string>.Failure(CryptoErrors.EmptySalt);

        try
        {
            // Kết hợp chuỗi gốc và Salt để chống lại kiểu tấn công Rainbow Table
            string combinedInput = plainText + salt;
            byte[] inputBytes = Encoding.UTF8.GetBytes(combinedInput);

            byte[] hashBytes = SHA256.HashData(inputBytes);

            // Chuyển đổi sang định dạng Hex để lưu trữ chuỗi băm đồng bộ (thường là 64 ký tự)
            string hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return Result<string>.Success(hashString);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(CryptoErrors.HashingFailed);
        }
    }
}