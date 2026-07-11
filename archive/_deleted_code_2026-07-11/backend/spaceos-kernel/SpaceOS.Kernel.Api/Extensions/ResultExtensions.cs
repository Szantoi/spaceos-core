// SpaceOS.Kernel.Api/Extensions/ResultExtensions.cs
using Ardalis.Result;
using HttpResult = Microsoft.AspNetCore.Http.IResult;

namespace SpaceOS.Kernel.Api.Extensions;

/// <summary>Maps <see cref="Result{T}"/> and <see cref="Result"/> values from the Application layer to ASP.NET Core <see cref="Microsoft.AspNetCore.Http.IResult"/> responses.</summary>
public static class ResultExtensions
{
    /// <summary>Converts a <see cref="Result{T}"/> to the appropriate <see cref="Microsoft.AspNetCore.Http.IResult"/> HTTP response.</summary>
    public static HttpResult ToApiResult<T>(this Result<T> result) => result.Status switch
    {
        ResultStatus.Ok        => Results.Ok(result.Value),
        ResultStatus.NoContent => Results.NoContent(),
        ResultStatus.NotFound  => Results.Problem(
            title: "Not Found",
            statusCode: 404,
            type: "https://tools.ietf.org/html/rfc7807"),
        ResultStatus.Conflict  => Results.Problem(
            title: "Conflict",
            statusCode: 409,
            type: "https://httpstatuses.io/409"),
        ResultStatus.Invalid   => Results.ValidationProblem(
            result.ValidationErrors
                .GroupBy(e => e.Identifier)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
            statusCode: 422),
        ResultStatus.Unauthorized => Results.Problem(
            title: "Unauthorized",
            statusCode: 401,
            type: "https://tools.ietf.org/html/rfc7235#section-3.1"),
        ResultStatus.Error     => Results.Problem(
            title: "Domain Rule Violation",
            detail: result.Errors.FirstOrDefault(),
            statusCode: 400,
            type: "https://httpstatuses.io/400"),
        _                      => Results.Problem(statusCode: 500)
    };

    /// <summary>Converts a non-generic <see cref="Result"/> to the appropriate <see cref="Microsoft.AspNetCore.Http.IResult"/> HTTP response.</summary>
    public static HttpResult ToApiResult(this Result result) => result.Status switch
    {
        ResultStatus.Ok        => Results.Ok(),
        ResultStatus.NoContent => Results.NoContent(),
        ResultStatus.NotFound  => Results.Problem(
            title: "Not Found",
            statusCode: 404,
            type: "https://tools.ietf.org/html/rfc7807"),
        ResultStatus.Conflict  => Results.Problem(
            title: "Conflict",
            statusCode: 409,
            type: "https://httpstatuses.io/409"),
        ResultStatus.Invalid   => Results.ValidationProblem(
            result.ValidationErrors
                .GroupBy(e => e.Identifier)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
            statusCode: 422),
        ResultStatus.Unauthorized => Results.Problem(
            title: "Unauthorized",
            statusCode: 401,
            type: "https://tools.ietf.org/html/rfc7235#section-3.1"),
        ResultStatus.Error     => Results.Problem(
            title: "Domain Rule Violation",
            detail: result.Errors.FirstOrDefault(),
            statusCode: 400,
            type: "https://httpstatuses.io/400"),
        _                      => Results.Problem(statusCode: 500)
    };

    /// <summary>
    /// Converts a <see cref="Result{T}"/> to a 201 Created response using a named route for the Location header.
    /// Returns 422 on validation failure and 500 on unexpected errors.
    /// </summary>
    public static HttpResult ToCreatedResult<T>(
        this Result<T> result,
        string routeName,
        Func<T, object> routeValues) => result.Status switch
    {
        ResultStatus.Ok      => Results.CreatedAtRoute(routeName, routeValues(result.Value), result.Value),
        ResultStatus.Invalid => Results.ValidationProblem(
            result.ValidationErrors
                .GroupBy(e => e.Identifier)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
            statusCode: 422),
        _                    => Results.Problem(statusCode: 500)
    };
}
