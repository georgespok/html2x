using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Geometry.Published;

namespace Html2x.LayoutEngine.Fragments;

internal sealed record PublishedBlockFragmentBinding(PublishedBlock Source, BlockFragment Fragment);
