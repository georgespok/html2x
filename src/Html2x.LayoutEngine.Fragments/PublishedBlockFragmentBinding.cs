using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Published;

namespace Html2x.LayoutEngine.Fragments;

internal sealed record PublishedBlockFragmentBinding(PublishedBlock Source, BlockFragment Fragment);
