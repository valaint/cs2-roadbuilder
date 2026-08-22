using System;
using RealRoadBuilder.Core.Evaluation;
using RealRoadBuilder.Core.Fitting;
using RealRoadBuilder.Core.Terrain;

namespace RealRoadBuilder.Core.Planning;

public sealed class HighwayPlanningOptions
{
    private int _maximumIterations = 3;
    private double _preliminaryProfileSampleIntervalMeters = 20.0;
    private int _verticalOptimizationIterations = 300;
    private double _terrainAttraction = 0.18;
    private double _routeSearchCostWeight = 0.05;

    public HighwayPlanningOptions()
    {
        Search = new CorridorSearchOptions();
        Fit = new EngineeringAlignmentFitOptions();
        Construction = new ConstructionEvaluationOptions();
        Feedback = new RerouteFeedbackOptions();
    }

    public CorridorSearchOptions Search { get; set; }

    public EngineeringAlignmentFitOptions Fit { get; set; }

    public ConstructionEvaluationOptions Construction { get; set; }

    public RerouteFeedbackOptions Feedback { get; set; }

    public int MaximumIterations
    {
        get => _maximumIterations;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(MaximumIterations));
            }

            _maximumIterations = value;
        }
    }

    public double PreliminaryProfileSampleIntervalMeters
    {
        get => _preliminaryProfileSampleIntervalMeters;
        set => _preliminaryProfileSampleIntervalMeters = ValidatePositive(
            value,
            nameof(PreliminaryProfileSampleIntervalMeters));
    }

    public int VerticalOptimizationIterations
    {
        get => _verticalOptimizationIterations;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(VerticalOptimizationIterations));
            }

            _verticalOptimizationIterations = value;
        }
    }

    public double TerrainAttraction
    {
        get => _terrainAttraction;
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(TerrainAttraction));
            }

            _terrainAttraction = value;
        }
    }

    public double RouteSearchCostWeight
    {
        get => _routeSearchCostWeight;
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(RouteSearchCostWeight));
            }

            _routeSearchCostWeight = value;
        }
    }

    public bool StopWhenNoMajorStructures { get; set; } = true;

    private static double ValidatePositive(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(name);
        }

        return value;
    }
}
