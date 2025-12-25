# Refactoring Contract: Logic Folder Reorganization

**Feature**: 001-logic-domain-refactor
**Date**: 2025-12-24
**Contract Version**: 1.0

## Purpose

This contract defines the pre-refactoring requirements, refactoring validation criteria, and post-refactoring verification steps to ensure a successful, zero-breakage structural reorganization of the Logic folder.

## Pre-Refactoring Checklist

### 1. Environment Preparation

- [ ] **Backup created**: `git tag baseline-pre-refactor` applied to current commit
- [ ] **Working directory clean**: `git status` shows no uncommitted changes
- [ ] **Branch created**: On `001-logic-domain-refactor` branch (not main/master)
- [ ] **Latest changes pulled**: `git fetch && git pull origin main` to sync with latest

### 2. Test Baseline Establishment

- [ ] **Full test suite executed**: `dotnet test tests/LunaDraw.Tests/LunaDraw.Tests.csproj`
- [ ] **Test pass rate documented**: Record current pass/fail count (expected: 100% pass)
- [ ] **Test output saved**: Copy test output to `specs/001-logic-domain-refactor/baseline-tests.txt`
- [ ] **Build successful**: `dotnet build LunaDraw.csproj -f net10.0-windows10.0.19041.0` completes without errors

### 3. Performance Baseline Establishment

- [ ] **Drawing load time measured**: Benchmark `LoadDrawingAsync()` operation (record median time over 10 runs)
- [ ] **Thumbnail generation measured**: Benchmark `GenerateThumbnailAsync()` operation (record median time over 10 runs)
- [ ] **Baseline documented**: Save performance metrics to `specs/001-logic-domain-refactor/baseline-performance.md`

### 4. Dependency Analysis

- [ ] **DI registrations identified**: All Handler/Utils classes registered in MauiProgram.cs listed
- [ ] **Consumer analysis complete**: All ViewModels, Pages using Handler/Utils namespaces identified
- [ ] **Test dependencies identified**: All test files importing Handler/Utils namespaces listed

## Refactoring Validation Criteria

### Success Criteria (Must Pass All)

#### SC-001: Folder Structure

- [ ] **Zero files in `Logic/Handlers/`**: Directory must be empty (ready for deletion)
- [ ] **Zero files in `Logic/Utils/`**: Directory must be empty (ready for deletion)
- [ ] **All domain folders created**:
  - [ ] `Logic/Storage/` exists
  - [ ] `Logic/Input/` exists
  - [ ] `Logic/Canvas/` exists
  - [ ] `Logic/History/` exists
  - [ ] `Logic/Playback/` exists
  - [ ] `Logic/Selection/` exists
  - [ ] `Logic/Layers/` exists

#### SC-002: Class Decomposition

- [ ] **No files exceed 400 lines** (except explicitly documented exceptions)
- [ ] **DrawingStorageMomento decomposed**: 10+ files in `Logic/Storage/` replacing original
- [ ] **DrawingThumbnailHandler decomposed**: 3+ files in `Logic/Canvas/` replacing original
- [ ] **Extension methods created**: `SkiaSharpExtensions.cs` contains rendering extension methods

#### SC-003: Namespace Consistency

- [ ] **All namespaces match folder paths**:
  - [ ] `Logic/Storage/*` → `LunaDraw.Logic.Storage`
  - [ ] `Logic/Input/*` → `LunaDraw.Logic.Input`
  - [ ] `Logic/Canvas/*` → `LunaDraw.Logic.Canvas`
  - [ ] `Logic/History/*` → `LunaDraw.Logic.History`
  - [ ] `Logic/Playback/*` → `LunaDraw.Logic.Playback`
  - [ ] `Logic/Selection/*` → `LunaDraw.Logic.Selection`
  - [ ] `Logic/Layers/*` → `LunaDraw.Logic.Layers`

#### SC-004: Functional Preservation

- [ ] **100% test pass rate**: All unit tests pass without modification to test logic
  - Only namespace/import updates allowed in test files
  - Zero new test failures introduced
