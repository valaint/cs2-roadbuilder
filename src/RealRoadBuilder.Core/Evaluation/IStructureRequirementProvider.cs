using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Evaluation;

/// <summary>
/// Optional adapter for map features that force a structure choice, such as a
/// water crossing that must be bridged or a protected/covered corridor that
/// must remain underground. The CS2 adapter can populate this later.
/// </summary>
public interface IStructureRequirementProvider
{
    StructureRequirement GetRequirement(PlanarPoint position);
}
