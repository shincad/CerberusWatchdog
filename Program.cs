using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace EnhancedWatchdog
{
    internal static class Program
    {
        private static Mutex mutex;
        private const string MutexName = "EnhancedWatchdogSingleInstance";

        [STAThread]
        static void Main(string[] args)
        {
            // 중복 실행 방지를 위한 Mutex 생성
            bool createdNew;
            mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // 이미 실행 중인 경우 메시지 표시 후 종료
                MessageBox.Show("Enhanced Watchdog is already running.",
                    "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // 애플리케이션 초기화
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 명령줄 인수 파싱
                bool startMinimized = ParseCommandLineArg(args, "/minimized", "-minimized");
                bool autoStart = ParseCommandLineArg(args, "/autostart", "-autostart");

                // 설정 파일에서 자동 시작 옵션 확인
                autoStart = autoStart || LoadAutoStartSettingFromConfig();

                // 메인 폼 생성 및 초기화
                var mainForm = new Form1(autoStart);

                // 최소화 옵션 처리
                if (startMinimized)
                {
                    mainForm.WindowState = FormWindowState.Minimized;
                    mainForm.ShowInTaskbar = false;
                }

                // 애플리케이션 실행
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                // 예외 처리
                HandleUnhandledException(ex);
            }
            finally
            {
                // 리소스 정리
                mutex?.ReleaseMutex();
                mutex?.Dispose();
            }
        }

        private static bool ParseCommandLineArg(string[] args, params string[] argNames)
        {
            return args.Any(arg => argNames.Contains(arg, StringComparer.OrdinalIgnoreCase));
        }

        private static bool LoadAutoStartSettingFromConfig()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "watchdog_settings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonConvert.DeserializeObject<WatchdogData>(json);
                    return settings?.Settings?.AutoStart ?? false;
                }
            }
            catch
            {
                // 설정 파일 읽기 실패 시 기본값 반환
            }
            return false;
        }

        private static void HandleUnhandledException(Exception ex)
        {
            try
            {
                // 오류 로그 기록 (실제 구현에서는 로그 파일에 기록할 수 있음)
                string errorLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                File.AppendAllText(errorLogPath,
                    $"[{DateTime.Now}] Unhandled exception: {ex}\n\n");

                // 사용자에게 오류 메시지 표시
                MessageBox.Show($"A critical error occurred:\n{ex.Message}\n\n" +
                    "The error has been logged. Please contact support.",
                    "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // 오류 처리 중 발생한 예외는 무시
            }
        }
    }

    
}