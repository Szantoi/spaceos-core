// Identity.Api/Controllers/IdentityControllerBase.cs

using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Identity.Api.Controllers;

/// <summary>
/// Base controller that bridges Ardalis.Result&lt;T&gt; (returns ActionResult&lt;T&gt;)
/// to the IActionResult return type used by all action methods.
/// </summary>
public abstract class IdentityControllerBase : ControllerBase
{
    protected IActionResult Respond<T>(Result<T> result) =>
        ((IConvertToActionResult)result.ToActionResult(this)).Convert();
}
