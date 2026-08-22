using System;

namespace RealRoadBuilder.Core.Geometry;

public readonly struct PlanarVector : IEquatable<PlanarVector>
{
    private const double Epsilon = 1e-12;

    public PlanarVector(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }

    public double Y { get; }

    public double Length => Math.Sqrt((X * X) + (Y * Y));

    public double LengthSquared => (X * X) + (Y * Y);

    public PlanarVector Normalized()
    {
        double length = Length;
        if (length <= Epsilon)
        {
            throw new InvalidOperationException("Cannot normalize a zero-length vector.");
        }

        return new PlanarVector(X / length, Y / length);
    }

    public PlanarVector LeftNormal()
    {
        return new PlanarVector(-Y, X);
    }

    public PlanarVector RightNormal()
    {
        return new PlanarVector(Y, -X);
    }

    public static double Dot(PlanarVector first, PlanarVector second)
    {
        return (first.X * second.X) + (first.Y * second.Y);
    }

    public static double Cross(PlanarVector first, PlanarVector second)
    {
        return (first.X * second.Y) - (first.Y * second.X);
    }

    public static double AngleBetween(PlanarVector first, PlanarVector second)
    {
        PlanarVector firstNormalized = first.Normalized();
        PlanarVector secondNormalized = second.Normalized();
        double cosine = Dot(firstNormalized, secondNormalized);
        cosine = Math.Max(-1.0, Math.Min(1.0, cosine));
        return Math.Acos(cosine);
    }

    public bool Equals(PlanarVector other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y);
    }

    public override bool Equals(object? obj)
    {
        return obj is PlanarVector other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }
    }

    public static PlanarVector operator +(PlanarVector first, PlanarVector second)
    {
        return new PlanarVector(first.X + second.X, first.Y + second.Y);
    }

    public static PlanarVector operator -(PlanarVector first, PlanarVector second)
    {
        return new PlanarVector(first.X - second.X, first.Y - second.Y);
    }

    public static PlanarVector operator -(PlanarVector vector)
    {
        return new PlanarVector(-vector.X, -vector.Y);
    }

    public static PlanarVector operator *(PlanarVector vector, double scalar)
    {
        return new PlanarVector(vector.X * scalar, vector.Y * scalar);
    }

    public static PlanarVector operator *(double scalar, PlanarVector vector)
    {
        return vector * scalar;
    }

    public static PlanarVector operator /(PlanarVector vector, double scalar)
    {
        if (Math.Abs(scalar) <= Epsilon)
        {
            throw new DivideByZeroException("Cannot divide a vector by zero.");
        }

        return new PlanarVector(vector.X / scalar, vector.Y / scalar);
    }

    public override string ToString()
    {
        return $"<{X:0.###}, {Y:0.###}>";
    }
}
