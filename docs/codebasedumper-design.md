# CodebaseDumper — design document

## 1 · idea-refine

### Problem statement
*How might we let any Windows developer instantly export their codebase as a
single readable text file — without bash, WSL, or command knowledge — so they
can share rich context with AI tools like Claude or ChatGPT?*

### Variations explored
Before committing to one direction, five alternatives were evaluated:

| Direction | Core idea | Why rejected / deferred |
|---|---|---|
| PowerShell script + config file | Zero-install, scriptable | No GUI — user confirmed GUI is the goal |
| Windows context-menu shell extension | Right-click any folder → dump | Requires registry editing; high install friction for OSS users |
| VS Code extension | Integrated into editor, no separate window | Scoped to VS Code users only; excludes editors like Rider, Notepad++ |
| **WPF GUI + preview panel** | Click-and-go, visual feedback | **Chosen** |
| Electron / Tauri cross-platform | Works on Mac/Linux too | Overkill — user confirmed Windows public release |

**Why WPF over WinForms:** WPF has proper data-binding (MVVM), better
accessibility APIs (`AutomationProperties`), and ships a nicer-looking app to
a public OSS audience without third-party UI libraries.

**Unique differentiator identified:** Token-count estimate displayed before
export. No comparable free Windows tool shows this. It directly answers the
AI-context use case: "will this fit in Claude's context window?"

### Key assumptions to validate
- [ ] Users want txt output, not markdown with code fences (markdown would render
  better in some AI tools — test with target users)
- [ ] Single-file output is preferred (large repos may need chunked output —
  add file-size warning if >2 MB)
