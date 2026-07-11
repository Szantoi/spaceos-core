using Ardalis.Result;
using HttpIResult = Microsoft.AspNetCore.Http.IResult;

namespace SpaceOS.Modules.Sales.Api.Extensions;

/// <summary>
/// Maps Ardalis <see cref="Result{T}"/> and <see cref="Result"/> to ASP.NET Core <see cref="HttpIResult"/>.
/// </summary>
internal static class ResultExtensions
{
    /// <summary>Maps a <see cref="Result{T}"/> to an <see cref="HttpIResult"/> (200 OK on success).</summary>
    public static HttpIResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess) return Results.Ok(result.Value);
        return MapError(result.Status, result.Errors, result.ValidationErrors);
    }

    /// <summary>Maps a <see cref="Result"/> to an <see cref="HttpIResult"/> (200 OK on success).</summary>
    public static HttpIResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess) return Results.Ok();
        return MapError(result.Status, result.Errors, result.ValidationErrors);
    }

    /// <summary>Maps a successful <see cref="Result{T}"/> to a 201 Created response.</summary>
    public static HttpIResult ToCreatedResult<T>(this Result<T> result, string location)
    {
        if (result.IsSuccess) return Results.Created(location, result.Value);
        return result.ToHttpResult();
    }

    private static HttpIResult MapError(
        ResultStatus status,
        IEnumerable<string> errors,
        IEnumerable<ValidationError> validationErrors)
        => status switch
        {
            ResultStatus.NotFound     => Results.NotFound(),
            ResultStatus.Forbidden    => Results.Forbid(),
            ResultStatus.Unauthorized => Results.Unauthorized(),
            ResultStatus.Invalid      => Results.UnprocessableEntity(validationErrors),
            ResultStatus.Conflict     => Results.Conflict(),
            _                         => Results.Problem(detail: errors.FirstOrDefault())
        };
}
