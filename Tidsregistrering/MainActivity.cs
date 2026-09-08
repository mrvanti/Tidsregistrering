using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    // Local-only deployment setting. Change this value and rebuild to use another PIN.
    private const string DefaultAdminPin = "1234";
    private readonly AdminSession adminSession = new(DefaultAdminPin);
    private AppData data = new();
    private LocalStore store = null!;
    private ExerciseRepository exercises = null!;
    private ParticipantRepository participants = null!;
    private AttendanceRepository attendance = null!;
    private Spinner exercisePicker = null!;
    private ListView absentList = null!;
    private ListView presentList = null!;
    private TextView emptyState = null!;
    private TextView exerciseContext = null!;
    private Button addParticipantButton = null!;
    private LinearLayout adminActions = null!;
    private TextView adminStatus = null!;
    private Button removeParticipantButton = null!;
    private Button removeExerciseButton = null!;
    private List<Exercise> shownExercises = [];
    private List<Participant> shownAbsent = [];
    private List<Participant> shownPresent = [];
    private Timer? adminExpiryTimer;
    private bool adminMode;
    private bool removingParticipant;
    private bool refreshingExercisePicker;

    private DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Today);

    private DateOnly CurrentDate => selectedDate;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);

        store = new LocalStore(this);
        data = store.Load();
        exercises = new ExerciseRepository(data);
        participants = new ParticipantRepository(data);
        attendance = new AttendanceRepository(data);
        exercisePicker = FindViewById<Spinner>(Resource.Id.exercise_picker)!;
        absentList = FindViewById<ListView>(Resource.Id.absent_list)!;
        presentList = FindViewById<ListView>(Resource.Id.present_list)!;
        emptyState = FindViewById<TextView>(Resource.Id.empty_state)!;
        exerciseContext = FindViewById<TextView>(Resource.Id.exercise_context)!;
        addParticipantButton = FindViewById<Button>(Resource.Id.add_participant_button)!;
        adminActions = FindViewById<LinearLayout>(Resource.Id.admin_actions)!;
        adminStatus = FindViewById<TextView>(Resource.Id.admin_status)!;
        removeParticipantButton = FindViewById<Button>(Resource.Id.remove_participant_button)!;
        removeExerciseButton = FindViewById<Button>(Resource.Id.remove_exercise_button)!;

        UpdateSessionDate();
        FindViewById<Button>(Resource.Id.session_date)!.Click += (_, _) => ShowDatePicker();
        FindViewById<Button>(Resource.Id.admin_button)!.Click += (_, _) => ShowPinDialog();
        FindViewById<Button>(Resource.Id.add_exercise_button)!.Click += (_, _) =>
        {
            if (adminMode) ShowAddExerciseDialog();
        };
        removeParticipantButton.Click += (_, _) => ToggleParticipantRemoval();
        removeExerciseButton.Click += (_, _) => ShowRemoveExerciseDialog();
        addParticipantButton.Click += (_, _) => ShowAddParticipantDialog();
        exercisePicker.ItemSelected += (_, _) =>
        {
            if (refreshingExercisePicker) return;

            SaveSelectedExercise();
            RefreshAttendance();
        };
        absentList.ItemClick += (_, eventArgs) => HandleParticipantClick(eventArgs.Position, false);
        presentList.ItemClick += (_, eventArgs) => HandleParticipantClick(eventArgs.Position, true);

        RefreshExercises(store.LoadSelectedExerciseId());
    }

    public override bool DispatchTouchEvent(MotionEvent? ev)
    {
        if (adminMode && ev?.Action == MotionEventActions.Up)
        {
            ResetAdminExpiry();
        }

        return base.DispatchTouchEvent(ev);
    }

    protected override void OnDestroy()
    {
        adminExpiryTimer?.Dispose();
        base.OnDestroy();
    }

    private Exercise? SelectedExercise => exercisePicker.SelectedItemPosition >= 0 && exercisePicker.SelectedItemPosition < shownExercises.Count
        ? shownExercises[exercisePicker.SelectedItemPosition]
        : null;

    private void UpdateSessionDate() => FindViewById<TextView>(Resource.Id.session_date)!.Text =
        CurrentDate.ToString("dddd d MMMM", new System.Globalization.CultureInfo("sv-SE"));

    private void ShowDatePicker()
    {
        var dialog = new DatePickerDialog(
            this,
            (_, eventArgs) =>
            {
                selectedDate = new DateOnly(eventArgs.Year, eventArgs.Month + 1, eventArgs.DayOfMonth);
                RefreshAttendance();
            },
            CurrentDate.Year,
            CurrentDate.Month,
            CurrentDate.Day);
        dialog.Show();
    }

    private string ExerciseLabel(Exercise exercise)
    {
        var weekdayIndex = exercise.Weekday == DayOfWeek.Sunday ? 6 : (int)exercise.Weekday - 1;
        return $"{Resources!.GetStringArray(Resource.Array.weekdays)![weekdayIndex]} {exercise.Time} – {exercise.Name}";
    }

    private void RefreshExercises(Guid? selectedId = null)
    {
        shownExercises = exercises.ListSorted().ToList();
        refreshingExercisePicker = true;
        try
        {
            exercisePicker.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, shownExercises.Select(ExerciseLabel).ToList());
            var idToSelect = selectedId ?? store.LoadSelectedExerciseId();
            var position = idToSelect is null ? 0 : shownExercises.FindIndex(exercise => exercise.Id == idToSelect);
            if (position < 0 && shownExercises.Count > 0)
            {
                position = 0;
            }

            if (position >= 0)
            {
                exercisePicker.SetSelection(position);
            }
        }
        finally
        {
            refreshingExercisePicker = false;
        }

        SaveSelectedExercise();
        RefreshAttendance();
    }

    private void RefreshAttendance()
    {
        UpdateSessionDate();
        var exercise = SelectedExercise;
        if (exercise is null)
        {
            exerciseContext.Text = GetString(Resource.String.no_selected_exercise);
            emptyState.Visibility = Android.Views.ViewStates.Visible;
            addParticipantButton.Enabled = false;
            absentList.Adapter = null;
            presentList.Adapter = null;
            return;
        }

        emptyState.Visibility = Android.Views.ViewStates.Gone;
        exerciseContext.Text = ExerciseLabel(exercise);
        addParticipantButton.Enabled = true;

        var exerciseParticipants = participants.ListForExercise(exercise.Id);
        var presentIds = attendance.GetForSession(exercise.Id, CurrentDate)
            .Where(entry => entry.IsPresent)
            .Select(entry => entry.ParticipantId)
            .ToHashSet();

        shownAbsent = exerciseParticipants.Where(participant => !presentIds.Contains(participant.Id)).ToList();
        shownPresent = exerciseParticipants.Where(participant => presentIds.Contains(participant.Id)).ToList();
        absentList.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, shownAbsent.Select(participant => participant.ToString()).ToList());
        presentList.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, shownPresent.Select(participant => participant.ToString()).ToList());
    }

    private void ShowPinDialog()
    {
        var input = new EditText(this) { InputType = Android.Text.InputTypes.ClassNumber | Android.Text.InputTypes.NumberVariationPassword };
        var dialog = new AlertDialog.Builder(this)!;
        dialog.SetTitle(Resource.String.admin_pin_title);
        dialog.SetView(input);
        dialog.SetNegativeButton(Resource.String.cancel, (_, _) => { });
        dialog.SetPositiveButton(Resource.String.continue_label, (_, _) =>
            {
                if (adminSession.TrySignIn(input.Text ?? string.Empty))
                {
                    EnableAdminMode();
                }
                else
                {
                    Toast.MakeText(this, Resource.String.wrong_pin, ToastLength.Short)!.Show();
                }
            });
        dialog.Show();
    }

    private void EnableAdminMode()
    {
        adminMode = true;
        adminActions.Visibility = ViewStates.Visible;
        adminStatus.Visibility = ViewStates.Visible;
        ResetAdminExpiry();
        Toast.MakeText(this, Resource.String.admin_enabled, ToastLength.Short)!.Show();
    }

    private void ResetAdminExpiry()
    {
        adminSession.RecordActivity();
        adminExpiryTimer?.Dispose();
        adminExpiryTimer = new Timer(_ => RunOnUiThread(DisableAdminMode), null, TimeSpan.FromMinutes(3), Timeout.InfiniteTimeSpan);
    }

    private void DisableAdminMode()
    {
        if (!adminMode) return;

        adminMode = false;
        adminSession.End();
        adminActions.Visibility = ViewStates.Gone;
        adminStatus.Visibility = ViewStates.Gone;
        removingParticipant = false;
        removeParticipantButton.SetText(Resource.String.remove_participant);
        adminExpiryTimer?.Dispose();
        adminExpiryTimer = null;
        Toast.MakeText(this, Resource.String.admin_expired, ToastLength.Short)!.Show();
    }

    private void ToggleParticipantRemoval()
    {
        if (!adminMode) return;

        removingParticipant = !removingParticipant;
        removeParticipantButton.SetText(removingParticipant ? Resource.String.cancel_removal : Resource.String.remove_participant);
        Toast.MakeText(this, removingParticipant ? Resource.String.select_participant_to_remove : Resource.String.removal_cancelled, ToastLength.Short)!.Show();
    }

    private void HandleParticipantClick(int position, bool fromPresent)
    {
        if (removingParticipant)
        {
            ShowRemoveParticipantDialog(position, fromPresent);
            return;
        }

        SetAttendance(position, fromPresent, !fromPresent);
    }

    private void ShowRemoveParticipantDialog(int position, bool fromPresent)
    {
        var participants = fromPresent ? shownPresent : shownAbsent;
        if (position < 0 || position >= participants.Count || SelectedExercise is not Exercise exercise) return;
        var participant = participants[position];
        var message = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            GetString(Resource.String.remove_participant_confirmation),
            participant.FirstName,
            participant.Surname,
            exercise.Name);
        var dialog = new AlertDialog.Builder(this)!;
        dialog.SetTitle(Resource.String.remove_participant);
        dialog.SetMessage(message);
        dialog.SetNegativeButton(Resource.String.cancel, (_, _) => { });
        dialog.SetPositiveButton(Resource.String.remove, (_, _) => ArchiveParticipant(participant, exercise));
        dialog.Show();
    }

    private void ArchiveParticipant(Participant participant, Exercise exercise)
    {
        if (!participants.Archive(participant.Id)) return;
        removingParticipant = false;
        removeParticipantButton.SetText(Resource.String.remove_participant);
        SaveAndRefresh(exercise.Id);
    }

    private void ShowAddExerciseDialog()
    {
        var form = LayoutInflater!.Inflate(Resource.Layout.dialog_exercise, null)!;
        var name = form.FindViewById<EditText>(Resource.Id.exercise_name)!;
        var time = form.FindViewById<EditText>(Resource.Id.exercise_time)!;
        var weekday = form.FindViewById<Spinner>(Resource.Id.weekday_picker)!;
        weekday.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, Resources!.GetStringArray(Resource.Array.weekdays)!);

        var dialog = new AlertDialog.Builder(this)!;
        dialog.SetTitle(Resource.String.add_exercise);
        dialog.SetView(form);
        dialog.SetNegativeButton(Resource.String.cancel, (_, _) => { });
        dialog.SetPositiveButton(Resource.String.save, (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(name.Text) || !TimeOnly.TryParse(time.Text, out var parsedTime))
                {
                    Toast.MakeText(this, Resource.String.invalid_exercise, ToastLength.Long)!.Show();
                    return;
                }

                var exercise = new Exercise { Name = name.Text.Trim(), Time = parsedTime.ToString("HH:mm"), Weekday = (DayOfWeek)weekday.SelectedItemPosition + 1 };
                if (!exercises.TryAdd(exercise))
                {
                    Toast.MakeText(this, Resource.String.duplicate_exercise, ToastLength.Long)!.Show();
                    return;
                }

                SaveAndRefresh(exercise.Id);
            });
        dialog.Show();
    }

    private void ShowRemoveExerciseDialog()
    {
        if (!adminMode || shownExercises.Count == 0) return;

        var picker = new Spinner(this);
        picker.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, shownExercises.Select(ExerciseLabel).ToList());
        var current = SelectedExercise;
        var position = current is null ? 0 : shownExercises.FindIndex(exercise => exercise.Id == current.Id);
        picker.SetSelection(Math.Max(0, position));

        var dialog = new AlertDialog.Builder(this)!;
        dialog.SetTitle(Resource.String.remove_exercise);
        dialog.SetView(picker);
        dialog.SetNegativeButton(Resource.String.cancel, (_, _) => { });
        dialog.SetPositiveButton(Resource.String.continue_label, (_, _) =>
        {
            if (picker.SelectedItemPosition < 0 || picker.SelectedItemPosition >= shownExercises.Count) return;
            ShowRemoveExerciseConfirmation(shownExercises[picker.SelectedItemPosition]);
        });
        dialog.Show();
    }

    private void ShowRemoveExerciseConfirmation(Exercise exercise)
    {
        var attendanceCount = exercises.CountAttendance(exercise.Id);
        var message = attendanceCount == 0
            ? string.Format(GetString(Resource.String.remove_exercise_confirmation), ExerciseLabel(exercise))
            : string.Format(GetString(Resource.String.remove_exercise_with_attendance_confirmation), ExerciseLabel(exercise), attendanceCount);
        var dialog = new AlertDialog.Builder(this)!;
        dialog.SetTitle(Resource.String.remove_exercise);
        dialog.SetMessage(message);
        dialog.SetNegativeButton(Resource.String.cancel, (_, _) => { });
        dialog.SetPositiveButton(Resource.String.remove, (_, _) => RemoveExercise(exercise));
        dialog.Show();
    }

    private void RemoveExercise(Exercise exercise)
    {
        if (!exercises.Delete(exercise.Id)) return;

        SaveAndRefresh();
    }

    private void ShowAddParticipantDialog()
    {
        var exercise = SelectedExercise;
        if (exercise is null) return;

        var form = LayoutInflater!.Inflate(Resource.Layout.dialog_participant, null)!;
        var firstName = form.FindViewById<EditText>(Resource.Id.first_name)!;
        var surname = form.FindViewById<EditText>(Resource.Id.surname)!;
        var personalNumber = form.FindViewById<EditText>(Resource.Id.personal_number)!;
        var trainer = form.FindViewById<CheckBox>(Resource.Id.trainer)!;

        var dialog = new AlertDialog.Builder(this)!;
        dialog.SetTitle(Resource.String.add_participant);
        dialog.SetView(form);
        dialog.SetNegativeButton(Resource.String.cancel, (_, _) => { });
        dialog.SetPositiveButton(Resource.String.save, (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(firstName.Text) || string.IsNullOrWhiteSpace(surname.Text) || string.IsNullOrWhiteSpace(personalNumber.Text))
                {
                    Toast.MakeText(this, Resource.String.required_participant_fields, ToastLength.Long)!.Show();
                    return;
                }

                participants.Add(new Participant
                {
                    ExerciseId = exercise.Id,
                    FirstName = firstName.Text.Trim(),
                    Surname = surname.Text.Trim(),
                    PersonalNumber = personalNumber.Text.Trim(),
                    IsTrainer = trainer.Checked
                });
                SaveAndRefresh(exercise.Id);
            });
        dialog.Show();
    }

    private void SetAttendance(int position, bool fromPresent, bool present)
    {
        var participants = fromPresent ? shownPresent : shownAbsent;
        if (position < 0 || position >= participants.Count || SelectedExercise is not Exercise exercise) return;
        var participant = participants[position];

        attendance.SetAttendance(exercise.Id, participant.Id, CurrentDate, present);
        SaveAndRefresh(exercise.Id);
    }

    private void SaveAndRefresh(Guid? selectedId = null)
    {
        try
        {
            store.Save(data);
            RefreshExercises(selectedId);
        }
        catch (InvalidOperationException)
        {
            Toast.MakeText(this, Resource.String.save_failed, ToastLength.Long)!.Show();
        }
    }

    private void SaveSelectedExercise()
    {
        try
        {
            store.SaveSelectedExerciseId(SelectedExercise?.Id);
        }
        catch (InvalidOperationException)
        {
            Toast.MakeText(this, Resource.String.save_failed, ToastLength.Long)!.Show();
        }
    }
}
