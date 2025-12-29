# Layout Services Contract

## Purpose
Provide layout dependencies via composition without exposing renderer or font implementation details.

## Contents
- Text measurer
- Font source (if measurer does not encapsulate resolution)

## Constraints
- Layout depends only on abstractions
- Services must be injectable for deterministic tests
