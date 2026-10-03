// ArcticJoiner - instant Roblox joiner for Froststrap users
// Single-file C# WinForms app. No NuGet packages, no dependencies.
// Build: see build.cmd (uses the csc.exe that ships with Windows .NET Framework).
//
// What it does:
//   - Paste any Roblox join link (or pass it as a command-line argument)
//   - It parses the link and instantly launches Froststrap with the right
//     roblox:// deep link. No browser, no extra steps.
//
// Supported link formats:
//   - https://www.roblox.com/games/start?placeId=...&launchData=...
//   - https://www.roblox.com/games/12345/Game-Name?gameInstanceId=...
//   - https://www.roblox.com/games/12345/Game-Name?privateServerLinkCode=...
//   - https://www.roblox.com/share?code=...&type=Server
//   - raw roblox:// links (passed straight through)
//   - raw IDs (placeId digits, or a bare share code)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ArcticJoiner
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var settings = Settings.Load();
            var form = new JoinerForm(settings);

            // If a link was passed on the command line, join instantly.
            // Single instance: a second launch forwards its link to the running
            // instance through a queue file and exits immediately.
            bool createdNew;
            var mutex = new System.Threading.Mutex(true, "ArcticJoinerSingleInstance", out createdNew);
            if (!createdNew)
            {
                if (args != null && args.Length > 0)
                {
                    string joined = string.Join(" ", args);
                    if (!string.IsNullOrWhiteSpace(joined))
                    {
                        try
                        {
                            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                            File.WriteAllText(Path.Combine(exeDir, "incoming-link.txt"), joined);
                        }
                        catch
                        {
                            try
                            {
                                string dir = Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                    "ArcticJoiner");
                                Directory.CreateDirectory(dir);
                                File.WriteAllText(Path.Combine(dir, "incoming-link.txt"), joined);
                            }
                            catch { }
                        }
                    }
                }
                return;
            }

            if (args != null && args.Length > 0)
            {
                string joined = string.Join(" ", args);
                if (!string.IsNullOrWhiteSpace(joined))
                {
                    form.Shown += (s, e) => form.TryJoin(joined, closeAfter: true);
                }
            }

            Application.Run(form);
        }
    }

    internal sealed class Settings
    {
        public string FroststrapPath = "";
        public bool InstantHotkeyJoin = true;
        public bool CloseAfterJoin = false;
        public int HotkeyMods = 0;        // no modifiers needed by default
        public string HotkeyKey = "Insert";
        public int DeleteMods = 0;        // second keybind: extract a link from copied text
        public string DeleteKey = "Delete";
        public bool AlwaysOnTop = false;
        public bool KillBeforeJoin = true;

        private static string SettingsFile
        {
            get
            {
                // Everything lives next to the exe so the app folder is self-contained.
                string dir = Path.GetDirectoryName(Application.ExecutablePath);
                string file = Path.Combine(dir, "settings.txt");
                if (!File.Exists(file))
                {
                    // One-time migration from the old %AppData% location.
                    string old = Path.Combine(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "ArcticJoiner"),
                        "settings.txt");
                    try
                    {
                        if (File.Exists(old)) File.Copy(old, file, false);
                    }
                    catch { }
                }
                return file;
            }
        }

        public static Settings Load()
        {
            var s = new Settings();
            try
            {
                if (File.Exists(SettingsFile))
                {
                    foreach (string line in File.ReadAllLines(SettingsFile))
                    {
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        string key = line.Substring(0, eq).Trim();
                        string val = line.Substring(eq + 1).Trim();
                        if (key == "froststrapPath") s.FroststrapPath = val;
                        else if (key == "instantHotkeyJoin" || key == "autoJoinOnPaste") s.InstantHotkeyJoin = val == "1"; // legacy key migrated
                        else if (key == "closeAfterJoin") s.CloseAfterJoin = val == "1";
                        else if (key == "hotkeyMods") { int n; if (int.TryParse(val, out n)) s.HotkeyMods = n; }
                        else if (key == "hotkeyKey") s.HotkeyKey = val.Length > 0 ? val : "Insert";
                        else if (key == "deleteMods") { int n; if (int.TryParse(val, out n)) s.DeleteMods = n; }
                        else if (key == "deleteKey") s.DeleteKey = val.Length > 0 ? val : "Delete";
                        else if (key == "alwaysOnTop") s.AlwaysOnTop = val == "1";
                        else if (key == "killBeforeJoin") s.KillBeforeJoin = val == "1";
                    }
                }
            }
            catch { }
            return s;
        }

        public void Save()
        {
            try
            {
                string file = SettingsFile;
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllText(file, new StringBuilder()
                    .AppendLine("froststrapPath=" + FroststrapPath)
                    .AppendLine("instantHotkeyJoin=" + (InstantHotkeyJoin ? "1" : "0"))
                    .AppendLine("closeAfterJoin=" + (CloseAfterJoin ? "1" : "0"))
                    .AppendLine("hotkeyMods=" + HotkeyMods)
                    .AppendLine("hotkeyKey=" + HotkeyKey)
                    .AppendLine("deleteMods=" + DeleteMods)
                    .AppendLine("deleteKey=" + DeleteKey)
                    .AppendLine("alwaysOnTop=" + (AlwaysOnTop ? "1" : "0"))
                    .AppendLine("killBeforeJoin=" + (KillBeforeJoin ? "1" : "0"))
                    .ToString());
            }
            catch { }
        }
    }
}

namespace ArcticJoiner
{
    internal sealed class JoinerForm : Form
    {
        private readonly Settings _settings;
        private readonly Button _joinButton;
        private readonly Label _status;
        private readonly CheckBox _autoJoinCheck;
        private readonly CheckBox _closeAfterCheck;
        private readonly CheckBox _onTopCheck;
        private readonly CheckBox _killCheck;
        private readonly TextBox _froststrapPathBox;
        private readonly Button _browseButton;
        private readonly Button _openSettingsButton;
        private readonly Button _prevButton;
        private readonly Panel _settingsPanel;
        private readonly Button _updateButton;
        private readonly ComboBox _linkBox;
        private readonly Label _hotkeyLabel;
        private readonly Label _extractLabel;
        private readonly Button _changeHotkeyButton;
        private readonly List<string> _history = new List<string>();
        private NotifyIcon _tray;
        private System.Windows.Forms.Timer _updateTimer;
        private System.Windows.Forms.Timer _queueTimer;
        private bool _reallyExit;
        private bool _captureHotkey;
        private int _captureTarget = 1; // 1 = join hotkey, 2 = extract keybind
        private bool _trayTipShown;
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 1;
        private const int HOTKEY_ID2 = 2; // extract-link keybind (Delete)
        private string _lastAutoJoined = "";

