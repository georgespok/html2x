# Testing Guidelines

This document outlines testing standards for Html2x.

### Foundational Practices

- Code must be developed using incremental TDD: introduce one failing test at a time, implement the minimal passing code, and refactor before adding the next test. Trivial scaffolding (constructors, simple properties, passive DTOs) is exempt.
- Tests MUST exercise observable behavior (e.g., rendered output, pagination results) and MUST NOT rely on reflection-based contract checks of internal types.
- Reflection APIs (e.g., `Activator.CreateInstance`, `Type.GetType`, `MethodInfo.Invoke`) are explicitly prohibited in test code.

### Test Strategy and Coverage Discipline
Tests are first-class and focus on observable behavior, not implementation details. Code MUST follow incremental TDD: introduce one failing test, implement the minimal passing code, then refactor before the next test. Trivial scaffolding (constructors, simple properties, passive DTOs) is exempt. Tests MUST exercise outcomes such as rendered output, pagination results, logging, or API responses and MUST NOT rely on reflection-based contract checks. Reflection APIs (e.g., Activator.CreateInstance, Type.GetType, MethodInfo.Invoke) are prohibited in test code. Prioritize tests for business logic and complex flows, use parameterized tests for multi-scenario logic, and keep tests independent and readable. Each layer/module MUST be testable in isolation.
Rationale: Guarantees quality, documents behavior, and enables safe refactoring.

## Test Organization

**Tests MUST be treated with the same quality standards as production code.**

### Clean Code Principles for Tests

- **SOLID**: Apply Single Responsibility, Open/Closed, and other SOLID principles
- **DRY**: Don't Repeat Yourself - eliminate code duplication
- **KISS**: Keep It Simple, Stupid - avoid over-engineering tests
- **Readability**: Tests should be easy to read and understand

### Theory Tests Priority

- **Theory tests (`[Theory]` with `[InlineData]`) are PRIORITY for parameterized testing scenarios.** Analyze test classes regularly to ensure optimal organization using xUnit features.
- **Regular Analysis**: Analyze test classes regularly to ensure optimal organization using xUnit features
- **Consolidation**: Consolidate similar test scenarios using `[Theory]` with `[InlineData]`, `[MemberData]`, or `[ClassData]`
- **Helper Methods**: Create helper methods to eliminate repetitive arrange sections


#### When to Use Theory Tests
- ✅ **Use `[Theory]` with `[InlineData]`**: For simple parameterized tests with 2-5 parameters
- ✅ **Use `[Theory]` with `[MemberData]`**: For complex test data or dynamic data generation
- ✅ **Use `[Theory]` with `[ClassData]`**: For reusable test data classes
- ✅ **Consolidate Similar Scenarios**: Multiple test cases that follow the same pattern
- ❌ **Don't Use `[Fact]`**: For scenarios that can be parameterized

## Test Quality

- **Short Tests**: Keep test methods short and focused (ideally under 15 lines)
- **Clean Tests**: Apply the same clean code principles to test code
- **Descriptive Names**: Use descriptive test method names following `MethodName_Scenario_ExpectedResult` pattern
- **Functional Organization**: Organize tests by functionality, not by implementation details

## Test Coverage Goals

- Unit tests for core logic
- Integration tests for end-to-end conversion pipeline
- Deterministic tests (same input → same output across platforms)

## Integration Testing Strategy

Integration tests focus on **cross-module communication** with the goal of minimal number of tests and maximum coverage of important interactions.

#### Implementation Guidelines

1. **Focus on major use cases** rather than individual method combinations
2. **Test end-to-end workflows** that span multiple modules
3. **Verify integration contracts** between components
4. **Minimize test duplication** with unit tests
5. **Use real or realistic dependencies** where appropriate
6. **Test error propagation** across module boundaries