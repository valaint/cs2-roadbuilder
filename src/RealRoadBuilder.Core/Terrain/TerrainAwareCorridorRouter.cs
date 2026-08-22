using System;
using System.Collections.Generic;
using RealRoadBuilder.Core.Design;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Terrain;

public sealed class TerrainAwareCorridorRouter
{
    private static readonly (int X, int Y)[] NeighborOffsets =
    {
        (-1, -1),
        (0, -1),
        (1, -1),
        (-1, 0),
        (1, 0),
        (-1, 1),
        (0, 1),
        (1, 1),
    };

    public CorridorRoute FindRoute(
        PlanarPoint start,
        PlanarPoint end,
        ITerrainSampler terrainSampler,
        RoadDesignRule designRule,
        CorridorSearchOptions? options = null,
        ICorridorConstraintProvider? constraintProvider = null,
        bool allowExceptionalGrade = false)
    {
        if (terrainSampler == null)
        {
            throw new ArgumentNullException(nameof(terrainSampler));
        }

        if (designRule == null)
        {
            throw new ArgumentNullException(nameof(designRule));
        }

        if (start.DistanceTo(end) <= 1e-9)
        {
            throw new ArgumentException("Corridor start and end must be different points.");
        }

        options ??= new CorridorSearchOptions();

        if (constraintProvider != null &&
            (constraintProvider.IsBlocked(start) || constraintProvider.IsBlocked(end)))
        {
            throw new InvalidOperationException("The corridor start or end point is blocked.");
        }

        double maximumGradePercent = allowExceptionalGrade
            ? designRule.ExceptionalMaximumGradePercent
            : designRule.MaximumGradePercent;

        SearchBounds searchBounds = CreateSearchBounds(start, end, options);
        SearchState startState = new(0, 0, 0, 0);
        double startElevationMeters = terrainSampler.GetElevationMeters(start);
        double endElevationMeters = terrainSampler.GetElevationMeters(end);

        Dictionary<SearchState, double> costFromStart = new();
        Dictionary<SearchState, SearchState> cameFrom = new();
        MinHeap openSet = new();

        costFromStart[startState] = 0.0;
        openSet.Enqueue(
            startState,
            EstimateRemainingCost(start, end));

        int expandedStates = 0;

        while (openSet.Count > 0)
        {
            OpenEntry currentEntry = openSet.Dequeue();
            SearchState currentState = currentEntry.State;

            if (!costFromStart.TryGetValue(currentState, out double currentCost))
            {
                continue;
            }

            PlanarPoint currentPosition = GetWorldPosition(start, currentState, options.CellSizeMeters);
            double expectedPriority = currentCost + EstimateRemainingCost(currentPosition, end);
            if (currentEntry.Priority > expectedPriority + 1e-6)
            {
                continue;
            }

            expandedStates++;
            if (expandedStates > options.MaximumExpandedStates)
            {
                throw new InvalidOperationException(
                    $"Corridor search exceeded the configured limit of {options.MaximumExpandedStates} expanded states.");
            }

            double currentElevationMeters = currentState.Equals(startState)
                ? startElevationMeters
                : terrainSampler.GetElevationMeters(currentPosition);

            if (TryCreateGoalConnection(
                    currentState,
                    currentPosition,
                    currentElevationMeters,
                    end,
                    endElevationMeters,
                    maximumGradePercent,
                    options,
                    constraintProvider,
                    out double finalSegmentCost))
            {
                List<CorridorWaypoint> routeWaypoints = ReconstructRoute(
                    start,
                    end,
                    endElevationMeters,
                    terrainSampler,
                    currentState,
                    cameFrom,
                    options.CellSizeMeters);

                return new CorridorRoute(
                    CorridorRouteSimplifier.RemoveCollinearWaypoints(routeWaypoints),
                    currentCost + finalSegmentCost,
                    expandedStates);
            }

            foreach ((int offsetX, int offsetY) in NeighborOffsets)
            {
                int nextGridX = currentState.GridX + offsetX;
                int nextGridY = currentState.GridY + offsetY;

                if (!searchBounds.Contains(nextGridX, nextGridY))
                {
                    continue;
                }

                SearchState nextState = new(nextGridX, nextGridY, offsetX, offsetY);
                PlanarPoint nextPosition = GetWorldPosition(start, nextState, options.CellSizeMeters);

                if (constraintProvider != null && constraintProvider.IsBlocked(nextPosition))
                {
                    continue;
                }

                double nextElevationMeters = terrainSampler.GetElevationMeters(nextPosition);
                double horizontalDistanceMeters = currentPosition.DistanceTo(nextPosition);
                double gradePercent = CalculateGradePercent(
                    currentElevationMeters,
                    nextElevationMeters,
                    horizontalDistanceMeters);

                if (Math.Abs(gradePercent) > maximumGradePercent + 1e-9)
                {
                    continue;
                }

                double movementCost = CalculateMovementCost(
                    currentState,
                    nextState,
                    horizontalDistanceMeters,
                    gradePercent,
                    maximumGradePercent,
                    nextPosition,
                    options,
                    constraintProvider);

                double tentativeCost = currentCost + movementCost;
                if (costFromStart.TryGetValue(nextState, out double existingCost) &&
                    tentativeCost >= existingCost - 1e-9)
                {
                    continue;
                }

                cameFrom[nextState] = currentState;
                costFromStart[nextState] = tentativeCost;
                double priority = tentativeCost + EstimateRemainingCost(nextPosition, end);
                openSet.Enqueue(nextState, priority);
            }
        }

        throw new InvalidOperationException(
            $"No corridor satisfying the {maximumGradePercent:0.###}% grade limit was found inside the configured search area.");
    }

