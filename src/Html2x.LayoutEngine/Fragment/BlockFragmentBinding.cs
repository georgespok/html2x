using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment;

public sealed record BlockFragmentBinding(BlockBox Source, BlockFragment Fragment);
