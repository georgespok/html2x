namespace Html2x.Renderers.Pdf.Drawing;

internal readonly record struct MatchScore(
    int SlantMismatch,
    int WeightDistance,
    string Family,
    string Path,
    int FaceIndex);