- [ ] **Build succeeds**: `dotnet build` completes without errors or warnings
- [ ] **Application launches**: App starts without runtime errors

#### SC-005: Git History Preservation

- [ ] **Git history intact**: For each relocated file, `git log --follow` shows commit history
- [ ] **File moves used `git mv`**: All relocations used `git mv` command (not delete + create)

#### SC-006: DI Registration Correctness

- [ ] **All services registered**: MauiProgram.cs registers all new interfaces/classes
- [ ] **No runtime DI errors**: Application resolves all dependencies without `ServiceNotFoundException`

## Post-Refactoring Verification Steps

### 1. Automated Verification

Execute the following commands and ensure all pass:

```powershell
# Clean build
dotnet clean
dotnet build LunaDraw.csproj -f net10.0-windows10.0.19041.0

# Expected: Build succeeds with 0 errors, 0 warnings

# Run tests
dotnet test tests/LunaDraw.Tests/LunaDraw.Tests.csproj --logger "console;verbosity=detailed"

# Expected: All tests pass (same count as baseline)

# Verify folder cleanup
Test-Path "Logic/Handlers" -PathType Container
# Expected: False (folder should be deleted)

Test-Path "Logic/Utils" -PathType Container
# Expected: False (folder should be deleted)
```

### 2. Manual Verification

#### Application Functionality

- [ ] **Launch app**: Application starts without errors
- [ ] **Create drawing**: Draw with freehand tool works
- [ ] **Save drawing**: Save operation succeeds
- [ ] **Load drawing**: Load operation retrieves saved drawing correctly
- [ ] **Delete drawing**: Delete operation removes drawing file
- [ ] **Duplicate drawing**: Duplicate operation creates copy
- [ ] **Rename drawing**: Rename operation updates drawing name
- [ ] **Thumbnail generation**: Gallery shows thumbnails correctly
- [ ] **Undo/redo**: Undo and redo operations work
- [ ] **Layer operations**: Add, remove, move layers works
- [ ] **Selection**: Select and manipulate elements works
- [ ] **Playback**: Movie mode recording and playback works

#### Code Quality

- [ ] **Namespace alignment**: Spot-check 5 random files - namespace matches folder path
- [ ] **No orphaned imports**: No `using LunaDraw.Logic.Handlers` or `using LunaDraw.Logic.Utils` remain
- [ ] **Extension methods work**: Call site like `externalPath.Render(canvas, paint)` compiles
- [ ] **Documentation updated**: CLAUDE.md references to Handlers/Utils folder structure updated

### 3. Performance Verification

- [ ] **Drawing load time unchanged**: Re-benchmark `LoadDrawingAsync()` (compare to baseline)
  - **Tolerance**: ±5% of baseline median time
- [ ] **Thumbnail generation unchanged**: Re-benchmark `GenerateThumbnailAsync()` (compare to baseline)
  - **Tolerance**: ±5% of baseline median time
- [ ] **No memory leaks**: Visual Studio profiler shows no new memory leaks
  - Run app for 5 minutes, perform 20 drawing operations, check memory does not grow unbounded

### 4. Git History Verification

For each relocated file, verify history is preserved:

```powershell
# Example: Verify CanvasInputHandler history
git log --follow Logic/Input/CanvasInputHandler.cs

# Expected: Full commit history visible (not just "renamed file" commit)

# Verify for all relocated files (spot-check at least 10)
```

## Rollback Procedure

**If any validation criteria fails**:

### Option 1: Fix Forward (Preferred)

