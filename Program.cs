using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace UniversalUnityAssemblyDumper;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public sealed class MainForm : Form
{
    private readonly TextBox _gamePathText = new();
    private readonly Button _browseButton = new();
    private readonly TextBox _dumpNameText = new();
    private readonly Label _outputPreview = new();
    private readonly ToggleOption _dumpCs = new();
    private readonly ToggleOption _dumpIl = new();
    private readonly ToggleOption _dumpTypes = new();
    private readonly Button _runButton = new();
    private readonly Button _openOutputButton = new();
    private readonly Label _statusLabel = new();
    private readonly TextBox _logText = new();

    private bool _dumpNameWasEdited;
    private bool _suppressDumpNameEdited;
    private string? _lastOutputFolder;

    public MainForm()
    {
        Text = "Universal Unity Assembly Dumper";
        Width = 900;
        Height = 640;
        MinimumSize = new Size(820, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Palette.Window;
        ForeColor = Palette.Text;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        try
        {
            var extracted = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (extracted != null) Icon = extracted;
        }
        catch
        {
            // If Windows cannot extract the EXE icon during debugging, keep the default form icon.
        }

        var header = new HeaderPanel
        {
            Left = 0,
            Top = 0,
            Width = ClientSize.Width,
            Height = 112,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
        };

        var logo = new LogoMark
        {
            Left = 18,
            Top = 18,
            Width = 76,
            Height = 76
        };

        var title = new Label
        {
            Text = "Universal Unity Assembly Dumper",
            Left = 110,
            Top = 22,
            Width = 620,
            Height = 30,
            Font = new Font("Segoe UI", 15F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Palette.Text,
            BackColor = Color.Transparent
        };

        var subtitle = new Label
        {
            Text = "Browse to any Unity game root, GameName_Data folder, or Managed folder. Dumps go to C:\\<DumpName>.",
            Left = 112,
            Top = 56,
            Width = 720,
            Height = 38,
            ForeColor = Palette.Muted,
            BackColor = Color.Transparent
        };

        header.Controls.AddRange(new Control[] { logo, title, subtitle });

        var inputPanel = new BorderPanel
        {
            Left = 18,
            Top = 128,
            Width = ClientSize.Width - 36,
            Height = 214,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Palette.Panel
        };

        var gameLabel = MakeLabel("Game folder / GameName_Data folder", 16, 14, 360, 22, Palette.Text);
        _gamePathText.SetBounds(16, 42, inputPanel.Width - 150, 28);
        _gamePathText.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        _gamePathText.PlaceholderText = @"Example: C:\Program Files (x86)\Steam\steamapps\common\GameName or GameName_Data";
        StyleTextBox(_gamePathText);

        _browseButton.SetBounds(inputPanel.Width - 122, 41, 106, 30);
        _browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _browseButton.Text = "Browse";
        StyleButton(_browseButton, true);

        var dumpLabel = MakeLabel("Dump folder name", 16, 86, 260, 22, Palette.Text);
        _dumpNameText.SetBounds(16, 114, inputPanel.Width - 32, 28);
        _dumpNameText.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        _dumpNameText.PlaceholderText = "Auto-fills from the selected game, but you can rename it";
        StyleTextBox(_dumpNameText);

        _outputPreview.SetBounds(16, 150, inputPanel.Width - 32, 22);
        _outputPreview.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        _outputPreview.ForeColor = Palette.Accent;
        _outputPreview.BackColor = Color.Transparent;
        _outputPreview.AutoEllipsis = true;

        _statusLabel.SetBounds(16, 176, inputPanel.Width - 32, 22);
        _statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        _statusLabel.ForeColor = Palette.Muted;
        _statusLabel.BackColor = Color.Transparent;
        _statusLabel.Text = "Waiting for a folder.";
        _statusLabel.AutoEllipsis = true;

        inputPanel.Controls.AddRange(new Control[]
        {
            gameLabel, _gamePathText, _browseButton,
            dumpLabel, _dumpNameText, _outputPreview, _statusLabel
        });

        var optionsPanel = new BorderPanel
        {
            Left = 18,
            Top = 358,
            Width = ClientSize.Width - 36,
            Height = 78,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Palette.Panel
        };

        var optionsLabel = MakeLabel("Dump options", 16, 12, 200, 22, Palette.Text);
        _dumpCs.SetBounds(18, 38, 180, 30);
        _dumpCs.Text = "C# project";
        _dumpCs.Checked = true;
        StyleCheckBox(_dumpCs);

        _dumpIl.SetBounds(210, 38, 120, 30);
        _dumpIl.Text = "IL dump";
        _dumpIl.Checked = true;
        StyleCheckBox(_dumpIl);

        _dumpTypes.SetBounds(342, 38, 150, 30);
        _dumpTypes.Text = "Type list";
        _dumpTypes.Checked = true;
        StyleCheckBox(_dumpTypes);

        _runButton.SetBounds(optionsPanel.Width - 246, 25, 110, 34);
        _runButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _runButton.Text = "Start Dump";
        StyleButton(_runButton, true);

        _openOutputButton.SetBounds(optionsPanel.Width - 126, 25, 110, 34);
        _openOutputButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _openOutputButton.Text = "Open Folder";
        _openOutputButton.Enabled = false;
        StyleButton(_openOutputButton, false);

        optionsPanel.Controls.AddRange(new Control[]
        {
            optionsLabel, _dumpCs, _dumpIl, _dumpTypes, _runButton, _openOutputButton
        });

        var logLabel = MakeLabel("Log", 18, 452, 120, 22, Palette.Text);
        logLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;

        _logText.SetBounds(18, 478, ClientSize.Width - 36, ClientSize.Height - 496);
        _logText.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
        _logText.Multiline = true;
        _logText.ReadOnly = true;
        _logText.ScrollBars = ScrollBars.Vertical;
        _logText.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
        _logText.BackColor = Palette.Log;
        _logText.ForeColor = Palette.Text;
        _logText.BorderStyle = BorderStyle.FixedSingle;

        Controls.AddRange(new Control[] { header, inputPanel, optionsPanel, logLabel, _logText });

        _browseButton.Click += BrowseButton_Click;
        _runButton.Click += async (_, _) => await RunDumpAsync();
        _openOutputButton.Click += (_, _) => OpenOutputFolder();
        _gamePathText.TextChanged += (_, _) => AutoFillDumpNameFromPath();
        _dumpNameText.TextChanged += (_, _) =>
        {
            if (!_suppressDumpNameEdited)
                _dumpNameWasEdited = true;

            UpdateOutputPreview();
        };

        UpdateOutputPreview();
    }

    private static Label MakeLabel(string text, int left, int top, int width, int height, Color color)
    {
        return new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            ForeColor = color,
            BackColor = Color.Transparent
        };
    }

    private static void StyleTextBox(TextBox textBox)
    {
        textBox.BackColor = Palette.Input;
        textBox.ForeColor = Palette.Text;
        textBox.BorderStyle = BorderStyle.FixedSingle;
    }

    private static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.BackColor = Color.Transparent;
        checkBox.ForeColor = Palette.Text;
        checkBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        checkBox.Cursor = Cursors.Hand;
    }

    private static void StyleButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = primary ? Palette.AccentDark : Palette.Button;
        button.ForeColor = Palette.Text;
        button.FlatAppearance.BorderColor = primary ? Palette.Accent : Palette.Border;
        button.FlatAppearance.MouseOverBackColor = primary ? Palette.Accent : Palette.ButtonHover;
        button.FlatAppearance.MouseDownBackColor = Palette.AccentDark;
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the Unity game folder, GameName_Data folder, or Managed folder",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _dumpNameWasEdited = false;
            _gamePathText.Text = dialog.SelectedPath;
        }
    }

    private void AutoFillDumpNameFromPath()
    {
        if (_dumpNameWasEdited)
        {
            UpdateOutputPreview();
            UpdateStatusPreview();
            return;
        }

        var selectedPath = _gamePathText.Text.Trim();
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            SetDumpNameWithoutMarkingEdited(string.Empty);
            UpdateStatusPreview();
            return;
        }

        var gameName = TryGuessGameName(selectedPath);
        if (!string.IsNullOrWhiteSpace(gameName))
            SetDumpNameWithoutMarkingEdited(SanitizeFolderName(gameName + "_Dumps"));

        UpdateOutputPreview();
        UpdateStatusPreview();
    }

    private void SetDumpNameWithoutMarkingEdited(string value)
    {
        _suppressDumpNameEdited = true;
        try
        {
            _dumpNameText.Text = value;
        }
        finally
        {
            _suppressDumpNameEdited = false;
        }
    }

    private static string? TryGuessGameName(string selectedPath)
    {
        try
        {
            var dir = new DirectoryInfo(selectedPath);
            if (!dir.Exists) return null;

            if (dir.Name.Equals("Managed", StringComparison.OrdinalIgnoreCase))
            {
                var parent = dir.Parent;
                if (parent != null && parent.Name.EndsWith("_Data", StringComparison.OrdinalIgnoreCase))
                    return parent.Name[..^5];
            }

            if (dir.Name.EndsWith("_Data", StringComparison.OrdinalIgnoreCase))
                return dir.Name[..^5];

            var dataFolder = FindDataFolder(dir.FullName);
            if (dataFolder != null)
            {
                var dataName = new DirectoryInfo(dataFolder).Name;
                if (dataName.EndsWith("_Data", StringComparison.OrdinalIgnoreCase))
                    return dataName[..^5];
            }

            return dir.Name;
        }
        catch
        {
            return null;
        }
    }

    private void UpdateOutputPreview()
    {
        var folderName = SanitizeFolderName(_dumpNameText.Text.Trim());
        var outputFolder = string.IsNullOrWhiteSpace(folderName)
            ? @"C:\<dump folder name>"
            : Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\", folderName);

        _outputPreview.Text = "Output: " + outputFolder;
    }

    private void UpdateStatusPreview()
    {
        var selectedPath = _gamePathText.Text.Trim();
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            _statusLabel.ForeColor = Palette.Muted;
            _statusLabel.Text = "Waiting for a folder.";
            return;
        }

        if (!Directory.Exists(selectedPath))
        {
            _statusLabel.ForeColor = Palette.Warn;
            _statusLabel.Text = "Folder does not exist yet.";
            return;
        }

        var resolved = ResolveUnityAssembly(selectedPath);
        if (resolved == null)
        {
            _statusLabel.ForeColor = Palette.Warn;
            _statusLabel.Text = "Assembly-CSharp.dll was not found under a Managed folder.";
            return;
        }

        _statusLabel.ForeColor = Palette.Accent;
        _statusLabel.Text = "Ready: " + resolved.AssemblyPath;
    }

    private async Task RunDumpAsync()
    {
        var runCs = _dumpCs.Checked;
        var runIl = _dumpIl.Checked;
        var runTypes = _dumpTypes.Checked;

        _runButton.Enabled = false;
        _browseButton.Enabled = false;
        _dumpCs.Enabled = false;
        _dumpIl.Enabled = false;
        _dumpTypes.Enabled = false;
        _openOutputButton.Enabled = false;
        _logText.Clear();

        try
        {
            if (!runCs && !runIl && !runTypes)
            {
                MessageBox.Show(this, "Select at least one dump option.", "Nothing selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedPath = _gamePathText.Text.Trim();
            var dumpFolderName = SanitizeFolderName(_dumpNameText.Text.Trim());

            if (string.IsNullOrWhiteSpace(selectedPath) || !Directory.Exists(selectedPath))
            {
                MessageBox.Show(this, "Select a valid game folder, GameName_Data folder, or Managed folder.", "Invalid folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(dumpFolderName))
            {
                MessageBox.Show(this, "Enter a dump folder name.", "Invalid dump name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var resolved = ResolveUnityAssembly(selectedPath);
            if (resolved == null)
            {
                MessageBox.Show(this,
                    "Could not find Managed\\Assembly-CSharp.dll.\n\nThis tool is meant for Unity games that include a GameName_Data\\Managed folder.",
                    "Assembly not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var ilspy = LocateIlspyCmd();
            if (ilspy == null)
            {
                MessageBox.Show(this,
                    "ilspycmd was not found. Install it first, then reopen this app.\n\nCommand:\n    dotnet tool install --global ilspycmd\n\nUpdate command:\n    dotnet tool update --global ilspycmd",
                    "ilspycmd missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var outRoot = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\", dumpFolderName);
            _lastOutputFolder = outRoot;

            AppendLog("Game data folder: " + resolved.DataFolder);
            AppendLog("Managed folder: " + resolved.ManagedFolder);
            AppendLog("Assembly: " + resolved.AssemblyPath);
            AppendLog("Output: " + outRoot);
            AppendLog("ilspycmd: " + ilspy);
            AppendLog("");
            AppendLog("Selected dump options:");
            AppendLog("  C# project: " + YesNo(runCs));
            AppendLog("  IL dump: " + YesNo(runIl));
            AppendLog("  Type list: " + YesNo(runTypes));
            AppendLog("");

            Directory.CreateDirectory(outRoot);

            // Important: remove old outputs created by this tool before this run.
            // Otherwise an unchecked option can look like it still ran because the old folder/file is still sitting there.
            AppendLog("Cleaning old dump outputs for this folder...");
            CleanKnownDumpOutputs(outRoot);
            AppendLog("Clean complete.");
            AppendLog("");

            if (runCs)
            {
                var csOut = Path.Combine(outRoot, KnownCsFolderName);
                Directory.CreateDirectory(csOut);
                AppendLog("Decompiling full C# project...");
                await RunProcessAsync(ilspy, new[] { "-p", "--nested-directories", "-r", resolved.ManagedFolder, "-o", csOut, resolved.AssemblyPath });
                AppendLog("C# project dump complete.");
                AppendLog("");
            }
            else
            {
                AppendLog("Skipped C# project dump.");
            }

            if (runIl)
            {
                var ilOut = Path.Combine(outRoot, KnownIlFolderName);
                Directory.CreateDirectory(ilOut);
                AppendLog("Dumping full IL...");
                await RunProcessAsync(ilspy, new[] { "-il", "-o", ilOut, resolved.AssemblyPath });
                AppendLog("IL dump complete.");
                AppendLog("");
            }
            else
            {
                AppendLog("Skipped IL dump.");
            }

            if (runTypes)
            {
                var typesOut = Path.Combine(outRoot, KnownTypesFileName);
                AppendLog("Listing all types...");
                var output = await RunProcessCaptureStdoutAsync(ilspy, new[] { "-l", "cisd", resolved.AssemblyPath });
                await File.WriteAllTextAsync(typesOut, output, Encoding.UTF8);
                AppendLog("Types list complete: " + typesOut);
                AppendLog("");
            }
            else
            {
                AppendLog("Skipped type list.");
            }

            AppendLog("Done.");
            _statusLabel.ForeColor = Palette.Accent;
            _statusLabel.Text = "Done. Output created at " + outRoot;
            _openOutputButton.Enabled = Directory.Exists(outRoot);
        }
        catch (Exception ex)
        {
            AppendLog("");
            AppendLog("ERROR: " + ex.Message);
            _statusLabel.ForeColor = Palette.Warn;
            _statusLabel.Text = "Dump failed: " + ex.Message;
            MessageBox.Show(this, ex.Message, "Dump failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _runButton.Enabled = true;
            _browseButton.Enabled = true;
            _dumpCs.Enabled = true;
            _dumpIl.Enabled = true;
            _dumpTypes.Enabled = true;
        }
    }

    private const string KnownCsFolderName = "Assembly-CSharp_FULL_CS";
    private const string KnownIlFolderName = "Assembly-CSharp_FULL_IL";
    private const string KnownTypesFileName = "Assembly-CSharp_TYPES.txt";

    private static string YesNo(bool value) => value ? "YES" : "NO";

    private static void CleanKnownDumpOutputs(string outRoot)
    {
        DeleteFolderIfExists(Path.Combine(outRoot, KnownCsFolderName));
        DeleteFolderIfExists(Path.Combine(outRoot, KnownIlFolderName));
        DeleteFileIfExists(Path.Combine(outRoot, KnownTypesFileName));
    }

    private static void DeleteFolderIfExists(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static ResolvedAssembly? ResolveUnityAssembly(string selectedPath)
    {
        var selected = Path.GetFullPath(selectedPath);

        var selectedDll = Path.Combine(selected, "Assembly-CSharp.dll");
        if (File.Exists(selectedDll) && new DirectoryInfo(selected).Name.Equals("Managed", StringComparison.OrdinalIgnoreCase))
        {
            var data = Directory.GetParent(selected)?.FullName ?? selected;
            return new ResolvedAssembly(data, selected, selectedDll);
        }

        var managed = Path.Combine(selected, "Managed");
        var dll = Path.Combine(managed, "Assembly-CSharp.dll");
        if (File.Exists(dll))
            return new ResolvedAssembly(selected, managed, dll);

        var dataFolder = FindDataFolder(selected);
        if (dataFolder != null)
        {
            managed = Path.Combine(dataFolder, "Managed");
            dll = Path.Combine(managed, "Assembly-CSharp.dll");
            if (File.Exists(dll))
                return new ResolvedAssembly(dataFolder, managed, dll);
        }

        return null;
    }

    private static string? FindDataFolder(string root)
    {
        try
        {
            return Directory.EnumerateDirectories(root, "*_Data", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(dataDir => File.Exists(Path.Combine(dataDir, "Managed", "Assembly-CSharp.dll")));
        }
        catch
        {
            return null;
        }
    }

    private static string? LocateIlspyCmd()
    {
        var candidates = new List<string>();

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            candidates.Add(Path.Combine(userProfile, ".dotnet", "tools", "ilspycmd.exe"));
            candidates.Add(Path.Combine(userProfile, ".dotnet", "tools", "ilspycmd"));
        }

        candidates.Add("ilspycmd.exe");
        candidates.Add("ilspycmd");

        foreach (var candidate in candidates)
        {
            try
            {
                if (Path.IsPathRooted(candidate) && !File.Exists(candidate)) continue;

                var psi = new ProcessStartInfo
                {
                    FileName = candidate,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add("--version");

                using var process = Process.Start(psi);
                if (process == null) continue;
                process.WaitForExit(3000);
                if (!process.HasExited)
                {
                    try { process.Kill(true); } catch { }
                    continue;
                }

                if (process.ExitCode == 0)
                    return candidate;
            }
            catch
            {
                // Try the next candidate.
            }
        }

        return null;
    }

    private static void ResetFolder(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
        Directory.CreateDirectory(path);
    }

    private async Task RunProcessAsync(string fileName, IReadOnlyList<string> args)
    {
        var psi = CreateProcessStartInfo(fileName, args);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start process: " + fileName);

        var stdoutTask = ReadStreamToLogAsync(process.StandardOutput);
        var stderrTask = ReadStreamToLogAsync(process.StandardError);

        await process.WaitForExitAsync();
        await Task.WhenAll(stdoutTask, stderrTask);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {fileName} {string.Join(" ", args)}");
    }

    private async Task<string> RunProcessCaptureStdoutAsync(string fileName, IReadOnlyList<string> args)
    {
        var psi = CreateProcessStartInfo(fileName, args);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start process: " + fileName);

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = ReadStreamToLogAsync(process.StandardError);

        await process.WaitForExitAsync();
        var stdout = await stdoutTask;
        await stderrTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {fileName} {string.Join(" ", args)}");

        return stdout;
    }

    private static ProcessStartInfo CreateProcessStartInfo(string fileName, IReadOnlyList<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        return psi;
    }

    private async Task ReadStreamToLogAsync(StreamReader reader)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;
            if (!string.IsNullOrWhiteSpace(line)) AppendLog(line);
        }
    }

    private void AppendLog(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AppendLog(text)));
            return;
        }

        _logText.AppendText(text + Environment.NewLine);
    }

    private void OpenOutputFolder()
    {
        if (string.IsNullOrWhiteSpace(_lastOutputFolder) || !Directory.Exists(_lastOutputFolder)) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = _lastOutputFolder,
            UseShellExecute = true
        });
    }

    private static string SanitizeFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        var invalid = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
        var cleaned = Regex.Replace(name, $"[{invalid}]", "_").Trim();
        while (cleaned.Contains("__", StringComparison.Ordinal)) cleaned = cleaned.Replace("__", "_");
        return cleaned.Trim(' ', '.');
    }

    private sealed record ResolvedAssembly(string DataFolder, string ManagedFolder, string AssemblyPath);
}

internal static class Palette
{
    public static readonly Color Window = Color.FromArgb(13, 18, 14);
    public static readonly Color HeaderTop = Color.FromArgb(4, 6, 5);
    public static readonly Color HeaderBottom = Color.FromArgb(18, 30, 17);
    public static readonly Color Panel = Color.FromArgb(17, 24, 18);
    public static readonly Color Input = Color.FromArgb(7, 11, 8);
    public static readonly Color Log = Color.FromArgb(3, 5, 4);
    public static readonly Color Button = Color.FromArgb(24, 39, 25);
    public static readonly Color ButtonHover = Color.FromArgb(42, 82, 33);
    public static readonly Color Accent = Color.FromArgb(90, 220, 42);
    public static readonly Color AccentDark = Color.FromArgb(36, 120, 18);
    public static readonly Color Border = Color.FromArgb(49, 82, 41);
    public static readonly Color Text = Color.FromArgb(232, 240, 236);
    public static readonly Color Muted = Color.FromArgb(158, 184, 151);
    public static readonly Color Warn = Color.FromArgb(235, 188, 86);
}

public sealed class ToggleOption : CheckBox
{
    private bool _hover;
    private bool _pressed;

    public ToggleOption()
    {
        AutoCheck = false;
        Appearance = Appearance.Normal;
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
    }

    protected override void OnClick(EventArgs e)
    {
        if (Enabled)
            Checked = !Checked;

        base.OnClick(e);
        Invalidate();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Enabled && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter))
        {
            Checked = !Checked;
            e.Handled = true;
            e.SuppressKeyPress = true;
            Invalidate();
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
        _pressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && Enabled)
        {
            _pressed = true;
            Focus();
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _pressed = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? Palette.Panel);

        var outer = new Rectangle(0, 0, Width - 1, Height - 1);
        var fillColor = Checked ? Color.FromArgb(28, 68, 22) : Palette.Button;
        if (_hover && Enabled) fillColor = Checked ? Color.FromArgb(38, 92, 28) : Palette.ButtonHover;
        if (_pressed && Enabled) fillColor = Color.FromArgb(25, 75, 18);
        if (!Enabled) fillColor = Color.FromArgb(18, 22, 18);

        using (var path = RoundedRect(outer, 8))
        using (var fill = new SolidBrush(fillColor))
        using (var border = new Pen(Checked ? Palette.Accent : Palette.Border, Focused ? 2F : 1F))
        {
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        }

        var box = new Rectangle(9, (Height - 16) / 2, 16, 16);
        using (var boxFill = new SolidBrush(Checked ? Palette.AccentDark : Palette.Input))
        using (var boxBorder = new Pen(Checked ? Palette.Accent : Palette.Border, 1.5F))
        {
            e.Graphics.FillRectangle(boxFill, box);
            e.Graphics.DrawRectangle(boxBorder, box);
        }

        if (Checked)
        {
            using var checkPen = new Pen(Color.White, 2F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            e.Graphics.DrawLines(checkPen, new[]
            {
                new Point(box.Left + 3, box.Top + 8),
                new Point(box.Left + 7, box.Bottom - 4),
                new Point(box.Right - 3, box.Top + 4)
            });
        }

        var textColor = Enabled ? Palette.Text : Palette.Muted;
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            new Rectangle(32, 0, Width - 34, Height),
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public sealed class HeaderPanel : Panel
{
    public HeaderPanel()
    {
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var brush = new LinearGradientBrush(ClientRectangle, Palette.HeaderTop, Palette.HeaderBottom, LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(brush, ClientRectangle);

        using var linePen = new Pen(Palette.AccentDark, 2F);
        e.Graphics.DrawLine(linePen, 0, Height - 1, Width, Height - 1);

        base.OnPaint(e);
    }
}

public sealed class BorderPanel : Panel
{
    public BorderPanel()
    {
        DoubleBuffered = true;
        Padding = new Padding(1);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Palette.Border, 1F);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}

public sealed class LogoMark : Control
{
    public LogoMark()
    {
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(4, 4, Width - 8, Height - 8);

        using (var bg = new GraphicsPath())
        {
            bg.AddArc(rect.Left, rect.Top, 18, 18, 180, 90);
            bg.AddArc(rect.Right - 18, rect.Top, 18, 18, 270, 90);
            bg.AddArc(rect.Right - 18, rect.Bottom - 18, 18, 18, 0, 90);
            bg.AddArc(rect.Left, rect.Bottom - 18, 18, 18, 90, 90);
            bg.CloseFigure();

            using var fill = new LinearGradientBrush(rect, Color.FromArgb(67, 190, 23), Color.FromArgb(18, 78, 8), LinearGradientMode.ForwardDiagonal);
            e.Graphics.FillPath(fill, bg);
            using var border = new Pen(Palette.Accent, 2F);
            e.Graphics.DrawPath(border, bg);
        }

        using (var terrainPen = new Pen(Color.FromArgb(90, 17, 66, 8), 1.5F))
        {
            for (var x = rect.Left + 8; x < rect.Right; x += 18)
                e.Graphics.DrawLine(terrainPen, x, rect.Top + 3, x + 18, rect.Bottom - 4);
            for (var y = rect.Top + 16; y < rect.Bottom; y += 16)
                e.Graphics.DrawLine(terrainPen, rect.Left + 4, y, rect.Right - 4, y + 5);
        }

        var black = Brushes.Black;
        var head = new Point[]
        {
            new(rect.Left - 6, rect.Bottom + 2), new(rect.Left - 4, rect.Top + 46),
            new(rect.Left + 16, rect.Top + 30), new(rect.Left + 28, rect.Top + 18),
            new(rect.Left + 42, rect.Top + 28), new(rect.Left + 50, rect.Top + 9),
            new(rect.Left + 60, rect.Top + 42), new(rect.Right + 4, rect.Bottom + 2)
        };
        e.Graphics.FillPolygon(black, head);
        e.Graphics.FillPolygon(black, new[] { new Point(rect.Left + 46, rect.Top + 26), new Point(rect.Left + 66, rect.Top + 2), new Point(rect.Left + 63, rect.Top + 45) });

        using var eyeBrush = new SolidBrush(Color.FromArgb(238, 246, 244));
        e.Graphics.FillPolygon(eyeBrush, new[] { new Point(rect.Left + 22, rect.Top + 39), new Point(rect.Left + 42, rect.Top + 35), new Point(rect.Left + 48, rect.Top + 46), new Point(rect.Left + 36, rect.Top + 56), new Point(rect.Left + 23, rect.Top + 52) });
        e.Graphics.FillPolygon(eyeBrush, new[] { new Point(rect.Left + 52, rect.Top + 39), new Point(rect.Left + 67, rect.Top + 23), new Point(rect.Left + 66, rect.Top + 41), new Point(rect.Left + 57, rect.Top + 53) });
    }
}
