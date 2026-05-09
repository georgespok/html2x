using Html2x.RenderModel.Fragments;

namespace Html2x.Resources;

internal interface IImageResourceReader
{
    ImageResourceResult Load(string src);
}
