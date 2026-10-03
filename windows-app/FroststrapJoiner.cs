// FroststrapJoiner - instant Roblox joiner for Froststrap users
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

namespace FroststrapJoiner
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
        public bool AutoJoinOnPaste = true;
        public bool CloseAfterJoin = false;

        private static string SettingsFile
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FroststrapJoiner");
                return Path.Combine(dir, "settings.txt");
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
                        else if (key == "autoJoinOnPaste") s.AutoJoinOnPaste = val == "1";
                        else if (key == "closeAfterJoin") s.CloseAfterJoin = val == "1";
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
                    .AppendLine("autoJoinOnPaste=" + (AutoJoinOnPaste ? "1" : "0"))
                    .AppendLine("closeAfterJoin=" + (CloseAfterJoin ? "1" : "0"))
                    .ToString());
            }
            catch { }
        }
    }
}

namespace FroststrapJoiner
{
    internal sealed class JoinerForm : Form
    {
        private readonly Settings _settings;
        private readonly TextBox _linkBox;
        private readonly Button _joinButton;
        private readonly Label _status;
        private readonly CheckBox _autoJoinCheck;
        private readonly CheckBox _closeAfterCheck;
        private readonly TextBox _froststrapPathBox;
        private readonly Button _browseButton;
        private readonly Button _openSettingsButton;
        private readonly Panel _settingsPanel;
        private readonly Button _updateButton;
        private string _lastAutoJoined = "";

