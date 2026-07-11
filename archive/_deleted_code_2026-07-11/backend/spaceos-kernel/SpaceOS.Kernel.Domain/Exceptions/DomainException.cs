using System;

namespace SpaceOS.Kernel.Domain.Exceptions;

/// <summary>
/// Base exception for all business rule violations in the SpaceOS Kernel.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Initialises a new <see cref="DomainException"/> with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the business rule violation.</param>
    public DomainException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initialises a new <see cref="DomainException"/> with the specified message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the business rule violation.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
