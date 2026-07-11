// SpaceOS.Kernel.Domain/Specifications/PagedSpecification.cs
using Ardalis.Specification;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>
/// Base specification that applies offset/limit paging to any query.
/// Inherit and add a <c>Query.Where(...)</c> filter in the sub-class constructor.
/// </summary>
/// <typeparam name="T">The aggregate root type being queried.</typeparam>
public abstract class PagedSpecification<T> : Specification<T>
{
    /// <summary>
    /// Initialises the paging constraints on the specification.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum number of items per page.</param>
    protected PagedSpecification(int page, int pageSize)
    {
        Query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }
}
