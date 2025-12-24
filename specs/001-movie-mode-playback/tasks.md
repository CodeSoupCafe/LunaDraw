---
description: "Task list template for feature implementation"
---

# Tasks: Movie Mode (Playback)

**Input**: Design documents from `/specs/001-movie-mode-playback/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are explicitly requested in the Constitution (Principle III) and Plan.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)

## Path Conventions

- **Single project**: `src/`, `tests/` at repository root (LunaDraw root)

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create directory `Logic/Handlers` and `tests/LunaDraw.Tests/Features/MovieMode`
- [x] T002 [P] Create `PlaybackSpeed` and `PlaybackState` enums in `Logic/Models/PlaybackState.cs`
- [x] T003 [P] Update `IDrawableElement` in `Logic/Models/IDrawableElement.cs` to include `DateTimeOffset CreatedAt`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Define `IRecordingHandler` interface in `Logic/Handlers/IRecordingHandler.cs`
- [x] T005 Define `IPlaybackHandler` interface in `Logic/Handlers/IPlaybackHandler.cs`
- [x] T006 [P] Create `DrawingEvent` model in `Logic/Models/DrawingEvent.cs` (if strictly needed, otherwise covered by IDrawableElement updates)
- [x] T007 Register `IRecordingHandler` and `IPlaybackHandler` in `MauiProgram.cs` (as placeholders/impl)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Automatic Recording (Priority: P1) 🎯 MVP

**Goal**: Automatically capture timestamped creation events for every stroke to enable future playback.

**Independent Test**: Draw shapes, save file, inspect file/memory to verify `CreatedAt` timestamps are populated and persisted.

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T008 [P] [US1] Create unit tests for `RecordingHandler` in `tests/LunaDraw.Tests/Features/MovieMode/RecordingHandlerTests.cs` (Verify `RecordCreation` sets timestamp)
- [x] T009 [P] [US1] Create integration test for Serialization in `tests/LunaDraw.Tests/Features/MovieMode/SerializationTests.cs` (Verify `CreatedAt` persists)

### Implementation for User Story 1

- [x] T010 [US1] Implement `RecordingHandler` in `Logic/Handlers/RecordingHandler.cs`
- [x] T011 [US1] Integrate `RecordingHandler` into `CanvasInputHandler` (or wherever elements are created) to call `RecordCreation`
- [x] T012 [US1] Update `DrawablePath` and other concrete implementations to property initialize `CreatedAt`
- [x] T013 [US1] Update JSON Serialization logic (if custom) in `Logic/Utils/DrawingStorageMomento.cs` to include `CreatedAt` property

**Checkpoint**: At this point, new drawings will implicitly contain the data needed for playback.

---

## Phase 4: User Story 2 - In-App Playback (Priority: P2)

**Goal**: Allow users to watch a replay of their drawing process on the canvas.

**Independent Test**: Load a drawing, tap "Play", verify elements appear sequentially.

### Tests for User Story 2

- [x] T014 [P] [US2] Create unit tests for `PlaybackHandler` in `tests/LunaDraw.Tests/Features/MovieMode/PlaybackHandlerTests.cs` (Verify sorting, speed calculation, state transitions)
- [x] T015 [P] [US2] Create unit tests for `PlaybackViewModel` in `tests/LunaDraw.Tests/Features/MovieMode/PlaybackViewModelTests.cs`

### Implementation for User Story 2

- [x] T016 [US2] Implement `PlaybackHandler` logic in `Logic/Handlers/PlaybackHandler.cs` (Load layers, sort elements, DispatcherTimer loop)
- [x] T017 [US2] Implement `PlaybackViewModel` in `Logic/ViewModels/PlaybackViewModel.cs` (Expose Play/Pause commands, Speed property)
- [x] T018 [US2] Create `PlaybackControls` UI in `Components/PlaybackControls.xaml`
- [x] T019 [US2] Integrate `PlaybackControls` into `Pages/MainPage.xaml` (or relevant view)
- [x] T020 [US2] Wire up "Play" button in `ToolbarView` or `DrawingGalleryPopup` to trigger `PlaybackViewModel`

**Checkpoint**: Users can now record and watch their drawings.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T021 [P] Verify behavior when app is backgrounded (Interruption handling)
- [x] T022 Code cleanup: Remove any debug logging
- [x] T023 Run verification against `specs/001-movie-mode-playback/quickstart.md`
- [x] T024 Add unit tests for edge cases (0 elements, 10k elements performance check)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Phase 1.
- **User Story 1 (Phase 3)**: Depends on Foundational.
- **User Story 2 (Phase 4)**: Depends on Foundational (and practically needs US1 data to be useful, though logic can be tested with mock data).

### Implementation Strategy

1. **MVP**: Complete Phases 1, 2, and 3. This ensures data integrity for all future drawings.
2. **Feature Complete**: Complete Phase 4 to enable the actual "Movie Mode" experience.
3. **Polish**: Phase 5.

### Parallel Opportunities

- T008 (Recording Tests) and T014 (Playback Tests) can be written in parallel.
- T010 (Recording Logic) and T017 (Playback VM) can be developed in parallel once interfaces are defined in Phase 2.
