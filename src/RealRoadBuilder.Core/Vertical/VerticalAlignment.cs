using System;
using System.Collections.Generic;
using System.Linq;

namespace RealRoadBuilder.Core.Vertical;

public sealed class VerticalAlignment
{
    private const double ContinuityTolerance = 1e-5;
    private readonly IReadOnlyList<VerticalAlignmentElement> _elements;

    public VerticalAlignment(IEnumerable<VerticalAlignmentElement> elements)
    {
        if (elements == null)
        {
            throw new ArgumentNullException(nameof(elements));
        }

        List<VerticalAlignmentElement> materializedElements = elements.ToList();
        if (materializedElements.Count == 0)
        {
            throw new ArgumentException("A vertical alignment requires at least one element.", nameof(elements));
        }

        for (int index = 1; index < materializedElements.Count; index++)
        {
            VerticalAlignmentElement previous = materializedElements[index - 1];
            VerticalAlignmentElement current = materializedElements[index];

            if (Math.Abs(previous.EndStationMeters - current.StartStationMeters) > ContinuityTolerance)
            {
                throw new ArgumentException(
                    $"Vertical alignment elements {index - 1} and {index} have a station gap or overlap.",
                    nameof(elements));
            }

            if (Math.Abs(previous.EndElevationMeters - current.StartElevationMeters) > ContinuityTolerance)
            {
                throw new ArgumentException(
                    $"Vertical alignment elements {index - 1} and {index} are not elevation-continuous.",
                    nameof(elements));
            }
        }

        _elements = materializedElements.AsReadOnly();
    }

    public IReadOnlyList<VerticalAlignmentElement> Elements => _elements;

    public double StartStationMeters => _elements[0].StartStationMeters;

    public double EndStationMeters => _elements[_elements.Count - 1].EndStationMeters;

    public double TotalLengthMeters => EndStationMeters - StartStationMeters;

    public IEnumerable<ParabolicVerticalCurveElement> Curves =>
        _elements.OfType<ParabolicVerticalCurveElement>();
}
