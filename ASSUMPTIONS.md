# Assumptions

## Planning baseline (2026-09-07)

- **Technology selected (2026-09-07).** The supplied solution uses .NET 10 for Android (`net10.0-android`), API 24+, C#, and Android XML views. The initial implementation will use Android `SharedPreferences` with a JSON document for local persistence, avoiding a database connection and extra package dependencies. CSV export implementation remains to be selected when that planned function begins.
- **Exercises own their participants.** This follows the requirement that new participants are added to the currently shown exercise rather than to a global register. Removing an exercise will therefore also remove its participant roster after the required warning/export option.
- **An attendance record represents one occurrence of an exercise.** The requirements do not prescribe a date-selection workflow. The initial attendance screen records against the device's current date; selecting or correcting a different session date remains a required design/implementation task before export or clearing features are built.
- **LOK group allocation is an export-time presentation/calculation feature.** The supplied LOK logic says groups must reflect genuinely separate eligible activities and must not fictitiously split one activity. Implementation must preserve the raw attendance record and make the grouping rules transparent in the export.
