# Agent Guide

## Mission

Build the offline Android judo-training attendance application described in `requirements.md`. The grouping guidance in `lok-stod-logic.md` is a domain constraint, not permission to create fictional activities.

## First reads

Before starting any task, read:

1. `PLANS.md` for the assigned function and its prerequisites.
2. `requirements.md` for product behavior.
3. `lok-stod-logic.md` for export grouping constraints when working on attendance or export.
4. `ASSUMPTIONS.md` for unresolved decisions and their scope.

## How to take work

- Choose one unchecked sub-function from `PLANS.md` whose prerequisites are complete.
- State the exact sub-function identifier in your task/commit/hand-off.
- Keep the change limited to that item. Do not opportunistically refactor adjacent features.
- If the item reveals an unstated product or technical decision, add it to `ASSUMPTIONS.md` before making the irreversible choice. Include the choice, rationale, and impact.
- Mark a plan checkbox complete only after the implementation and its planned verification pass. Add a brief implementation note only when it materially helps the next agent.

## Product invariants

- Android only, local/offline only, and not dependent on internet access.
- Participants belong to a specific exercise; there is no global participant register unless the requirements are intentionally changed.
- The attendance screen displays first name, surname, and `YYMMDD` only.
- Admin features require the local PIN; the default is `1234`; access expires after three minutes of idleness.
- Exporting must not silently alter or clear attendance data.
- Removing an exercise with attendance must warn the admin and offer export before deletion.
- Clearing attendance must preserve exercises and participant rosters.
- Never log, unnecessarily display, or expose full personal numbers.
- LOK grouping must maximize valid groups only for genuinely separate, eligible activities. Never manufacture groups simply to increase support.

## Engineering expectations

- Use the stack selected in `PLANS.md` function 0.1; do not introduce a second framework or persistence system without recording the decision.
- Keep UI, domain logic, storage, and export boundaries testable.
- Make data migrations backward-safe once persisted data exists.
- Add focused tests for domain rules, persistence, and regressions. Keep LOK allocation logic deterministic.
- Keep text and dates suitable for Swedish users; avoid hard-coded English user-facing text where the product needs Swedish UI.
- Prefer small, reviewable changes and preserve unrelated work already in the repository.

## Definition of done

An assigned sub-function is done when:

- Its acceptance behavior in `PLANS.md` is implemented.
- Relevant automated tests pass, plus any stated manual check is completed.
- Build/lint/format checks required by the chosen stack pass.
- Persistent-data changes have migration and restart behavior considered.
- The `PLANS.md` item is updated accurately, and any new uncertainty is recorded in `ASSUMPTIONS.md`.

## Handoff format

Report the completed sub-function ID, changed files, verification run, and any remaining dependency or decision. Do not claim a feature is complete when a prerequisite, policy decision, or verification step is still open.
