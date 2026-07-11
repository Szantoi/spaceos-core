using Ardalis.Result;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;

namespace SpaceOS.Modules.Abstractions.Domain.Services;

public interface ITemplateValidator
{
    Result Validate(ProductTemplate template);
}
