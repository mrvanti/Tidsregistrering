using Android.App;
using Android.OS;
using Android.Widget;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    private readonly DateOnly today = DateOnly.FromDateTime(DateTime.Today);
    private AppData data = new();
    private LocalStore store = null!;
    private Spinner exercisePicker = null!;
    private ListView absentList = null!;
    private ListView presentList = null!;
    private TextView emptyState = null!;
    private Button addParticipantButton = null!;
    private List<Exercise> shownExercises = [];
    private List<Participant> shownAbsent = [];
    private List<Participant> shownPresent = [];

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);

        store = new LocalStore(this);
        data = store.Load();
        exercisePicker = FindViewById<Spinner>(Resource.Id.exercise_picker)!;
        absentList = FindViewById<ListView>(Resource.Id.absent_list)!;
        presentList = FindViewById<ListView>(Resource.Id.present_list)!;
        emptyState = FindViewById<TextView>(Resource.Id.empty_state)!;
        addParticipantButton = FindViewById<Button>(Resource.Id.add_participant_button)!;

        FindViewById<TextView>(Resource.Id.session_date)!.Text = today.ToString("dddd d MMMM", new System.Globalization.CultureInfo("sv-SE"));
        FindViewById<Button>(Resource.Id.admin_button)!.Click += (_, _) => ShowPinDialog();
        addParticipantButton.Click += (_, _) => ShowAddParticipantDialog();
        exercisePicker.ItemSelected += (_, _) => RefreshAttendance();
        absentList.ItemClick += (_, eventArgs) => SetAttendance(eventArgs.Position, false, true);
        presentList.ItemClick += (_, eventArgs) => SetAttendance(eventArgs.Position, true, false);

        RefreshExercises();
    }

    private Exercise? SelectedExercise => exercisePicker.SelectedItemPosition >= 0 && exercisePicker.SelectedItemPosition < shownExercises.Count
        ? shownExercises[exercisePicker.SelectedItemPosition]
        : null;

    private IEnumerable<Exercise> SortedExercises() => data.Exercises
        .OrderBy(exercise => exercise.Time)
        .ThenBy(exercise => exercise.SwedishWeekdayOrder)
        .ThenBy(exercise => exercise.Name, StringComparer.CurrentCultureIgnoreCase);

    private void RefreshExercises(Guid? selectedId = null)
    {
        shownExercises = SortedExercises().ToList();
        exercisePicker.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, shownExercises.Select(exercise => exercise.ToString()).ToList());
        var position = selectedId is null ? 0 : shownExercises.FindIndex(exercise => exercise.Id == selectedId);
        if (position >= 0)
        {
            exercisePicker.SetSelection(position);
        }

        RefreshAttendance();
    }

    private void RefreshAttendance()
    {
        var exercise = SelectedExercise;
        if (exercise is null)
        {
            emptyState.Visibility = Android.Views.ViewStates.Visible;
            addParticipantButton.Enabled = false;
            absentList.Adapter = null;
            presentList.Adapter = null;
            return;
        }

        emptyState.Visibility = Android.Views.ViewStates.Gone;
        addParticipantButton.Enabled = true;

        var participants = data.Participants
            .Where(participant => participant.ExerciseId == exercise.Id)
            .OrderBy(participant => participant.Surname, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(participant => participant.FirstName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var presentIds = data.AttendanceEntries
            .Where(entry => entry.ExerciseId == exercise.Id && entry.Date == today && entry.IsPresent)
            .Select(entry => entry.ParticipantId)
            .ToHashSet();

        shownAbsent = participants.Where(participant => !presentIds.Contains(participant.Id)).ToList();
        shownPresent = participants.Where(participant => presentIds.Contains(participant.Id)).ToList();
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
                if (input.Text == "1234")
                {
                    ShowAddExerciseDialog();
                }
                else
                {
                    Toast.MakeText(this, Resource.String.wrong_pin, ToastLength.Short)!.Show();
                }
            });
        dialog.Show();
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
                if (string.IsNullOrWhiteSpace(name.Text) || !TimeOnly.TryParse(time.Text, out _))
                {
                    Toast.MakeText(this, Resource.String.invalid_exercise, ToastLength.Long)!.Show();
                    return;
                }

                var exercise = new Exercise { Name = name.Text.Trim(), Time = time.Text.Trim(), Weekday = (DayOfWeek)weekday.SelectedItemPosition + 1 };
                data.Exercises.Add(exercise);
                SaveAndRefresh(exercise.Id);
            });
        dialog.Show();
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

                data.Participants.Add(new Participant
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

        data.AttendanceEntries.RemoveAll(entry => entry.ExerciseId == exercise.Id && entry.ParticipantId == participant.Id && entry.Date == today);
        data.AttendanceEntries.Add(new AttendanceEntry { ExerciseId = exercise.Id, ParticipantId = participant.Id, Date = today, IsPresent = present });
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
}
