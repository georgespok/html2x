namespace Html2x.Abstractions.Images;

public interface IImageProvider
{
    ImageLoadResult Load(string src, string baseDirectory, long maxBytes);
}