1. Identify the specific failing criterion
2. Fix the issue incrementally (don't revert entire refactoring)
3. Re-run verification for that criterion
4. Continue until all criteria pass

### Option 2: Rollback to Baseline

```powershell
# Reset to baseline
git reset --hard baseline-pre-refactor

# Clean working directory
git clean -fd

# Re-run baseline tests to confirm stability
dotnet test tests/LunaDraw.Tests/LunaDraw.Tests.csproj
```

**Only use Option 2 if**:
- Multiple criteria are failing
- Root cause is unclear
- Time-sensitive deadline requires stable state

## Acceptance Criteria

**Refactoring is ACCEPTED when**:

✅ All Pre-Refactoring Checklist items completed
✅ All Refactoring Validation Criteria passed
✅ All Post-Refactoring Verification Steps completed
✅ Performance within tolerance (±5% of baseline)
✅ Zero functional regressions identified

**Sign-Off** (Complete after all criteria met):

- [ ] Developer sign-off: Refactoring complete, all criteria met
- [ ] Code review sign-off: Code review conducted, changes approved
- [ ] QA sign-off: Manual testing conducted, no regressions found

## Appendix: Validation Script

**Optional**: Create a PowerShell script to automate validation:

```powershell
# specs/001-logic-domain-refactor/validate-refactoring.ps1

Write-Host "Validating Logic Folder Refactoring..." -ForegroundColor Cyan

# SC-001: Folder Structure
Write-Host "`n[SC-001] Checking folder structure..." -ForegroundColor Yellow
$handlersExists = Test-Path "Logic/Handlers" -PathType Container
$utilsExists = Test-Path "Logic/Utils" -PathType Container
$storageExists = Test-Path "Logic/Storage" -PathType Container
$inputExists = Test-Path "Logic/Input" -PathType Container
$canvasExists = Test-Path "Logic/Canvas" -PathType Container
$historyExists = Test-Path "Logic/History" -PathType Container
$playbackExists = Test-Path "Logic/Playback" -PathType Container
$selectionExists = Test-Path "Logic/Selection" -PathType Container
$layersExists = Test-Path "Logic/Layers" -PathType Container

if (-not $handlersExists -and -not $utilsExists -and $storageExists -and $inputExists -and $canvasExists -and $historyExists -and $playbackExists -and $selectionExists -and $layersExists) {
    Write-Host "[SC-001] PASS: Folder structure correct" -ForegroundColor Green
} else {
    Write-Host "[SC-001] FAIL: Folder structure incorrect" -ForegroundColor Red
    if ($handlersExists) { Write-Host "  - Logic/Handlers still exists" -ForegroundColor Red }
    if ($utilsExists) { Write-Host "  - Logic/Utils still exists" -ForegroundColor Red }
    if (-not $storageExists) { Write-Host "  - Logic/Storage missing" -ForegroundColor Red }
    # ... etc.
}

# SC-002: Class Decomposition
Write-Host "`n[SC-002] Checking class decomposition..." -ForegroundColor Yellow
$over400LineFiles = Get-ChildItem -Path "Logic/Storage", "Logic/Input", "Logic/Canvas", "Logic/History", "Logic/Playback", "Logic/Selection", "Logic/Layers" -Recurse -Filter *.cs | Where-Object { (Get-Content $_.FullName | Measure-Object -Line).Lines -gt 400 }
if ($over400LineFiles.Count -eq 0) {
    Write-Host "[SC-002] PASS: No files exceed 400 lines" -ForegroundColor Green
} else {
    Write-Host "[SC-002] FAIL: Files exceeding 400 lines found:" -ForegroundColor Red
    $over400LineFiles | ForEach-Object { Write-Host "  - $($_.FullName): $((Get-Content $_.FullName | Measure-Object -Line).Lines) lines" -ForegroundColor Red }
}

# SC-004: Build and Test
Write-Host "`n[SC-004] Running build and tests..." -ForegroundColor Yellow
dotnet build LunaDraw.csproj -f net10.0-windows10.0.19041.0 --nologo --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "[SC-004] Build PASS" -ForegroundColor Green
} else {
    Write-Host "[SC-004] Build FAIL" -ForegroundColor Red
}

dotnet test tests/LunaDraw.Tests/LunaDraw.Tests.csproj --nologo --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "[SC-004] Tests PASS" -ForegroundColor Green
} else {
    Write-Host "[SC-004] Tests FAIL" -ForegroundColor Red
}

Write-Host "`nValidation complete. Review results above." -ForegroundColor Cyan
```

---

**Contract Status**: ✅ Defined - Ready for implementation
