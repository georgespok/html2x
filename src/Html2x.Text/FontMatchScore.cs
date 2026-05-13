namespace Html2x.Text;

internal readonly record struct FontMatchScore(
    int SlantMismatch,
    int WeightDistance,
    string Family,
    string FilePath,
    int FaceIndex);
