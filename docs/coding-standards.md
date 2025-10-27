# Coding Standards

This document outlines the code quality standards for Html2x.

## Clean Code Principles

- **SOLID**: Apply Single Responsibility, Open/Closed, and other SOLID principles
- **DRY**: Don't Repeat Yourself - eliminate code duplication
- **KISS**: Keep It Simple, Stupid - avoid over-engineering
- **Readability**: Code should be easy to read and understand

## Code Organization

- **Separation of Concerns**: Keep parsing and rendering logic separate
- **Extensibility**: Design for adding new renderers without modifying core
- **Platform Independence**: Pure .NET, no platform-specific dependencies

## Additional Guidelines

- Use meaningful names for variables, methods, and classes
- Keep methods focused and small
- Add XML documentation comments for public APIs
- Follow .NET naming conventions (PascalCase for public members, camelCase for private)