using System;

namespace RealRoadBuilder.Core.Geometry;

public readonly struct PlanarPoint : IEquatable<PlanarPoint>
{
    public PlanarPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }

    public double Y { get; }

    public double DistanceTo(PlanarPoint other)
    {
        double deltaX = other.X - X;
        double deltaY = other.Y - Y;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    public bool Equals(PlanarPoint other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y);
    }

    public override bool Equals(object? obj)
    {
        return obj is PlanarPoint other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }
    }

    public static PlanarVector operator -(PlanarPoint end, PlanarPoint start)
    {
        return new PlanarVector(end.X - start.X, end.Y - start.Y);
    }

    public static PlanarPoint operator +(PlanarPoint point, PlanarVector vector)
    {
        return new PlanarPoint(point.X + vector.X, point.Y + vector.Y);
    }

    public static PlanarPoint operator -(PlanarPoint point, PlanarVector vector)
    {
        return new PlanarPoint(point.X - vector.X, point.Y - vector.Y);
    }

    public override string ToString()
    {
        return $"({X:0.###}, {Y:0.###})";
    }
}
