using System;
using System.Collections.Generic;
using System.Linq;
using RealRoadBuilder.Core.Geometry;

namespace RealRoadBuilder.Core.Alignment;

public sealed class HorizontalAlignment
{
    private const double ContinuityToleranceMeters = 1e-5;
    private readonly IReadOnlyList<HorizontalAlignmentElement> _elements;

    public HorizontalAlignment(IEnumerable<HorizontalAlignmentElement> elements)
    {
        if (elements == null)
        {
            throw new ArgumentNullException(nameof(elements));
        }

        List<HorizontalAlignmentElement> materializedElements = elements.ToList();
        if (materializedElements.Count == 0)
        {
            throw new ArgumentException("An alignment requires at least one element.", nameof(elements));
        }

        for (int index = 1; index < materializedElements.Count; index++)
        {
            HorizontalAlignmentElement previous = materializedElements[index - 1];
            HorizontalAlignmentElement current = materializedElements[index];
            if (previous.End.DistanceTo(current.Start) > ContinuityToleranceMeters)
            {
                throw new ArgumentException(
                    $"Alignment elements {index - 1} and {index} are not continuous.",
                    nameof(elements));
            }
        }

        _elements = materializedElements.AsReadOnly();
        TotalLengthMeters = materializedElements.Sum(element => element.LengthMeters);
    }

    public IReadOnlyList<HorizontalAlignmentElement> Elements => _elements;

    public PlanarPoint Start => _elements[0].Start;

    public PlanarPoint End => _elements[_elements.Count - 1].End;

    public double TotalLengthMeters { get; }

    public IEnumerable<CircularArcElement> Curves => _elements.OfType<CircularArcElement>();

    public IEnumerable<TransitionSpiralElement> TransitionSpirals =>
        _elements.OfType<TransitionSpiralElement>();
}
