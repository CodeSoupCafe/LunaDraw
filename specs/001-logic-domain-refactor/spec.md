# Feature Specification: Logic Folder Domain-Based Reorganization

**Feature Branch**: `001-logic-domain-refactor`
**Created**: 2025-12-24
**Status**: Draft
**Input**: User description: "We are to modify the structural layout of the logic folder. We are not making ANY other changes. The new layout must simplify the logic into domains. For instance, the DrawingStorageMomento can be split into their individual components and placed in separate files such as LoadAllDrawingsAsync can be placed in Logic/Storage/LoadAllHandler. Not only simplifying the naming convention along the way, but also reducing the need for change (SoC). We can apply this to items in the Handlers and Utils folders. Not all classes require this change, for instance ClipboardMemento could be one file but just moved to Logic/Storage/ClipboardMomento. We are more concerned with heavier classes (especially with nearly 500 lines of code. All Memento and Facade classes can likey just be moved the same as well. Along the way, we can better organize the code by making methods static where possible and referencing them from the respective domain and into extension methods for their use-case. For instance, RenderPath in DrawingThumbnailHandler could be placed in SkiaSharpExtensions. In the end, we should no longer have a Handlers OR Utils OR Services (as this word is banned from use) folders left."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Locate Code by Domain (Priority: P1)

Developers need to find code based on what it does (its domain responsibility) rather than its technical classification. When looking for storage-related functionality, developers should navigate to a Storage domain folder. When looking for input handling, they should find it in an Input domain folder.

**Why this priority**: This is the foundation of the reorganization. If code cannot be easily located by domain, the entire refactoring fails to deliver value. This directly impacts developer productivity and code maintainability.

**Independent Test**: Can be fully tested by reviewing the new folder structure and confirming that related functionality is grouped together by domain, and that the Handlers and Utils folders no longer exist.

**Acceptance Scenarios**:

1. **Given** a developer needs to modify drawing storage logic, **When** they navigate the Logic folder, **Then** they find all storage-related code in a single Storage domain folder
2. **Given** a developer needs to find input handling code, **When** they browse the Logic folder structure, **Then** they locate it in an Input or Canvas domain folder, not in a generic Handlers folder
3. **Given** the refactoring is complete, **When** reviewing the Logic folder, **Then** no Handlers, Utils, or Services folders exist

---

### User Story 2 - Understand Single Responsibility Classes (Priority: P2)

Developers need to work with focused, single-responsibility classes rather than large multi-purpose classes. When opening a file, it should have a clear, singular purpose that can be understood quickly.

**Why this priority**: Large classes (500+ lines) violate Single Responsibility Principle and are harder to understand, test, and maintain. Breaking them into focused units improves code quality and reduces cognitive load.

**Independent Test**: Can be tested by reviewing file line counts and confirming that classes previously exceeding 400 lines have been decomposed into focused, single-responsibility units with clear names.

**Acceptance Scenarios**:

1. **Given** DrawingStorageMomento was 541 lines with multiple responsibilities, **When** it is refactored, **Then** each operation (Load, Save, Delete, etc.) exists in its own focused file within the Storage domain
2. **Given** DrawingThumbnailHandler was 485 lines, **When** it is refactored, **Then** its responsibilities are separated into focused units with reusable rendering logic moved to extension methods
3. **Given** any class file, **When** a developer opens it, **Then** the file contains a single, clearly-named responsibility that can be understood within 2 minutes

---

### User Story 3 - Reuse Static Utilities via Extensions (Priority: P3)

Developers need to access reusable utility methods as extension methods on the types they operate on, rather than calling static methods on handler classes.

**Why this priority**: Extension methods improve discoverability and provide a more fluent API. However, this is lower priority than structural organization and single responsibility decomposition.

**Independent Test**: Can be tested by verifying that rendering and other reusable utilities previously in handler classes are now available as extension methods in the Extensions folder.

**Acceptance Scenarios**:

1. **Given** RenderPath was a method in DrawingThumbnailHandler, **When** refactored, **Then** it becomes a static extension method in SkiaSharpExtensions
2. **Given** a developer needs to render a path, **When** they have an SKPath object, **Then** they can call the extension method directly on the path object
3. **Given** any static utility method operating on a specific type, **When** reviewing the codebase, **Then** it is implemented as an extension method where appropriate

---

### Edge Cases

- What happens when a class has mixed responsibilities that span multiple domains?
  - Split the class along domain boundaries, creating focused classes in each relevant domain folder
- How should we handle classes that are already well-factored and under 200 lines?
  - Simple relocation to the appropriate domain folder without decomposition
- What if a method could reasonably be an extension method OR remain in a domain class?
  - Prefer extension methods only for truly reusable, stateless operations; keep domain-specific logic in domain classes
