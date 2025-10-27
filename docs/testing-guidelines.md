# Testing Guidelines

This document outlines testing standards for Html2x.

## TDD Approach

Write ONE failing test → implement minimal code to pass → refactor → repeat

## Test Organization

- **Theory Tests Priority**: Theory tests (`[Theory]` with `[InlineData]`) are PRIORITY for parameterized testing scenarios
- **Regular Analysis**: Analyze test classes regularly to ensure optimal organization using xUnit features
- **Consolidation**: Consolidate similar test scenarios using `[Theory]` with `[InlineData]`, `[MemberData]`, or `[ClassData]`
- **Helper Methods**: Create helper methods to eliminate repetitive arrange sections

## Test Quality

- **Short Tests**: Keep test methods short and focused (ideally under 15 lines)
- **Clean Tests**: Apply the same clean code principles to test code
- **Descriptive Names**: Use descriptive test method names following `MethodName_Scenario_ExpectedResult` pattern
- **Functional Organization**: Organize tests by functionality, not by implementation details

## Test Coverage Goals

- Unit tests for core logic
- Integration tests for end-to-end conversion pipeline
- Deterministic tests (same input → same output across platforms)