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
        public bool AutoRejoin = false;   // rejoin the last server if Roblox closes
        public int WindowLeft = -1;       // remembered main-window position (-1 = centered)
        public int WindowTop = -1;

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
                        else if (key == "autoRejoin") s.AutoRejoin = val == "1";
                        else if (key == "windowLeft") { int n; if (int.TryParse(val, out n)) s.WindowLeft = n; }
                        else if (key == "windowTop") { int n; if (int.TryParse(val, out n)) s.WindowTop = n; }
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
                    .AppendLine("autoRejoin=" + (AutoRejoin ? "1" : "0"))
                    .AppendLine("windowLeft=" + WindowLeft)
                    .AppendLine("windowTop=" + WindowTop)
                    .ToString());
            }
            catch { }
        }
    }

    // One public server of a game, as returned by the servers/Public API.
    internal sealed class ServerInfo
    {
        public string Id;
        public int Playing;
        public int MaxPlayers;

        public override string ToString()
        {
            return Playing + " / " + MaxPlayers + " players  -  " + Id;
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
        private readonly Button _updateButtonPanel;
        private readonly ComboBox _linkBox;
        private readonly Label _hotkeyLabel;
        private readonly Label _extractLabel;
        private readonly Button _changeHotkeyButton;
        private readonly Button _changeDeleteKeyButton;
        private readonly Button _serversButton;
        private readonly Button _clearHistoryButton;
        private readonly CheckBox _rejoinCheck;
        private readonly List<string> _history = new List<string>();
        private ToolTip _prevTip;
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
        private string _lastJoinLink = "";
        private string _lastJoinPlaceId = null;
        private string _lastJoinServerId = null;
        private bool _rejoinArmed;
        private bool _robloxWasRunning;
        private System.Windows.Forms.Timer _rejoinTimer;

        public JoinerForm(Settings settings)
        {
            _settings = settings;

            Text = "Arctic Joiner";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            if (_settings.WindowLeft != -1 && _settings.WindowTop != -1 &&
                SystemInformation.VirtualScreen.Contains(new Point(_settings.WindowLeft, _settings.WindowTop)))
            {
                StartPosition = FormStartPosition.Manual;
                Location = new Point(_settings.WindowLeft, _settings.WindowTop);
            }
            else
            {
                StartPosition = FormStartPosition.CenterScreen;
            }
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
                Size = new Size(110, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _joinButton.Click += (s, e) => TryJoin(_linkBox.Text, closeAfter: false);

            _prevButton = new Button
            {
                Text = "Prev Server",
                Location = new Point(134, 103),
                Size = new Size(160, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _prevTip = new ToolTip();
            _prevTip.SetToolTip(_prevButton, "The server you joined most recently.");
            _prevButton.Click += (s, e) =>
            {
                if (_history.Count == 0)
                {
                    SetStatus("No previous server to rejoin yet - join a game first.", true);
                    return;
                }
                string entry = _history[0];
                string target = HistoryTargetLabel(entry);
                SetStatus("Rejoining " + target + "...", false);
                if (TryJoin(entry, closeAfter: false))
                {
                    SetStatus("Rejoined " + target + ".", false);
                }
            };
            UpdatePrevButton();

            _serversButton = new Button
            {
                Text = "Servers",
                Location = new Point(440, 103),
                Size = new Size(164, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _serversButton.Click += (s, e) => OpenServerBrowser();

            _clearHistoryButton = new Button
            {
                Text = "Clear history",
                Location = new Point(0, 290),
                Size = new Size(160, 26)
            };
            _clearHistoryButton.Click += (s, e) => ClearHistory();

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
                Text = "Settings",
                Location = new Point(302, 103),
                Size = new Size(130, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _openSettingsButton.Click += (s, e) =>
            {
                _settingsPanel.Visible = !_settingsPanel.Visible;
                ClientSize = new Size(ClientSize.Width, _settingsPanel.Visible ? 533 : 235);
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
                Size = new Size(588, 330),
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
                Size = new Size(588, 30),
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

            // Mirror of the main install button, so an available update can be
            // installed from inside this panel too (the main button sits behind
            // the panel while it is open).
            _updateButtonPanel = new Button
            {
                Text = "",
                Visible = false,
                Enabled = false,
                Location = new Point(396, 56),
                Size = new Size(104, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _updateButtonPanel.Click += (s, e) => RunSelfUpdate();

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
                Location = new Point(0, 232),
                Size = new Size(220, 26)
            };
            fastFlagsButton.Click += (s, e) =>
            {
                ApplyFastFlags();
            };

            var revertFlagsButton = new Button
            {
                Text = "Revert fast flags",
                Location = new Point(230, 232),
                Size = new Size(120, 26)
            };
            revertFlagsButton.Click += (s, e) =>
            {
                RevertFastFlags();
            };

            _rejoinCheck = new CheckBox
            {
                Text = "Auto-rejoin if Roblox closes or crashes",
                Checked = _settings.AutoRejoin,
                AutoSize = true,
                Location = new Point(0, 264)
            };
            _rejoinCheck.CheckedChanged += (s, e) =>
            {
                _settings.AutoRejoin = _rejoinCheck.Checked;
                _settings.Save();
                if (_rejoinCheck.Checked) StartRejoinWatch();
                else { _rejoinArmed = false; if (_rejoinTimer != null) _rejoinTimer.Stop(); }
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
                Text = "Arctic Joiner - PRESS A KEY NOW (Esc to cancel)";
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

            _changeDeleteKeyButton = new Button
            {
                Text = "Change key (" + DeleteDescription() + ")...",
                Location = new Point(508, 126),
                Size = new Size(80, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _changeDeleteKeyButton.Click += (s, e) =>
            {
                _captureHotkey = true;
                _captureTarget = 2;
                UnregisterHotkey();
                ActiveControl = _changeDeleteKeyButton;
                Text = "Arctic Joiner - PRESS A KEY NOW (Esc to cancel)";
                SetStatus("Press the new extract key now (Esc to cancel) - nothing will be pasted.", false);
            };

            _settingsPanel.Controls.Add(pathLabel);
            _settingsPanel.Controls.Add(_froststrapPathBox);
            _settingsPanel.Controls.Add(_browseButton);
            _settingsPanel.Controls.Add(checkUpdateButton);
            _settingsPanel.Controls.Add(_updateButtonPanel);
            _settingsPanel.Controls.Add(_hotkeyLabel);
            _settingsPanel.Controls.Add(_changeHotkeyButton);
            _settingsPanel.Controls.Add(fastFlagsButton);
            _settingsPanel.Controls.Add(revertFlagsButton);
            _settingsPanel.Controls.Add(_rejoinCheck);
            _settingsPanel.Controls.Add(_clearHistoryButton);
            _settingsPanel.Controls.Add(_extractLabel);
            _settingsPanel.Controls.Add(_changeDeleteKeyButton);
            _settingsPanel.Controls.Add(hint);

            Controls.Add(pasteLabel);
            Controls.Add(_linkBox);
            Controls.Add(_joinButton);
            Controls.Add(_status);
            Controls.Add(_autoJoinCheck);
            Controls.Add(_closeAfterCheck);
            Controls.Add(_openSettingsButton);
            Controls.Add(_prevButton);
            Controls.Add(_serversButton);
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
            if (_settings.AutoRejoin) StartRejoinWatch();

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

        public bool TryJoin(string raw, bool closeAfter)
        {
            raw = ResolveHistoryEntry(raw);
            string deepLink = LinkParser.Parse(raw);
            if (deepLink == null)
            {
                SetStatus("Could not read a place ID, server ID, launch data, or private code from that input.", true);
                return false;
            }

            // Remember this join for auto-rejoin and the server browser.
            _lastJoinLink = deepLink;
            string joinedPlace = PlaceIdOf(deepLink);
            if (joinedPlace != null) _lastJoinPlaceId = joinedPlace;
            string joinedServer = GameInstanceIdOf(deepLink);
            if (joinedServer != null) _lastJoinServerId = joinedServer;

            try
            {
                _rejoinArmed = false; // pause auto-rejoin while we intentionally swap clients
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

                if (_settings.AutoRejoin)
                {
                    _rejoinArmed = true;
                    _robloxWasRunning = IsRobloxRunning();
                }

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
                return true;
            }
            catch (Exception ex)
            {
                SetStatus("Launch failed: " + ex.Message + " (Is Froststrap installed?)", true);
                return false;
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
                        if (_updateButtonPanel != null)
                        {
                            _updateButtonPanel.Text = "Install v" + newer;
                            _updateButtonPanel.Visible = true;
                            _updateButtonPanel.Enabled = true;
                        }
                    });
                }
                catch { }
            }));
        }

        private void RunSelfUpdate()
        {
            _updateButton.Enabled = false;
            if (_updateButtonPanel != null) _updateButtonPanel.Enabled = false;
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
                        Invoke((MethodInvoker)delegate
                        {
                            _updateButton.Enabled = true;
                            if (_updateButtonPanel != null) _updateButtonPanel.Enabled = true;
                        });
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
            if (_history.Count > 0)
            {
                var removeMenu = new ToolStripMenuItem("Remove a recent server...");
                foreach (string entry in _history)
                {
                    string copy = entry;
                    removeMenu.DropDownItems.Add(HistoryDisplay(copy), null, (s, e) => RemoveHistoryEntry(copy));
                }
                menu.Items.Add(removeMenu);
                menu.Items.Add("Clear history", null, (s, e) => ClearHistory());
            }
            menu.Items.Add("Open Arctic Joiner", null, (s, e) => { Show(); Activate(); });
            menu.Items.Add("Live server stats", null, (s, e) => OpenLiveServer());
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

        private void SaveWindowPosition()
        {
            try
            {
                if (WindowState != FormWindowState.Normal) return;
                _settings.WindowLeft = Location.X;
                _settings.WindowTop = Location.Y;
                _settings.Save();
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveWindowPosition();
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
            if (_rejoinTimer != null) _rejoinTimer.Stop();
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
            if (_changeDeleteKeyButton != null)
            {
                _changeDeleteKeyButton.Text = "Change key (" + DeleteDescription() + ")...";
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
                    Text = "Arctic Joiner";
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
                    string label;
                    string desc;
                    if (_captureTarget == 2)
                    {
                        _settings.DeleteMods = mods;
                        _settings.DeleteKey = e.KeyCode.ToString();
                        label = "Extract keybind";
                        desc = DeleteDescription();
                    }
                    else
                    {
                        _settings.HotkeyMods = mods;
                        _settings.HotkeyKey = e.KeyCode.ToString();
                        label = "Hotkey";
                        desc = HotkeyDescription();
                    }
                    _settings.Save(); // saved immediately, no extra step
                    _captureHotkey = false;
                    ApplyHotkey();
                    Text = "Arctic Joiner";
                    SetStatus(label + " set to " + desc + " and saved.", false);
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
            "FFlagRenderDebugCheckThreading2",
            "FFlagDebugGraphicsSkipVramChecks"
        };

        // A tiny, strict JSON reader for the flat flag files. It accepts one
        // object of string keys with string/number/bool values and fails on
        // anything unexpected. A failed parse means "leave this file alone", so
        // a minified or hand-edited file can never be wiped by a rewrite.
        // (The old line-based reader lost every flag after the first line of a
        // minified file - that is how custom settings could disappear.)
        private static bool TryParseFlagsJson(string text, System.Collections.Generic.Dictionary<string, string> result)
        {
            result.Clear();
            if (text == null) return false;
            int i = 0;
            int n = text.Length;
            while (i < n && char.IsWhiteSpace(text[i])) i++;
            if (i >= n || text[i] != '{') return false;
            i++;
            while (true)
            {
                while (i < n && char.IsWhiteSpace(text[i])) i++;
                if (i >= n) return false;
                if (text[i] == '}') { i++; break; }
                if (result.Count > 0)
                {
                    if (text[i] != ',') return false;
                    i++;
                    while (i < n && char.IsWhiteSpace(text[i])) i++;
                    if (i >= n) return false;
                }
                string key;
                if (text[i] != '"' || !TryReadJsonString(text, ref i, out key)) return false;
                while (i < n && char.IsWhiteSpace(text[i])) i++;
                if (i >= n || text[i] != ':') return false;
                i++;
                while (i < n && char.IsWhiteSpace(text[i])) i++;
                if (i >= n) return false;
                string value;
                if (text[i] == '"')
                {
                    if (!TryReadJsonString(text, ref i, out value)) return false;
                }
                else
                {
                    int start = i;
                    while (i < n && text[i] != ',' && text[i] != '}' && !char.IsWhiteSpace(text[i])) i++;
                    if (i == start) return false;
                    value = text.Substring(start, i - start);
                    if (value != "true" && value != "false" && value != "null")
                    {
                        double number;
                        if (!double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out number)) return false;
                    }
                }
                result[key] = value;
            }
            while (i < n && char.IsWhiteSpace(text[i])) i++;
            return i == n;
        }

        private static bool TryReadJsonString(string text, ref int i, out string value)
        {
            i++;
            var sb = new StringBuilder();
            while (true)
            {
                if (i >= text.Length) { value = null; return false; }
                char c = text[i];
                if (c == '"') { i++; value = sb.ToString(); return true; }
                if (c == '\\')
                {
                    i++;
                    if (i >= text.Length) { value = null; return false; }
                    char e = text[i];
                    if (e == 'n') sb.Append('\n');
                    else if (e == 't') sb.Append('\t');
                    else if (e == 'r') sb.Append('\r');
                    else if (e == 'b') sb.Append('\b');
                    else if (e == 'f') sb.Append('\f');
                    else if (e == 'u')
                    {
                        if (i + 4 >= text.Length) { value = null; return false; }
                        int code;
                        if (!int.TryParse(text.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out code))
                        {
                            value = null;
                            return false;
                        }
                        sb.Append((char)code);
                        i += 4;
                    }
                    else sb.Append(e);
                    i++;
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }
        }

        private static string EscapeJsonText(string value)
        {
            var sb = new StringBuilder();
            foreach (char c in value ?? "")
            {
                if (c == '"') sb.Append("\\\"");
                else if (c == '\\') sb.Append("\\\\");
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\t') sb.Append("\\t");
                else if (c == '\r') sb.Append("\\r");
                else sb.Append(c);
            }
            return sb.ToString();
        }

        // Returns null when the file exists but cannot be parsed safely - callers
        // must skip null results instead of rewriting the file.
        private static System.Collections.Generic.Dictionary<string, string> ReadFlagsFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return new System.Collections.Generic.Dictionary<string, string>();
                var flags = new System.Collections.Generic.Dictionary<string, string>();
                if (!TryParseFlagsJson(File.ReadAllText(path), flags)) return null;
                return flags;
            }
            catch
            {
                return null;
            }
        }

        private static string BuildFlagsJson(System.Collections.Generic.Dictionary<string, string> flags)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            int i = 0;
            foreach (System.Collections.Generic.KeyValuePair<string, string> kv in flags)
            {
                if (i++ > 0) sb.AppendLine(",");
                sb.Append("  \"").Append(EscapeJsonText(kv.Key)).Append("\": \"").Append(EscapeJsonText(kv.Value)).Append("\"");
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


            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] targets =
            {
                Path.Combine(localApp, "Froststrap", "Modifications", "ClientSettings", "ClientAppSettings.json"),
                Path.Combine(localApp, "Roblox", "ClientSettings", "ClientAppSettings.json"),
                Path.Combine(localApp, "Roblox", "ClientAppSettings.json")
            };
            int written = 0;
            int skipped = 0;
            foreach (string file in targets)
            {
                try
                {
                    string dir = Path.GetDirectoryName(file);
                    if (!Directory.Exists(dir))
                    {
                        // Create the folder only when its parent exists, so we never
                        // invent a Roblox/Froststrap install that is not there.
                        string parent = Path.GetDirectoryName(dir);
                        if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent)) continue;
                        try { Directory.CreateDirectory(dir); } catch { continue; }
                    }
                    var flags = ReadFlagsFile(file);
                    if (flags == null)
                    {
                        skipped++; // unreadable file - never risk wiping it
                        continue;
                    }
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
                ? "Fast flags applied to " + written + " file(s) (merged with your existing settings)." +
                  (skipped > 0 ? " " + skipped + " file(s) were left untouched because they could not be read safely." : "")
                : "No Froststrap/Roblox config folder found - install Froststrap first, then try again.", written > 0);
        }

        // Removes only the speed flags I added, keeping any other user flags.
        private void RevertFastFlags()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] targets =
            {
                Path.Combine(localApp, "Froststrap", "Modifications", "ClientSettings", "ClientAppSettings.json"),
                Path.Combine(localApp, "Roblox", "ClientSettings", "ClientAppSettings.json"),
                Path.Combine(localApp, "Roblox", "ClientAppSettings.json")
            };
            int removed = 0;
            foreach (string file in targets)
            {
                try
                {
                    var flags = ReadFlagsFile(file);
                    if (flags == null || flags.Count == 0) continue;
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

        internal static string FetchGameName(string placeId)
        {
            // This is a desktop app, not a browser, so Roblox's API can be called
            // directly - the roproxy mirror is only a fallback for when the direct
            // route is unreachable.
            string name = TryFetchGameName(placeId, "https://apis.roblox.com", "https://games.roblox.com");
            if (name != null) return name;
            return TryFetchGameName(placeId, "https://apis.roproxy.com", "https://games.roproxy.com");
        }

        private static string TryFetchGameName(string placeId, string apiBase, string gamesBase)
        {
            try
            {
                using (var wc = new System.Net.WebClient())
                {
                    wc.Headers.Add("User-Agent", "ArcticJoiner");
                    // place -> universe -> game name
                    string universeJson = wc.DownloadString(apiBase + "/universes/v1/places/" + placeId + "/universe");
                    var uniMatch = System.Text.RegularExpressions.Regex.Match(universeJson, "\"universeId\"\\s*:\\s*(\\d+)");
                    if (!uniMatch.Success) return null;
                    string json = wc.DownloadString(gamesBase + "/v1/games?universeIds=" + uniMatch.Groups[1].Value);
                    var nameMatch = System.Text.RegularExpressions.Regex.Match(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                    return nameMatch.Success ? nameMatch.Groups[1].Value : null;
                }
            }
            catch { return null; }
        }

        // Fetches every public server of a place (id + player counts). The app is
        // a desktop program, so it calls Roblox's API directly (no browser CORS
        // limits); the roproxy mirror is only a fallback. Returns an empty list
        // when both routes fail.
        internal static List<ServerInfo> FetchServers(string placeId)
        {
            List<ServerInfo> list = TryFetchServersFrom(
                "https://games.roblox.com/v1/games/" + placeId + "/servers/Public?limit=100");
            if (list != null) return list;
            list = TryFetchServersFrom(
                "https://games.roproxy.com/v1/games/" + placeId + "/servers/Public?limit=100");
            return list != null ? list : new List<ServerInfo>();
        }

        // Returns null when the request fails; an empty list means the game really
        // has no public servers right now.
        private static List<ServerInfo> TryFetchServersFrom(string url)
        {
            try
            {
                using (var wc = new System.Net.WebClient())
                {
                    wc.Headers.Add("User-Agent", "ArcticJoiner");
                    string json = wc.DownloadString(url);
                    var list = new List<ServerInfo>();
                    foreach (string chunk in json.Split(new string[] { "\"id\":\"" }, StringSplitOptions.None))
                    {
                        int q = chunk.IndexOf('"');
                        if (q <= 0) continue;
                        string id = chunk.Substring(0, q);
                        if (!IsServerId(id)) continue;
                        list.Add(new ServerInfo
                        {
                            Id = id,
                            Playing = ExtractInt(chunk, "\"playing\":"),
                            MaxPlayers = ExtractInt(chunk, "\"maxPlayers\":")
                        });
                    }
                    return list;
                }
            }
            catch { return null; }
        }

        // Server ids are GUIDs; player ids in the same payload are numbers, so
        // this keeps us from mistaking players for servers.
        internal static bool IsServerId(string value)
        {
            if (value == null || value.Length != 36) return false;
            foreach (char c in value)
            {
                bool hex = char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex && c != '-') return false;
            }
            return true;
        }

        internal static int ExtractInt(string text, string key)
        {
            int i = text.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return 0;
            i += key.Length;
            int j = i;
            while (j < text.Length && char.IsDigit(text[j])) j++;
            int n;
            return int.TryParse(text.Substring(i, j - i), out n) ? n : 0;
        }

        // The least-full server that still has room, or null.
        internal static ServerInfo PickEmptiest(List<ServerInfo> servers)
        {
            ServerInfo best = null;
            foreach (ServerInfo s in servers)
            {
                if (s.MaxPlayers > 0 && s.Playing >= s.MaxPlayers) continue;
                if (best == null || s.Playing < best.Playing) best = s;
            }
            return best;
        }

        // Pulls the gameInstanceId (the server) out of a deep link.
        private static string GameInstanceIdOf(string text)
        {
            var m = System.Text.RegularExpressions.Regex.Match(text ?? "", "gameInstanceId=([^&]+)");
            return m.Success ? m.Groups[1].Value : null;
        }

        // Pulls the placeId out of any link/deep link we have stored.
        internal static string PlaceIdOf(string text)
        {
            var m = System.Text.RegularExpressions.Regex.Match(text ?? "", "placeId=(\\d+)");
            if (m.Success) return m.Groups[1].Value;
            m = System.Text.RegularExpressions.Regex.Match(text ?? "", "/games/(\\d+)");
            return m.Success ? m.Groups[1].Value : null;
        }

        private void FetchGameNameAndToast(string deepLink)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(deepLink, "placeId=(\\d+)");
                if (!match.Success) return;
                string name = FetchGameName(match.Groups[1].Value) ?? "the game";
                string server = ServerIdFrom(deepLink);
                string label = name + (server.Length > 0 ? " - server " + ShortId(server) : "");
                SetStatusUi("Launched " + label + ".", false);
                try
                {
                    Invoke((MethodInvoker)delegate
                    {
                        if (_tray != null)
                        {
                            _tray.ShowBalloonTip(2500, "Arctic Joiner", "Launched " + label + ".", ToolTipIcon.Info);
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
            UpdatePrevButton();
        }

        // ---- History helpers ----

        private void RemoveHistoryEntry(string raw)
        {
            _history.RemoveAll(h => h.Equals(raw, StringComparison.OrdinalIgnoreCase));
            _historyNames.Remove(raw);
            try { File.WriteAllLines(HistoryFile(), _history.ToArray()); } catch { }
            RenderHistoryItems();
            BuildTrayIcon();
            SetStatus("Removed one recent server.", false);
        }

        private void ClearHistory()
        {
            _history.Clear();
            _historyNames.Clear();
            try { if (File.Exists(HistoryFile())) File.Delete(HistoryFile()); } catch { }
            RenderHistoryItems();
            BuildTrayIcon();
            SetStatus("Recent history cleared.", false);
        }

        private string FriendlyName(string entry)
        {
            string name;
            if (_historyNames.TryGetValue(entry, out name) && !string.IsNullOrWhiteSpace(name)) return name;
            var m = System.Text.RegularExpressions.Regex.Match(entry ?? "", "placeId=(\\d+)");
            if (m.Success) return "Place " + m.Groups[1].Value;
            var c = System.Text.RegularExpressions.Regex.Match(entry ?? "", "code=([^&]+)");
            if (c.Success) return "a private server";
            return "the previous server";
        }

        private static string ServerIdFrom(string entry)
        {
            entry = entry ?? "";
            var m = System.Text.RegularExpressions.Regex.Match(entry, "gameInstanceId=([^&]+)");
            if (m.Success) return m.Groups[1].Value;
            var c = System.Text.RegularExpressions.Regex.Match(entry, "code=([^&]+)");
            if (c.Success) return c.Groups[1].Value;
            return "";
        }

        private static string ShortId(string id)
        {
            id = id ?? "";
            return id.Length <= 8 ? id : id.Substring(0, 8);
        }

        private static string TruncateText(string value, int max)
        {
            value = value ?? "";
            if (value.Length <= max) return value;
            if (max <= 3) return value.Substring(0, max);
            return value.Substring(0, max - 3) + "...";
        }

        private string HistoryTargetLabel(string entry)
        {
            string name = FriendlyName(entry);
            string server = ServerIdFrom(entry);
            return server.Length > 0 ? name + " (server " + ShortId(server) + ")" : name;
        }

        private string CurrentPlaceId()
        {
            string p = PlaceIdOf(_linkBox.Text);
            if (p != null) return p;
            if (_lastJoinPlaceId != null) return _lastJoinPlaceId;
            if (_history.Count > 0) return PlaceIdOf(_history[0]);
            return null;
        }

        // ---- Live server stats ----

        internal string LivePlaceId() { return _lastJoinPlaceId; }
        internal string LiveServerId() { return _lastJoinServerId; }

        internal void SetLiveServer(string placeId, string serverId)
        {
            if (!string.IsNullOrEmpty(placeId)) _lastJoinPlaceId = placeId;
            if (!string.IsNullOrEmpty(serverId)) _lastJoinServerId = serverId;
        }

        private void OpenLiveServer()
        {
            var form = new LiveServerForm(this);
            form.Show(this);
        }

        private void OpenServerBrowser()
        {
            string placeId = CurrentPlaceId();
            var form = new ServerBrowserForm(placeId, (deepLink) => TryJoin(deepLink, closeAfter: false));
            form.Show(this);
        }

        // Watches the Roblox client; if it closes while armed, rejoins the last server.
        private void StartRejoinWatch()
        {
            if (_rejoinTimer == null)
            {
                _rejoinTimer = new System.Windows.Forms.Timer { Interval = 4000 };
                _rejoinTimer.Tick += (s, e) => CheckRejoin();
            }
            if (!_rejoinTimer.Enabled) _rejoinTimer.Start();
        }

        private static bool IsRobloxRunning()
        {
            try { return Process.GetProcessesByName("RobloxPlayerBeta").Length > 0; }
            catch { return false; }
        }

        private void CheckRejoin()
        {
            bool running = IsRobloxRunning();
            if (_rejoinArmed && _robloxWasRunning && !running)
            {
                _rejoinArmed = false; // one-shot: do not loop if it exits again
                if (!string.IsNullOrEmpty(_lastJoinLink))
                {
                    SetStatus("Roblox closed - rejoining your last server...", false);
                    TryJoin(_lastJoinLink, closeAfter: false);
                }
            }
            _robloxWasRunning = running;
        }

        private void UpdatePrevButton()
        {
            if (_prevButton == null) return;
            if (_history.Count == 0)
            {
                _prevButton.Text = "Prev Server";
                if (_prevTip != null) _prevTip.SetToolTip(_prevButton, "No previous server yet.");
                return;
            }
            string entry = _history[0];
            _prevButton.Text = "Prev: " + TruncateText(FriendlyName(entry), 12);
            if (_prevTip != null) _prevTip.SetToolTip(_prevButton, "Rejoin: " + HistoryTargetLabel(entry));
        }

    }
}

namespace ArcticJoiner
{
    // Lists a game's public servers with player counts; join any of them.
    internal sealed class ServerBrowserForm : Form
    {
        private readonly TextBox _placeBox;
        private readonly Button _loadBtn;
        private readonly Label _status;
        private readonly ListBox _list;
        private readonly Button _joinBtn;
        private readonly Button _emptiestBtn;
        private readonly Button _refreshBtn;
        private readonly CheckBox _autoRefreshCheck;
        private readonly Action<string> _onJoin;
        private System.Windows.Forms.Timer _autoTimer;
        private readonly List<ServerInfo> _servers = new List<ServerInfo>();
        private string _placeId;

        public ServerBrowserForm(string initialPlaceId, Action<string> onJoin)
        {
            _onJoin = onJoin;
            _placeId = initialPlaceId;

            Text = "Server Browser";
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(560, 470);

            var label = new Label { Text = "Place ID or game link:", AutoSize = true, Location = new Point(12, 14) };
            _placeBox = new TextBox { Location = new Point(12, 34), Width = 400, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _loadBtn = new Button { Text = "Load", Location = new Point(420, 32), Size = new Size(128, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _status = new Label { Location = new Point(12, 66), Size = new Size(536, 18), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _list = new ListBox
            {
                Location = new Point(12, 90),
                Size = new Size(536, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            _joinBtn = new Button { Text = "Join Selected", Location = new Point(12, 418), Size = new Size(170, 32), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            _emptiestBtn = new Button { Text = "Join Emptiest", Location = new Point(194, 418), Size = new Size(170, 32), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            _refreshBtn = new Button { Text = "Refresh", Location = new Point(376, 418), Size = new Size(172, 32), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            _autoRefreshCheck = new CheckBox { Text = "Auto-refresh every 30 seconds", AutoSize = true, Location = new Point(12, 394), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };

            Controls.Add(label);
            Controls.Add(_placeBox);
            Controls.Add(_loadBtn);
            Controls.Add(_status);
            Controls.Add(_list);
            Controls.Add(_joinBtn);
            Controls.Add(_emptiestBtn);
            Controls.Add(_refreshBtn);
            Controls.Add(_autoRefreshCheck);

            if (!string.IsNullOrEmpty(initialPlaceId)) _placeBox.Text = initialPlaceId;

            _loadBtn.Click += (s, e) => LoadServers();
            _refreshBtn.Click += (s, e) => LoadServers();
            _placeBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadServers(); } };
            _joinBtn.Click += (s, e) => JoinSelected();
            _emptiestBtn.Click += (s, e) => JoinEmptiest();
            _list.DoubleClick += (s, e) => JoinSelected();

            _autoRefreshCheck.CheckedChanged += (s, e) => { if (_autoRefreshCheck.Checked) LoadServers(); };
            _autoTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            _autoTimer.Tick += (s, e) => { if (_autoRefreshCheck.Checked && _placeId != null) LoadServers(); };
            _autoTimer.Start();
            FormClosed += (s, e) => { if (_autoTimer != null) _autoTimer.Stop(); };

            if (!string.IsNullOrEmpty(_placeId)) Shown += (s, e) => LoadServers();
        }

        private void SetStatus(string text, bool error)
        {
            _status.Text = text;
            _status.ForeColor = error ? Color.Firebrick : SystemColors.ControlText;
        }

        private static bool AllDigits(string text)
        {
            if (text.Length == 0) return false;
            foreach (char c in text) if (!char.IsDigit(c)) return false;
            return true;
        }

        private string ParsePlaceId()
        {
            string p = JoinerForm.PlaceIdOf(_placeBox.Text);
            if (p == null)
            {
                string t = (_placeBox.Text ?? "").Trim();
                if (t.Length > 0 && AllDigits(t)) p = t;
            }
            return p;
        }

        private void LoadServers()
        {
            string placeId = ParsePlaceId();
            if (placeId == null) { SetStatus("Enter a place ID or a Roblox game link first.", true); return; }
            _placeId = placeId;
            _loadBtn.Enabled = false;
            SetStatus("Loading servers...", false);
            System.Threading.Tasks.Task.Run((Action)(() =>
            {
                var servers = JoinerForm.FetchServers(placeId);
                string gameName = JoinerForm.FetchGameName(placeId);
                try
                {
                    Invoke((MethodInvoker)delegate
                    {
                        _loadBtn.Enabled = true;
                        Text = "Server Browser" + (string.IsNullOrEmpty(gameName) ? "" : " - " + gameName);
                        _servers.Clear();
                        _servers.AddRange(servers);
                        _list.Items.Clear();
                        foreach (ServerInfo sv in _servers)
                        {
                            _list.Items.Add(sv.Playing + " / " + sv.MaxPlayers + " players   -   " + sv.Id);
                        }
                        SetStatus(_servers.Count == 0
                            ? "No public servers right now - try again in a moment."
                            : "Showing " + _servers.Count + " public servers. Double-click one to join.", _servers.Count == 0);
                    });
                }
                catch { }
            }));
        }

        private void JoinServer(string serverId)
        {
            if (_onJoin != null && _placeId != null)
            {
                _onJoin("roblox://placeId=" + _placeId + "&gameInstanceId=" + serverId);
            }
        }

        private void JoinSelected()
        {
            int i = _list.SelectedIndex;
            if (i < 0 || i >= _servers.Count) { SetStatus("Pick a server first.", true); return; }
            JoinServer(_servers[i].Id);
        }

        private void JoinEmptiest()
        {
            ServerInfo best = JoinerForm.PickEmptiest(_servers);
            if (best == null) { SetStatus("Every server is full right now.", true); return; }
            SetStatus("Joining the emptiest server: " + best.Playing + " / " + best.MaxPlayers + " players.", false);
            JoinServer(best.Id);
        }
    }


}

namespace ArcticJoiner
{
    // Reads Roblox/Froststrap log files to find the server the client is in.
    internal static class RobloxLogs
    {
        internal static List<string> Directories()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dirs = new List<string>();
            dirs.Add(Path.Combine(localApp, "Roblox", "logs"));
            dirs.Add(Path.Combine(localApp, "Froststrap", "Logs"));
            dirs.Add(Path.Combine(localApp, "Froststrap", "logs"));
            dirs.Add(Path.Combine(localApp, "Bloxstrap", "Logs"));
            dirs.Add(Path.Combine(localApp, "Bloxstrap", "logs"));
            return dirs;
        }

        // Newest .log across every known folder (Froststrap/Bloxstrap may move them).
        internal static string NewestLogFile(out string debug)
        {
            var sb = new StringBuilder();
            string best = null;
            DateTime bestTime = DateTime.MinValue;
            foreach (string dir in Directories())
            {
                bool there = false;
                try { there = Directory.Exists(dir); } catch { }
                sb.AppendLine("checking " + dir + (there ? "" : "  (not found)"));
                if (!there) continue;
                try
                {
                    foreach (string file in Directory.GetFiles(dir, "*.log"))
                    {
                        DateTime t = File.GetLastWriteTime(file);
                        if (t > bestTime) { bestTime = t; best = file; }
                    }
                }
                catch { }
            }
            if (best != null) sb.AppendLine("newest: " + best + "  (" + bestTime.ToString("HH:mm:ss") + ")");
            debug = sb.ToString();
            return best;
        }

        internal static string ReadTail(string file, int maxLines)
        {
            try
            {
                string[] lines = File.ReadAllLines(file);
                int start = lines.Length > maxLines ? lines.Length - maxLines : 0;
                var sb = new StringBuilder();
                for (int i = start; i < lines.Length; i++) sb.AppendLine(lines[i]);
                return sb.ToString();
            }
            catch { return null; }
        }

        internal static string LastLines(string text, int count)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            int start = lines.Length > count ? lines.Length - count : 0;
            var sb = new StringBuilder();
            for (int i = start; i < lines.Length; i++)
            {
                if (lines[i].Length > 0) sb.AppendLine(lines[i]);
            }
            return sb.ToString();
        }

        // Takes the LAST match in the log, which is the most recent join.
        internal static bool TryFindCurrentServer(string logText, out string placeId, out string serverId)
        {
            placeId = null;
            serverId = null;
            if (string.IsNullOrEmpty(logText)) return false;

            var pm = System.Text.RegularExpressions.Regex.Matches(logText, @"Joining game '([0-9]+)'");
            if (pm.Count == 0) pm = System.Text.RegularExpressions.Regex.Matches(logText, @"placeId=([0-9]+)");
            if (pm.Count == 0) pm = System.Text.RegularExpressions.Regex.Matches(logText, @"place ([0-9]{6,})");
            if (pm.Count > 0) placeId = pm[pm.Count - 1].Groups[1].Value;

            string[] patterns =
            {
                @"(?i)serverid\s*[:=]\s*([0-9a-f\-]{36})",
                @"(?i)gameinstanceid\s*[:=]\s*([0-9a-f\-]{36})",
                @"(?i)gameid\s*[:=]\s*([0-9a-f\-]{36})",
                @"(?i)(?:job|rcc|instance)id\s*[:=]\s*([0-9a-f\-]{36})"
            };
            foreach (string pattern in patterns)
            {
                var m = System.Text.RegularExpressions.Regex.Matches(logText, pattern);
                if (m.Count > 0)
                {
                    serverId = m[m.Count - 1].Groups[1].Value;
                    break;
                }
            }
            return placeId != null || serverId != null;
        }
    }

    // A small always-on-top window with the live stats of the server you are in.
    internal sealed class LiveServerForm : Form
    {
        private readonly JoinerForm _main;
        private readonly Label _gameLabel;
        private readonly Label _serverLabel;
        private readonly Label _playersLabel;
        private readonly Label _updatedLabel;
        private readonly TextBox _debugBox;
        private readonly CheckBox _topCheck;
        private System.Windows.Forms.Timer _timer;

        public LiveServerForm(JoinerForm main)
        {
            _main = main;
            Text = "Arctic Joiner - Live server";
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(440, 310);
            MinimumSize = new Size(380, 240);
            TopMost = true;

            _gameLabel = new Label
            {
                Text = "No game yet",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = false,
                Location = new Point(12, 12),
                Size = new Size(416, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _serverLabel = new Label
            {
                Text = "",
                AutoSize = false,
                Location = new Point(12, 40),
                Size = new Size(416, 18),
                ForeColor = SystemColors.GrayText,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _playersLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                AutoSize = false,
                Location = new Point(12, 62),
                Size = new Size(416, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _updatedLabel = new Label
            {
                Text = "",
                AutoSize = false,
                Location = new Point(12, 96),
                Size = new Size(416, 18),
                ForeColor = SystemColors.GrayText,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var refreshBtn = new Button { Text = "Refresh", Location = new Point(12, 120), Size = new Size(110, 28) };
            var scanBtn = new Button { Text = "Scan Roblox logs", Location = new Point(130, 120), Size = new Size(140, 28) };
            _topCheck = new CheckBox { Text = "Always on top", Checked = true, AutoSize = true, Location = new Point(284, 126) };
            _debugBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false,
                Location = new Point(12, 156),
                Size = new Size(416, 142),
                Font = new Font("Consolas", 8F),
                BackColor = Color.FromArgb(245, 245, 245),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.Add(_gameLabel);
            Controls.Add(_serverLabel);
            Controls.Add(_playersLabel);
            Controls.Add(_updatedLabel);
            Controls.Add(refreshBtn);
            Controls.Add(scanBtn);
            Controls.Add(_topCheck);
            Controls.Add(_debugBox);

            refreshBtn.Click += (s, e) => RefreshStats();
            scanBtn.Click += (s, e) => ScanLogs();
            _topCheck.CheckedChanged += (s, e) => TopMost = _topCheck.Checked;

            Shown += (s, e) => RefreshStats();
            _timer = new System.Windows.Forms.Timer { Interval = 30000 };
            _timer.Tick += (s, e) => RefreshStats();
            _timer.Start();
            FormClosed += (s, e) => { if (_timer != null) _timer.Stop(); };

            _debugBox.Text = "Press Refresh to show the server you joined through Arctic Joiner."
                + Environment.NewLine
                + "If Roblox picked the server itself, press \"Scan Roblox logs\" and send the output so the pattern can be tuned.";
        }

        private void RefreshStats()
        {
            string placeId = _main.LivePlaceId();
            string serverId = _main.LiveServerId();
            if (string.IsNullOrEmpty(placeId) && string.IsNullOrEmpty(serverId))
            {
                _gameLabel.Text = "No server yet";
                _serverLabel.Text = "";
                _playersLabel.Text = "";
                _updatedLabel.Text = "Join a game through Arctic Joiner, or press \"Scan Roblox logs\".";
                return;
            }
            _serverLabel.Text = string.IsNullOrEmpty(serverId) ? "(server chosen by Roblox)" : "Server " + serverId;
            _updatedLabel.Text = "Updating...";
            string place = placeId;
            string server = serverId;
            System.Threading.Tasks.Task.Run((Action)(() =>
            {
                string name = place != null ? JoinerForm.FetchGameName(place) : null;
                List<ServerInfo> servers = place != null ? JoinerForm.FetchServers(place) : new List<ServerInfo>();
                ServerInfo mine = null;
                if (server != null)
                {
                    foreach (ServerInfo sv in servers)
                    {
                        if (string.Equals(sv.Id, server, StringComparison.OrdinalIgnoreCase)) { mine = sv; break; }
                    }
                }
                try
                {
                    Invoke((MethodInvoker)delegate
                    {
                        _gameLabel.Text = name != null ? name : (place != null ? "Place " + place : "Unknown game");
                        if (mine != null) _playersLabel.Text = mine.Playing + " / " + mine.MaxPlayers + " players";
                        else if (server == null) _playersLabel.Text = servers.Count + " public servers";
                        else _playersLabel.Text = "not in the public list";
                        _updatedLabel.Text = "Updated " + DateTime.Now.ToString("HH:mm:ss") + " (every 30s)";
                    });
                }
                catch { }
            }));
        }

        private void ScanLogs()
        {
            _updatedLabel.Text = "Scanning Roblox logs...";
            System.Threading.Tasks.Task.Run((Action)(() =>
            {
                string debug;
                string file = RobloxLogs.NewestLogFile(out debug);
                string result;
                if (file == null)
                {
                    result = "No .log file found." + Environment.NewLine + Environment.NewLine + debug;
                }
                else
                {
                    string tail = RobloxLogs.ReadTail(file, 400);
                    string placeId, serverId;
                    RobloxLogs.TryFindCurrentServer(tail, out placeId, out serverId);
                    result = debug + Environment.NewLine
                        + "matched placeId : " + (placeId != null ? placeId : "(none)") + Environment.NewLine
                        + "matched serverId: " + (serverId != null ? serverId : "(none)") + Environment.NewLine;
                    if (serverId == null || placeId == null)
                    {
                        result += Environment.NewLine + "Nothing complete matched - send the lines below so the pattern can be tuned." + Environment.NewLine;
                    }
                    result += Environment.NewLine + "---- last 20 log lines ----" + Environment.NewLine + RobloxLogs.LastLines(tail, 20);
                    if (placeId != null || serverId != null) _main.SetLiveServer(placeId, serverId);
                }
                try
                {
                    Invoke((MethodInvoker)delegate
                    {
                        _debugBox.Text = result;
                        if (_main.LivePlaceId() != null) RefreshStats();
                    });
                }
                catch { }
            }));
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
        public const string Version = "2.9.1";

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
