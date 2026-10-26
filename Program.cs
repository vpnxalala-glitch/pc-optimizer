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
        private Label lblStatus;
        private Label lblInfo;
        private Label lblDesc;
        private bool isOptimized = false;
        
        string statusPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCOptimizer_Status.txt");

        public MainForm()
        {
            InitializeComponent();
            LoadStatus(); 
        }

        // Функция для создания круглых кнопок
        private GraphicsPath GetRoundPath(RectangleF rect, int radius)
        {
            float r2 = radius / 2f;
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
            this.Size = new Size(450, 400);
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Заголовок
            lblInfo = new Label();
            lblInfo.Text = "ОПТИМИЗАЦИЯ WINDOWS";
            lblInfo.ForeColor = Color.FromArgb(255, 215, 0); // Золотой цвет
            lblInfo.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblInfo.Location = new Point(0, 30);
            lblInfo.Size = new Size(450, 30);
            lblInfo.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblInfo);

            Label lblSub = new Label();
            lblSub.Text = "Ускорение системы и Minecraft";
            lblSub.ForeColor = Color.Gray;
            lblSub.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lblSub.Location = new Point(0, 65);
            lblSub.Size = new Size(450, 20);
            lblSub.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblSub);

            // Кнопка ВКЛ
            btnOn = new Button();
            btnOn.Text = "ВКЛ";
            btnOn.Location = new Point(50, 120);
            btnOn.Size = new Size(150, 60);
            btnOn.BackColor = Color.FromArgb(0, 200, 100);
            btnOn.ForeColor = Color.White;
            btnOn.FlatStyle = FlatStyle.Flat;
            btnOn.FlatAppearance.BorderSize = 0;
            btnOn.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            btnOn.Cursor = Cursors.Hand;
            btnOn.Click += new EventHandler(BtnOn_Click);
            // Скругляем кнопку при отрисовке
            btnOn.Paint += (s, e) => {
                GraphicsPath path = GetRoundPath(new RectangleF(0, 0, btnOn.Width, btnOn.Height), 20);
                btnOn.Region = new Region(path);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            };
            this.Controls.Add(btnOn);

            // Кнопка ВЫКЛ
            btnOff = new Button();
            btnOff.Text = "ВЫКЛ";
            btnOff.Location = new Point(250, 120);
            btnOff.Size = new Size(150, 60);
            btnOff.BackColor = Color.FromArgb(220, 50, 50);
            btnOff.ForeColor = Color.White;
            btnOff.FlatStyle = FlatStyle.Flat;
            btnOff.FlatAppearance.BorderSize = 0;
            btnOff.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            btnOff.Cursor = Cursors.Hand;
            btnOff.Enabled = false;
            btnOff.Click += new EventHandler(BtnOff_Click);
            btnOff.Paint += (s, e) => {
                GraphicsPath path = GetRoundPath(new RectangleF(0, 0, btnOff.Width, btnOff.Height), 20);
                btnOff.Region = new Region(path);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            };
            this.Controls.Add(btnOff);

            // Статус
            lblStatus = new Label();
            lblStatus.Text = "Статус: Ожидание";
            lblStatus.ForeColor = Color.White;
            lblStatus.Font = new Font("Segoe UI", 12, FontStyle.Regular);
            lblStatus.Location = new Point(0, 220);
            lblStatus.Size = new Size(450, 30);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblStatus);

            // Описание внизу
            lblDesc = new Label();
            lblDesc.Text = "Нажмите ВКЛ для оптимизации.\nНажмите ВЫКЛ для возврата настроек.";
            lblDesc.ForeColor = Color.DarkGray;
            lblDesc.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblDesc.Location = new Point(0, 300);
            lblDesc.Size = new Size(450, 40);
            lblDesc.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblDesc);
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
                    btnOn.BackColor = Color.FromArgb(50, 50, 50); // Тускнеет
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
                // 1. Очистка Minecraft
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

                // 2. Очистка Temp
                string tempPath = Path.GetTempPath();
                try {
                    DirectoryInfo di = new DirectoryInfo(tempPath);
                    foreach (FileInfo file in di.GetFiles()) {
                        try { file.Delete(); } catch { }
                    }
                } catch { }

                // 3. Сброс DNS
                ProcessStartInfo psiNet = new ProcessStartInfo("cmd.exe", "/c ipconfig /flushdns & netsh int tcp set global autotuninglevel=normal");
                psiNet.WindowStyle = ProcessWindowStyle.Hidden;
                psiNet.CreateNoWindow = true;
                Process.Start(psiNet);

                // 4. Очистка RAM
                Process[] processes = Process.GetProcesses();
                foreach (Process proc in processes) {
                    try { EmptyWorkingSet(proc.Handle); } catch { }
                }

                // 5. Питание
                ProcessStartInfo psi2 = new ProcessStartInfo("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                psi2.WindowStyle = ProcessWindowStyle.Hidden;
                psi2.CreateNoWindow = true;
                Process.Start(psi2);

                lblStatus.Text = "Статус: Успешно оптимизировано!";
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
                // 1. Питание
                ProcessStartInfo psi = new ProcessStartInfo("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.CreateNoWindow = true;
                Process.Start(psi);

                // 2. Сеть
                ProcessStartInfo psiNet = new ProcessStartInfo("cmd.exe", "/c netsh int tcp set global autotuninglevel=restricted");
                psiNet.WindowStyle = ProcessWindowStyle.Hidden;
                psiNet.CreateNoWindow = true;
                Process.Start(psiNet);

                lblStatus.Text = "Статус: Настройки восстановлены";
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
