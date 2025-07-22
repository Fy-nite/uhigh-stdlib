# Patch Notes: Inline Test Framework Enhancements

## New Features

- **[Call] Attribute**  
  Allows testing method return values and arguments directly.

- **[Timeout] Attribute**  
  Specify a maximum execution time for test methods.

- **[TestCategory] Attribute**  
  Tag and group tests for easier organization and filtering.

- **[Ignore] Attribute**  
  Mark tests to be skipped during test runs.

- **[Setup] and [Cleanup] Attributes**  
  Run setup/cleanup methods before/after each test in a class.

- **[CustomComparer] Attribute**  
  Use custom equality comparers for complex result or field comparisons.

- **[TestOutput] Attribute**  
  Capture and verify expected console output from test methods.

## Improvements

- Test runner now supports more flexible and expressive test scenarios.
- Example test class (`StdLib.Examples.tests`) demonstrates all new features.

## Usage

See `inlinetests.examples.cs` for sample usage of all new attributes.
