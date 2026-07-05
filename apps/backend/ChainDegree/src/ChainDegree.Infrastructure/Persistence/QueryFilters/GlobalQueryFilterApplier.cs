// ============================================================================
// GlobalQueryFilterApplier.cs
// ============================================================================
// Tầng Infrastructure (Persistence/QueryFilters/).
//
// TRÁCH NHIỆM DUY NHẤT: quét toàn bộ entity type trong Model, kiểm tra
// entity nào implement các marker interface (ISoftDeletable,
// IInstitutionScoped, ...) và tự động đăng ký HasQueryFilter tương ứng.
//
// Vì sao tách khỏi DbContext.OnModelCreating:
//   - Testability: có thể new GlobalQueryFilterApplier(logger).Apply(...)
//     trong unit test với 1 ModelBuilder giả lập, không cần DbContext thật.
//   - Single Responsibility: DbContext lo việc khai báo DbSet + model;
//     class này lo riêng việc "entity nào cần lọc gì".
//   - Mở rộng: thêm marker interface mới (IAuditable, ITenantScoped...)
//     chỉ cần thêm 1 method Apply*Filter + gọi trong vòng lặp bên dưới,
//     không đụng vào DbContext.
//
// LƯU Ý QUAN TRỌNG VỀ EXPRESSION CAPTURE (IInstitutionScoped filter):
//   HasQueryFilter được build 1 LẦN DUY NHẤT lúc OnModelCreating (thường là
//   lần đầu DbContext được resolve, EF Core cache lại Model). Nếu ta
//   Expression.Constant(giá trị _currentInstitutionId) tại thời điểm build,
//   giá trị đó sẽ bị "đóng băng" và dùng chung cho MỌI DbContext instance
//   sau này — sai hoàn toàn với yêu cầu multi-tenant (mỗi request/scope có
//   1 currentInstitutionId khác nhau).
//   Giải pháp: dùng Expression.Field trỏ tới FIELD (không phải giá trị) của
//   THAM SỐ dbContextInstance được truyền vào. EF Core, khi dịch expression
//   này thành SQL, sẽ đọc field đó từ chính DbContext instance đang thực thi
//   query — tức đọc "sống" mỗi lần, không dùng giá trị đã cache.
// ============================================================================

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;

namespace ChainDegree.Core.Infrastructure.Persistence.QueryFilters;

public sealed class GlobalQueryFilterApplier
{
    private readonly ILogger _logger;

    public GlobalQueryFilterApplier(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Quét toàn bộ entity trong model, áp filter tương ứng theo interface
    /// mà entity implement. Gọi 1 lần trong OnModelCreating.
    /// </summary>
    /// <param name="modelBuilder">ModelBuilder đang được cấu hình.</param>
    /// <param name="dbContextInstance">
    /// Instance DbContext hiện tại — dùng để build Expression.Field trỏ tới
    /// _currentInstitutionId, đảm bảo filter đọc giá trị "sống" mỗi query,
    /// không bị đóng băng giá trị lúc build model.
    /// </param>
    public void Apply(ModelBuilder modelBuilder, ChainDegreeDbContext dbContextInstance)
    {
        var appliedCount = 0;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // Mỗi Apply*Filter trả về Expression? (null nếu entity không
            // implement interface tương ứng). Gộp các filter bằng AndAlso
            // nếu 1 entity implement nhiều interface cùng lúc.
            var parameter = Expression.Parameter(clrType, "e");

            var combinedFilter = CombineFilters(
                parameter,
                TryBuildSoftDeleteFilter(clrType, parameter),
                TryBuildInstitutionScopeFilter(clrType, parameter, dbContextInstance)
            );

            if (combinedFilter is null)
            {
                continue; // entity không cần global filter nào — bỏ qua, không log rác
            }

            var lambda = Expression.Lambda(combinedFilter, parameter);
            modelBuilder.Entity(clrType).HasQueryFilter(lambda);

            appliedCount++;
            _logger.LogInformation(
                "Global query filter applied to entity {EntityName}: {FilterExpression}",
                clrType.Name,
                lambda.ToString());
        }

        _logger.LogInformation(
            "GlobalQueryFilterApplier completed: {Count} entities received global query filters",
            appliedCount);
    }

    // ------------------------------------------------------------------
    // Soft-delete filter: e => e.DeletedAt == null
    // ------------------------------------------------------------------
    private static Expression? TryBuildSoftDeleteFilter(Type clrType, ParameterExpression parameter)
    {
        if (!typeof(ISoftDeletable).IsAssignableFrom(clrType))
        {
            return null;
        }

        var deletedAtProperty = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
        var nullConstant = Expression.Constant(null, typeof(DateTime?));
        return Expression.Equal(deletedAtProperty, nullConstant);
    }

    // ------------------------------------------------------------------
    // Institution-scope filter: e => e.InstitutionId == dbContext._currentInstitutionId
    // ------------------------------------------------------------------
    private static Expression? TryBuildInstitutionScopeFilter(
        Type clrType,
        ParameterExpression parameter,
        ChainDegreeDbContext dbContextInstance)
    {
        if (!typeof(IInstitutionScoped).IsAssignableFrom(clrType))
        {
            return null;
        }

        var institutionIdProperty = Expression.Property(parameter, nameof(IInstitutionScoped.InstitutionId));

        // Entity.InstitutionId là Guid, nhưng _currentInstitutionId là Guid? (nullable).
        // Expression.Equal giữa Guid và Guid? sẽ lỗi runtime — phải convert về cùng kiểu.
        var nullableInstitutionId = Expression.Convert(institutionIdProperty, typeof(Guid?));

        // QUAN TRỌNG: Expression.Constant(dbContextInstance) chỉ "chốt" instance
        // DbContext hiện tại làm điểm neo cho Expression.Field — bản thân field
        // _currentInstitutionId vẫn được đọc "sống" từ instance đó mỗi lần EF Core
        // thực thi filter, vì mỗi request có 1 DbContext instance riêng (Scoped
        // lifetime) nên "instance hiện tại" luôn đúng là instance của request đó.
        var contextConstant = Expression.Constant(dbContextInstance);

        // Dùng nameof thay vì hardcode string "_currentInstitutionId": nếu field
        // bị đổi tên, compiler báo lỗi ngay tại đây thay vì fail runtime khi
        // GetField trả về null.
        var currentInstitutionIdField = typeof(ChainDegreeDbContext).GetField(
            nameof(ChainDegreeDbContext._currentInstitutionId),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (currentInstitutionIdField is null)
        {
            throw new InvalidOperationException(
                $"Cannot find field '{nameof(ChainDegreeDbContext._currentInstitutionId)}' on " +
                $"{nameof(ChainDegreeDbContext)}. The field may have been renamed or removed during refactoring.");
        }

        var currentInstitutionIdAccess = Expression.Field(contextConstant, currentInstitutionIdField);

        return Expression.Equal(nullableInstitutionId, currentInstitutionIdAccess);
    }

    // ------------------------------------------------------------------
    // Gộp nhiều filter bằng AndAlso, bỏ qua filter null
    // ------------------------------------------------------------------
    private static Expression? CombineFilters(ParameterExpression parameter, params Expression?[] filters)
    {
        Expression? result = null;

        foreach (var filter in filters)
        {
            if (filter is null) continue;

            result = result is null
                ? filter
                : Expression.AndAlso(result, filter);
        }

        return result;
    }
}
