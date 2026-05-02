namespace Html2x.Text;

internal readonly record struct MatchScore(
    int SlantMismatch,
    int WeightDistance,
    string Family,
    string Path,
    int FaceIndex);