    private static SearchBounds CreateSearchBounds(
        PlanarPoint start,
        PlanarPoint end,
        CorridorSearchOptions options)
    {
        double minimumX = Math.Min(start.X, end.X) - options.SearchMarginMeters;
        double maximumX = Math.Max(start.X, end.X) + options.SearchMarginMeters;
        double minimumY = Math.Min(start.Y, end.Y) - options.SearchMarginMeters;
        double maximumY = Math.Max(start.Y, end.Y) + options.SearchMarginMeters;

        int minimumGridX = (int)Math.Floor((minimumX - start.X) / options.CellSizeMeters);
        int maximumGridX = (int)Math.Ceiling((maximumX - start.X) / options.CellSizeMeters);
        int minimumGridY = (int)Math.Floor((minimumY - start.Y) / options.CellSizeMeters);
        int maximumGridY = (int)Math.Ceiling((maximumY - start.Y) / options.CellSizeMeters);

        return new SearchBounds(minimumGridX, maximumGridX, minimumGridY, maximumGridY);
    }

    private static bool TryCreateGoalConnection(
        SearchState currentState,
        PlanarPoint currentPosition,
        double currentElevationMeters,
        PlanarPoint end,
        double endElevationMeters,
        double maximumGradePercent,
        CorridorSearchOptions options,
        ICorridorConstraintProvider? constraintProvider,
        out double finalSegmentCost)
    {
        double distanceToEndMeters = currentPosition.DistanceTo(end);
        double connectionRadiusMeters = options.CellSizeMeters * Math.Sqrt(2.0) * 1.01;

        if (distanceToEndMeters > connectionRadiusMeters)
        {
            finalSegmentCost = 0.0;
            return false;
        }

        if (distanceToEndMeters <= 1e-9)
        {
            finalSegmentCost = 0.0;
            return true;
        }

        double gradePercent = CalculateGradePercent(
            currentElevationMeters,
            endElevationMeters,
            distanceToEndMeters);

        if (Math.Abs(gradePercent) > maximumGradePercent + 1e-9)
        {
            finalSegmentCost = 0.0;
            return false;
        }

        PlanarVector connectionVector = (end - currentPosition).Normalized();
        double turnPenalty = 0.0;

        if (currentState.DirectionX != 0 || currentState.DirectionY != 0)
        {
            PlanarVector previousDirection = new(
                currentState.DirectionX,
                currentState.DirectionY);
            turnPenalty = CalculateTurnPenalty(
                previousDirection,
                connectionVector,
                options.TurnPenaltyWeight);
        }

        double additionalCost = constraintProvider?.GetAdditionalCost(end) ?? 0.0;
        if (additionalCost < 0.0)
        {
            throw new InvalidOperationException("Corridor additional cost providers may not return negative costs.");
        }

        double normalizedGrade = maximumGradePercent <= 1e-12
            ? 0.0
            : Math.Abs(gradePercent) / maximumGradePercent;

        finalSegmentCost =
            distanceToEndMeters +
            (options.GradePenaltyWeight * distanceToEndMeters * normalizedGrade * normalizedGrade) +
            turnPenalty +
            (options.AdditionalCostWeight * additionalCost);

        return true;
    }