        public JoinerForm(Settings settings)
        {
            _settings = settings;

            Text = "Arctic Joiner";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(620, 235);

            // Use the exe's own compiled-in icon for the window/taskbar.
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
            TopMost = _settings.AlwaysOnTop;

            var pasteLabel = new Label
            {
                Text = "Paste a Roblox link and press Enter:",
                AutoSize = true,
                Location = new Point(16, 14)
            };

            _linkBox = new ComboBox
            {
                Location = new Point(16, 38),
                Width = 588,
                DropDownStyle = ComboBoxStyle.DropDown,
                FlatStyle = FlatStyle.System,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _linkBox.Font = new Font(_linkBox.Font, FontStyle.Bold);
            _history.AddRange(LoadHistory());
            RenderHistoryItems();
            // Resolve game names for existing history entries in the background.
            foreach (string entry in _history)
            {
                var match = System.Text.RegularExpressions.Regex.Match(entry, "placeId=(\\d+)");
                if (!match.Success) continue;
                string copy = entry;
                string placeId = match.Groups[1].Value;
                System.Threading.Tasks.Task.Run((Action)(() =>
                {
                    string name = FetchGameName(placeId);
                    if (name == null) return;
                    try
                    {
                        Invoke((MethodInvoker)delegate
                        {
                            _historyNames[copy] = name;
                            RenderHistoryItems();
                            BuildTrayIcon();
                        });
                    }
                    catch { }
                }));
            }
            _linkBox.KeyDown += LinkBoxKeyDown;

            _joinButton = new Button
            {
                Text = "Join",
                Location = new Point(16, 100),
                Size = new Size(160, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _joinButton.Click += (s, e) => TryJoin(_linkBox.Text, closeAfter: false);

            _prevButton = new Button
            {
                Text = "Prev Server",
                Location = new Point(186, 103),
                Size = new Size(100, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _prevButton.Click += (s, e) =>
            {
                if (_history.Count > 0) TryJoin(_history[0], closeAfter: false);
            };

            _status = new Label
            {
                Text = "Ready (v" + Updater.Version + "). Paste a link to join instantly.",
                AutoSize = false,
                Size = new Size(588, 20),
                Location = new Point(16, 74),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _autoJoinCheck = new CheckBox
            {
                Text = "Join instantly using hotkey",
                Checked = _settings.InstantHotkeyJoin,
                AutoSize = true,
                Location = new Point(16, 148)
            };
            _autoJoinCheck.CheckedChanged += (s, e) =>
            {
                _settings.InstantHotkeyJoin = _autoJoinCheck.Checked;
                _settings.Save();
                ApplyHotkey(); // registers or unregisters the global hotkey right away
            };

            _closeAfterCheck = new CheckBox
            {
                Text = "Close after joining",
                Checked = _settings.CloseAfterJoin,
                AutoSize = true,
                Location = new Point(240, 148)
            };
            _closeAfterCheck.CheckedChanged += (s, e) =>
            {
                _settings.CloseAfterJoin = _closeAfterCheck.Checked;
                _settings.Save();
            };

            _onTopCheck = new CheckBox
            {
                Text = "Always on top",
                Checked = TopMost,
                AutoSize = true,
                Location = new Point(410, 148)
            };
            _onTopCheck.CheckedChanged += (s, e) =>
            {
                TopMost = _onTopCheck.Checked;
                SaveTopMost(_onTopCheck.Checked);
            };

            _killCheck = new CheckBox
            {
                Text = "Close Roblox before joining",
                Checked = _settings.KillBeforeJoin,
                AutoSize = true,
                Location = new Point(16, 172)
            };
            _killCheck.CheckedChanged += (s, e) =>
            {
                _settings.KillBeforeJoin = _killCheck.Checked;
                _settings.Save();
            };

            _openSettingsButton = new Button
            {
                Text = "Froststrap settings",
                Location = new Point(296, 103),
                Size = new Size(150, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _openSettingsButton.Click += (s, e) =>
            {
                _settingsPanel.Visible = !_settingsPanel.Visible;
                ClientSize = new Size(ClientSize.Width, _settingsPanel.Visible ? 455 : 195);
            };

            _updateButton = new Button
            {
                Text = "",
                Visible = false,
                Enabled = false,
                Location = new Point(16, 200),
                Size = new Size(400, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _updateButton.Click += (s, e) => RunSelfUpdate();

            _settingsPanel = new Panel
            {
                Location = new Point(16, 195),
                Size = new Size(588, 262),
                Visible = false
            };

            var pathLabel = new Label
            {
                Text = "Froststrap.exe location (optional - leave empty to use the roblox:// protocol handler):",
                AutoSize = false,
                Size = new Size(588, 20)
            };
            _froststrapPathBox = new TextBox
            {
                Text = _settings.FroststrapPath,
                Location = new Point(0, 26),
                Width = 500,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            if (string.IsNullOrWhiteSpace(_settings.FroststrapPath))
            {
                // Auto-detect Froststrap on first run so the box is never empty.
                string found = DetectFroststrap();
                if (found != null)
                {
                    _settings.FroststrapPath = found;
                    _settings.Save();
                    _froststrapPathBox.Text = found;
                }
            }
            _froststrapPathBox.TextChanged += (s, e) =>
            {
                _settings.FroststrapPath = _froststrapPathBox.Text.Trim();
                _settings.Save();
            };
            _browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(508, 24),
                Size = new Size(80, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _browseButton.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog
                {
                    Filter = "Froststrap (Froststrap.exe)|Froststrap.exe|All executables (*.exe)|*.exe",
                    Title = "Select Froststrap.exe"
                })
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _froststrapPathBox.Text = dlg.FileName;
                    }
                }
            };

            var hint = new Label
            {
                Text = "Tip: empty Froststrap box = the roblox:// handler is used (Froststrap, if registered).",
                AutoSize = false,
                Size = new Size(588, 60),
                Location = new Point(0, 196),
                ForeColor = SystemColors.GrayText
            };

            var checkUpdateButton = new Button
            {
                Text = "Check for updates",
                Location = new Point(508, 56),
                Size = new Size(80, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            checkUpdateButton.Click += (s, e) =>
            {
                SetStatus("Checking GitHub for updates...", false);
                CheckForUpdatesAsync(true);
            };

            _hotkeyLabel = new Label
            {
                Text = "Global hotkey: " + HotkeyDescription() +
                       " - joins whatever Roblox link is on your clipboard, from anywhere in Windows.",
                AutoSize = false,
                Size = new Size(588, 34),
                Location = new Point(0, 92),
                ForeColor = SystemColors.GrayText
            };

            var fastFlagsButton = new Button
            {
                Text = "Apply fast flags (faster launch)",
                Location = new Point(0, 210),
                Size = new Size(220, 26)
            };
            fastFlagsButton.Click += (s, e) =>
            {
                ApplyFastFlags();
            };

            var revertFlagsButton = new Button
            {
                Text = "Revert fast flags",
                Location = new Point(230, 210),
                Size = new Size(120, 26)
            };
            revertFlagsButton.Click += (s, e) =>
            {
                RevertFastFlags();
            };

            _changeHotkeyButton = new Button
            {
                Text = "Change hotkey (" + HotkeyDescription() + ")...",
                Location = new Point(0, 126),
                Width = 500
            };
            _changeHotkeyButton.Click += (s, e) =>
            {
                _captureHotkey = true;
                _captureTarget = 1;
                // Unregister the current global hotkey first, otherwise pressing
                // the new key ALSO fires the old hotkey and joins from the clipboard.
                UnregisterHotkey();
                // Pull focus away from the paste box so nothing can be typed or
                // pasted into it while the next keystroke is being captured.
                ActiveControl = _changeHotkeyButton;
                SetStatus("Press the new hotkey now (Esc to cancel) - nothing will be pasted.", false);
            };

            _extractLabel = new Label
            {
                Text = "Extract key: " + DeleteDescription() +
                       " - pulls the Roblox link out of copied text, including [text](link) format, and joins it.",
                AutoSize = false,
                Size = new Size(588, 34),
                Location = new Point(0, 160),
                ForeColor = SystemColors.GrayText
            };

            var changeDeleteKeyButton = new Button
            {
                Text = "Change key (" + DeleteDescription() + ")...",
                Location = new Point(508, 126),
                Size = new Size(80, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            changeDeleteKeyButton.Click += (s, e) =>
            {
                _captureHotkey = true;
                _captureTarget = 2;
                UnregisterHotkey();
                ActiveControl = changeDeleteKeyButton;
                SetStatus("Press the new extract key now (Esc to cancel) - nothing will be pasted.", false);
            };

            _settingsPanel.Controls.Add(pathLabel);
            _settingsPanel.Controls.Add(_froststrapPathBox);
            _settingsPanel.Controls.Add(_browseButton);
            _settingsPanel.Controls.Add(checkUpdateButton);
            _settingsPanel.Controls.Add(_hotkeyLabel);
            _settingsPanel.Controls.Add(_changeHotkeyButton);
            _settingsPanel.Controls.Add(fastFlagsButton);
            _settingsPanel.Controls.Add(revertFlagsButton);
            _settingsPanel.Controls.Add(_extractLabel);
            _settingsPanel.Controls.Add(changeDeleteKeyButton);
            _settingsPanel.Controls.Add(hint);

            Controls.Add(pasteLabel);
            Controls.Add(_linkBox);
            Controls.Add(_joinButton);
            Controls.Add(_status);
            Controls.Add(_autoJoinCheck);
            Controls.Add(_closeAfterCheck);
            Controls.Add(_openSettingsButton);
            Controls.Add(_prevButton);
            Controls.Add(_onTopCheck);
            Controls.Add(_killCheck);
            Controls.Add(_settingsPanel);
            Controls.Add(_updateButton);

            AcceptButton = _joinButton;

            // Housekeeping: remove leftovers from a previous self-update,
            // then check GitHub for a newer version once the window is shown
            // (checking in the constructor can race the window handle and
            // silently drop the result), and re-check every 15 minutes so
            // long-running tray sessions still see new updates.
            CleanupOldBinary();
            Shown += (s, e) => CheckForUpdatesAsync(false);
            _updateTimer = new System.Windows.Forms.Timer { Interval = 900000 };
            _updateTimer.Tick += (s, e) => CheckForUpdatesAsync(false);
            _updateTimer.Start();

            // Tray + global hotkey setup.
            KeyPreview = true;
            BuildTrayIcon();
            ApplyHotkey();

            // Poll the incoming-link queue so a second instance's link is
            // picked up by this one.
            _queueTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _queueTimer.Tick += (s, e) => ProcessIncomingLink();
            _queueTimer.Start();

            // Register arcticjoiner:// so the website can hand links straight
            // to this app instead of relying on the roblox:// handler.
            RegisterArcticProtocol();
        }

        private static void RegisterArcticProtocol()
        {
            try
            {
                string exe = Application.ExecutablePath;
                using (Microsoft.Win32.RegistryKey root =
                    Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software\\Classes\\arcticjoiner"))
                {
                    root.SetValue("", "URL:Arctic Joiner Protocol");
                    root.SetValue("URL Protocol", "");
                    using (Microsoft.Win32.RegistryKey icon = root.CreateSubKey("DefaultIcon"))
                    {
                        icon.SetValue("", "\"" + exe + "\",0");
                    }
                    using (Microsoft.Win32.RegistryKey command = root.CreateSubKey("shell\\open\\command"))
                    {
                        command.SetValue("", "\"" + exe + "\" \"%1\"");
                    }
                }
            }
            catch { }
        }

        private void ProcessIncomingLink()
        {
            foreach (string path in QueueFilePaths())
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    string link = File.ReadAllText(path).Trim();
                    try { File.Delete(path); } catch { }
                    if (link.Length > 0)
                    {
                        Show();
                        Activate();
                        TryJoin(link, closeAfter: false);
                    }
                }
                catch { }
            }
        }

        private static string[] QueueFilePaths()
        {
            return new string[]
            {
                Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "incoming-link.txt"),
                Path.Combine(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "ArcticJoiner"),
                    "incoming-link.txt")
            };
        }

        private void SaveTopMost(bool value)
        {
            _settings.AlwaysOnTop = value;
            _settings.Save();
        }

        private void LinkBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                TryJoin(_linkBox.Text, closeAfter: false);
            }
        }

        // Map a history display name (e.g. "Tower of Hell") back to its raw link.
        private string ResolveHistoryEntry(string text)
        {
            string trimmed = (text ?? "").Trim();
            foreach (string entry in _history)
            {
                if (HistoryDisplay(entry).Equals(trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }
            return trimmed;
        }

        public void TryJoin(string raw, bool closeAfter)
        {
            raw = ResolveHistoryEntry(raw);
            string deepLink = LinkParser.Parse(raw);
            if (deepLink == null)
            {
                SetStatus("Could not read a place ID, server ID, launch data, or private code from that input.", true);
                return;
            }

            try
            {
                // Close any running Roblox/Froststrap first so the new join does
                // not stack a second client in the taskbar.
                if (_settings.KillBeforeJoin)
                {
                    KillRunningRoblox();
                    System.Threading.Thread.Sleep(100); // short settle for handle release
                }

                string exe = _settings.FroststrapPath;
                if (!string.IsNullOrWhiteSpace(exe) && File.Exists(exe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = "\"" + deepLink + "\"",
                        UseShellExecute = false
                    });
                    SetStatus("Launched Froststrap with: " + deepLink, false);
                }
                else
                {
                    Process.Start(new ProcessStartInfo(deepLink) { UseShellExecute = true });
                    SetStatus("Opened join link (roblox:// handler): " + deepLink, false);
                }

                RecordHistory(raw);

                // Fetch the game's name and show a toast so you know what launched.
                System.Threading.Tasks.Task.Run((Action)(() => FetchGameNameAndToast(deepLink)));

                // Clear the box so the UI is ready for the next link right away.
                _lastAutoJoined = "";
                _linkBox.Text = "";

                if (closeAfter || _settings.CloseAfterJoin)
                {
                    _reallyExit = true; // actually exit, do not hide to the tray
                    Close();
                }
            }
            catch (Exception ex)
            {
                SetStatus("Launch failed: " + ex.Message + " (Is Froststrap installed?)", true);
            }
        }

        private void SetStatus(string message, bool error)
        {
            _status.Text = message;
            _status.ForeColor = error ? Color.Firebrick : SystemColors.ControlText;
        }

        private void CleanupOldBinary()
        {
            try
            {
                string old = Application.ExecutablePath + ".old";
                if (File.Exists(old)) File.Delete(old);
            }
            catch { }
        }

        private void CheckForUpdatesAsync()
        {
            CheckForUpdatesAsync(false);
        }

        private void CheckForUpdatesAsync(bool verbose)
        {
            System.Threading.Tasks.Task.Run((Action)(() =>
            {
                string newer = null;
                bool reachable = false;
                try
                {
                    string remote = Updater.DownloadText(Updater.VersionUrl).Trim();
                    reachable = true;
                    if (remote.Length > 0 && Updater.IsNewer(remote, Updater.Version)) newer = remote;
                }
                catch { }
                if (newer == null)
                {
                    if (verbose)
                    {
                        SetStatusUi(reachable
                            ? "You are on the latest version (v" + Updater.Version + ")."
                            : "Could not reach GitHub to check for updates.", !reachable);
                    }
                    return;
                }
                try
                {
                    Invoke((MethodInvoker)delegate
                    {
                        _updateButton.Text = "Update available (v" + newer + ") - install";
                        _updateButton.Visible = true;
                        _updateButton.Enabled = true;
                    });
                }
                catch { }
            }));
        }

        private void RunSelfUpdate()
        {
            _updateButton.Enabled = false;
            SetStatus("Fetching the latest version from GitHub...", false);
            System.Threading.Tasks.Task.Run((Action)(() =>
            {
                try
                {
                    string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                    string dir = Path.Combine(exeDir, "update");
                    Directory.CreateDirectory(dir);
                    string cs = Path.Combine(dir, "ArcticJoiner.cs");
                    Updater.DownloadFile(Updater.SourceUrl, cs);
                    string ico = Path.Combine(dir, "icon.ico");
                    try { Updater.DownloadFile(Updater.IconUrl, ico); } catch { ico = null; }

                    string newExe = Path.Combine(exeDir, "ArcticJoiner.new.exe");
                    try { if (File.Exists(newExe)) File.Delete(newExe); } catch { }

                    string err = Updater.CompileUpdate(cs, ico, newExe);
                    if (err != null) throw new Exception("build failed: " + err);

                    string current = Application.ExecutablePath;
                    string old = current + ".old";
                    try { if (File.Exists(old)) File.Delete(old); } catch { }
                    File.Move(current, old);
                    File.Move(newExe, current);

                    // Update leftovers stay inside the app folder, but do not pile up.
                    try { Directory.Delete(dir, true); } catch { }

                    SetStatusUi("Updated to the latest version. Restarting...", false);

                    // Tear down the tray and hotkey on the UI thread BEFORE the
                    // new instance starts, otherwise the tray icon glitches and
                    // two instances end up fighting over the hotkey.
                    try
                    {
                        Invoke((MethodInvoker)delegate
                        {
                            UnregisterHotkey();
                            if (_tray != null)
                            {
                                _tray.Visible = false;
                                _tray.Dispose();
                                _tray = null;
                            }
                        });
                    }
                    catch { }

                    Process.Start(current);
                    _reallyExit = true; // Close() must exit for real, not hide to tray
                    Invoke((MethodInvoker)delegate { Close(); });
                }
                catch (Exception ex)
                {
                    SetStatusUi("Update failed: " + ex.Message +
                        " (tip: the app folder must be writable - keep it in your user folder, not Program Files)", true);
                    try
                    {
                        Invoke((MethodInvoker)delegate { _updateButton.Enabled = true; });
                    }
                    catch { }
                }
            }));
        }

        private void SetStatusUi(string message, bool error)
        {
            try
            {
                Invoke((MethodInvoker)delegate { SetStatus(message, error); });
            }
            catch { }
        }

        // ---- Tray, global hotkey, history ----

        private void BuildTrayIcon()
        {
            var menu = new ContextMenuStrip();
            // Quick rejoin: the last few joined links, named where known.
            foreach (string entry in _history)
            {
                string copy = entry;
                menu.Items.Add(HistoryDisplay(copy), null, (s, e) => TryJoin(copy, closeAfter: false));
            }
            if (_history.Count > 0) menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            menu.Items.Add("Open Arctic Joiner", null, (s, e) => { Show(); Activate(); });
            menu.Items.Add("Join from clipboard", null, (s, e) => OnHotkey());
            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => { _reallyExit = true; Close(); });
            if (_tray == null)
            {
                _tray = new NotifyIcon
                {
                    Icon = Icon,
                    Text = "Arctic Joiner",
                    Visible = true
                };
                // Left-click the tray icon: open the window straight away.
                _tray.MouseClick += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        Show();
                        Activate();
                        _linkBox.Focus();
                    }
                };
            }
            _tray.ContextMenuStrip = menu;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Closing the window hides to the tray instead of exiting.
            if (!_reallyExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                if (!_trayTipShown)
                {
                    _trayTipShown = true;
                    _tray.ShowBalloonTip(2500, "Arctic Joiner",
                        "Still running in the tray. Right-click the tray icon to exit.", ToolTipIcon.Info);
                }
                return;
            }
            UnregisterHotkey();
            if (_updateTimer != null) _updateTimer.Stop();
            if (_queueTimer != null) _queueTimer.Stop();
            if (_tray != null) _tray.Dispose();
            base.OnFormClosing(e);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                if ((int)m.WParam == HOTKEY_ID) OnHotkey();
                else if ((int)m.WParam == HOTKEY_ID2) OnExtractHotkey();
            }
            base.WndProc(ref m);
        }

        private void UnregisterHotkey()
        {
            try { UnregisterHotKey(Handle, HOTKEY_ID); } catch { }
            try { UnregisterHotKey(Handle, HOTKEY_ID2); } catch { }
        }

        private static Keys ParseKey(string name)
        {
            try { return (Keys)new KeysConverter().ConvertFromString(name); }
            catch { return Keys.None; }
        }

        private string DeleteDescription()
        {
            string mods = "";
            if ((_settings.DeleteMods & 2) != 0) mods += "Ctrl+";
            if ((_settings.DeleteMods & 4) != 0) mods += "Shift+";
            if ((_settings.DeleteMods & 1) != 0) mods += "Alt+";
            return mods + _settings.DeleteKey;
        }

        // Second keybind: pull a Roblox link out of copied text, including the
        // markdown [text](link) form, and join it.
        private void OnExtractHotkey()
        {
            if (_captureHotkey) return;
            string text = null;
            for (int attempt = 0; attempt < 3 && text == null; attempt++)
            {
                try { text = Clipboard.GetText(); }
                catch { System.Threading.Thread.Sleep(100); }
            }
            if (string.IsNullOrWhiteSpace(text)) return;

            // Prefer what is inside (...) of a markdown link.
            var paren = System.Text.RegularExpressions.Regex.Match(
                text, "\\(([^\\s)]*roblox\\.com[^\\s)]*)\\)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            string url = paren.Success ? paren.Groups[1].Value : null;
            if (url == null)
            {
                var plain = System.Text.RegularExpressions.Regex.Match(
                    text, "(?:https?:\\/\\/|www\\.)[^\\s\\]]*roblox\\.com[^\\s\\)]*",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                url = plain.Success ? plain.Value : null;
            }
            Show();
            Activate();
            if (url == null)
            {
                SetStatus("No Roblox link found in the copied text.", true);
                return;
            }
            _lastAutoJoined = url;
            _linkBox.Text = url;
            TryJoin(url, closeAfter: false);
        }

        private string HotkeyDescription()
        {
            string mods = "";
            if ((_settings.HotkeyMods & 2) != 0) mods += "Ctrl+";
            if ((_settings.HotkeyMods & 4) != 0) mods += "Shift+";
            if ((_settings.HotkeyMods & 1) != 0) mods += "Alt+";
            return mods + _settings.HotkeyKey;
        }

        private void ApplyHotkey()
        {
            UnregisterHotkey();
            // The checkbox simply enables/disables the global hotkey.
            if (!_settings.InstantHotkeyJoin)
            {
                if (_hotkeyLabel != null)
                {
                    _hotkeyLabel.Text = "Global hotkey is disabled - tick 'Join instantly using hotkey' to enable it.";
                }
                return;
            }
            Keys key = ParseKey(_settings.HotkeyKey);
            if (key != Keys.None)
            {
                RegisterHotKey(Handle, HOTKEY_ID, (uint)_settings.HotkeyMods, (uint)key);
            }
            Keys key2 = ParseKey(_settings.DeleteKey);
            if (key2 != Keys.None)
            {
                RegisterHotKey(Handle, HOTKEY_ID2, (uint)_settings.DeleteMods, (uint)key2);
            }
            if (_hotkeyLabel != null)
            {
                _hotkeyLabel.Text = "Global hotkey: " + HotkeyDescription() +
                    " - joins whatever Roblox link is on your clipboard, from anywhere in Windows.";
            }
            if (_changeHotkeyButton != null)
            {
                _changeHotkeyButton.Text = "Change hotkey (" + HotkeyDescription() + ")...";
            }
        }

        private void OnHotkey()
        {
            if (_captureHotkey) return; // never join while picking a new hotkey
            string text = null;
            for (int attempt = 0; attempt < 3 && text == null; attempt++)
            {
                try { text = Clipboard.GetText(); }
                catch { System.Threading.Thread.Sleep(100); }
            }
            if (string.IsNullOrWhiteSpace(text)) return;

            // Always open the window and insert the clipboard text into the box.
            Show();
            Activate();
            string entry = text.Trim();
            _lastAutoJoined = entry;
            _linkBox.Text = entry;
            _linkBox.SelectionStart = entry.Length;
            if (LinkParser.Parse(entry) == null)
            {
                SetStatus("That is not a Roblox link - edit it and press Join.", true);
                return;
            }
            if (!_settings.InstantHotkeyJoin)
            {
                SetStatus("Link inserted - press Join (or Enter) to launch.", false);
                return;
            }
            TryJoin(entry, closeAfter: false);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_captureHotkey)
            {
                // Swallow EVERY key during capture so nothing gets typed or pasted.
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (e.KeyCode == Keys.Escape)
                {
                    _captureHotkey = false;
                    ApplyHotkey(); // re-register the old hotkey
                    SetStatus("Hotkey change cancelled - still set to " + HotkeyDescription() + ".", false);
                    return;
                }
                if (e.KeyCode != Keys.ControlKey &&
                    e.KeyCode != Keys.ShiftKey && e.KeyCode != Keys.Menu)
                {
                    int mods = 0;
                    if (e.Control) mods |= 2;
                    if (e.Shift) mods |= 4;
                    if (e.Alt) mods |= 1;
                    if (_captureTarget == 2)
                    {
                        _settings.DeleteMods = mods;
                        _settings.DeleteKey = e.KeyCode.ToString();
                    }
                    else
                    {
                        _settings.HotkeyMods = mods;
                        _settings.HotkeyKey = e.KeyCode.ToString();
                    }
                    _settings.Save(); // saved immediately, no extra step
                    _captureHotkey = false;
                    ApplyHotkey();
                    SetStatus((_captureTarget == 2 ? "Extract keybind" : "Hotkey") +
                        " set to " + (_captureTarget == 2 ? DeleteDescription() : HotkeyDescription()) + " and saved.", false);
                }
                return;
            }
            base.OnKeyDown(e);
        }

        // Kills any running Roblox client so a fresh join does not stack instances.
        private static void KillRunningRoblox()
        {
            try
            {
                foreach (Process p in Process.GetProcessesByName("RobloxPlayerBeta"))
                {
                    try { p.Kill(); } catch { }
                }
                foreach (Process p in Process.GetProcessesByName("RobloxPlayerLauncher"))
                {
                    try { p.Kill(); } catch { }
                }
                foreach (Process p in Process.GetProcessesByName("Froststrap"))
                {
                    try { p.Kill(); } catch { }
                }
                foreach (Process p in Process.GetProcessesByName("RobloxCrashHandler"))
                {
                    try { p.Kill(); } catch { }
                }
            }
            catch { }
        }

        // Speed-focused flags, merged INTO any existing config (never overwrites user settings).
        private static readonly string[] SpeedFlagKeys = new string[]
        {
            "FFlagDebugSkipSplashScreen",
            "FFlagSkipBootstrapperUpdateCheck",
            "FFlagDebugDisableLoadingScreen",
            "DFIntLoadingScreenDelay",
            "FLogNetwork",
            "FFlagDebugDisableTelemetryAppShellInit",
            "FFlagDebugDisableTelemetryV2Counter",
            "FFlagDebugDisableTelemetryV2Event",
            "FFlagDebugDisableTelemetryV2Stat",
            "FFlagDebugDisableTelemetryPoint",
            "FFlagRenderDebugCheckThreading2"
            "FFlagRenderDebugCheckThreading2",
            "FFlagDebugGraphicsSkipVramChecks"
        };

        private static System.Collections.Generic.Dictionary<string, string> ReadFlagsFile(string path)
        {
            var result = new System.Collections.Generic.Dictionary<string, string>();
            try
            {
                if (!File.Exists(path)) return result;
                foreach (string line in File.ReadAllLines(path))
                {
                    string t = line.Trim().TrimEnd(',');
                    int colon = t.IndexOf(':');
                    if (colon < 2) continue;
                    string key = t.Substring(0, colon).Trim().Trim('"');
                    string val = t.Substring(colon + 1).Trim().Trim('"');
                    if (key.Length > 0) result[key] = val;
                }
            }
            catch { }
            return result;
        }

        private static string BuildFlagsJson(System.Collections.Generic.Dictionary<string, string> flags)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            int i = 0;
            foreach (System.Collections.Generic.KeyValuePair<string, string> kv in flags)
            {
                if (i++ > 0) sb.AppendLine(",");
                sb.Append("  \"").Append(kv.Key).Append("\": \"").Append(kv.Value).Append("\"");
            }
            sb.AppendLine();
            sb.Append("}");
            return sb.ToString();
        }

        private void ApplyFastFlags()
        {
            var speed = new System.Collections.Generic.Dictionary<string, string>();
            speed["FFlagDebugSkipSplashScreen"] = "True";
            speed["FFlagSkipBootstrapperUpdateCheck"] = "True";
            speed["FLogNetwork"] = "7";
            speed["FFlagDebugDisableTelemetryAppShellInit"] = "True";
            speed["FFlagDebugDisableTelemetryV2Counter"] = "True";
            speed["FFlagDebugDisableTelemetryV2Event"] = "True";
            speed["FFlagDebugDisableTelemetryV2Stat"] = "True";
            speed["FFlagDebugDisableTelemetryPoint"] = "True";
            speed["FFlagDebugDisableLoadingScreen"] = "True";
            speed["DFIntLoadingScreenDelay"] = "0";
            speed["FFlagRenderDebugCheckThreading2"] = "True";
            speed["FFlagDebugGraphicsSkipVramChecks"] = "True";
            speed["FFlagRenderDebugCheckThreading2"] = "True";

            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] targets =
            {
                Path.Combine(localApp, "Froststrap", "Modifications", "ClientSettings", "ClientAppSettings.json"),
                Path.Combine(localApp, "Roblox", "ClientAppSettings.json")
            };
            int written = 0;
            foreach (string file in targets)
            {
                try
                {
                    string dir = Path.GetDirectoryName(file);
                    if (!Directory.Exists(dir)) continue;
                    var flags = ReadFlagsFile(file);
                    foreach (System.Collections.Generic.KeyValuePair<string, string> kv in speed)
                    {
                        flags[kv.Key] = kv.Value;
                    }
                    File.WriteAllText(file, BuildFlagsJson(flags));
                    written++;
                }
                catch { }
            }
            SetStatus(written > 0
                ? "Fast flags applied (merged with your existing settings)."
                : "No Froststrap/Roblox config folder found - install Froststrap first, then try again.", written > 0);
        }

        // Removes only the speed flags I added, keeping any other user flags.
        private void RevertFastFlags()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] targets =
            {
                Path.Combine(localApp, "Froststrap", "Modifications", "ClientSettings", "ClientAppSettings.json"),
                Path.Combine(localApp, "Roblox", "ClientAppSettings.json")
            };
            int removed = 0;
            foreach (string file in targets)
            {
                try
                {
                    var flags = ReadFlagsFile(file);
                    if (flags.Count == 0) continue;
                    bool changed = false;
                    foreach (string key in SpeedFlagKeys)
                    {
                        if (flags.Remove(key)) changed = true;
                    }
                    if (changed)
                    {
                        if (flags.Count > 0) File.WriteAllText(file, BuildFlagsJson(flags));
                        else File.Delete(file);
                        removed++;
                    }
                }
                catch { }
            }
            SetStatus(removed > 0
                ? "Speed flags reverted - your other settings (like CSG detail distances) are untouched."
                : "No speed flags found to remove.", removed > 0);
        }

        private static string DetectFroststrap()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] roots =
            {
                localApp,
                Path.Combine(localApp, "Programs"),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Path.Combine(localApp, "Roblox"),
                Path.Combine(localApp, "Programs", "Roblox")
            };
            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                try
                {
                    foreach (string dir in Directory.GetDirectories(root))
                    {
                        string name = Path.GetFileName(dir);
                        if (name.IndexOf("Froststrap", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string candidate = Path.Combine(dir, "Froststrap.exe");
                            if (File.Exists(candidate)) return candidate;
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        private static string HistoryFile()
        {
            // Lives next to the exe, same as settings.
            return Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "history.txt");
        }

        private List<string> LoadHistory()
        {
            var result = new List<string>();
            try
            {
                if (File.Exists(HistoryFile()))
                {
                    foreach (string line in File.ReadAllLines(HistoryFile()))
                    {
                        string t = line.Trim();
                        if (t.Length > 0 && !result.Contains(t)) result.Add(t);
                        if (result.Count >= 8) break;
                    }
                }
            }
            catch { }
            return result;
        }

        private static string FetchGameName(string placeId)
        {
            try
            {
                using (var wc = new System.Net.WebClient())
                {
                    wc.Headers.Add("User-Agent", "ArcticJoiner");
                    // place -> universe -> game name
                    string universeJson = wc.DownloadString(
                        "https://apis.roproxy.com/universes/v1/places/" + placeId + "/universe");
                    var uniMatch = System.Text.RegularExpressions.Regex.Match(universeJson, "\"universeId\"\\s*:\\s*(\\d+)");
                    if (!uniMatch.Success) return null;
                    string json = wc.DownloadString(
                        "https://games.roproxy.com/v1/games?universeIds=" + uniMatch.Groups[1].Value);
                    var nameMatch = System.Text.RegularExpressions.Regex.Match(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                    return nameMatch.Success ? nameMatch.Groups[1].Value : null;
                }
            }
            catch { return null; }
        }

        private void FetchGameNameAndToast(string deepLink)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(deepLink, "placeId=(\\d+)");
                if (!match.Success) return;
                string name = FetchGameName(match.Groups[1].Value) ?? "the game";
                SetStatusUi("Launched " + name + ".", false);
                try
                {
                    Invoke((MethodInvoker)delegate
                    {
                        if (_tray != null)
                        {
                            _tray.ShowBalloonTip(2500, "Arctic Joiner", "Launched " + name + ".", ToolTipIcon.Info);
                        }
                    });
                }
                catch { }
            }
            catch { }
        }

        private void RecordHistory(string raw)
        {
            string entry = (raw ?? "").Trim();
            if (entry.Length == 0) return;
            _history.RemoveAll(h => h.Equals(entry, StringComparison.OrdinalIgnoreCase));
            _history.Insert(0, entry);
            if (_history.Count > 8) _history.RemoveRange(8, _history.Count - 8);
            try
            {
                string file = HistoryFile();
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllLines(file, _history.ToArray());
            }
            catch { }
            RenderHistoryItems();

            // Resolve a readable game name in the background for this new entry.
            var match = System.Text.RegularExpressions.Regex.Match(entry, "placeId=(\\d+)");
            if (match.Success)
            {
                string placeId = match.Groups[1].Value;
                string copy = entry;
                System.Threading.Tasks.Task.Run((Action)(() =>
                {
                    string name = FetchGameName(placeId);
                    if (name == null) return;
                    try
                    {
                        Invoke((MethodInvoker)delegate
                        {
                            _historyNames[copy] = name;
                            RenderHistoryItems();
                            BuildTrayIcon();
                        });
                    }
                    catch { }
                }));
            }
        }

        private System.Collections.Generic.Dictionary<string, string> _historyNames =
            new System.Collections.Generic.Dictionary<string, string>();

        private string HistoryDisplay(string entry)
        {
            string name;
            if (_historyNames.TryGetValue(entry, out name)) return name;
            return entry;
        }

        private void RenderHistoryItems()
        {
            _linkBox.Items.Clear();
            foreach (string item in _history) _linkBox.Items.Add(HistoryDisplay(item));
        }
    }
}

namespace ArcticJoiner
{
    internal static class LinkParser
    {
        // Returns a roblox:// deep link for any supported input, or null if nothing usable.
        public static string Parse(string raw)
        {
            string text = (raw ?? "").Trim();
            if (text.Length == 0) return null;

            // Already a deep link - pass straight through.
            if (text.StartsWith("roblox://", StringComparison.OrdinalIgnoreCase)) return text;

            // Our own protocol from the website: arcticjoiner://placeId=... works the same.
            if (text.StartsWith("arcticjoiner://", StringComparison.OrdinalIgnoreCase))
            {
                return "roblox://" + text.Substring("arcticjoiner://".Length);
            }

            string placeId = null, instanceId = null, launchData = null, privateCode = null;

            if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                string url = text.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? "https://" + text : text;
                try
                {
                    var uri = new Uri(url);
                    var query = ParseQuery(uri.Query);
                    string path = uri.AbsolutePath;

                    // /share?code=...&type=Server
                    if (path.IndexOf("/share", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        privateCode = Get(query, "code");
                    }

                    if (privateCode == null)
                    {
                        placeId = Get(query, "placeId");
                        instanceId = FirstNonEmpty(Get(query, "gameInstanceId"), Get(query, "jobId"));
                        launchData = Get(query, "launchData");
                        privateCode = FirstNonEmpty(
                            Get(query, "privateServerLinkCode"),
                            Get(query, "privateServerCode"));

                        // /games/12345/Game-Name (ID in the path, no query param)
                        if (placeId == null && path.IndexOf("/games/", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string[] parts = path.Split('/');
                            long parsedId;
                            for (int i = 0; i < parts.Length - 1; i++)
                            {
                                if (parts[i].Equals("games", StringComparison.OrdinalIgnoreCase) &&
                                    long.TryParse(parts[i + 1], out parsedId))
                                {
                                    placeId = parts[i + 1];
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (UriFormatException)
                {
                    return null;
                }
            }
            else
            {
                // Bare pasted value: accept a bare placeId (digits) or a bare share code.
                if (IsDigits(text)) placeId = text;
                else if (LooksLikeShareCode(text)) privateCode = NormalizeCode(text);
                else return null;
            }

            if (privateCode != null)
            {
                return "roblox://navigation/share_links?code=" + Uri.EscapeDataString(privateCode) + "&type=Server";
            }
            if (placeId != null)
            {
                var sb = new StringBuilder("roblox://placeId=").Append(placeId);
                if (instanceId != null) sb.Append("&gameInstanceId=").Append(Uri.EscapeDataString(instanceId));
                if (launchData != null) sb.Append("&launchData=").Append(Uri.EscapeDataString(launchData));
                return sb.ToString();
            }
            return null; // a launch key without a place ID cannot join anything
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(query)) return result;
            string q = query.StartsWith("?") ? query.Substring(1) : query;
            foreach (string pair in q.Split('&'))
            {
                if (pair.Length == 0) continue;
                int eq = pair.IndexOf('=');
                string key = eq < 0 ? pair : pair.Substring(0, eq);
                string val = eq < 0 ? "" : pair.Substring(eq + 1);
                key = Uri.UnescapeDataString(key).Replace('+', ' ').Trim();
                val = Uri.UnescapeDataString(val).Replace('+', ' ').Trim();
                if (key.Length > 0 && !result.ContainsKey(key)) result[key] = val;
            }
            return result;
        }

        private static string Get(Dictionary<string, string> query, string key)
        {
            string v;
            return query.TryGetValue(key, out v) && v.Length > 0 ? v : null;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string v in values) if (!string.IsNullOrEmpty(v)) return v;
            return null;
        }

        private static bool IsDigits(string text)
        {
            foreach (char c in text) if (c < '0' || c > '9') return false;
            return text.Length > 0;
        }

        private static bool LooksLikeShareCode(string text)
        {
            // Share/private codes are long alphanumeric blobs with no spaces or slashes.
            foreach (char c in text)
            {
                if (!char.IsLetterOrDigit(c)) return false;
            }
            return text.Length >= 16;
        }

        private static string NormalizeCode(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text) if (char.IsLetterOrDigit(c)) sb.Append(c);
            return sb.ToString();
        }
    }
}

namespace ArcticJoiner
{
    // Fetches updates from the GitHub Pages repo (main branch, windows-app folder).
    internal static class Updater
    {
        public const string Version = "2.5.0";

        // A double-quote character, used when building compiler arguments
        // without needing escaped quotes in the source.
        public const char Q = '"';

        public const string BaseUrl =
            "https://raw.githubusercontent.com/Arctic0Dev/Arctic0Dev.github.io/main/windows-app/";
        public const string VersionUrl = BaseUrl + "version.txt";
        public const string SourceUrl = BaseUrl + "ArcticJoiner.cs";
        public const string IconUrl = BaseUrl + "icon.ico";

        static Updater()
        {
            // Old .NET Framework defaults may not negotiate TLS 1.2, which GitHub
            // requires. Force it when possible (silently ignored if unsupported).
            try
            {
                System.Net.ServicePointManager.SecurityProtocol =
                    (System.Net.SecurityProtocolType)3072;
            }
            catch { }
        }

        // Compares dotted versions, e.g. "1.2.0" > "1.1.9".
        public static bool IsNewer(string remote, string local)
        {
            if (string.IsNullOrWhiteSpace(remote) || string.IsNullOrWhiteSpace(local)) return false;
            int[] r = ParseVersion(remote);
            int[] l = ParseVersion(local);
            for (int i = 0; i < 3; i++)
            {
                if (r[i] != l[i]) return r[i] > l[i];
            }
            return false;
        }

        private static int[] ParseVersion(string text)
        {
            var parts = new int[3];
            string[] chunks = (text ?? "").Trim().Split('.');
            for (int i = 0; i < 3; i++)
            {
                int n;
                parts[i] = (i < chunks.Length && int.TryParse(chunks[i], out n)) ? n : 0;
            }
            return parts;
        }

        // GitHub's raw-file CDN can serve stale content for a few minutes after
        // a push, so downloads always carry a unique cache-busting query.
        private static string Busted(string url)
        {
            return url + (url.IndexOf('?') >= 0 ? "&" : "?") + "t=" +
                   System.DateTime.UtcNow.Ticks.ToString();
        }

        // Downloads via the GitHub API (not aggressively cached, unlike
        // raw.githubusercontent.com which can serve stale content on some
        // networks even with cache-busting queries). Falls back to raw.
        private static string ApiUrlFor(string rawUrl)
        {
            if (rawUrl.StartsWith(BaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return "https://api.github.com/repos/Arctic0Dev/Arctic0Dev.github.io/contents/windows-app/" +
                       rawUrl.Substring(BaseUrl.Length);
            }
            return null;
        }

        private static System.Net.WebClient NewApiClient()
        {
            var wc = new System.Net.WebClient();
            wc.Headers.Add("User-Agent", "ArcticJoiner");
            wc.Headers.Add("Accept", "application/vnd.github.raw");
            return wc;
        }

        public static string DownloadText(string url)
        {
            string api = ApiUrlFor(url);
            if (api != null)
            {
                try
                {
                    using (var wc = NewApiClient()) { return wc.DownloadString(api); }
                }
                catch { }
            }
            using (var wc = new System.Net.WebClient())
            {
                return wc.DownloadString(Busted(url));
            }
        }

        public static void DownloadFile(string url, string dest)
        {
            string api = ApiUrlFor(url);
            if (api != null)
            {
                try
                {
                    using (var wc = NewApiClient())
                    {
                        System.IO.File.WriteAllBytes(dest, wc.DownloadData(api));
                    }
                    return;
                }
                catch { }
            }
            using (var wc = new System.Net.WebClient())
            {
                wc.DownloadFile(Busted(url), dest);
            }
        }

        public static string FindCsc()
        {
            string[] roots =
            {
                System.Environment.Is64BitProcess
                    ? System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows", "Microsoft.NET\\Framework64")
                    : System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows", "Microsoft.NET\\Framework")
            };
            foreach (string root in roots)
            {
                if (!System.IO.Directory.Exists(root)) continue;
                // Prefer the newest installed Framework version.
                string best = null;
                foreach (string dir in System.IO.Directory.GetDirectories(root, "v4.0.*"))
                {
                    string candidate = System.IO.Path.Combine(dir, "csc.exe");
                    if (System.IO.File.Exists(candidate)) best = candidate;
                }
                if (best != null) return best;
            }
            return null;
        }

        // Compiles an updated source file. Returns null on success or an error message.
        public static string CompileUpdate(string csPath, string iconPath, string outPath)
        {
            string csc = FindCsc();
            if (csc == null)
            {
                return "no .NET Framework C# compiler found on this PC";
            }
            string extras = "";
            if (iconPath != null && System.IO.File.Exists(iconPath))
            {
                extras = " /win32icon:" + Q + iconPath + Q +
                         " /resource:" + Q + csPath + Q + ",ArcticJoiner.cs";
            }
            var psi = new ProcessStartInfo
            {
                FileName = csc,
                Arguments = "/nologo /target:winexe /optimize+" + extras +
                            " /out:" + Q + outPath + Q +
                            " /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll " + Q + csPath + Q,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0 || !System.IO.File.Exists(outPath))
                {
                    string trimmed = (output ?? "").Trim();
                    return trimmed.Length > 0 ? trimmed : "compiler exited with code " + p.ExitCode;
                }
            }
            return null;
        }
    }
}
