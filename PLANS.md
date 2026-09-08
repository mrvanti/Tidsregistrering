# Implementation Plan

## Purpose and working rules

Build an offline Android attendance app for judo training that stores data locally, tracks attendance by exercise, and exports records with a transparent LOK-stöd grouping calculation.

Each unchecked sub-function below is intended to be a small, independently assignable work item. An assignee should only edit files within its item, add focused tests where applicable, and update the item's status/notes when done. Do not begin a dependent item until its prerequisite is complete.

The product requirements are in `requirements.md`; the grouping policy is in `lok-stod-logic.md`. These documents are the source of truth. `ASSUMPTIONS.md` records open choices that must be confirmed or decided during implementation.

## 0. Decisions and project foundation

### 0.1 Select the application stack

- [x] Choose the Android language, UI toolkit, build system, local persistence solution, CSV library, and file-sharing/export approach.
- [x] Record the choices and supported Android version range in `README.md`.
- [x] Create the minimal buildable Android project and verify a clean build on the target toolchain.

### 0.2 Define product data and terminology

- [x] Define models for `Exercise`, `Participant`, `AttendanceSession`, `AttendanceEntry`, and an export/grouping result.
- [x] Define identifiers, required fields, timestamps, and deletion/retention behavior.
- [x] Resolve the session-date workflow and update `ASSUMPTIONS.md`/requirements documentation with the decision.
- [x] Specify which participant fields appear in the attendance list: first name, surname, and `YYMMDD` derived from the personal number.

### 0.3 Establish quality baseline

- [x] Configure formatting, linting, unit tests, and a repeatable build command.
- [x] Add a short developer setup and test guide.
- [x] Ensure no network permission or network-dependent functionality is introduced.

## 1. Local data layer

**Prerequisite:** 0.1, 0.2

### 1.1 Persistent storage schema

- [x] Implement persistent local storage for all core models.
- [x] Add schema creation and safe migration/versioning rules.
- [ ] Verify data survives app restart and device process recreation.

### 1.2 Exercise repository

- [x] Create, list, retrieve, and delete exercises.
- [x] Sort exercises by time, weekday, then name.
- [x] Prevent or define handling for duplicate exercises.

### 1.3 Participant repository

- [x] Create, list, and delete exercise-owned participants.
- [x] Store first name, surname, 12-digit personal number, and trainer flag.
- [x] Derive the displayed `YYMMDD` safely from the stored personal number.

### 1.4 Attendance repository

- [x] Create/retrieve an attendance session for an exercise and date.
- [x] Mark a participant present or absent without deleting participant data.
- [x] Query raw attendance by exercise and date range for export.
- [x] Clear attendance records while preserving exercises and participants.

### 1.5 Data-layer verification

- [x] Unit-test CRUD, sort order, restart persistence, and deletion behavior.
- [x] Unit-test attendance state changes and bulk clearing.

## 2. Main attendance experience

**Prerequisite:** 1.1–1.4

### 2.1 Exercise selection and context

- [x] Display a sorted exercise picker.
- [x] Update the screen header with the selected exercise's weekday/time/name context.
- [x] Restore a sensible selected exercise after app restart and handle an empty exercise list.

### 2.2 Attendance list

- [x] Show absent and present participants as adjacent lists (or an equivalently clear two-state interaction).
- [x] Move a participant between states with an accessible tap action.
- [x] Display only first name, surname, and `YYMMDD` in each row.
- [x] Persist each change immediately and recover correctly after recreation.

### 2.3 Add participant flow

- [ ] Provide a discoverable “add participant” action in the active exercise context.
- [ ] Collect first name, surname, personal number, and trainer status.
- [ ] Add the participant to the active exercise only.
- [ ] Show practical field errors; do not add personal-number validation beyond the stated requirement unless explicitly decided.

### 2.4 Main-flow verification

- [ ] Add UI/integration tests for selecting an exercise, adding a participant, and toggling attendance.
- [ ] Manually check small and large participant lists on a representative Android device/emulator.

## 3. Admin access and controls

**Prerequisite:** 2.1–2.3

### 3.1 Admin session

