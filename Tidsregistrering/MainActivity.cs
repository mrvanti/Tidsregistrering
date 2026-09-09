using Android.App;
using Android.Content;
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
    private const int ExportRequestCode = 1001;
    private AdminSession adminSession = null!;
    private AppData data = new();
    private LocalStore store = null!;
    private ExerciseRepository exercises = null!;
    private ParticipantRepository participants = null!;
    private AttendanceRepository attendance = null!;
    private Spinner exercisePicker = null!;
    private ListView absentTrainerList = null!;
    private ListView absentParticipantList = null!;
    private ListView presentTrainerList = null!;
    private ListView presentParticipantList = null!;
    private TextView emptyState = null!;
    private TextView exerciseContext = null!;
    private Button addParticipantButton = null!;
    private LinearLayout adminActions = null!;
    private LinearLayout mainHeader = null!;
    private LinearLayout attendancePage = null!;
    private TextView adminStatus = null!;
    private Button removeParticipantButton = null!;
    private Button removeExerciseButton = null!;
    private Button exportButton = null!;
    private Button exportAllButton = null!;
    private Button clearAttendanceButton = null!;
    private Button changePinButton = null!;
    private Button closeAdminButton = null!;
    private LinearLayout removalPage = null!;
    private TextView removalContext = null!;
    private ListView removalList = null!;
    private Button closeRemovalButton = null!;
    private List<Participant> shownRemovalParticipants = [];
    private List<Exercise> shownExercises = [];
    private List<Participant> shownAbsentTrainers = [];
    private List<Participant> shownAbsentParticipants = [];
    private List<Participant> shownPresentTrainers = [];
    private List<Participant> shownPresentParticipants = [];
    private LinearLayout absentTrainerSection = null!;
    private LinearLayout absentParticipantSection = null!;
    private LinearLayout presentTrainerSection = null!;
    private LinearLayout presentParticipantSection = null!;
    private Timer? adminExpiryTimer;
    private bool adminMode;
    private bool removingParticipant;
    private bool refreshingExercisePicker;
    private string? pendingExportContent;
    private bool pendingExportIsGlobal;
    private bool attendanceExported;

    private DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Today);

    private DateOnly CurrentDate => selectedDate;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);

        store = new LocalStore(this);
        data = store.Load();
        adminSession = new AdminSession(store.LoadAdminPin() ?? DefaultAdminPin);
        exercises = new ExerciseRepository(data);
        participants = new ParticipantRepository(data);
        attendance = new AttendanceRepository(data);
        exercisePicker = FindViewById<Spinner>(Resource.Id.exercise_picker)!;
        absentTrainerList = FindViewById<ListView>(Resource.Id.absent_trainer_list)!;
        absentParticipantList = FindViewById<ListView>(Resource.Id.absent_participant_list)!;
        presentTrainerList = FindViewById<ListView>(Resource.Id.present_trainer_list)!;
        presentParticipantList = FindViewById<ListView>(Resource.Id.present_participant_list)!;
        emptyState = FindViewById<TextView>(Resource.Id.empty_state)!;
        exerciseContext = FindViewById<TextView>(Resource.Id.exercise_context)!;
        addParticipantButton = FindViewById<Button>(Resource.Id.add_participant_button)!;
        adminActions = FindViewById<LinearLayout>(Resource.Id.admin_actions)!;
        mainHeader = FindViewById<LinearLayout>(Resource.Id.main_header)!;
        attendancePage = FindViewById<LinearLayout>(Resource.Id.attendance_page)!;
        absentTrainerSection = FindViewById<LinearLayout>(Resource.Id.absent_trainer_section)!;
        absentParticipantSection = FindViewById<LinearLayout>(Resource.Id.absent_participant_section)!;
        presentTrainerSection = FindViewById<LinearLayout>(Resource.Id.present_trainer_section)!;
        presentParticipantSection = FindViewById<LinearLayout>(Resource.Id.present_participant_section)!;
        adminStatus = FindViewById<TextView>(Resource.Id.admin_status)!;
        removeParticipantButton = FindViewById<Button>(Resource.Id.remove_participant_button)!;
        removeExerciseButton = FindViewById<Button>(Resource.Id.remove_exercise_button)!;
        exportButton = FindViewById<Button>(Resource.Id.export_button)!;
        exportAllButton = FindViewById<Button>(Resource.Id.export_all_button)!;
        clearAttendanceButton = FindViewById<Button>(Resource.Id.clear_attendance_button)!;
        changePinButton = FindViewById<Button>(Resource.Id.change_pin_button)!;
        closeAdminButton = FindViewById<Button>(Resource.Id.close_admin_button)!;
        removalPage = FindViewById<LinearLayout>(Resource.Id.removal_page)!;
        removalContext = FindViewById<TextView>(Resource.Id.removal_context)!;
        removalList = FindViewById<ListView>(Resource.Id.removal_list)!;
        closeRemovalButton = FindViewById<Button>(Resource.Id.close_removal_button)!;

        UpdateSessionDate();
        FindViewById<Button>(Resource.Id.session_date)!.Click += (_, _) => ShowDatePicker();
        FindViewById<Button>(Resource.Id.admin_button)!.Click += (_, _) => ShowPinDialog();
        FindViewById<Button>(Resource.Id.add_exercise_button)!.Click += (_, _) =>
        {
            if (adminMode) ShowAddExerciseDialog();
        };
        removeParticipantButton.Click += (_, _) => ShowParticipantRemovalPage();
        removeExerciseButton.Click += (_, _) => ShowRemoveExerciseDialog();
        exportButton.Click += (_, _) => ShowExportExerciseDialog();
        exportAllButton.Click += (_, _) => StartGlobalCsvExport();
        clearAttendanceButton.Click += (_, _) => ShowClearAttendanceDialog();
        changePinButton.Click += (_, _) => ShowChangePinDialog();
        closeAdminButton.Click += (_, _) => DisableAdminMode(showExpiry: false);
        closeRemovalButton.Click += (_, _) => CloseParticipantRemovalPage();
        removalList.ItemClick += (_, eventArgs) => ShowRemoveParticipantDialog(shownRemovalParticipants[eventArgs.Position]);
        addParticipantButton.Click += (_, _) => ShowAddParticipantDialog();
        exercisePicker.ItemSelected += (_, _) =>
        {
            if (refreshingExercisePicker) return;

            SaveSelectedExercise();
            RefreshAttendance();
        };
        absentTrainerList.ItemClick += (_, eventArgs) => SetAttendance(shownAbsentTrainers[eventArgs.Position], true);
        absentParticipantList.ItemClick += (_, eventArgs) => SetAttendance(shownAbsentParticipants[eventArgs.Position], true);
        presentTrainerList.ItemClick += (_, eventArgs) => SetAttendance(shownPresentTrainers[eventArgs.Position], false);
        presentParticipantList.ItemClick += (_, eventArgs) => SetAttendance(shownPresentParticipants[eventArgs.Position], false);

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

    private Exercise? SelectedExercise => exercisePicker.SelectedItemPosition > 0 && exercisePicker.SelectedItemPosition <= shownExercises.Count
        ? shownExercises[exercisePicker.SelectedItemPosition - 1]
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
            CurrentDate.Month - 1,
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
            var labels = new[] { GetString(Resource.String.select_exercise) }.Concat(shownExercises.Select(ExerciseLabel)).ToList();
            exercisePicker.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, labels);
            var idToSelect = selectedId ?? store.LoadSelectedExerciseId();
            var position = idToSelect is null ? 0 : shownExercises.FindIndex(exercise => exercise.Id == idToSelect) + 1;

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
            addParticipantButton.Visibility = Android.Views.ViewStates.Gone;
            SetSection(absentTrainerSection, absentTrainerList, []);
            SetSection(absentParticipantSection, absentParticipantList, []);
            SetSection(presentTrainerSection, presentTrainerList, []);
            SetSection(presentParticipantSection, presentParticipantList, []);
            return;
        }

        emptyState.Visibility = Android.Views.ViewStates.Gone;
        exerciseContext.Text = ExerciseLabel(exercise);
        addParticipantButton.Visibility = Android.Views.ViewStates.Visible;

        var exerciseParticipants = participants.ListForExercise(exercise.Id);
        var presentIds = attendance.GetForSession(exercise.Id, CurrentDate)
            .Where(entry => entry.IsPresent)
            .Select(entry => entry.ParticipantId)
            .ToHashSet();

        shownAbsentTrainers = exerciseParticipants.Where(participant => !presentIds.Contains(participant.Id) && participant.IsTrainer).ToList();
        shownAbsentParticipants = exerciseParticipants.Where(participant => !presentIds.Contains(participant.Id) && !participant.IsTrainer).ToList();
        shownPresentTrainers = exerciseParticipants.Where(participant => presentIds.Contains(participant.Id) && participant.IsTrainer).ToList();
        shownPresentParticipants = exerciseParticipants.Where(participant => presentIds.Contains(participant.Id) && !participant.IsTrainer).ToList();
        SetSection(absentTrainerSection, absentTrainerList, shownAbsentTrainers);
        SetSection(absentParticipantSection, absentParticipantList, shownAbsentParticipants);
        SetSection(presentTrainerSection, presentTrainerList, shownPresentTrainers);
        SetSection(presentParticipantSection, presentParticipantList, shownPresentParticipants);
    }

    private void SetSection(LinearLayout section, ListView list, IReadOnlyList<Participant> sectionParticipants)
    {
        section.Visibility = sectionParticipants.Count == 0 ? ViewStates.Gone : ViewStates.Visible;
        list.Adapter = new ParticipantListAdapter(this, sectionParticipants);
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
        mainHeader.Visibility = ViewStates.Gone;
        exercisePicker.Visibility = ViewStates.Gone;
        exerciseContext.Visibility = ViewStates.Gone;
        attendancePage.Visibility = ViewStates.Gone;
        adminActions.Visibility = ViewStates.Visible;
        adminStatus.Visibility = ViewStates.Visible;
        ResetAdminExpiry();
        Toast.MakeText(this, Resource.String.admin_enabled, ToastLength.Short)!.Show();
    }

    private void ResetAdminExpiry()
    {
        adminSession.RecordActivity();
        adminExpiryTimer?.Dispose();
        adminExpiryTimer = new Timer(_ => RunOnUiThread(() => DisableAdminMode(showExpiry: true)), null, TimeSpan.FromMinutes(1), Timeout.InfiniteTimeSpan);
    }

    private void DisableAdminMode(bool showExpiry = true)
    {
        if (!adminMode) return;

        adminMode = false;
        mainHeader.Visibility = ViewStates.Visible;
        exercisePicker.Visibility = ViewStates.Visible;
        exerciseContext.Visibility = ViewStates.Visible;
        attendancePage.Visibility = ViewStates.Visible;
        removalPage.Visibility = ViewStates.Gone;
        adminSession.End();
        adminActions.Visibility = ViewStates.Gone;
        adminStatus.Visibility = ViewStates.Gone;
        removingParticipant = false;
        removeParticipantButton.SetText(Resource.String.remove_participant);
        adminExpiryTimer?.Dispose();
        adminExpiryTimer = null;
        if (showExpiry)
        {
            Toast.MakeText(this, Resource.String.admin_expired, ToastLength.Short)!.Show();
        }
    }

    private void ShowChangePinDialog()
    {
        if (!adminMode) return;

        var form = new LinearLayout(this) { Orientation = Orientation.Vertical };
        var newPin = new EditText(this) { Hint = GetString(Resource.String.new_pin), InputType = Android.Text.InputTypes.ClassNumber | Android.Text.InputTypes.NumberVariationPassword };
        var confirmation = new EditText(this) { Hint = GetString(Resource.String.confirm_pin), InputType = Android.Text.InputTypes.ClassNumber | Android.Text.InputTypes.NumberVariationPassword };
        form.AddView(newPin);
        form.AddView(confirmation);
        var dialog = new AlertDialog.Builder(this)!;
        dialog.SetTitle(Resource.String.change_pin);
        dialog.SetView(form);
        dialog.SetNegativeButton(Resource.String.cancel, (_, _) => { });
        dialog.SetPositiveButton(Resource.String.save, (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(newPin.Text) || newPin.Text != confirmation.Text)
            {
                Toast.MakeText(this, Resource.String.pin_mismatch, ToastLength.Long)!.Show();
                return;
            }

            try
            {
                store.SaveAdminPin(newPin.Text);
                adminSession = new AdminSession(newPin.Text);
                ResetAdminExpiry();
                Toast.MakeText(this, Resource.String.pin_changed, ToastLength.Short)!.Show();
            }
            catch (InvalidOperationException)
            {
                Toast.MakeText(this, Resource.String.save_failed, ToastLength.Long)!.Show();
            }
        });
        dialog.Show();
    }

    private void ToggleParticipantRemoval()
    {
        if (!adminMode) return;

        removingParticipant = !removingParticipant;
        removeParticipantButton.SetText(removingParticipant ? Resource.String.cancel_removal : Resource.String.remove_participant);
        Toast.MakeText(this, removingParticipant ? Resource.String.select_participant_to_remove : Resource.String.removal_cancelled, ToastLength.Short)!.Show();
    }

    private void ShowParticipantRemovalPage()
    {
        if (!adminMode || SelectedExercise is not Exercise exercise) return;

        shownRemovalParticipants = participants.ListForExercise(exercise.Id).ToList();
        removalContext.Text = ExerciseLabel(exercise);
        removalList.Adapter = new ParticipantListAdapter(this, shownRemovalParticipants);
        adminActions.Visibility = ViewStates.Gone;
        removalPage.Visibility = ViewStates.Visible;
    }

    private void CloseParticipantRemovalPage()
    {
        removalPage.Visibility = ViewStates.Gone;
        if (adminMode) adminActions.Visibility = ViewStates.Visible;
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
        var participantsList = fromPresent
            ? shownPresentTrainers.Concat(shownPresentParticipants).ToList()
            : shownAbsentTrainers.Concat(shownAbsentParticipants).ToList();
        if (position < 0 || position >= participantsList.Count || SelectedExercise is not Exercise exercise) return;
        var participant = participantsList[position];
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

    private void ShowRemoveParticipantDialog(Participant participant)
    {
        if (SelectedExercise is not Exercise exercise) return;

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
        CloseParticipantRemovalPage();
        SaveAndRefresh(exercise.Id);
    }

    private void ShowAddExerciseDialog()
    {
        var form = LayoutInflater!.Inflate(Resource.Layout.dialog_exercise, null)!;
        var name = form.FindViewById<EditText>(Resource.Id.exercise_name)!;
        var time = form.FindViewById<EditText>(Resource.Id.exercise_time)!;
        var weekday = form.FindViewById<Spinner>(Resource.Id.weekday_picker)!;
        weekday.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, Resources!.GetStringArray(Resource.Array.weekdays)!);
        time.Click += (_, _) => ShowTimePicker(time);

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

    private void ShowTimePicker(EditText time)
    {
        var current = TimeOnly.TryParse(time.Text, out var parsed) ? parsed : new TimeOnly(18, 0);
        new TimePickerDialog(this, (_, eventArgs) => time.Text = new TimeOnly(eventArgs.HourOfDay, eventArgs.Minute).ToString("HH:mm"), current.Hour, current.Minute, true).Show();
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
        if (attendanceCount > 0)
        {
            dialog.SetNeutralButton(Resource.String.export, (_, _) => StartCsvExport(exercise));
        }

        dialog.SetPositiveButton(Resource.String.remove, (_, _) => RemoveExercise(exercise));
        dialog.Show();
    }

    private void ShowExportExerciseDialog()
    {
        if (!adminMode || shownExercises.Count == 0) return;

        var picker = new Spinner(this);
        picker.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, shownExercises.Select(ExerciseLabel).ToList());
        var position = SelectedExercise is { } selected ? shownExercises.FindIndex(exercise => exercise.Id == selected.Id) : 0;
        picker.SetSelection(Math.Max(0, position));
        var dialog = new AlertDialog.Builder(this)!;
        dialog.SetTitle(Resource.String.export);
        dialog.SetMessage(GetString(Resource.String.export_range_description) + "\n\n" + GetString(Resource.String.lok_grouping_warning));
        dialog.SetView(picker);
        dialog.SetNegativeButton(Resource.String.cancel, (_, _) => { });
        dialog.SetPositiveButton(Resource.String.export, (_, _) =>
        {
            if (picker.SelectedItemPosition >= 0 && picker.SelectedItemPosition < shownExercises.Count)
            {
                StartCsvExport(shownExercises[picker.SelectedItemPosition]);
            }
        });
        dialog.Show();
    }

    private void StartCsvExport(Exercise exercise)
    {
        var entries = attendance.GetRawAttendance(exercise.Id, DateOnly.MinValue, DateOnly.MaxValue);
        pendingExportContent = new AttendanceCsvExporter().BuildExerciseCsv(
            exercise,
            data.Participants.Where(participant => participant.ExerciseId == exercise.Id),
            entries);
        pendingExportIsGlobal = false;
        var intent = new Intent(Intent.ActionCreateDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType("text/csv");
        intent.PutExtra(Intent.ExtraTitle, $"närvaro-{exercise.Id:N}.csv");
        StartActivityForResult(intent, ExportRequestCode);
    }

    private void StartGlobalCsvExport()
    {
        if (!adminMode) return;

        pendingExportContent = new AttendanceCsvExporter().BuildGlobalCsv(data.Exercises, data.Participants, data.AttendanceEntries);
        pendingExportIsGlobal = true;
        var intent = new Intent(Intent.ActionCreateDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType("text/csv");
        intent.PutExtra(Intent.ExtraTitle, "närvaro-alla.csv");
        StartActivityForResult(intent, ExportRequestCode);
    }

    private void ShowClearAttendanceDialog()
    {
        if (!adminMode) return;

        if (!attendanceExported)
        {
            var exportDialog = new AlertDialog.Builder(this)!;
            exportDialog.SetTitle(Resource.String.clear_attendance);
            exportDialog.SetMessage(Resource.String.export_before_clear);
            exportDialog.SetNegativeButton(Resource.String.cancel, (_, _) => { });
            exportDialog.SetPositiveButton(Resource.String.export_all, (_, _) => StartGlobalCsvExport());
            exportDialog.Show();
            return;
        }

        var clearDialog = new AlertDialog.Builder(this)!;
        clearDialog.SetTitle(Resource.String.clear_attendance);
        clearDialog.SetMessage(Resource.String.clear_attendance_confirmation);
        clearDialog.SetNegativeButton(Resource.String.cancel, (_, _) => { });
        clearDialog.SetPositiveButton(Resource.String.clear_attendance, (_, _) => ClearAttendance());
        clearDialog.Show();
    }

    private void ClearAttendance()
    {
        attendance.ClearAll();
        attendanceExported = false;
        SaveAndRefresh();
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode != ExportRequestCode) return;

        try
        {
            if (resultCode != Result.Ok || data?.Data is null || pendingExportContent is null)
            {
                Toast.MakeText(this, Resource.String.export_cancelled, ToastLength.Short)!.Show();
                return;
            }

            using var stream = ContentResolver!.OpenOutputStream(data.Data);
            using var writer = new StreamWriter(stream ?? throw new InvalidOperationException("Could not open export file."), new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            writer.Write(pendingExportContent);
            if (pendingExportIsGlobal)
            {
                attendanceExported = true;
            }
            Toast.MakeText(this, Resource.String.export_succeeded, ToastLength.Long)!.Show();
        }
        catch (Exception)
        {
            Toast.MakeText(this, Resource.String.export_failed, ToastLength.Long)!.Show();
        }
        finally
        {
            pendingExportContent = null;
            pendingExportIsGlobal = false;
        }
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
        var participantsList = fromPresent
            ? shownPresentTrainers.Concat(shownPresentParticipants).ToList()
            : shownAbsentTrainers.Concat(shownAbsentParticipants).ToList();
        if (position < 0 || position >= participantsList.Count || SelectedExercise is not Exercise exercise) return;
        var participant = participantsList[position];

        SetAttendance(participant, present);
    }

    private void SetAttendance(Participant participant, bool present)
    {
        if (SelectedExercise is not Exercise exercise) return;

        attendance.SetAttendance(exercise.Id, participant.Id, CurrentDate, present);
        attendanceExported = false;
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