    private static double CalculateMovementCost(
        SearchState currentState,
        SearchState nextState,
        double distanceMeters,
        double gradePercent,
        double maximumGradePercent,
        PlanarPoint nextPosition,
        CorridorSearchOptions options,
        ICorridorConstraintProvider? constraintProvider)
    {
        double normalizedGrade = maximumGradePercent <= 1e-12
            ? 0.0
            : Math.Abs(gradePercent) / maximumGradePercent;

        double gradePenalty =
            options.GradePenaltyWeight *
            distanceMeters *
            normalizedGrade *
            normalizedGrade;

        double turnPenalty = 0.0;
        if (currentState.DirectionX != 0 || currentState.DirectionY != 0)
        {
            PlanarVector previousDirection = new(
                currentState.DirectionX,
                currentState.DirectionY);
            PlanarVector nextDirection = new(
                nextState.DirectionX,
                nextState.DirectionY);
            turnPenalty = CalculateTurnPenalty(
                previousDirection,
                nextDirection,
                options.TurnPenaltyWeight);
        }

        double additionalCost = constraintProvider?.GetAdditionalCost(nextPosition) ?? 0.0;
        if (additionalCost < 0.0)
        {
            throw new InvalidOperationException("Corridor additional cost providers may not return negative costs.");
        }

        return
            distanceMeters +
            gradePenalty +
            turnPenalty +
            (options.AdditionalCostWeight * additionalCost);
    }

    private static double CalculateTurnPenalty(
        PlanarVector previousDirection,
        PlanarVector nextDirection,
        double turnPenaltyWeight)
    {
        double angleRadians = PlanarVector.AngleBetween(previousDirection, nextDirection);
        return turnPenaltyWeight * angleRadians;
    }

    private static double CalculateGradePercent(
        double startElevationMeters,
        double endElevationMeters,
        double horizontalDistanceMeters)
    {
        if (horizontalDistanceMeters <= 1e-9)
        {
            return 0.0;
        }

        return ((endElevationMeters - startElevationMeters) / horizontalDistanceMeters) * 100.0;
    }

    private static double EstimateRemainingCost(PlanarPoint point, PlanarPoint end)
    {
        return point.DistanceTo(end);
    }

    private static PlanarPoint GetWorldPosition(
        PlanarPoint start,
        SearchState state,
        double cellSizeMeters)
    {
        return new PlanarPoint(
            start.X + (state.GridX * cellSizeMeters),
            start.Y + (state.GridY * cellSizeMeters));
    }

    private static List<CorridorWaypoint> ReconstructRoute(
        PlanarPoint start,
        PlanarPoint end,
        double endElevationMeters,
        ITerrainSampler terrainSampler,
        SearchState finalState,
        IReadOnlyDictionary<SearchState, SearchState> cameFrom,
        double cellSizeMeters)
    {
        List<SearchState> reversedStates = new();
        SearchState state = finalState;
        reversedStates.Add(state);

        while (cameFrom.TryGetValue(state, out SearchState previousState))
        {
            state = previousState;
            reversedStates.Add(state);
        }

        reversedStates.Reverse();

        List<CorridorWaypoint> waypoints = new(reversedStates.Count + 1);
        foreach (SearchState routeState in reversedStates)
        {
            PlanarPoint position = GetWorldPosition(start, routeState, cellSizeMeters);
            double elevationMeters = routeState.GridX == 0 && routeState.GridY == 0
                ? terrainSampler.GetElevationMeters(start)
                : terrainSampler.GetElevationMeters(position);
            waypoints.Add(new CorridorWaypoint(position, elevationMeters));
        }

        CorridorWaypoint lastWaypoint = waypoints[waypoints.Count - 1];
        if (lastWaypoint.Position.DistanceTo(end) > 1e-9)
        {
            waypoints.Add(new CorridorWaypoint(end, endElevationMeters));
        }

        return waypoints;
    }

