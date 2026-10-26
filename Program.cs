using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PCOptimizer
{
    class Program
    {
        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwndProc);

        static void Main(string[] args)
        {
            Console.Title = "PC Optimizer & Minecraft Booster";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("========================================");
            Console.WriteLine("      PC OPTIMIZER & MINECRAFT BOOSTER   ");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine("Запуск оптимизации...\n");

            CleanMinecraft();
            OptimizeWindows();
            ClearRAM();
            SetHighPerformance();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[+] Оптимизация завершена успешно!");
            Console.ResetColor();
            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        static void CleanMinecraft()
        {
            Console.Write("[*] Поиск и очистка кэша Minecraft... ");
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string mcPath = Path.Combine(appData, ".minecraft");
            if (Directory.Exists(mcPath))
            {
                string[] dirsToClear = { "logs", "crash-reports", "webcache2" };
                long deletedSize = 0;
                foreach (var dir in dirsToClear)
                {
                    string fullPath = Path.Combine(mcPath, dir);
                    if (Directory.Exists(fullPath))
                    {
                        try {
                            DirectoryInfo di = new DirectoryInfo(fullPath);
                            foreach (FileInfo file in di.GetFiles()) {
                                deletedSize += file.Length;
                                file.Delete();
                            }
                        } catch { }
                    }
                }
                Console.WriteLine($"Готово! Освобождено {deletedSize / 1024 / 1024} МБ.");
            } else {
                Console.WriteLine("Minecraft не найден.");
            }
        }

        static void OptimizeWindows()
        {
            Console.Write("[*] Отключение визуальных эффектов... ");
            try {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
                if (key != null) { key.SetValue("VisualFXSetting", 2, RegistryValueKind.DWord); key.Close(); }
                Console.WriteLine("Готово!");
            } catch { Console.WriteLine("Ошибка."); }
        }

        static void ClearRAM()
        {
            Console.Write("[*] Очистка оперативной памяти... ");
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes) {
                try { EmptyWorkingSet(p.Handle); } catch { }
            }
            Console.WriteLine("Готово!");
        }

        static void SetHighPerformance()
        {
            Console.Write("[*] Включение режима высокой производительности... ");
            try {
                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = "powercfg", Arguments = "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
                    WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true
                };
                Process.Start(psi);
                Console.WriteLine("Готово!");
            } catch { Console.WriteLine("Ошибка."); }
        }
    }
}