- How do we handle dependencies between domain folders?
  - Allow cross-domain dependencies through interfaces and dependency injection; domains should be cohesive but not completely isolated

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: All code currently in Logic/Handlers folder MUST be reorganized into domain-based folders
- **FR-002**: All code currently in Logic/Utils folder MUST be reorganized into domain-based folders
- **FR-003**: Domain folders MUST group related functionality (e.g., Storage, Input, Canvas, History, Playback)
- **FR-004**: Classes exceeding 400 lines MUST be evaluated for decomposition into single-responsibility units
- **FR-005**: Each decomposed operation MUST be placed in its own file with a clear, descriptive name
- **FR-006**: Memento and Facade classes MUST be relocated to appropriate domain folders maintaining their current structure if well-factored
- **FR-007**: Static utility methods operating on specific types MUST be evaluated for conversion to extension methods
- **FR-008**: Reusable rendering logic MUST be moved to SkiaSharpExtensions
- **FR-009**: All file relocations MUST preserve namespace structure reflecting the new folder hierarchy
- **FR-010**: NO Handlers, Utils, or Services folders MUST remain after refactoring
- **FR-011**: Existing functionality MUST remain completely unchanged (pure structural refactoring only)
- **FR-012**: All existing tests MUST continue to pass without modification to test logic (only namespace/import updates allowed)
- **FR-013**: Domain folder names MUST be clear, singular nouns representing the domain (e.g., Storage, not StorageStuff)
- **FR-014**: File names MUST follow the pattern [Operation][Entity]Handler for operation handlers (e.g., LoadAllDrawingsHandler, SaveDrawingHandler)

### Key Entities

- **Domain Folder**: Represents a cohesive area of functionality (Storage, Input, Canvas, History, Playback, Selection, Layers, etc.)
- **Operation Handler**: A focused class responsible for a single operation within a domain (e.g., LoadAllDrawingsHandler)
- **Facade**: An interface-based abstraction providing a simplified API over complex subsystems (remains largely unchanged, relocated to domain)
- **Memento**: A class managing state serialization/persistence (relocated to appropriate domain, may be decomposed if large)
- **Extension Method**: A static utility method attached to a type for reusability (placed in Logic/Extensions folder)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero files remain in Logic/Handlers and Logic/Utils folders after refactoring
- **SC-002**: All classes exceeding 400 lines are reduced through single-responsibility decomposition or are explicitly documented as exceptions with justification
- **SC-003**: Developers can locate code 40% faster by navigating domain folders rather than generic Handlers/Utils folders (measured by time-to-find in developer surveys)
- **SC-004**: 100% of existing unit tests pass without changes to test logic (only namespace/import updates)
- **SC-005**: Code review feedback indicates improved code organization and clarity (measured by survey of team members)
- **SC-006**: Each domain folder contains cohesive, related functionality that can be described in a single sentence
- **SC-007**: File names clearly indicate purpose without requiring the developer to open the file (measured by developer comprehension survey)

## Assumptions *(optional)*

- **AS-001**: The current test suite adequately covers existing functionality, allowing us to verify no behavioral changes
- **AS-002**: Developers are familiar with domain-driven design concepts and will understand the new structure
- **AS-003**: Build system and dependency injection configuration can handle namespace changes
- **AS-004**: No external dependencies or tools rely on specific file paths within the Logic folder
- **AS-005**: The team agrees on domain boundaries (Storage, Input, Canvas, History, etc.) or will iterate on naming

## Scope *(optional)*

### In Scope

- Reorganizing Logic/Handlers folder contents into domain folders
- Reorganizing Logic/Utils folder contents into domain folders
- Decomposing large classes (400+ lines) into single-responsibility units
- Converting appropriate static utilities to extension methods
- Relocating Memento and Facade classes to domain folders
- Updating namespaces to reflect new folder structure
- Updating import statements throughout the codebase

### Out of Scope

- Changing ANY functionality or business logic
- Modifying test logic (beyond namespace/import updates)
- Refactoring code outside the Logic folder
- Adding new features or capabilities
- Performance optimization
- Changing public APIs or interfaces (unless required for namespace updates)
- Reorganizing Logic/Models, Logic/Messages, Logic/Tools, Logic/Constants, Logic/ViewModels folders (these remain unchanged unless explicitly needed)

## Dependencies *(optional)*

- **DEP-001**: Requires comprehensive test coverage to verify no behavioral changes during refactoring
- **DEP-002**: May require updates to dependency injection registration if namespaces change
- **DEP-003**: May require build script updates if any scripts reference specific file paths
- **DEP-004**: Depends on team agreement on domain folder names and boundaries

## Non-Functional Requirements *(optional)*

- **NFR-001**: Refactoring MUST NOT introduce any performance degradation
- **NFR-002**: All existing code documentation and comments MUST be preserved
- **NFR-003**: Git history MUST be preserved through proper use of git mv for file relocations
- **NFR-004**: Refactoring MUST be completable in incremental commits to minimize merge conflicts
- **NFR-005**: New folder structure MUST be intuitive enough for new team members to navigate without extensive documentation
