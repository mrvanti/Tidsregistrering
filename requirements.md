# Attendence app for Judo-trainings

This is a small attendence app to streamline the application of "LOK-stöd", see **lok-stod.md** and **lok-stod-logic.md**.

It should run on Android only and will not be distributed via store.
The application will not need internet access.

## UI functions

### Toggle attendence on participants
I see two lists sitting right next to each other where a person is moved to the right side if it is added to the list and back again to the left if need be.

The list should only show First name, surename and YYmmdd

### Add new participants (name and "personnummer")
An "Add new participant" button in the end of the list or on the top, outside the list (depending on what is standard).
Small "form" to add new participant:
- First name
- surename 
- 12-digit personal number (no validation required)
- checkbox for "Tränare?"

Participants are added to the current showing exercise, not globally.

### Pick exercise
A dropdownlist where all the excercies are shown. Should be sorted on:
- Time
- Weekday
- Name

When a new exercise is picked it should update the header that shows the users which day is picked

### Admin-mode
There should be an admin mode, logged in via pin-code (default can be 1234) that unlocks the following functions:
- Remove participants
- Add new exercise
- Remove excersie
- Export attendance
- Clear all attendance

The admin mode should automatically "log out" after 3 mminutes of "idleness"

### Remove participants
The ability to remove participant from the list of participants, unclear how this is best done.

### Add new exercise
A small form with 3 fields:
- Name of exercise
- Time
- Weekday

### Remove excersie
A small form containg:
- dropdown on all current exercies
- remove button

If there are attendances on an exercie it should tell the admin that and also ask if it should export them before removing the exercie.

### Export attendance
Export the attendance,  to .csv or similar
Small form with:
- Pick exercise (same dropdown-logic as exercise picker in main form)
- Export button

### Remove all attendance
Export all current attendance, globally, and wipe the attendance.

## Non UI functions

### Save data between sessions
I don't know what the best way to store data between sessions is but I will not use a database connection, the application is the be used without internet connection

### Split csv export into groups (business logic is LOK-support)
When exporting an exercise the exporting should split into groups after the logic to maximize the LOK-support (as per) the instructions in the **lok-stod-logic.md**.