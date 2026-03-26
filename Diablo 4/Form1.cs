using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Deployment.Application;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Timers;
using System.Threading;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace Diablo_4
{
    public partial class Form1 : Form
    {
        private readonly string filePath = EnsureFileExists("Diablo IV.txt");
        internal static bool localIP = false;
        private bool isWeekend = false;
        private WeekendMotivation weekendMotivation;
        private TimeSpan totalDuration = new TimeSpan();

        public Form1()
        {
            try
            {
                InitializeComponent();
                CloseAnotherInstance();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Došlo k chybě při inicializaci formuláře: {ex.Message}\n\n Aplikace bude zavřena, řekni vývojáři, že je pako :).", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                localIP = GetLocalIPAddress();

                bool isMessageBoxOpen = false;
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 50 };
                timer.Tick += (s, ev) =>
                {
                    if (File.Exists(filePath))
                    {
                        FileStream stream = null;
                        while (stream == null)
                        {
                            try
                            {
                                stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            }
                            catch (IOException)
                            {
                                // Soubor je stále využíván, počkáme a zkusíme to znovu
                                Thread.Sleep(500);
                            }
                        }

                        using (stream)
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string fileContent = reader.ReadLine();
                            if (DateTime.TryParse(fileContent, out DateTime lastWriteTime))
                            {
                                TimeSpan timeSinceLastWrite = DateTime.Now - lastWriteTime;
                                messageLabel.Text = $"Už jsi nepařila: {timeSinceLastWrite.Days} dní, {timeSinceLastWrite.Hours} hodiny,\n  {timeSinceLastWrite.Minutes} minuty, {timeSinceLastWrite.Seconds} sekund a {timeSinceLastWrite.Milliseconds} miliseků :).";
                                messageLabel.Invalidate();
                                messageLabel.Update();
                            }
                            else
                            {
                                if (!isMessageBoxOpen)
                                {
                                    isMessageBoxOpen = true;
                                    MessageBox.Show("Nelze načíst datum a čas ze souboru.");
                                    Environment.Exit(0);
                                }
                            }
                        }
                    }
                };
                timer.Start();

                ProcessMonitor processMonitor = new ProcessMonitor(filePath, "Diablo IV", "DragonAgeInquisition", "Diablo III64", "devenv", "Code", "Dragon Age The Veilguard", "DragonAge2", "daorigins");

                System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer { Interval = 800 };
                updateTimer.Tick += (s, ev) =>
                {
                    try
                    {
                        int currentWeekOfYear = processMonitor.GetIso8601WeekOfYear(DateTime.Now);
                        List<TimeSpan> actuaAndLastlWeek = processMonitor.GetDurations(filePath, currentWeekOfYear);
                        if (actuaAndLastlWeek[0] != TimeSpan.Zero)
                        {
                            totalDuration = actuaAndLastlWeek[0];
                            string formattedDuration = $"{Math.Floor(totalDuration.TotalHours)} hodin, {totalDuration.Minutes} minut a {totalDuration.Seconds} vteřin";
                            this.weekDuration.Text = $"Tento týden \n {formattedDuration}";
                            OpenWeekendMotivation();
                            if (processMonitor.isRunning)
                            {
                                this.Visible = false;
                            }
                        }
                        else
                        {
                            this.weekDuration.Text = "Chtělo by to roztočit grafárnu.";
                            OpenWeekendMotivation();
                            if (processMonitor.isRunning)
                            {
                                this.Visible = false;
                            }
                        }

                        if (actuaAndLastlWeek[1] != TimeSpan.Zero)
                        {
                            TimeSpan lastWeekTotalDuration = actuaAndLastlWeek[1];
                            string formattedDuration = $"{Math.Floor(lastWeekTotalDuration.TotalHours)} hodin, {lastWeekTotalDuration.Minutes} minut a {lastWeekTotalDuration.Seconds} vteřin";
                            this.lastWeekDuration.Text = $"Minulý týden \n {formattedDuration}";
                        }
                        else
                        {
                            this.lastWeekDuration.Text = "Minulý týden se nezadařilo.";
                        }
                    }
                    catch (Exception ex)
                    {
                        updateTimer.Stop();
                        MessageBox.Show($"Došlo k chybě při aktualizaci časovače: {ex.Message}\n\n Aplikace bude zavřena, řekni vývojáři, že je pako :).", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(0);
                    }
                };
                updateTimer.Start();

                await Task.Delay(1000); // Krátké zpoždění, aby se zajistilo, že updateTimer skutečně začne
                CheckForUpdates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Došlo k chybě při načítání formuláře: {ex.Message}\n\n Aplikace bude zavřena, řekni vývojáři, že je pako :).", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }

        private void OpenWeekendMotivation()
        {
            if (!isWeekend && totalDuration.TotalHours < 25 && (DateTime.Now.DayOfWeek == DayOfWeek.Friday || DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday))
            {
                this.weekendMotivation = new WeekendMotivation();
                isWeekend = true;
                weekendMotivation.ShowDialog();
            }
        }

        private void CheckForUpdates()
        {
            if (IsHostAvailable("192.168.0.35") && ApplicationDeployment.IsNetworkDeployed)
            {
                ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
                ad.CheckForUpdateCompleted += ad_CheckForUpdateCompleted;
                ad.CheckForUpdateProgressChanged += ad_CheckForUpdateProgressChanged;
                ad.CheckForUpdateAsync();
            }
        }

        void ad_CheckForUpdateCompleted(object sender, CheckForUpdateCompletedEventArgs e)
        {
            if (e.UpdateAvailable)
            {              
                DialogResult dialogResult;
                if (!isWeekend)
                {
                    dialogResult = MessageBox.Show(this, "Je dostupná aktualizace, přeješ si ji nainstalovat?", "Dostupná aktualizace", MessageBoxButtons.YesNo);
                }
                else
                {
                    dialogResult = MessageBox.Show(weekendMotivation, "Je dostupná aktualizace, přeješ si ji nainstalovat?", "Dostupná aktualizace", MessageBoxButtons.YesNo);
                }

                if (dialogResult == DialogResult.Yes)
                {
                    ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
                    ad.UpdateProgressChanged += ad_CheckForUpdateProgressChanged;
                    ad.UpdateCompleted += ad_UpdateCompleted;
                    ad.UpdateAsync();
                }
            }
        }

        void ad_CheckForUpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
        {
            //Zde můžete aktualizovat UI pro zobrazení postupu kontroly aktualizace
        }

        void ad_UpdateCompleted(object sender, AsyncCompletedEventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Aktualizace byla nainstalována, přejete si aplikaci restartovat?", "Restart aplikace", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                if (isWeekend)
                {
                    Task.Run(() =>
                    {
                        weekendMotivation.Close();
                    });
                }

                Application.Restart();
            }
        }

        bool IsHostAvailable(string hostNameOrAddress)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(hostNameOrAddress);
                return reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                return false;
            }
        }

        private static string EnsureFileExists(string fileName)
        {
            string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDirectory = Path.Combine(appDataDirectory, "Diablo Log");
            Directory.CreateDirectory(appDirectory); // Vytvoří adresář, pokud neexistuje
            string filePath = Path.Combine(appDirectory, fileName);

            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }

            // Kontrola, zda je soubor prázdný
            if (new FileInfo(filePath).Length == 0)
            {
                // Uložení aktuálního data a času do souboru
                File.WriteAllText(filePath, DateTime.Now.ToString() + Environment.NewLine);
            }

            return filePath;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true; // Cancel the closing event
            this.Visible = false; // Set the form visibility to false
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            if (((MouseEventArgs)e).Button == MouseButtons.Left)
            {
                Application.Restart();
            }
            else if (((MouseEventArgs)e).Button == MouseButtons.Right)
            {
                Point cursorPosition = Cursor.Position;
                cursorPosition.X -= contextMenuStrip1.Width / 2;
                cursorPosition.Y -= contextMenuStrip1.Height / 2;
                contextMenuStrip1.Show(cursorPosition);
            }
        }

        private void Form1_VisibleChanged(object sender, EventArgs e)
        {
            notifyIcon1.Visible = !this.Visible;
        }

        private void toolStripMenuItem1_MouseDown(object sender, MouseEventArgs e)
        {
            Task.Run(() =>
            {
                Environment.Exit(0);
            });
        }

        private void contextMenuStrip1_MouseLeave(object sender, EventArgs e)
        {
            contextMenuStrip1.Hide();
        }

        private static void CloseAnotherInstance()
        {
            Task.Run(() =>
            {
                // Získání všech procesů s názvem "Diablo 4"
                var diabloProcesses = Process.GetProcessesByName("Diablo 4");

                // Pokud běží více než jeden proces "Diablo 4"
                if (diabloProcesses.Length > 1)
                {
                    // Seřazení procesů podle času spuštění a získání všech kromě toho, který byl spuštěn naposledy
                    var processesToClose = diabloProcesses.OrderByDescending(p => p.StartTime).Skip(1);

                    // Zavření všech procesů kromě toho, který byl spuštěn naposledy
                    foreach (var process in processesToClose)
                    {
                        process.Kill();
                    }
                }
            });
        }

        public bool GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && (ip.ToString() == "192.168.0.35" || ip.ToString() == "169.254.123.157"))
                {
                    return true;
                }
                else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && (ip.ToString() != "192.168.0.35" || ip.ToString() != "169.254.123.157"))
                {
                    return false;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }

    public class ProcessMonitor
    {
        private System.Timers.Timer _timer;
        private System.Timers.Timer _webTimer;
        private string[] _processNames;
        private string _filePath;
        private DateTime? _processStartTime;
        private int _weekOfYear;
        private long _lastLogPosition = 0;
        private DateTime? _firstDetectionTime;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        private bool _isWebRunning = false;
        private bool _isCheckingTabs = false; // Přidání zámku
        public bool isRunning = false;

        public ProcessMonitor(string filePath, params string[] processNames)
        {
            _processNames = processNames;
            _timer = new System.Timers.Timer(500); // Kontroluje procesy
            _timer.Elapsed += FirstCheckProcess;
            _timer.Elapsed += WriteProcessDuration;
            _filePath = filePath;
            _webTimer = new System.Timers.Timer(5000); // Kontroluje otevřené webové stránky
            _webTimer.Elapsed += CheckAllOpenTabsForUdemy; // Připojení synchronní metody
            Start();
        }

        public void Start()
        {
            _timer.Start();

            if (Form1.localIP)
            {
                _webTimer.Start();
            }
        }

        public void Stop()
        {
            _timer.Stop();

            if (Form1.localIP)
            {
                _webTimer.Stop();
            }
        }

        private void FirstCheckProcess(object sender, ElapsedEventArgs e)
        {
            bool isAnyProcessRunning = false;

            foreach (var processName in _processNames)
            {
                var processExists = Process.GetProcesses().Any(x => x.ProcessName == processName);
                if (processExists || _isWebRunning)
                {
                    isAnyProcessRunning = true;

                    if (!_firstDetectionTime.HasValue)
                    {
                        // První detekce procesu, uložíme čas
                        _firstDetectionTime = DateTime.Now;
                    }
                    else if ((DateTime.Now - _firstDetectionTime.Value).TotalMilliseconds >= 200)
                    {
                        // Proces běží alespoň 1000 milisekund, provedeme zápis
                        string dateTimeString = DateTime.Now.ToString() + Environment.NewLine;
                        FileStream stream = null;
                        while (stream == null)
                        {
                            try
                            {
                                stream = new FileStream(_filePath, FileMode.Open, FileAccess.Write, FileShare.Read);
                            }
                            catch (IOException)
                            {
                                // Soubor je stále využíván, počkáme a zkusíme to znovu
                                Thread.Sleep(50);
                            }
                        }

                        using (stream)
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            writer.Write(dateTimeString);
                        }

                        // Resetujeme _firstDetectionTime pro další detekci
                        _firstDetectionTime = null;
                    }
                }
            }

            if (!isAnyProcessRunning)
            {
                // Proces není spuštěn, resetujeme _firstDetectionTime
                _firstDetectionTime = null;
            }
        }

        private void WriteProcessDuration(object sender, ElapsedEventArgs e)
        {
            bool isAnyProcessRunning = false;

            foreach (var processName in _processNames)
            {
                var processExists = Process.GetProcesses().Any(x => x.ProcessName == processName);
                if (processExists || _isWebRunning)
                {
                    isAnyProcessRunning = true;
                    isRunning = true;

                    if (!_processStartTime.HasValue)
                    {
                        // Proces byl právě spuštěn
                        _processStartTime = DateTime.Now;
                        _weekOfYear = GetIso8601WeekOfYear((DateTime)_processStartTime);
                        _lastLogPosition = new FileInfo(_filePath).Length;
                    }

                    else if (_processStartTime.HasValue)
                    {
                        // Zkontrolujeme, zda od spuštění procesu uplynulo alespoň 1000 milisekund
                        var processEndTime = DateTime.Now;
                        var duration = processEndTime - _processStartTime.Value;
                        if (duration.TotalMilliseconds >= 200)
                        {
                            // Proces byl právě ukončen a uplynulo alespoň 1000 milisekund
                            // Uložíme dobu trvání procesu do souboru
                            var logEntry = $"{_weekOfYear}||{_processStartTime.Value:dd-MM-yyyy HH:mm:ss}||{duration.TotalSeconds}\n";
                            FileStream stream = null;
                            while (stream == null)
                            {
                                try
                                {
                                    stream = new FileStream(_filePath, FileMode.Open, FileAccess.Write, FileShare.Read);
                                    stream.Position = _lastLogPosition;
                                }
                                catch (IOException)
                                {
                                    // Soubor je stále využíván, počkáme a zkusíme to znovu
                                    Thread.Sleep(70);
                                }

                                using (stream)
                                using (var writer = new StreamWriter(stream))
                                {
                                    writer.Write(logEntry);
                                    writer.Flush();
                                    stream.SetLength(stream.Position); // Truncate the rest of the file
                                }
                            }
                        }
                    }
                }
            }

            if (!isAnyProcessRunning)
            {
                _processStartTime = null;
            }
        }

        public int GetIso8601WeekOfYear(DateTime time)
        {
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public List<TimeSpan> GetDurations(string filePath, int actualWeekOfYear)
        {
            var weeklyDurations = new TimeSpan();
            var lastWeekDuration = new TimeSpan();
            List<TimeSpan> durations = new List<TimeSpan>();
            DateTime actualDate = DateTime.Now;
            int actualYear = DateTime.Now.Year;
            int weeksInLastYear = GetIso8601WeekOfYear(DateTime.Parse($"31.12.{actualYear - 1}")) != 1 ? GetIso8601WeekOfYear(DateTime.Parse($"31.12.{actualYear - 1}")) : GetIso8601WeekOfYear(DateTime.Parse($"27.12.{actualYear - 1}"));

            IEnumerable<string> lines = null;
            while (lines == null)
            {
                try
                {
                    lines = File.ReadLines(filePath);
                }
                catch (IOException)
                {
                    // Soubor je stále využíván, počkáme a zkusíme to znovu
                    Thread.Sleep(200);
                }
            }

            foreach (var line in lines)
            {
                string[] separator = new string[] { "||" };
                var parts = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3)
                {
                    continue;
                }
                var weekOfYear = int.Parse(parts[0]);
                var startTimeYear = DateTime.Parse(parts[1]).Year;
                var duration = TimeSpan.FromSeconds(double.Parse(parts[2]));

                if (weekOfYear == actualWeekOfYear && startTimeYear == actualYear)
                {
                    weeklyDurations += duration;
                }
                else if (actualWeekOfYear == 1 && weekOfYear == weeksInLastYear && startTimeYear == actualYear - 1) 
                {
                    lastWeekDuration += duration;
                }
                else if (actualWeekOfYear != 1 && weekOfYear == actualWeekOfYear - 1 && startTimeYear == actualYear)
                {
                    lastWeekDuration += duration;
                }
            }

            durations.Add(weeklyDurations != TimeSpan.Zero ? weeklyDurations : new TimeSpan());
            durations.Add (lastWeekDuration != TimeSpan.Zero ? lastWeekDuration : new TimeSpan());

            return durations;
        }

        private void CheckAllOpenTabsForUdemy(object sender, ElapsedEventArgs e)
        {
            if (_isCheckingTabs) return; // Pokud již probíhá kontrola, vrátíme se
            _isCheckingTabs = true; // Nastavení zámku

            try
            {
                // Najít hlavní okno Firefoxu
                IntPtr firefoxWindow = FindWindow("MozillaWindowClass", null);
                bool foundUdemy = false;

                if (firefoxWindow != IntPtr.Zero)
                {
                    // Použití UIAutomation pro přístup k elementům Firefoxu
                    AutomationElement element = AutomationElement.FromHandle(firefoxWindow);

                    // Najít všechny URL bary (textboxy pro adresy)
                    var urlBars = element.FindAll(TreeScope.Descendants,
                        new AndCondition(
                            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                            new PropertyCondition(AutomationElement.IsOffscreenProperty, false), // Musí být viditelný
                            new PropertyCondition(AutomationElement.IsEnabledProperty, true) // Musí být povolený
                        ));

                    foreach (AutomationElement urlBar in urlBars)
                    {
                        string currentUrl = ((ValuePattern)urlBar.GetCurrentPattern(ValuePattern.Pattern)).Current.Value;

                        if (currentUrl.Contains("udemy.com"))
                        {
                            foundUdemy = true;
                            break;
                        }
                    }

                    _isWebRunning = foundUdemy;
                }
                else
                {
                    _isWebRunning = false;
                }
            }
            finally
            {
                _isCheckingTabs = false; // Uvolnění zámku
            }

        }
    }
}
