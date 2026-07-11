// SpaceOS.Kernel.Domain/Specifications/ActiveStageDefinitionsSpec.cs
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>Returns all active <see cref="StageDefinition"/> records, ordered by <see cref="StageDefinition.StageCode"/>.</summary>
public sealed class ActiveStageDefinitionsSpec : Specification<StageDefinition>
{
    /// <summary>Initialises a new <see cref="ActiveStageDefinitionsSpec"/>.</summary>
    public ActiveStageDefinitionsSpec()
    {
        Query.Where(sd => sd.IsActive).OrderBy(sd => sd.StageCode);
    }
}
