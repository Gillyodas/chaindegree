using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Infrastructure.Cryptography.Services;

public class JsonCanonicalizer : IJsonCanonicalizer
{
    private static readonly JsonSerializerOptions _serializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false // Chống khoảng trắng và xuống dòng
    };

    public Result<string> Canonicalize<T>(T data) where T : class
    {
        if (data == null)
            return Result<string>.Failure(CryptoErrors.NullDataPayload);

        try
        {
            // 1. Chuỗi hóa object ban đầu sang JSON thô
            var jsonString = JsonSerializer.Serialize(data, _serializeOptions);
            var jsonNode = JsonNode.Parse(jsonString);

            if (jsonNode is JsonObject jsonObject)
            {
                // 2. Thực hiện sắp xếp đệ quy các thuộc tính theo bảng chữ cái
                var sortedObject = SortJsonObject(jsonObject);
                return Result<string>.Success(sortedObject.ToJsonString(_serializeOptions));
            }

            return Result<string>.Success(jsonString);
        }
        catch (Exception)
        {
            // Gói lỗi kỹ thuật của thư viện System.Text.Json thành Domain Error tường minh
            return Result<string>.Failure(CryptoErrors.CanonicalizationFailed);
        }
    }

    private JsonObject SortJsonObject(JsonObject obj)
    {
        var sortedProperties = obj
            .Select(p => new { p.Key, Value = p.Value?.DeepClone() })
            .OrderBy(p => p.Key)
            .ToList();

        var newObj = new JsonObject();
        foreach (var prop in sortedProperties)
        {
            if (prop.Value is JsonObject subObj)
            {
                newObj.Add(prop.Key, SortJsonObject(subObj));
            }
            else
            {
                newObj.Add(prop.Key, prop.Value);
            }
        }
        return newObj;
    }
}