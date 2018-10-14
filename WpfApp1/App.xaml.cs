using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Timers;
using System.Diagnostics;
using System.Threading;

public struct LASTINPUTINFO
{
    public uint cbSize;
    public uint dwTime;
}

public class Win32API
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdProcessId);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);

    public static uint GetIdleTime()
    {
        LASTINPUTINFO info = new LASTINPUTINFO();
        info.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(info);
        GetLastInputInfo(ref info);

        return ((uint)Environment.TickCount - info.dwTime);
    }

    public static string GetActiveWindowName()
    {
        IntPtr hWnd = GetForegroundWindow();
        int id;
        GetWindowThreadProcessId(hWnd, out id);
        Process p = Process.GetProcessById(id);
        Console.WriteLine(p.ProcessName);
        return p.ProcessName;
    }


}


namespace WpfApp1
{

    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private const int DEFAULT_CHECK_INTERVAL = 3000; // 3s
        private const int DEFAULT_IDLE_TILE = 300000;   // 300s 

        private MainWindow mainWindow;
        private int idleTime = 0;
        private int checkInterval = 0;
        private string dstPath = null;
        private System.Timers.Timer checkTimer = null;

        private string active_window_str = null;
        private string error_log = null;
        

        public static string[] CommandLineArgs { get; private set; }

        private bool setupAuguments(string[] args)
        {
            bool flag = true;
            string[] p = new string[] { null, null, null };
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    p[i] = args[i];
                }
            }
            catch (IndexOutOfRangeException)
            {

            }

            // エラーが起きたらデフォルトの値
            // 入力値から1000倍してプログラムで使うmsに変更
            try
            {
                this.checkInterval = int.Parse(p[0]) * 1000;
            }
            catch (Exception)
            {
                this.checkInterval = DEFAULT_CHECK_INTERVAL;
                flag = false;
            }

            try
            {
                this.idleTime = int.Parse(p[1]) * 1000;
            }
            catch (Exception)
            {
                this.idleTime = DEFAULT_IDLE_TILE;
                flag = false;
            }

            // 数値は正の整数のみ
            if(this.checkInterval <= 0)
            {
                this.checkInterval = DEFAULT_CHECK_INTERVAL;
                flag = false;
            }
            if(this.idleTime <= 0)
            {
                this.idleTime = DEFAULT_IDLE_TILE;
                flag = false;
            }

            // pathはFileWriter内でエラー処理するのでそのままを受け入れれば良い
            this.dstPath = p[2];

            return flag;
        }

        private void saveLog()
        { 
            string processName = Win32API.GetActiveWindowName();

            if (Win32API.GetIdleTime() >= this.idleTime)
            {
                processName = "IDLE";
            }

            string date = DateTime.Now.ToString("yyyyMMdd");
            string time = DateTime.Now.ToString("HHmmss");
            string[] args = { processName, date, time };

            FileWriter.FileWriter_RESULT res = FileWriter.write(args);
            if (res == FileWriter.FileWriter_RESULT.SUCCESS)
            {
                this.error_log = "Error: No errors.";
            }
            else if (res == FileWriter.FileWriter_RESULT.READ_ONLY_FILE_ERROR)
            {
                this.error_log = "Error: File is read only mode.";
            }
            else if (res == FileWriter.FileWriter_RESULT.FILE_OPEN_ERROR)
            {
                this.error_log = "Error: File open error.";
            }

            this.active_window_str = processName;

            // update UI
            new Thread(new ThreadStart(ChangeUI)).Start();
        }

        // 設定された時間で記録する
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            this.saveLog();
        }


        // TimerスレッドからGUIを更新するための準備
        private void ChangeUI()
        {
            string processName = Win32API.GetActiveWindowName();
            for (int i = 0; i <= 10; i++)
            {
                Updater uiUpdater = new Updater(UpdateUI);
                Dispatcher.BeginInvoke(DispatcherPriority.Send, uiUpdater, processName);
                Thread.Sleep(500);
            }
        }

        private delegate void Updater(string contentI);

        private void UpdateUI(string content)
        {
            this.mainWindow.setActiveWindowName(this.active_window_str);
            this.mainWindow.setContent2ErrorLog(this.error_log);
            //this.mainWindow.setActiveWindowName(content);
        }

        private void setupTimer()
        {
            this.checkTimer = new System.Timers.Timer(this.checkInterval);
            this.checkTimer.Elapsed += this.OnTimedEvent;
            this.checkTimer.Start();

        }



        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //if (e.Args.Length == 0)
            //    return;
            //CommandLineArgs = e.Args;

            //string dstPath = e.Args[2];

            this.mainWindow = new MainWindow();
            this.mainWindow.Show();

            // コマンドライン引数
            // 第一引数　ActiveWindowを記録する間隔(ms） Default 3000
            // 第二引数　IDLEタイムになるまでの時間(ms)  Default 300000
            // 第三引数　データの保存先。                Default C:/Users/XXX/Desktop/ActiveWindowLog_${data/time}.csv
            if (this.setupAuguments(e.Args) != true)
            {
            };

            int interval_s = this.checkInterval / 1000;
            int idleTime_s = this.idleTime / 1000;
            this.mainWindow.Settings.Content = ("Settings: Interval " + interval_s.ToString() + "s  Idle " + idleTime_s.ToString() + "s");
            this.mainWindow.OutputPath.Content = "Output : " + this.dstPath;


            if (FileWriter.init(this.dstPath) != FileWriter.FileWriter_RESULT.SUCCESS)
            {
                this.mainWindow.setContent2ErrorLog("Error: FILE ACCESS ERROR");
            }

            FileWriter.write(new string[] { "ActiveWindow", "date", "time" });
            this.saveLog();
            this.setupTimer();

        }

        

    }
}
