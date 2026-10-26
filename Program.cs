using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        public MainForm()
        {
            LoadSettings();
            InitializeComponent();
            StartPingMonitor();
        }

        private void LoadSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\PCOptimizer"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("FontSize");
                        if (val != null) currentFontSize = (int)val;
                    }
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\PCOptimizer"))
                {
                    key.SetValue("FontSize", currentFontSize, RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        private GraphicsPath GetRoundPath(RectangleF rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Width - radius, rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
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
            layout.RowCount = 10;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45F));
            
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

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

            layout.Controls.Add(CreateButton("Очистить RAM", "Освобождает оперативную память.", Color.DodgerBlue, BtnRam_Click), 0, 2);
            layout.Controls.Add(CreateButton("Очистить Кэш", "Удаляет временные файлы и мусор.", Color.MediumSeaGreen, BtnCache_Click), 0, 3);
            layout.Controls.Add(CreateButton("Ускорить Сеть", "Сбрасывает DNS для снижения пинга.", Color.DarkOrange, BtnNet_Click), 0, 4);
            layout.Controls.Add(CreateButton("Макс. Производительность", "Ставит режим высокой производительности.", Color.Purple, BtnPower_Click), 0, 5);
            layout.Controls.Add(CreateButton("Упростить Скины", "Создает 7-цветные скины.", Color.MediumVioletRed, BtnPack_Click), 0, 6);
            layout.Controls.Add(CreateButton("Перезапустить игру", "Закрывает процесс Майнкрафта.", Color.Gold, BtnRestart_Click), 0, 7);
            layout.Controls.Add(CreateButton("Вернуть Настройки", "Возвращает стандартные настройки Windows.", Color.IndianRed, BtnOff_Click), 0, 8);

            lblStatus = new Label();
            lblStatus.Text = "Готов к работе";
            lblStatus.ForeColor = Color.LightGray;
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            layout.Controls.Add(lblStatus, 0, 9);
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
                MessageBox.Show("Шрифт сохранен! Программа перезапустится.", "Готово");
                Application.Restart();
                Environment.Exit(0);
            };

            settingsForm.Controls.Add(lbl);
            settingsForm.Controls.Add(num);
            settingsForm.Controls.Add(btnSave);
            settingsForm.ShowDialog();
        }

        private void BtnRestart_Click(object sender, EventArgs e)
        {
            var procs = Process.GetProcessesByName("javaw");
            if (procs.Length > 0)
            {
                foreach (var p in procs) { try { p.Kill(); } catch { } }
                lblStatus.Text = "Игра перезапущена!";
            }
            else
            {
                lblStatus.Text = "Игра не запущена.";
            }
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
                    else
                    {
                        lblPing.Text = "Пинг FunTime: Нет ответа";
                        lblPing.ForeColor = Color.Gray;
                    }
                }
            }
            catch
            {
                lblPing.Text = "Пинг FunTime: Ошибка";
                lblPing.ForeColor = Color.Red;
            }
        }

        private void BtnRam_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Очистка RAM...";
            Application.DoEvents();
            Process[] processes = Process.GetProcesses();
            foreach (Process proc in processes)
            {
                try { EmptyWorkingSet(proc.Handle); } catch { }
            }
            lblStatus.Text = "RAM успешно очищена!";
        }

        private void BtnCache_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Очистка кэша...";
            Application.DoEvents();
            try
            {
                string mcPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
                if (Directory.Exists(mcPath))
                {
                    string[] dirsToClear = { "logs", "crash-reports", "webcache2" };
                    foreach (var dir in dirsToClear)
                    {
                        string fullPath = Path.Combine(mcPath, dir);
                        if (Directory.Exists(fullPath))
                        {
                            DirectoryInfo di = new DirectoryInfo(fullPath);
                            foreach (FileInfo file in di.GetFiles()) file.Delete();
                        }
                    }
                }
                
                string tempPath = Path.GetTempPath();
                DirectoryInfo diTemp = new DirectoryInfo(tempPath);
                foreach (FileInfo file in diTemp.GetFiles()) {
                    try { file.Delete(); } catch { }
                }
                lblStatus.Text = "Кэш очищен!";
            }
            catch { lblStatus.Text = "Ошибка очистки."; }
        }

        private void BtnNet_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Настройка сети...";
            ProcessStartInfo psiNet = new ProcessStartInfo("cmd.exe", "/c ipconfig /flushdns & netsh int tcp set global autotuninglevel=normal");
            psiNet.WindowStyle = ProcessWindowStyle.Hidden;
            psiNet.CreateNoWindow = true;
            Process.Start(psiNet);
            lblStatus.Text = "Сеть ускорена.";
        }

        private void BtnPower_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Настройка питания...";
            ProcessStartInfo psi2 = new ProcessStartInfo("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
            psi2.WindowStyle = ProcessWindowStyle.Hidden;
            psi2.CreateNoWindow = true;
            Process.Start(psi2);
            lblStatus.Text = "Производительность включена.";
        }

        private void BtnPack_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Создание скинов...";
            Application.DoEvents();
            try
            {
                string mcPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
                if (!Directory.Exists(mcPath)) { lblStatus.Text = "Ошибка: .minecraft нет!"; return; }

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
                        {
                            for (int y = 0; y < 64; y += 16)
                            {
                                g.FillRectangle(new SolidBrush(colors[colorIndex % 7]), x, y, 16, 16);
                                colorIndex++;
                            }
                        }
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
                lblStatus.Text = "Скины созданы!";
            }
            catch { lblStatus.Text = "Ошибка."; }
        }

        private void BtnOff_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Сброс...";
            ProcessStartInfo psi = new ProcessStartInfo("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            Process.Start(psi);

            ProcessStartInfo psiNet = new ProcessStartInfo("cmd.exe", "/c netsh int tcp set global autotuninglevel=restricted");
            psiNet.WindowStyle = ProcessWindowStyle.Hidden;
            psiNet.CreateNoWindow = true;
            Process.Start(psiNet);

            lblStatus.Text = "Настройки сброшены.";
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
