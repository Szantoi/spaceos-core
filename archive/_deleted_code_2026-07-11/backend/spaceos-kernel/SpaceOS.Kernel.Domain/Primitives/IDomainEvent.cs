using System;
using MediatR;

namespace SpaceOS.Kernel.Domain.Primitives;

/// <summary>
/// Marker interface for all Domain Events.
/// </summary>
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredOn { get; }
}
