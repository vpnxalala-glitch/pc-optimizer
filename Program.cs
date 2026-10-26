using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;

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
        private bool isOptimized = false;
        
        string statusPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCOptimizer_Status.txt");
        string backupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PCOptimizer_Backup.txt");

        public MainForm()
        {
            InitializeComponent();
            LoadStatus(); 
        }

        private void InitializeComponent()
        {
            this.Text = "PC Optimizer Pro";
            this.Size = new Size(420, 320);
            this.BackColor = Color.FromArgb(25, 25, 25);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            lblInfo = new Label();
            lblInfo.Text = "Полная оптимизация Windows и Minecraft";
            lblInfo.ForeColor = Color.LightGray;
            lblInfo.Location = new Point(50, 20);
            lblInfo.Size = new Size(320, 20);
            lblInfo.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblInfo);

            btnOn = new Button();
            btnOn.Text = "ВКЛ";
            btnOn.Location = new Point(70, 70);
            btnOn.Size = new Size(120, 50);
            btnOn.BackColor = Color.FromArgb(0, 230, 118);
            btnOn.FlatStyle = FlatStyle.Flat;
            btnOn.Font = new Font("Arial", 14, FontStyle.Bold);
            btnOn.Click += new EventHandler(BtnOn_Click);
            this.Controls.Add(btnOn);

            btnOff = new Button();
            btnOff.Text = "ВЫКЛ";
            btnOff.Location = new Point(220, 70);
            btnOff.Size = new Size(120, 50);
            btnOff.BackColor = Color.FromArgb(255, 82, 82);
            btnOff.FlatStyle = FlatStyle.Flat;
            btnOff.Font = new Font("Arial", 14, FontStyle.Bold);
            btnOff.Enabled = false;
            btnOff.Click += new EventHandler(BtnOff_Click);
            this.Controls.Add(btnOff);

            lblStatus = new Label();
            lblStatus.Text = "Статус: Ожидание";
            lblStatus.ForeColor = Color.White;
            lblStatus.Location = new Point(50, 150);
            lblStatus.Size = new Size(320, 20);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblStatus);

            Label lblDesc = new Label();
            lblDesc.Text = "ВКЛ: Оптимизация ПК.\nВЫКЛ: Возврат ваших личных настроек.";
            lblDesc.ForeColor = Color.Gray;
            lblDesc.Location = new Point(30, 200);
            lblDesc.Size = new Size(360, 60);
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
                    btnOff.Enabled = true;
                    lblStatus.Text = "Статус: Оптимизировано";
                    lblStatus.ForeColor = Color.FromArgb(0, 230, 118);
                }
            }
        }

        private void SaveStatus(string status)
        {
            try { File.WriteAllText(statusPath, status); } catch { }
        }

        private string GetCurrentPowerPlan()
        {
            try {
                ProcessStartInfo psi = new ProcessStartInfo("powercfg", "/getactivescheme");
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                Process p = Process.Start(psi);
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                
                int start = output.IndexOf("GUID: ") + 6;
                return output.Substring(start, 36);
            } catch { return ""; }
        }

        private void BtnOn_Click(object sender, EventArgs e)
        {
            if (isOptimized) return;
            lblStatus.Text = "Статус: Оптимизация...";
            Application.DoEvents();

            try
            {
                // 1. Запоминаем текущие настройки
                string currentPlan = GetCurrentPowerPlan();
                int currentVisualFX = 0;
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
                if (key != null) {
                    object val = key.GetValue("VisualFXSetting");
                    if (val != null) currentVisualFX = (int)val;
                    key.Close();
                }
                
                File.WriteAllText(backupPath, currentPlan + ";" + currentVisualFX);

                // 2. Очистка Minecraft
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

                // 3. Очистка Temp
                string tempPath = Path.GetTempPath();
                try {
                    DirectoryInfo di = new DirectoryInfo(tempPath);
                    foreach (FileInfo file in di.GetFiles()) {
                        try { file.Delete(); } catch { }
                    }
                } catch { }

                // 4. Сброс DNS и сеть
                ProcessStartInfo psiNet = new ProcessStartInfo("cmd.exe", "/c ipconfig /flushdns & netsh int tcp set global autotuninglevel=normal");
                psiNet.WindowStyle = ProcessWindowStyle.Hidden;
                psiNet.CreateNoWindow = true;
                Process.Start(psiNet);

                // 5. Отключение визуальных эффектов
                if (key != null) { key.SetValue("VisualFXSetting", 2, RegistryValueKind.DWord); key.Close(); }

                // 6. Очистка RAM
                Process[] processes = Process.GetProcesses();
                foreach (Process proc in processes) {
                    try { EmptyWorkingSet(proc.Handle); } catch { }
                }

                // 7. Высокая производительность
                ProcessStartInfo psi2 = new ProcessStartInfo("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                psi2.WindowStyle = ProcessWindowStyle.Hidden;
                psi2.CreateNoWindow = true;
                Process.Start(psi2);

                lblStatus.Text = "Статус: Успешно!";
                lblStatus.ForeColor = Color.FromArgb(0, 230, 118);
                btnOn.Enabled = false;
                btnOff.Enabled = true;
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
            Application.DoEvents();

            try
            {
                // 1. Читаем бэкап
                string oldPlan = "";
                int oldVisualFX = 1; 
                
                if (File.Exists(backupPath)) {
                    string[] parts = File.ReadAllText(backupPath).Split(';');
                    if (parts.Length >= 2) {
                        oldPlan = parts[0];
                        int.TryParse(parts[1], out oldVisualFX);
                    }
                }

                // 2. Возвращаем визуальные эффекты
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
                if (key != null) { key.SetValue("VisualFXSetting", oldVisualFX, RegistryValueKind.DWord); key.Close(); }

                // 3. Возвращаем старую схему питания
                if (!string.IsNullOrEmpty(oldPlan)) {
                    ProcessStartInfo psi = new ProcessStartInfo("powercfg", "/setactive " + oldPlan);
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                    psi.CreateNoWindow = true;
                    Process.Start(psi);
                }

                // 4. Сброс сети
                ProcessStartInfo psiNet = new ProcessStartInfo("cmd.exe", "/c netsh int tcp set global autotuninglevel=restricted");
                psiNet.WindowStyle = ProcessWindowStyle.Hidden;
                psiNet.CreateNoWindow = true;
                Process.Start(psiNet);

                lblStatus.Text = "Статус: Восстановлено";
                lblStatus.ForeColor = Color.FromArgb(255, 82, 82);
                btnOn.Enabled = true;
                btnOff.Enabled = false;
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
