using Android.Content;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tidsregistrering.Models;

namespace Tidsregistrering.Data;

public sealed class LocalStore
{
    private const string PreferencesName = "tidsregistrering";
    private const string DataKey = "app_data";
    private const string SelectedExerciseKey = "selected_exercise_id";
    private readonly ISharedPreferences preferences;

    public LocalStore(Context context)
    {
        preferences = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
            ?? throw new InvalidOperationException("Could not open local app storage.");
    }

    public AppData Load()
    {
        var json = preferences.GetString(DataKey, null);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new AppData();
        }

        try
        {
            return AppDataMigration.Migrate(JsonSerializer.Deserialize(json, AppJsonContext.Default.AppData) ?? new AppData());
        }
        catch (JsonException)
        {
            return new AppData();
        }
    }

    public void Save(AppData data)
    {
        data = AppDataMigration.Migrate(data);
        var json = JsonSerializer.Serialize(data, AppJsonContext.Default.AppData);
        if (!preferences.Edit()!.PutString(DataKey, json)!.Commit())
        {
            throw new InvalidOperationException("Could not save local app data.");
        }
    }

    public Guid? LoadSelectedExerciseId()
    {
        var value = preferences.GetString(SelectedExerciseKey, null);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public void SaveSelectedExerciseId(Guid? id)
    {
        var editor = preferences.Edit()!;
        if (id is null)
        {
            editor.Remove(SelectedExerciseKey);
        }
        else
        {
            editor.PutString(SelectedExerciseKey, id.Value.ToString());
        }

        if (!editor.Commit())
        {
            throw new InvalidOperationException("Could not save selected exercise.");
        }
    }
}

[JsonSerializable(typeof(AppData))]
internal partial class AppJsonContext : JsonSerializerContext;
