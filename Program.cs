using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PCOptimizer
{
    public partial class MainForm : Form
    {
        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwndProc);

        private Button btnOn;
        private Button btnOff;
        private Button btnPack;
        private Label lblStatus;
        private Label lblInfo;
        private bool isOptimized = false;
        
        string statusPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCOptimizer_Status.txt");

        public MainForm()
        {
            InitializeComponent();
            LoadStatus(); 
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
            this.Text = "PC Optimizer Pro + Skin Boost";
            this.Size = new Size(460, 460);
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            lblInfo = new Label();
            lblInfo.Text = "ОПТИМИЗАЦИЯ WINDOWS И MINECRAFT";
            lblInfo.ForeColor = Color.FromArgb(255, 215, 0);
            lblInfo.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblInfo.Location = new Point(0, 20);
            lblInfo.Size = new Size(460, 30);
            lblInfo.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblInfo);

            // Кнопка ВКЛ
            btnOn = new Button();
            btnOn.Text = "ВКЛ";
            btnOn.Location = new Point(45, 80);
            btnOn.Size = new Size(170, 50);
            btnOn.BackColor = Color.FromArgb(0, 200, 100);
            btnOn.ForeColor = Color.White;
            btnOn.FlatStyle = FlatStyle.Flat;
            btnOn.FlatAppearance.BorderSize = 0;
            btnOn.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            btnOn.Cursor = Cursors.Hand;
            btnOn.Click += new EventHandler(BtnOn_Click);
            btnOn.Paint += (s, e) => { btnOn.Region = new Region(GetRoundPath(new RectangleF(0, 0, btnOn.Width, btnOn.Height), 20)); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; };
            this.Controls.Add(btnOn);

            // Кнопка ВЫКЛ
            btnOff = new Button();
            btnOff.Text = "ВЫКЛ";
            btnOff.Location = new Point(245, 80);
            btnOff.Size = new Size(170, 50);
            btnOff.BackColor = Color.FromArgb(220, 50, 50);
            btnOff.ForeColor = Color.White;
            btnOff.FlatStyle = FlatStyle.Flat;
            btnOff.FlatAppearance.BorderSize = 0;
            btnOff.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            btnOff.Cursor = Cursors.Hand;
            btnOff.Enabled = false;
            btnOff.Click += new EventHandler(BtnOff_Click);
            btnOff.Paint += (s, e) => { btnOff.Region = new Region(GetRoundPath(new RectangleF(0, 0, btnOff.Width, btnOff.Height), 20)); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; };
            this.Controls.Add(btnOff);

            // Кнопка СКИНЫ
            btnPack = new Button();
            btnPack.Text = "УПРОСТИТЬ СКИНЫ (7 ЦВЕТОВ)";
            btnPack.Location = new Point(45, 150);
            btnPack.Size = new Size(370, 50);
            btnPack.BackColor = Color.FromArgb(100, 100, 255);
            btnPack.ForeColor = Color.White;
            btnPack.FlatStyle = FlatStyle.Flat;
            btnPack.FlatAppearance.BorderSize = 0;
            btnPack.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnPack.Cursor = Cursors.Hand;
            btnPack.Click += new EventHandler(BtnPack_Click);
            btnPack.Paint += (s, e) => { btnPack.Region = new Region(GetRoundPath(new RectangleF(0, 0, btnPack.Width, btnPack.Height), 20)); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; };
            this.Controls.Add(btnPack);

            // Статус
            lblStatus = new Label();
            lblStatus.Text = "Статус: Ожидание";
            lblStatus.ForeColor = Color.White;
            lblStatus.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            lblStatus.Location = new Point(0, 230);
            lblStatus.Size = new Size(460, 30);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblStatus);

            // Описание
            Label lblDesc = new Label();
            lblDesc.Text = "ВКЛ: Очистка памяти, кэша, настройка сети и питания.\nВЫКЛ: Возврат настроек Windows.\nУПРОСТИТЬ СКИНЫ: Создает ресурспак для Майнкрафта.";
            lblDesc.ForeColor = Color.Gray;
            lblDesc.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblDesc.Location = new Point(0, 290);
            lblDesc.Size = new Size(460, 80);
            lblDesc.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblDesc);
        }

        private void BtnPack_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Статус: Создание ресурспака...";
            lblStatus.ForeColor = Color.Yellow;
            Application.DoEvents();

            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string mcPath = Path.Combine(appData, ".minecraft");
                
                if (!Directory.Exists(mcPath)) {
                    lblStatus.Text = "Ошибка: Папка .minecraft не найдена!";
                    lblStatus.ForeColor = Color.Red;
                    return;
                }

                string rpPath = Path.Combine(mcPath, "resourcepacks", "NoSkinLag");
                string texturePath = Path.Combine(rpPath, "assets", "minecraft", "textures", "entity");

                Directory.CreateDirectory(texturePath);

                File.WriteAllText(Path.Combine(rpPath, "pack.mcmeta"), 
                    "{\n  \"pack\": {\n    \"pack_format\": 15,\n    \"description\": \"7-Color Skins\"\n  }\n}");

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
                            g.FillRectangle(blackBrush, 9, 11, 2, 2); // Левый глаз
                            g.FillRectangle(blackBrush, 13, 11, 2, 2); // Правый глаз
                            g.FillRectangle(blackBrush, 9, 14, 6, 1); // Рот
                            g.FillRectangle(blackBrush, 8, 13, 1, 1); // Уголок рта
                            g.FillRectangle(blackBrush, 15, 13, 1, 1); // Уголок рта
                        }
                    }
                    bmp.Save(Path.Combine(texturePath, "steve.png"), System.Drawing.Imaging.ImageFormat.Png);
                    bmp.Save(Path.Combine(texturePath, "alex.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                lblStatus.Text = "Статус: Ресурспак создан! Включите его в игре.";
                lblStatus.ForeColor = Color.FromArgb(0, 230, 118);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Ошибка: " + ex.Message;
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void LoadStatus()
        {
            if (File.Exists(statusPath))
            {
                string status = File.ReadAllText(statusPath).Trim();
                if (status == "ON")
                {
                    isOptimized = true;
                    btnOn.Enabled = false;
                    btnOn.BackColor = Color.FromArgb(50, 50, 50);
                    btnOff.Enabled = true;
                    btnOff.BackColor = Color.FromArgb(220, 50, 50);
                    lblStatus.Text = "Статус: Оптимизировано";
                    lblStatus.ForeColor = Color.FromArgb(0, 230, 118);
                }
            }
        }

        private void SaveStatus(string status)
        {
            try { File.WriteAllText(statusPath, status); } catch { }
        }

        private void BtnOn_Click(object sender, EventArgs e)
        {
            if (isOptimized) return;
            lblStatus.Text = "Статус: Оптимизация...";
            lblStatus.ForeColor = Color.Yellow;
            Application.DoEvents();

            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string mcPath = Path.Combine(appData, ".minecraft");
                if (Directory.Exists(mcPath))
                {
                    string[] dirsToClear = { "logs", "crash-reports", "webcache2" };
                    foreach (var dir in dirsToClear)
                    {
                        string fullPath = Path.Combine(mcPath, dir);
                        if (Directory.Exists(fullPath))
                        {
                            try {
                                DirectoryInfo di = new DirectoryInfo(fullPath);
                                foreach (FileInfo file in di.GetFiles()) file.Delete();
                            } catch { }
                        }
                    }
                }

                string tempPath = Path.GetTempPath();
                try {
                    DirectoryInfo di = new DirectoryInfo(tempPath);
                    foreach (FileInfo file in di.GetFiles()) {
                        try { file.Delete(); } catch { }
                    }
                } catch { }

                ProcessStartInfo psiNet = new ProcessStartInfo("cmd.exe", "/c ipconfig /flushdns & netsh int tcp set global autotuninglevel=normal");
                psiNet.WindowStyle = ProcessWindowStyle.Hidden;
                psiNet.CreateNoWindow = true;
                Process.Start(psiNet);

                Process[] processes = Process.GetProcesses();
                foreach (Process proc in processes) {
                    try { EmptyWorkingSet(proc.Handle); } catch { }
                }

                ProcessStartInfo psi2 = new ProcessStartInfo("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                psi2.WindowStyle = ProcessWindowStyle.Hidden;
                psi2.CreateNoWindow = true;
                Process.Start(psi2);

                lblStatus.Text = "Статус: Успешно!";
                lblStatus.ForeColor = Color.FromArgb(0, 230, 118);
                btnOn.Enabled = false;
                btnOn.BackColor = Color.FromArgb(50, 50, 50);
                btnOff.Enabled = true;
                btnOff.BackColor = Color.FromArgb(220, 50, 50);
                isOptimized = true;
                SaveStatus("ON"); 
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Ошибка: " + ex.Message;
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void BtnOff_Click(object sender, EventArgs e)
        {
            if (!isOptimized) return;
            lblStatus.Text = "Статус: Восстановление...";
            lblStatus.ForeColor = Color.Yellow;
            Application.DoEvents();

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.CreateNoWindow = true;
                Process.Start(psi);

                ProcessStartInfo psiNet = new ProcessStartInfo("cmd.exe", "/c netsh int tcp set global autotuninglevel=restricted");
                psiNet.WindowStyle = ProcessWindowStyle.Hidden;
                psiNet.CreateNoWindow = true;
                Process.Start(psiNet);

                lblStatus.Text = "Статус: Восстановлено";
                lblStatus.ForeColor = Color.FromArgb(255, 82, 82);
                btnOn.Enabled = true;
                btnOn.BackColor = Color.FromArgb(0, 200, 100);
                btnOff.Enabled = false;
                btnOff.BackColor = Color.FromArgb(50, 50, 50);
                isOptimized = false;
                SaveStatus("OFF"); 
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Ошибка: " + ex.Message;
                lblStatus.ForeColor = Color.Red;
            }
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
