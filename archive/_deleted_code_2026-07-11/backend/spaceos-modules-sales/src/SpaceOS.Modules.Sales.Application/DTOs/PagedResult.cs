namespace SpaceOS.Modules.Sales.Application.DTOs;

/// <summary>Generic paged list response with total count.</summary>
public sealed record SalesPagedResult<T>(IReadOnlyList<T> Items, int Total, int Skip, int Take);