        public JoinerForm(Settings settings)
        {
            _settings = settings;

            Text = "Froststrap Joiner";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(560, 190);

            var pasteLabel = new Label
            {
                Text = "Paste a Roblox link and press Enter:",
                AutoSize = true,
                Location = new Point(12, 12)
            };

            _linkBox = new TextBox
            {
                Location = new Point(12, 32),
                Width = 440,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _linkBox.KeyDown += LinkBoxKeyDown;
            _linkBox.TextChanged += (s, e) =>
            {
                string candidate = _linkBox.Text.Trim();
                if (!_settings.AutoJoinOnPaste) return;
                if (candidate.Equals(_lastAutoJoined, StringComparison.OrdinalIgnoreCase)) return;
                // Join as soon as a full link shows up in the box.
                if (candidate.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                    candidate.StartsWith("roblox://", StringComparison.OrdinalIgnoreCase))
                {
                    _lastAutoJoined = candidate;
                    TryJoin(candidate, closeAfter: false);
                }
            };

            _joinButton = new Button
            {
                Text = "Join",
                Location = new Point(460, 30),
                Width = 88,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _joinButton.Click += (s, e) => TryJoin(_linkBox.Text, closeAfter: false);

            _status = new Label
            {
                Text = "Ready. Paste a link to join instantly.",
                AutoSize = false,
                Size = new Size(536, 18),
                Location = new Point(12, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _autoJoinCheck = new CheckBox
            {
                Text = "Join instantly on paste",
                Checked = _settings.AutoJoinOnPaste,
                AutoSize = true,
                Location = new Point(12, 82)
            };
            _autoJoinCheck.CheckedChanged += (s, e) =>
            {
                _settings.AutoJoinOnPaste = _autoJoinCheck.Checked;
                _settings.Save();
            };

            _closeAfterCheck = new CheckBox
            {
                Text = "Close after joining",
                Checked = _settings.CloseAfterJoin,
                AutoSize = true,
                Location = new Point(180, 82)
            };
            _closeAfterCheck.CheckedChanged += (s, e) =>
            {
                _settings.CloseAfterJoin = _closeAfterCheck.Checked;
                _settings.Save();
            };

            _openSettingsButton = new Button
            {
                Text = "Froststrap settings",
                Location = new Point(12, 108),
                Width = 140
            };
            _openSettingsButton.Click += (s, e) =>
            {
                _settingsPanel.Visible = !_settingsPanel.Visible;
                ClientSize = new Size(ClientSize.Width, _settingsPanel.Visible ? 300 : 190);
            };

            _updateButton = new Button
            {
                Text = "",
                Visible = false,
                Enabled = false,
                Location = new Point(160, 108),
                Width = 260
            };
            _updateButton.Click += (s, e) => RunSelfUpdate();

            _settingsPanel = new Panel
            {
                Location = new Point(12, 140),
                Size = new Size(536, 150),
                Visible = false
            };

            var pathLabel = new Label
            {
                Text = "Froststrap.exe location (optional - leave empty to use the roblox:// protocol handler):",
                AutoSize = false,
                Size = new Size(536, 32)
            };
            _froststrapPathBox = new TextBox
            {
                Text = _settings.FroststrapPath,
                Location = new Point(0, 36),
                Width = 448
            };
            _froststrapPathBox.TextChanged += (s, e) =>
            {
                _settings.FroststrapPath = _froststrapPathBox.Text.Trim();
                _settings.Save();
            };
            _browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(456, 34),
                Width = 80
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
                Text = "Tip: if the box above is empty, the joiner opens the roblox:// link directly, so whatever app is registered for it (Froststrap, if you set it as default) launches.",
                AutoSize = false,
                Size = new Size(536, 70),
                Location = new Point(0, 70),
                ForeColor = SystemColors.GrayText
            };

            _settingsPanel.Controls.Add(pathLabel);
            _settingsPanel.Controls.Add(_froststrapPathBox);
            _settingsPanel.Controls.Add(_browseButton);
            _settingsPanel.Controls.Add(hint);

            Controls.Add(pasteLabel);
            Controls.Add(_linkBox);
            Controls.Add(_joinButton);
            Controls.Add(_status);
            Controls.Add(_autoJoinCheck);
            Controls.Add(_closeAfterCheck);
            Controls.Add(_openSettingsButton);
            Controls.Add(_settingsPanel);
            Controls.Add(_updateButton);

            AcceptButton = _joinButton;

            // Housekeeping: remove leftovers from a previous self-update,
            // then check GitHub for a newer version in the background.
            CleanupOldBinary();
            CheckForUpdatesAsync();
        }

        private void LinkBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                TryJoin(_linkBox.Text, closeAfter: false);
            }
        }

        public void TryJoin(string raw, bool closeAfter)
        {
            string deepLink = LinkParser.Parse(raw);
            if (deepLink == null)
            {
                SetStatus("Could not read a place ID, server ID, launch data, or private code from that input.", true);
                return;
            }

            try
            {
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

                if (closeAfter || _settings.CloseAfterJoin)
                {
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
            System.Threading.Tasks.Task.Run((Action)(() =>
            {
                string newer = null;
                try
                {
                    string remote = Updater.DownloadText(Updater.VersionUrl).Trim();
                    if (remote.Length > 0 && Updater.IsNewer(remote, Updater.Version)) newer = remote;
                }
                catch { }
                if (newer == null) return;
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
                    string dir = Path.Combine(Path.GetTempPath(), "FroststrapJoinerUpdate");
                    Directory.CreateDirectory(dir);
                    string cs = Path.Combine(dir, "FroststrapJoiner.cs");
                    Updater.DownloadFile(Updater.SourceUrl, cs);

                    string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                    string newExe = Path.Combine(exeDir, "FroststrapJoiner.new.exe");
                    try { if (File.Exists(newExe)) File.Delete(newExe); } catch { }

                    string err = Updater.CompileUpdate(cs, newExe);
                    if (err != null) throw new Exception("build failed: " + err);

                    string current = Application.ExecutablePath;
                    string old = current + ".old";
                    try { if (File.Exists(old)) File.Delete(old); } catch { }
                    File.Move(current, old);
                    File.Move(newExe, current);

                    SetStatusUi("Updated to the latest version. Restarting...", false);
                    Process.Start(current);
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
    }
}

namespace FroststrapJoiner
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
                            for (int i = 0; i < parts.Length - 1; i++)
                            {
                                if (parts[i].Equals("games", StringComparison.OrdinalIgnoreCase) &&
                                    long.TryParse(parts[i + 1], out long id))
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

namespace FroststrapJoiner
{
    // Fetches updates from the GitHub Pages repo (main branch, windows-app folder).
    internal static class Updater
    {
        public const string Version = "1.1.0";

        // A double-quote character, used when building compiler arguments
        // without needing escaped quotes in the source.
        public const char Q = '"';

        public const string BaseUrl =
            "https://raw.githubusercontent.com/Arctic-Furry/Arctic-Furry.github.io/main/windows-app/";
        public const string VersionUrl = BaseUrl + "version.txt";
        public const string SourceUrl = BaseUrl + "FroststrapJoiner.cs";

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

        public static string DownloadText(string url)
        {
            using (var wc = new System.Net.WebClient())
            {
                return wc.DownloadString(url);
            }
        }

        public static void DownloadFile(string url, string dest)
        {
            using (var wc = new System.Net.WebClient())
            {
                wc.DownloadFile(url, dest);
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
        public static string CompileUpdate(string csPath, string outPath)
        {
            string csc = FindCsc();
            if (csc == null)
            {
                return "no .NET Framework C# compiler found on this PC";
            }
            var psi = new ProcessStartInfo
            {
                FileName = csc,
                Arguments = "/nologo /target:winexe /optimize+ /out:" + Updater.Q + outPath + Updater.Q +
                            " /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll " + Updater.Q + csPath + Updater.Q,
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
