using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ChainDegree.Core.Infrastructure.Persistence.Interceptors
{
    public class DegreeProcessingInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            UpdateProcessingRecords(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            UpdateProcessingRecords(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void UpdateProcessingRecords(DbContext? context)
        {
            if (context == null) return;

            var degrees = context.ChangeTracker.Entries<Degree>().ToList();

            foreach (var entry in degrees)
            {
                var degree = entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    if (degree.Status == StatusEnum.Pending_Confirmation)
                    {
                        var record = new DegreeProcessingRecord
                        {
                            DegreeId = degree.Id,
                            ActionType = "Issue",
                            State = "Queued",
                            RetryCount = 0
                        };
                        context.Set<DegreeProcessingRecord>().Add(record);
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    var statusEntry = entry.Property(x => x.Status);
                    if (statusEntry.IsModified)
                    {
                        var oldStatus = (StatusEnum)statusEntry.OriginalValue!;
                        var newStatus = (StatusEnum)statusEntry.CurrentValue!;

                        if (newStatus == StatusEnum.Pending_Update)
                        {
                            var existingRecord = context.Set<DegreeProcessingRecord>().Local.FirstOrDefault(r => r.DegreeId == degree.Id)
                                                 ?? context.Set<DegreeProcessingRecord>().FirstOrDefault(r => r.DegreeId == degree.Id);

                            if (existingRecord == null)
                            {
                                existingRecord = new DegreeProcessingRecord { DegreeId = degree.Id };
                                context.Set<DegreeProcessingRecord>().Add(existingRecord);
                            }

                            existingRecord.ActionType = "Update";
                            existingRecord.State = "Queued";
                            existingRecord.RetryCount = 0;
                            existingRecord.NextRetryAt = null;
                            existingRecord.LastRetryAt = null;
                            existingRecord.LeaseUntil = null;
                            existingRecord.WorkerId = null;
                            existingRecord.LastError = null;
                            existingRecord.BlockchainTxHash = null;
                        }
                        else if (newStatus == StatusEnum.Pending_Revocation)
                        {
                            var existingRecord = context.Set<DegreeProcessingRecord>().Local.FirstOrDefault(r => r.DegreeId == degree.Id)
                                                 ?? context.Set<DegreeProcessingRecord>().FirstOrDefault(r => r.DegreeId == degree.Id);

                            if (existingRecord == null)
                            {
                                existingRecord = new DegreeProcessingRecord { DegreeId = degree.Id };
                                context.Set<DegreeProcessingRecord>().Add(existingRecord);
                            }

                            existingRecord.ActionType = "Revoke";
                            existingRecord.State = "Queued";
                            existingRecord.RetryCount = 0;
                            existingRecord.NextRetryAt = null;
                            existingRecord.LastRetryAt = null;
                            existingRecord.LeaseUntil = null;
                            existingRecord.WorkerId = null;
                            existingRecord.LastError = null;
                            existingRecord.BlockchainTxHash = null;
                        }
                        else if (newStatus == StatusEnum.Revoked && (oldStatus == StatusEnum.Pending_Confirmation || oldStatus == StatusEnum.Confirmation_Error))
                        {
                            var existingRecord = context.Set<DegreeProcessingRecord>().Local.FirstOrDefault(r => r.DegreeId == degree.Id)
                                                 ?? context.Set<DegreeProcessingRecord>().FirstOrDefault(r => r.DegreeId == degree.Id);

                            if (existingRecord != null)
                            {
                                context.Set<DegreeProcessingRecord>().Remove(existingRecord);
                            }
                        }
                    }
                }
            }
        }
    }
}