- [x] Add a PIN prompt with default PIN `1234`.
- [x] Keep the PIN implementation local and document how it can be changed.
- [x] Automatically end admin access after three minutes of inactivity.
- [x] Ensure normal attendance use remains available without admin access.

### 3.2 Participant removal

- [x] Expose a removal affordance only in admin mode.
- [x] Require confirmation that identifies the participant and active exercise.
- [x] Delete the exercise-owned participant according to the retention policy from 0.2.

### 3.3 Exercise administration

- [x] Add an exercise form with name, time, and weekday.
- [x] Validate required fields and make newly created exercises selectable immediately.
- [x] Add an exercise removal picker and confirmation.
- [ ] Detect associated attendance and offer export before deletion. (Attendance is detected and deletion warns it is permanent; export action pending.)
- [x] Define the cancellation/error path so no exercise is removed before the user confirms.

### 3.4 Admin verification

- [ ] Test incorrect/correct PIN, expiry after inactivity, and re-locking of controls.
- [ ] Test participant and exercise removal, including exercises with attendance.

## 4. LOK-stöd grouping and export

**Prerequisite:** 1.4; 3.3 for export-before-delete

### 4.1 Eligibility and policy safeguards

- [ ] Confirm the participant age/eligibility rules required for LOK-stöd; they are not specified in the current requirements.
- [ ] Model any eligibility result separately from raw attendance so source data is never silently changed.
- [ ] Show a clear warning that grouping may only represent genuinely separate eligible activities, never fictitious splits.

### 4.2 Group allocation engine

- [ ] Implement the supplied policy: maximize groups with one leader and at least three eligible participants.
- [ ] Distribute remaining participants across valid groups.
- [ ] Place surplus leaders as second leaders only after no more valid groups can be formed.
- [ ] Return no valid groups when there are insufficient leaders or eligible participants.
- [ ] Keep unassigned attendees/leaders explicit in the result.

### 4.3 Allocation-engine tests

- [ ] Test the documented case: 2 leaders and 6 participants produces two groups of 1+3.
- [ ] Test zero leaders, fewer than three eligible participants, surplus participants, and surplus leaders.
- [ ] Test that a second leader is not assigned while a separate valid group can be formed.
- [ ] Test deterministic results for stable exports.

### 4.4 Exercise export

- [ ] Build an admin-only export form using the same sorted exercise picker logic as the main screen.
- [ ] Define the export date/session range and CSV column specification before implementation.
- [ ] Export raw attendance plus a clearly identifiable grouping/allocation section or companion file.
- [ ] Create the file locally and invoke Android's user-controlled sharing/save flow.
- [ ] Report export success/failure without deleting any data.

### 4.5 Global export and clear

- [ ] Export all attendance records globally.
- [ ] Require explicit confirmation before clearing attendance.
- [ ] Clear only attendance records after a successful export (or explicitly handle a user-approved clear without export if product policy permits it).
- [ ] Verify exercises and participant rosters remain intact after the clear.

## 5. Product hardening and release

**Prerequisite:** 2–4

### 5.1 Usability and accessibility

- [ ] Verify Swedish-facing labels, dates, weekday names, and clear error/empty states.
- [ ] Support screen readers, touch targets, rotation/recreation, and small-screen layouts.
- [ ] Confirm that destructive actions have unambiguous confirmations.

### 5.2 Privacy and resilience

- [ ] Minimize exposure of personal numbers in UI, logs, and exports.
- [ ] Test app restart, storage errors, malformed legacy data, and interrupted export.
- [ ] Document backup/restore expectations for local-only data.

### 5.3 Release readiness

- [ ] Run the complete automated suite and a clean release build.
- [ ] Perform an offline-device acceptance pass covering the primary, admin, export, and clear flows.
- [ ] Add a concise operator guide: default PIN, export location/sharing, and data-retention behavior.

## Suggested work sequence

`0 → 1 → 2 → 3 → 4 → 5`

Within a completed phase, sub-functions without explicit interdependencies may be assigned in parallel. Do not parallelize edits to the same data model, repository, or screen without a designated integrator.
