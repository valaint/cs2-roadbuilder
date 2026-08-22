using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Terrain;

public interface ICorridorConstraintProvider
{
    bool IsBlocked(PlanarPoint point);

    double GetAdditionalCost(PlanarPoint point);
}
