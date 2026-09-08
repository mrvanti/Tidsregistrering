# Tidsregistrering

Offline Android attendance app for judo training.

## Stack

- C# / .NET 10 Android (`net10.0-android`)
- Android XML views; minimum API 24 (Android 7)
- Local JSON data in Android `SharedPreferences`
- Planned CSV export: in-app RFC 4180 writer through Android Storage Access Framework

## Build

```powershell
dotnet build .\Tidsregistrering\Tidsregistrering.slnx
```

The installed preview .NET SDK may print a misleading `Build FAILED` footer even when it returns exit code `0` and produces the application DLL.

## Developer setup and checks

Install the .NET 10 SDK with the Android workload. Restore once, then run these commands from repository root:

```powershell
dotnet restore .\Tidsregistrering\Tidsregistrering.slnx
dotnet build .\Tidsregistrering\Tidsregistrering.slnx --no-restore
dotnet test .\Tidsregistrering.Tests\Tidsregistrering.Tests.csproj --no-restore
dotnet format .\Tidsregistrering\Tidsregistrering.slnx whitespace --verify-no-changes --no-restore
```

`Directory.Build.props` makes compiler/analyzer warnings fail builds. `Tidsregistrering.Tests` contains platform-independent domain tests; after automated checks, use an Android API 24+ emulator or device for manual checks.

To confirm the repository remains free of network permissions, inspect `Tidsregistrering/AndroidManifest.xml` (if generated) and verify that no `android.permission.INTERNET` entry is present.

## Initial administration

Default local administrator PIN: `1234`.

It grants admin mode for three minutes of inactivity. The PIN is not yet configurable; that remains planned work.

Attendance defaults to today. Select the displayed date to record or review another training session; the selection is not restored after restarting the app.
