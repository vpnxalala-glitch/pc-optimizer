using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Net.NetworkInformation;
using Microsoft.Win32;

namespace PCOptimizer
{
    public partial class MainForm : Form
    {
        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwndProc);

        private TableLayoutPanel layout;
        private Label lblPing;
        private Label lblStatus;
        private Timer pingTimer;
        private int currentFontSize = 10;
        
        string statusPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCOptimizer_Status.txt");

        public MainForm()
        {
            LoadSettings();
            InitializeComponent();
            LoadStatus();
            StartPingMonitor();
        }

        private void LoadSettings()
        {
            try {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\PCOptimizer")) {
                    if (key != null) {
                        object val = key.GetValue("FontSize");
                        if (val != null) currentFontSize = (int)val;
                    }
                }
            } catch { }
        }

        private void SaveSettings()
        {
            try {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\PCOptimizer")) {
                    key.SetValue("FontSize", currentFontSize, RegistryValueKind.DWord);
                }
            } catch { }
        }

        private void InitializeComponent()
        {
            this.Text = "PC Optimizer Pro";
            this.Size = new Size(500, 650);
            this.MinimumSize = new Size(400, 550);
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", currentFontSize, FontStyle.Regular);

            layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 11;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45F));
            
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // ВКЛ
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // ВЫКЛ
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // Скины
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // Перезапуск
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Статус

            this.Controls.Add(layout);

            Label lblTitle = new Label();
            lblTitle.Text = "ОПТИМИЗАТОР";
            lblTitle.ForeColor = Color.Gold;
            lblTitle.Font = new Font("Segoe UI", currentFontSize + 2, FontStyle.Bold);
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            layout.Controls.Add(lblTitle, 0, 0);

            Button btnSettings = new Button();
            btnSettings.Text = "⚙";
            btnSettings.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            btnSettings.ForeColor = Color.White;
            btnSettings.BackColor = Color.Transparent;
            btnSettings.FlatStyle = FlatStyle.Flat;
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.Cursor = Cursors.Hand;
            btnSettings.Dock = DockStyle.Fill;
            btnSettings.Click += BtnSettings_Click;
            layout.Controls.Add(btnSettings, 1, 0);

            lblPing = new Label();
            lblPing.Text = "Пинг FunTime: измерение...";
            lblPing.ForeColor = Color.Cyan;
            lblPing.Font = new Font("Segoe UI", currentFontSize, FontStyle.Bold);
            lblPing.Dock = DockStyle.Fill;
            lblPing.TextAlign = ContentAlignment.MiddleCenter;
            layout.Controls.Add(lblPing, 0, 1);
            layout.SetColumnSpan(lblPing, 2);

            layout.Controls.Add(CreateButton("ВКЛ (Оптимизация)", "Чистит память, сеть, питание.", Color.FromArgb(0, 200, 100), BtnOn_Click), 0, 2);
            layout.Controls.Add(CreateButton("ВЫКЛ (Возврат)", "Сбрасывает настройки Windows.", Color.IndianRed, BtnOff_Click), 0, 3);
            layout.Controls.Add(CreateButton("Создать Скины + Открыть", "Создает пак и открывает папку.", Color.MediumVioletRed, BtnPack_Click), 0, 4);
            layout.Controls.Add(CreateButton("Перезапуск Игры", "Закрывает и открывает лаунчер заново.", Color.Gold, BtnRestart_Click), 0, 5);

            lblStatus = new Label();
            lblStatus.Text = "Готов к работе";
            lblStatus.ForeColor = Color.LightGray;
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            layout.Controls.Add(lblStatus, 0, 6);
            layout.SetColumnSpan(lblStatus, 2);
        }

        private Button CreateButton(string text, string tooltip, Color color, EventHandler clickEvent)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(20, 2, 20, 2);
            btn.BackColor = color;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", currentFontSize, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.Click += clickEvent;

            ToolTip tip = new ToolTip();
            tip.SetToolTip(btn, tooltip);
            return btn;
        }

        private void LoadStatus()
        {
            if (File.Exists(statusPath))
            {
                string status = File.ReadAllText(statusPath).Trim();
                if (status == "ON")
                {
                    lblStatus.Text = "Система оптимизирована";
                    lblStatus.ForeColor = Color.LimeGreen;
                }
            }
        }

        private void SaveStatus(string status)
        {
            try { File.WriteAllText(statusPath, status); } catch { }
        }

        private void BtnOn_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Оптимизация...";
            Application.DoEvents();

            try
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process proc in processes) { try { EmptyWorkingSet(proc.Handle); } catch { } }

                ProcessStartInfo psiNet = new ProcessStartInfo("cmd.exe", "/c ipconfig /flushdns & netsh int tcp set global autotuninglevel=normal");
                psiNet.WindowStyle = ProcessWindowStyle.Hidden; psiNet.CreateNoWindow = true; Process.Start(psiNet);

                ProcessStartInfo psi2 = new ProcessStartInfo("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                psi2.WindowStyle = ProcessWindowStyle.Hidden; psi2.CreateNoWindow = true; Process.Start(psi2);

                lblStatus.Text = "Система оптимизирована!";
                lblStatus.ForeColor = Color.LimeGreen;
                SaveStatus("ON");
            }
            catch (Exception ex) { lblStatus.Text = "Ошибка: " + ex.Message; }
        }

        private void BtnOff_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Возврат настроек...";
            Application.DoEvents();

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
                psi.WindowStyle = ProcessWindowStyle.Hidden; psi.CreateNoWindow = true; Process.Start(psi);

                ProcessStartInfo psiNet = new ProcessStartInfo("cmd.exe", "/c netsh int tcp set global autotuninglevel=restricted");
                psiNet.WindowStyle = ProcessWindowStyle.Hidden; psiNet.CreateNoWindow = true; Process.Start(psiNet);

                lblStatus.Text = "Настройки возвращены.";
                lblStatus.ForeColor = Color.LightGray;
                SaveStatus("OFF");
            }
            catch (Exception ex) { lblStatus.Text = "Ошибка: " + ex.Message; }
        }

        private void BtnRestart_Click(object sender, EventArgs e)
        {
            string[] launchers = { "FunTime", "LegacyLauncher", "TLauncher", "MinecraftLauncher" };
            string foundLauncher = "";
            
            foreach (string l in launchers)
            {
                Process[] p = Process.GetProcessesByName(l);
                if (p.Length > 0) { foundLauncher = p[0].MainModule.FileName; p[0].Kill(); break; }
            }

            var procs = Process.GetProcessesByName("javaw");
            foreach (var p in procs) { try { p.Kill(); } catch { } }

            if (!string.IsNullOrEmpty(foundLauncher))
            {
                Process.Start(foundLauncher);
                lblStatus.Text = "Игра и лаунчер перезапущены!";
            }
            else
            {
                lblStatus.Text = "Игра закрыта. Открой лаунчер вручную.";
            }
        }

        private void BtnPack_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Поиск папки игры...";
            Application.DoEvents();
            
            try
            {
                string mcPath = FindMinecraftPath();
                
                if (mcPath == null) {
                    lblStatus.Text = "Ошибка: Папка .minecraft не найдена!";
                    MessageBox.Show("Не удалось найти папку .minecraft. Убедитесь, что игра установлена.", "Ошибка");
                    return;
                }

                lblStatus.Text = "Создание скинов...";
                Application.DoEvents();

                string rpPath = Path.Combine(mcPath, "resourcepacks", "NoSkinLag");
                string texturePath = Path.Combine(rpPath, "assets", "minecraft", "textures", "entity");
                Directory.CreateDirectory(texturePath);

                File.WriteAllText(Path.Combine(rpPath, "pack.mcmeta"), "{\n  \"pack\": {\n    \"pack_format\": 15,\n    \"description\": \"7-Color Skins\"\n  }\n}");

                using (Bitmap bmp = new Bitmap(64, 64))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        Color[] colors = { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Cyan, Color.Blue, Color.Purple };
                        int colorIndex = 0;
                        for (int x = 0; x < 64; x += 16)
                            for (int y = 0; y < 64; y += 16) { g.FillRectangle(new SolidBrush(colors[colorIndex % 7]), x, y, 16, 16); colorIndex++; }

                        using (SolidBrush blackBrush = new SolidBrush(Color.Black))
                        {
                            g.FillRectangle(blackBrush, 9, 11, 2, 2); g.FillRectangle(blackBrush, 13, 11, 2, 2);
                            g.FillRectangle(blackBrush, 9, 14, 6, 1); g.FillRectangle(blackBrush, 8, 13, 1, 1);
                            g.FillRectangle(blackBrush, 15, 13, 1, 1);
                        }
                    }
                    bmp.Save(Path.Combine(texturePath, "steve.png"), System.Drawing.Imaging.ImageFormat.Png);
                    bmp.Save(Path.Combine(texturePath, "alex.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                
                Process.Start("explorer.exe", Path.Combine(mcPath, "resourcepacks"));
                lblStatus.Text = "Скины созданы и папка открыта!";
            }
            catch (Exception ex) { lblStatus.Text = "Ошибка: " + ex.Message; }
        }

        private string FindMinecraftPath()
        {
            // 1. Стандартный путь
            string standardPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            if (Directory.Exists(standardPath)) return standardPath;

            // 2. Папки популярных лаунчеров
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            
            string[] possiblePaths = {
                Path.Combine(appData, "FunTime"),
                Path.Combine(appData, "FunTimeLauncher"),
                Path.Combine(appData, ".minecraft_funtime"),
                Path.Combine(localAppData, "FunTime"),
                Path.Combine(appData, "TLauncher"),
                Path.Combine(appData, ".tlauncher"),
                Path.Combine(appData, ".minecraft_tlauncher")
            };

            foreach (var p in possiblePaths) {
                if (Directory.Exists(p)) {
                    // Если это папка лаунчера, проверяем наличие внутри .minecraft
                    string innerPath = Path.Combine(p, ".minecraft");
                    if (Directory.Exists(innerPath)) return innerPath;
                    return p; // Если внутри нет, возвращаем саму папку
                }
            }

            return null; // Если ничего не найдено
        }

        private void StartPingMonitor()
        {
            pingTimer = new Timer();
            pingTimer.Interval = 1000;
            pingTimer.Tick += PingTimer_Tick;
            pingTimer.Start();
        }

        private void PingTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                using (Ping p = new Ping())
                {
                    PingReply reply = p.Send("mc.funtime.su", 1000);
                    if (reply != null && reply.Status == IPStatus.Success)
                    {
                        lblPing.Text = "Пинг FunTime: " + reply.RoundtripTime + " мс";
                        if (reply.RoundtripTime < 50) lblPing.ForeColor = Color.LimeGreen;
                        else if (reply.RoundtripTime < 100) lblPing.ForeColor = Color.Yellow;
                        else lblPing.ForeColor = Color.Red;
                    }
                    else { lblPing.Text = "Пинг FunTime: Нет ответа"; lblPing.ForeColor = Color.Gray; }
                }
            }
            catch { lblPing.Text = "Пинг FunTime: Ошибка"; lblPing.ForeColor = Color.Red; }
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            Form settingsForm = new Form();
            settingsForm.Text = "Настройки шрифта";
            settingsForm.Size = new Size(300, 200);
            settingsForm.StartPosition = FormStartPosition.CenterParent;
            settingsForm.BackColor = Color.FromArgb(30, 30, 30);
            settingsForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            settingsForm.MaximizeBox = false;

            Label lbl = new Label();
            lbl.Text = "Размер шрифта (от 8 до 24):";
            lbl.ForeColor = Color.White;
            lbl.Location = new Point(20, 20);
            lbl.Size = new Size(250, 30);

            NumericUpDown num = new NumericUpDown();
            num.Location = new Point(20, 50);
            num.Size = new Size(100, 30);
            num.Minimum = 8;
            num.Maximum = 24;
            num.Value = currentFontSize;

            Button btnSave = new Button();
            btnSave.Text = "Применить";
            btnSave.Location = new Point(20, 100);
            btnSave.Size = new Size(100, 40);
            btnSave.BackColor = Color.Green;
            btnSave.ForeColor = Color.White;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Click += (s, ev) => {
                currentFontSize = (int)num.Value;
                SaveSettings();
                MessageBox.Show("Шрифт сохранен! Перезапуск.", "Готово");
                Application.Restart();
                Environment.Exit(0);
            };

            settingsForm.Controls.Add(lbl);
            settingsForm.Controls.Add(num);
            settingsForm.Controls.Add(btnSave);
            settingsForm.ShowDialog();
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
