// SpaceOS.Kernel.Application/Common/PagedList.cs

namespace SpaceOS.Kernel.Application.Common;

/// <summary>
/// Wraps a single page of results together with pagination metadata.
/// </summary>
/// <typeparam name="T">The item type contained in the page.</typeparam>
/// <param name="Items">The items on the current page.</param>
/// <param name="Page">The 1-based current page number.</param>
/// <param name="PageSize">The maximum number of items per page.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
public sealed record PagedList<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    /// <summary>
    /// The total number of pages, calculated from <see cref="TotalCount"/> and <see cref="PageSize"/>.
    /// Returns 0 when <see cref="TotalCount"/> is 0.
    /// </summary>
    public int TotalPages =>
        TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