    private readonly struct SearchBounds
    {
        public SearchBounds(
            int minimumGridX,
            int maximumGridX,
            int minimumGridY,
            int maximumGridY)
        {
            MinimumGridX = minimumGridX;
            MaximumGridX = maximumGridX;
            MinimumGridY = minimumGridY;
            MaximumGridY = maximumGridY;
        }

        public int MinimumGridX { get; }

        public int MaximumGridX { get; }

        public int MinimumGridY { get; }

        public int MaximumGridY { get; }

        public bool Contains(int gridX, int gridY)
        {
            return
                gridX >= MinimumGridX &&
                gridX <= MaximumGridX &&
                gridY >= MinimumGridY &&
                gridY <= MaximumGridY;
        }
    }

    private readonly struct SearchState : IEquatable<SearchState>
    {
        public SearchState(int gridX, int gridY, int directionX, int directionY)
        {
            GridX = gridX;
            GridY = gridY;
            DirectionX = directionX;
            DirectionY = directionY;
        }

        public int GridX { get; }

        public int GridY { get; }

        public int DirectionX { get; }

        public int DirectionY { get; }

        public bool Equals(SearchState other)
        {
            return
                GridX == other.GridX &&
                GridY == other.GridY &&
                DirectionX == other.DirectionX &&
                DirectionY == other.DirectionY;
        }

        public override bool Equals(object? obj)
        {
            return obj is SearchState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = GridX;
                hashCode = (hashCode * 397) ^ GridY;
                hashCode = (hashCode * 397) ^ DirectionX;
                hashCode = (hashCode * 397) ^ DirectionY;
                return hashCode;
            }
        }
    }

    private readonly struct OpenEntry
    {
        public OpenEntry(SearchState state, double priority)
        {
            State = state;
            Priority = priority;
        }

        public SearchState State { get; }

        public double Priority { get; }
    }

    private sealed class MinHeap
    {
        private readonly List<OpenEntry> _entries = new();

        public int Count => _entries.Count;

        public void Enqueue(SearchState state, double priority)
        {
            OpenEntry entry = new(state, priority);
            _entries.Add(entry);
            int index = _entries.Count - 1;

            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (_entries[parentIndex].Priority <= _entries[index].Priority)
                {
                    break;
                }

                (_entries[parentIndex], _entries[index]) =
                    (_entries[index], _entries[parentIndex]);
                index = parentIndex;
            }
        }

        public OpenEntry Dequeue()
        {
            if (_entries.Count == 0)
            {
                throw new InvalidOperationException("Cannot dequeue from an empty heap.");
            }

            OpenEntry result = _entries[0];
            int lastIndex = _entries.Count - 1;
            _entries[0] = _entries[lastIndex];
            _entries.RemoveAt(lastIndex);

            int index = 0;
            while (true)
            {
                int leftChildIndex = (index * 2) + 1;
                int rightChildIndex = leftChildIndex + 1;

                if (leftChildIndex >= _entries.Count)
                {
                    break;
                }

                int smallestChildIndex = leftChildIndex;
                if (rightChildIndex < _entries.Count &&
                    _entries[rightChildIndex].Priority < _entries[leftChildIndex].Priority)
                {
                    smallestChildIndex = rightChildIndex;
                }

                if (_entries[index].Priority <= _entries[smallestChildIndex].Priority)
                {
                    break;
                }

                (_entries[index], _entries[smallestChildIndex]) =
                    (_entries[smallestChildIndex], _entries[index]);
                index = smallestChildIndex;
            }

            return result;
        }
    }
}
