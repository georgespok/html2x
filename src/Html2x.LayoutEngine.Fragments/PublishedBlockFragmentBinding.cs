using Html2x.LayoutEngine.Contracts.Published;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Fragments;

internal sealed record PublishedBlockFragmentBinding(PublishedBlock Source, BlockFragment Fragment);