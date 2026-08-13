using System;
using System.Collections.Generic;

namespace ChainDegree.SharedKernel.Result
{
    public class PagedResult<T>
    {
        public IReadOnlyCollection<T> Items { get; }
        public int TotalCount { get; }
        public int PageIndex { get; }
        public int PageSize { get; }

        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public PagedResult(IReadOnlyCollection<T> items, int totalCount, int pageIndex, int pageSize)
        {
            Items = items ?? Array.Empty<T>();
            TotalCount = totalCount >= 0 ? totalCount : 0;
            PageIndex = pageIndex >= 1 ? pageIndex : 1;
            PageSize = pageSize >= 1 ? pageSize : 20;
        }
    }
}
