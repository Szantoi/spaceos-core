// SpaceOS.Kernel.Domain/Services/ModuleValidationResult.cs
namespace SpaceOS.Kernel.Domain.Services;

/// <summary>
/// Lightweight validation result returned by <see cref="IModuleRegistryService"/>.
/// Avoids an Ardalis.Result dependency in the Domain layer while providing
/// a clear pass/fail signal with an optional error message.
/// </summary>
public readonly record struct ModuleValidationResult
{
    /// <summary>Gets a value indicating whether the validation passed.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the error message when <see cref="IsValid"/> is <c>false</c>; otherwise <c>null</c>.</summary>
    public string? ErrorMessage { get; }

    private ModuleValidationResult(bool isValid, string? errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    /// <summary>Creates a successful (valid) result.</summary>
    /// <returns>A <see cref="ModuleValidationResult"/> with <see cref="IsValid"/> = <c>true</c>.</returns>
    public static ModuleValidationResult Success() => new(true, null);

    /// <summary>Creates a failed (invalid) result with the specified error message.</summary>
    /// <param name="errorMessage">Human-readable description of the validation failure.</param>
    /// <returns>A <see cref="ModuleValidationResult"/> with <see cref="IsValid"/> = <c>false</c>.</returns>
    public static ModuleValidationResult Failure(string errorMessage) => new(false, errorMessage);
}
