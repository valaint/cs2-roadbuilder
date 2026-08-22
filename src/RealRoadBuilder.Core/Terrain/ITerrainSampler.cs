using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Terrain;

public interface ITerrainSampler
{
    double GetElevationMeters(PlanarPoint point);
}
