using RealRoadBuilder.Core.Alignment;
using RealRoadBuilder.Core.Scoring;
using RealRoadBuilder.Core.Validation;

namespace RealRoadBuilder.Core.Generation;

public sealed class AlignmentCandidate
{
    public AlignmentCandidate(
        string name,
        HorizontalAlignment alignment,
        AlignmentValidationResult validation,
        AlignmentScore score)
    {
        Name = name;
        Alignment = alignment;
        Validation = validation;
        Score = score;
    }

    public string Name { get; }

    public HorizontalAlignment Alignment { get; }

    public AlignmentValidationResult Validation { get; }

    public AlignmentScore Score { get; }
}
