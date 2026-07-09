# Universal Unity Assembly Dumper

**Version:** 1.0.2

## What it does

- Lets you browse to a Unity game root folder, GameName_Data folder, or Managed folder.
- Finds Managed\Assembly-CSharp.dll automatically.
- Auto-fills the dump folder name from the game name.
  Example: Monsterpatch_Data becomes Monsterpatch_Dumps.
- Lets you rename the dump folder before running.
- Writes the output to C:\<DumpFolderName>.
- Can create:
  - Assembly-CSharp_FULL_CS
  - Assembly-CSharp_FULL_IL
  - Assembly-CSharp_TYPES.txt
- Dump options use custom toggle controls so they can be clicked on/off.
- Before each run, the app removes only its known old output folders/files from that dump folder so stale results do not look like a newly selected dump.

## Requirement

- ilspycmd must be installed.

## Easy install/update

- Run Install_or_Update_ilspycmd.bat.

## Manual install

```text
dotnet tool install --global ilspycmd
```

## Manual update

```text
dotnet tool update --global ilspycmd
```

## Build the single EXE

- Run Build_SingleExe.bat on Windows.
- The final EXE will be copied to:
  dist\UniversalUnityAssemblyDumper.exe

## Notes

- This is for Unity games that have the usual GameName_Data\Managed\Assembly-CSharp.dll layout.
- Non-Unity games, IL2CPP-only games, or games without Assembly-CSharp.dll will not dump with this specific tool.

## Fix notes for 1.0.1

- The Start Dump button now captures the checkbox states at the beginning of the run.
- The log prints which dump options are enabled.
- Old Assembly-CSharp_FULL_CS, Assembly-CSharp_FULL_IL, and Assembly-CSharp_TYPES.txt outputs are cleaned before the selected options run.

## Fix notes for 1.0.2

- Replaced the standard WinForms checkbox rendering with custom toggle controls.
- The entire green option pill is clickable, not only the tiny checkbox square.
- Selected options visibly show a green border and checkmark.
- Unchecked options stay unchecked and are skipped during dumping.
