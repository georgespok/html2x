using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Box;

namespace Html2x.LayoutEngine.Fragment;

public sealed record BlockFragmentBinding(BlockBox Source, BlockFragment Fragment);