- [ ] Token estimate at ~4 chars/token is accurate enough (validate against
  tiktoken for GPT and Claude's tokenizer)
- [ ] Extension presets (JS/TS, Python, C#, Go) cover 80% of user projects

### MVP scope — what's in
- Folder picker (FolderBrowserDialog)
- Extension filter with preset buttons (JS/TS · Python · C# · Go)
- Exclude dirs (defaults: node_modules, dist, .git, bin, obj, __pycache__)
- Preview panel: ASCII tree + file count + estimated token count
- Export to txt with progress bar + cancel
- "Open file" button after export completes
- Self-contained single .exe (no runtime install needed)

### Not doing — and why
| Feature | Reason |
|---|---|
| CLI mode | User confirmed GUI-only |
| Copy-to-clipboard | Not selected in requirements |
| Save/load profiles | Out of MVP scope; adds persistence complexity |
| Markdown / JSON output formats | Second release if user demand exists |
| Git diff mode (changed files only) | Separate product scope |
| Cloud share / upload | Out of scope entirely |

---

## 2 · api-and-interface-design

All C# contracts are defined here **before** any implementation is written.
The types are the spec.

### Domain types

```csharp
/// Represents a single matched file.
record FileEntry(
    string RelativePath,   // e.g. "src/utils/helper.js"
    string AbsolutePath,   // full Windows path
    long   SizeBytes
);

/// Immutable configuration snapshot passed through the pipeline.
record DumpConfig(
    string                 RootPath,
    IReadOnlyList<string>  IncludeGlobs,   // e.g. ["*.js", "*.json"]
    IReadOnlyList<string>  ExcludeDirs,    // e.g. ["node_modules", "dist"]
    string                 OutputPath,
    Encoding               OutputEncoding  // default: UTF-8
);

/// Progress update emitted during export.
record DumpProgress(
    int    ProcessedFiles,
    int    TotalFiles,
    string CurrentFile     // relative path currently being written
);

/// Data shown in the preview panel.
record PreviewData(
    string                 AsciiTree,
    IReadOnlyList<FileEntry> Files,
    int                    EstimatedTokens,  // ≈ totalChars / 4
    long                   TotalBytes
);

/// Result returned after a successful export.
record DumpResult(
    string   OutputPath,
    int      FileCount,
    int      EstimatedTokens,
    long     TotalBytes,
    TimeSpan Duration
);
```

### Module contracts

```csharp
interface IFileScanner
{
    /// Scans RootPath, returns files matching IncludeGlobs,
    /// excluding any path that contains an ExcludeDirs segment.
    /// Returns empty list (never null) when no files match.
    /// Throws: DirectoryNotFoundException, UnauthorizedAccessException
    IReadOnlyList<FileEntry> Scan(DumpConfig config);
}

interface ITreeBuilder
{
    /// Produces ASCII tree using ├──, └──, │ characters.
    /// Directories shown as "name/", files as "name.ext".
    string Build(IReadOnlyList<FileEntry> files, string rootPath);
}

interface ITokenEstimator
{
    /// Approximates token count using cl100k_base heuristic (~4 chars/token).
    int Estimate(long totalCharacters);
}

interface IPreviewProvider
{
    /// Combines scanner + tree + token estimate into one PreviewData.
    /// Throws: DirectoryNotFoundException if RootPath is invalid.
    /// Honours cancellation token.
    Task<PreviewData> BuildAsync(DumpConfig config, CancellationToken ct);
}

interface IDumpWriter
{
    /// Writes: header block + ASCII tree + per-file sections.
    /// Format per file: "===== FILE: {RelativePath} =====\n{content}\n"
    /// Throws: IOException on write failure.
    /// Throws: OperationCanceledException if ct is triggered.
    Task<DumpResult> WriteAsync(
        DumpConfig                config,
        IReadOnlyList<FileEntry>  files,
        string                    asciiTree,
        IProgress<DumpProgress>   progress,
        CancellationToken         ct
    );
}
```

### Error semantics
- All async methods accept `CancellationToken` — no blocking calls.
- File-system errors surface as typed exceptions, never swallowed or wrapped
  in generic `Exception`.
- No nullable return values — empty `IReadOnlyList<FileEntry>` means
  "no files found", not null.
- `IDumpWriter` never partially writes — if cancelled mid-write, the output
  file is deleted before throwing `OperationCanceledException`.

---

## 3 · planning-and-task-breakdown

### Dependency graph

```
DumpConfig (data model) ─── no dependencies
        │
        ├── IFileScanner ─────────────────────────────────┐
        │                                                  ↓
        ├── ITreeBuilder ────────────────────────── IPreviewProvider
        │                                                  │
        ├── ITokenEstimator ──────────────────────────────┘
        │
        └── IDumpWriter
                │
                ▼
           MainViewModel ← IPreviewProvider + IDumpWriter
                │
                ▼
           MainWindow (WPF bindings)
```

Implementation order: bottom of the graph first. Never write UI before
the interfaces it depends on exist and are tested.

### Slicing strategy
Work is sliced **vertically** — each slice delivers working, testable
functionality visible to the user, not a horizontal layer.

| Slice | User-visible outcome |
|---|---|
| 1 | User can pick a folder and see a file list |
| 2 | User can change extension filters and see the list update |
| 3 | User can see ASCII tree preview with token count |
| 4 | User can export with progress and cancel |
| 5 | "Open file" button appears after export; single .exe published |

### Tasks

#### Phase 1 — Core engine

**Task 1 · `DumpConfig` + `FileEntry` domain models** `[XS · 1 file]`

Description: Define the two core records that flow through the entire pipeline.
No logic, just types.

Acceptance criteria:
- [ ] Both records compile with all properties immutable
- [ ] `DumpConfig` has sensible defaults for `ExcludeDirs` and `OutputEncoding`
- [ ] No nullable reference types on any property

Verification: Project builds. Zero warnings.

Dependencies: None.

Files: `src/Models/DumpConfig.cs`

---

**Task 2 · `FileScanner` implementation** `[S · 2 files]`

Description: Walk the directory tree using `DirectoryInfo.EnumerateFiles`
with `SearchOption.AllDirectories`. Filter by glob patterns and exclude
directory segments.

Acceptance criteria:
- [ ] Returns files matching `IncludeGlobs`, sorted by `RelativePath`
- [ ] Excludes any file whose path contains a segment listed in `ExcludeDirs`
- [ ] Returns empty list (not null) when no files match
- [ ] Throws `DirectoryNotFoundException` for a missing `RootPath`
- [ ] Does not throw for inaccessible subdirectories (logs and skips)

Verification:
- [ ] Unit tests using `System.IO.Abstractions.TestingHelpers`
- [ ] Tests cover: matching, exclusion, empty result, invalid root

Files: `src/Engine/FileScanner.cs`, `tests/Engine/FileScannerTests.cs`

---

**Task 3 · `TreeBuilder` implementation** `[S · 2 files]`

Description: Convert a flat `IReadOnlyList<FileEntry>` into an ASCII tree
string. Group by directory. Use ├──, └──, │ box-drawing characters.

Acceptance criteria:
- [ ] Directories listed before files within each group
- [ ] Uses `├──` for non-last items, `└──` for last
- [ ] Empty file list produces just the root line `./`
- [ ] No trailing whitespace on any line

Verification:
- [ ] Snapshot tests: known file list → known tree string
- [ ] Edge case: single file, deeply nested file, unicode filenames

Files: `src/Engine/TreeBuilder.cs`, `tests/Engine/TreeBuilderTests.cs`

---

**☑ Checkpoint 1** — after Tasks 1–3
- [ ] All tests pass: `dotnet test`
- [ ] Build succeeds: `dotnet build`
- [ ] Review tree output format with human before proceeding

---

#### Phase 2 — Preview pipeline

**Task 4 · `TokenEstimator` + `PreviewProvider`** `[S · 3 files]`

Description: `TokenEstimator` wraps the `ceil(chars / 4)` formula.
`PreviewProvider` orchestrates `IFileScanner` + `ITreeBuilder` + `ITokenEstimator`
into a single `Task<PreviewData>`.

Acceptance criteria:
- [ ] Token estimate is `(int)Math.Ceiling(totalChars / 4.0)`
- [ ] `PreviewData.TotalBytes` = sum of all `FileEntry.SizeBytes`
- [ ] `PreviewData.AsciiTree` matches `ITreeBuilder.Build` output
- [ ] Cancellation token propagated to all async calls
- [ ] Throws `DirectoryNotFoundException` if `RootPath` invalid

Verification:
- [ ] Unit tests for `TokenEstimator` (boundary values)
- [ ] Unit tests for `PreviewProvider` with mocked dependencies

Files: `src/Engine/TokenEstimator.cs`, `src/Engine/PreviewProvider.cs`,
       `tests/Engine/PreviewProviderTests.cs`

---

**Task 5 · `MainViewModel` state machine** `[M · 2 files]`

Description: MVVM ViewModel with explicit `AppState` enum. All state
transitions guarded. Commands wired to `IPreviewProvider` and `IDumpWriter`.

```csharp
enum AppState { Idle, Scanning, Previewed, Exporting, Done, Error }
```

Acceptance criteria:
- [ ] `ExportCommand.CanExecute` returns true only in `Previewed` state
- [ ] `CancelCommand.CanExecute` returns true only in `Exporting` state
- [ ] `ScanCommand` triggered automatically when `RootPath` changes
- [ ] `ErrorMessage` populated (never null) in `Error` state
- [ ] All state transitions fire `INotifyPropertyChanged`

Verification:
- [ ] ViewModel unit tests with no WPF dependency (no `Application.Current`)
- [ ] Test each valid transition and each invalid transition (should no-op)

Files: `src/ViewModels/MainViewModel.cs`,
       `tests/ViewModels/MainViewModelTests.cs`

---

**☑ Checkpoint 2** — after Tasks 4–5
- [ ] Run `PreviewProvider` against a real project folder in a console harness
- [ ] Verify token estimate looks reasonable
- [ ] ViewModel state transitions verified by unit tests

---

#### Phase 3 — WPF UI

**Task 6 · `MainWindow` layout + `FolderPickerControl`** `[M · 3 files]`

Description: Two-column layout (config left 280 px, preview right). Folder
picker control with path display and browse button.

Acceptance criteria:
- [ ] Tab order: `PathTextBox → BrowseButton → ExtensionInput → ExcludeInput → ExportButton`
- [ ] `BrowseButton` opens `FolderBrowserDialog` on click, Space, and Enter
- [ ] Selecting a folder updates `ViewModel.RootPath` and triggers scan
- [ ] `PathTextBox` shows placeholder "Select a project folder…" when empty
- [ ] Window minimum size: 720 × 520 px

Verification:
- [ ] Manual: navigate entire window using keyboard only (no mouse)
- [ ] Narrator reads all labels correctly

Files: `src/Views/MainWindow.xaml`, `src/Views/MainWindow.xaml.cs`,
       `src/Views/Controls/FolderPickerControl.xaml`

---

**Task 7 · `FilterPanel` component** `[S · 2 files]`

Description: Extension chip list (removable pills + text input to add).
Preset buttons that populate defaults. Exclude dirs chip list (same pattern).

Acceptance criteria:
- [ ] Preset "JS/TS" populates: `["*.js","*.ts","*.jsx","*.tsx","*.json"]`
- [ ] Preset "Python" populates: `["*.py","*.toml","*.cfg","*.ini"]`
- [ ] Preset "C#" populates: `["*.cs","*.csproj","*.sln","*.json"]`
- [ ] Pressing Enter in text input adds chip; field clears
- [ ] "×" button on chip removes it and updates `ViewModel.IncludeGlobs`
- [ ] Each "×" button has `AutomationProperties.Name = "Remove {glob}"`

Verification:
- [ ] Manual add/remove test for each chip type
- [ ] Narrator announces chip removal correctly

Files: `src/Views/Controls/FilterPanel.xaml`,
       `src/Views/Controls/ChipListControl.xaml`

---

**Task 8 · `PreviewPanel` component** `[S · 2 files]`

Description: Shows state-appropriate content. Monospace tree in a
`ScrollViewer`. Stats bar with file count, token estimate, size.

Acceptance criteria:
- [ ] `Idle` state: shows "Select a folder to preview" placeholder
- [ ] `Scanning` state: shows spinner with "Scanning…" label
- [ ] `Previewed` / `Exporting` / `Done` state: tree text + stats bar
- [ ] Stats bar format: `"{N} files  ·  ~{K}K tokens  ·  {X} KB"`
- [ ] Token count shown as whole number (no decimals)
- [ ] `Error` state: shows error message in danger color with retry link
- [ ] `ProgressBar` has `AutomationProperties.LiveSetting = Polite`

Verification:
- [ ] Manual: cycle through all 5 states visually
- [ ] Dark mode: all text readable against background

Files: `src/Views/Controls/PreviewPanel.xaml`,
       `src/Views/Controls/PreviewPanel.xaml.cs`

---

**Task 9 · `ExportButton` + progress + completion** `[S · 1 file]`

Description: Action bar at bottom of window. Export button, progress bar,
cancel, open-file — each visible only in the right state.

Acceptance criteria:
- [ ] Export button disabled in `Idle` and `Scanning` states
- [ ] `ProgressBar` visible only during `Exporting`; shows 0–100%
- [ ] Cancel button visible only during `Exporting`; cancels and returns to `Previewed`
- [ ] "Open file" button visible only in `Done` state; opens output in Explorer
- [ ] On export error: `Error` state shown with retry button (re-enters `Previewed`)

Verification:
- [ ] Manual: cancel mid-export, verify partial file is deleted
- [ ] Manual: export completes, "Open file" opens correct path

Files: `src/Views/Controls/ActionBar.xaml`

---

**☑ Checkpoint 3** — after Tasks 6–9
- [ ] Full flow: pick folder → preview → export → open file
- [ ] Keyboard-only navigation confirmed
- [ ] No console errors or binding warnings in debug output

---

#### Phase 4 — Polish + release

**Task 10 · Self-contained `.exe` packaging** `[XS · 1 file]`

Acceptance criteria:
- [ ] `dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained`
  produces a single `.exe` under 80 MB
- [ ] `.exe` runs on a Windows 10 VM with no .NET runtime installed
- [ ] File metadata: version, icon, description set in `.csproj`

Verification:
- [ ] Test on clean Windows 10 VM (no SDK installed)

Files: `CodebaseDumper.csproj`

---

**☑ Checkpoint 4 — release ready**
- [ ] All tests pass on CI
- [ ] Single `.exe` verified on clean Windows 10 VM
- [ ] README includes: install instructions, screenshot, usage

---

## 4 · frontend-ui-engineering

### Component tree

```
MainWindow
├── TitleBar
│   ├── AppIcon (16×16)
│   ├── AppName "CodebaseDumper"
│   └── VersionBadge "v1.0"
│
├── ConfigPanel [280px fixed, left]
│   ├── FolderPickerControl
│   │   ├── PathTextBox  (readonly, placeholder "Select a project folder…")
│   │   └── BrowseButton "Browse…"
│   ├── SectionLabel "Include file types"
│   ├── ExtensionFilterControl
│   │   ├── PresetBar  [JS/TS] [Python] [C#] [Go]
│   │   └── ChipList   (removable chips + add-input)
│   ├── SectionLabel "Exclude directories"
│   └── ExcludeDirsControl
│       └── ChipList   (removable chips + add-input)
│
└── RightPanel [flex, right]
    ├── PreviewPanel
    │   ├── StatsBar  "{N} files  ·  ~{K}K tokens  ·  {X} KB"
    │   └── TreeScrollViewer (monospace, selectable text)
    │       └── [IdlePlaceholder | ScanningSpinner | TreeText | ErrorMessage]
    └── ActionBar
        ├── ExportButton    (primary style, disabled in Idle/Scanning)
        ├── ProgressBar     (visible in Exporting only)
        ├── CancelButton    (visible in Exporting only)
        └── OpenFileButton  (visible in Done only)
```

### State matrix — every component handles all states

| Component | Idle | Scanning | Previewed | Exporting | Done | Error |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| FolderPickerControl | enabled | enabled | enabled | disabled | enabled | enabled |
| FilterPanel | enabled | enabled | enabled | disabled | enabled | enabled |
| StatsBar | hidden | hidden | visible | visible (dimmed) | visible | hidden |
| TreeScrollViewer | placeholder | spinner | tree text | tree text | tree text | error msg |
| ExportButton | disabled | disabled | **enabled** | hidden | hidden | enabled (retry) |
| ProgressBar | hidden | hidden | hidden | **visible** | hidden | hidden |
| CancelButton | hidden | hidden | hidden | **visible** | hidden | hidden |
| OpenFileButton | hidden | hidden | hidden | hidden | **visible** | hidden |

### Accessibility checklist

- [ ] All buttons: `ToolTip` + `AutomationProperties.Name`
- [ ] Chip "×" buttons: `AutomationProperties.Name = "Remove {glob}"`
- [ ] `PathTextBox`: `AutomationProperties.LabeledBy` pointing to its section label
- [ ] `ProgressBar`: `AutomationProperties.LiveSetting = Polite`
- [ ] Error messages: `AutomationProperties.LiveSetting = Assertive`
- [ ] Tab order defined via `TabIndex` on all interactive elements
- [ ] No color-only state indicators (always pair color with text or icon)
- [ ] Minimum contrast ratio 4.5:1 for all body text

### Spacing and typography

```
Section headers:  13px / weight 500 / color text-secondary
Body text:        14px / weight 400
Monospace tree:   13px / Consolas, "Courier New", monospace
Chips:            12px / weight 400 / border-radius 4px / padding 3px 8px
Stats bar:        13px / weight 400 / color text-secondary

Spacing scale: 4, 8, 12, 16, 24px (no off-scale values)
Panel padding: 16px
Section gap: 20px
```

### What we are NOT doing in the UI

- No gradients, shadows, or blur
- No custom theming beyond system light/dark
- No animations except the progress bar fill
- No inline SVG icons — use Segoe Fluent Icons (ships with Windows 11)
  or fall back to text labels
