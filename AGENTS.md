# AGENTS.md — CodebaseDumper

## Tech stack
- C# 13 · .NET 9 · WPF (MVVM, no code-behind logic)
- xUnit for tests · System.IO.Abstractions.TestingHelpers for FS mocking
- No NuGet dependencies beyond the above without explicit approval

## Commands
- Build:   dotnet build
- Test:    dotnet test
- Publish: dotnet publish CodebaseDumper -f net9.0-windows -r win-x64 -p:PublishSingleFile=true --self-contained

## Code conventions
- Interface-first: define the interface before the implementation
- Domain types are C# records (immutable by default)
- All async methods accept CancellationToken as last parameter
- Return IReadOnlyList<T>, never null — empty list means "no results"
- Exceptions: let typed exceptions propagate; never catch-and-swallow
- MVVM: zero logic in .xaml.cs files; all state in ViewModel
- Namespace: CodebaseDumper.Models / .Engine / .ViewModels / .Views
- Code comments: Tất cả comment trong code (// ..., /// XML doc, <!-- XAML -->) phải viết bằng tiếng Việt

## Boundaries
- Never modify files outside the task's listed "Files to touch"
- Never add features not in the task's acceptance criteria
- If a requirement is ambiguous, output a CONFUSION block and stop
- Always run `dotnet test` before declaring a task complete

## Key design doc
See docs/design.md for full spec, interfaces, and state matrix.