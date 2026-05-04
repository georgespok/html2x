using System.Text;
using Html2x.RenderModel;
using SkiaSharp;

namespace Html2x.Resources;


internal enum ImageResourceStatus
{
    Ok,
    Missing,
    Oversize,
    InvalidDataUri,
    DecodeFailed,
    OutOfScope
}
