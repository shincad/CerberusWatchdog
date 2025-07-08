using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace EnhancedWatchdog
{
    internal static class Program
    {
        private static Mutex mutex;
        private const string MutexName = "EnhancedWatchdogSingleInstance";
        private const string WindowTitle = "Cerberus Watchdog V1.4-Written by shincad";

        // Windows API 함수들
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Session 0 문제 해결을 위한 API 함수들
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll")]
        private static extern bool ProcessIdToSessionId(uint dwProcessId, out uint pSessionId);

        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        [STAThread]
        static void Main(string[] args)
        {
            // Session 0 체크 - 서비스 세션에서 실행되는지 확인
            if (IsRunningInSession0())
            {
                // Session 0에서 실행 중이면 사용자 세션에서 다시 실행
                RestartInUserSession(args);
                return;
            }

            // 중복 실행 방지를 위한 Mutex 생성
            bool createdNew;
            mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // 이미 실행 중인 경우 기존 인스턴스를 활성화
                BringExistingInstanceToFront();
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

                autoStart = autoStart || startMinimized;

                // 메인 폼 생성 및 초기화
                var mainForm = new Form1(autoStart);

                // 시작 시 트레이로 최소화 (자동 시작 또는 minimized 플래그가 있을 때)
                if (startMinimized || autoStart)
                {
                    // Form이 로드된 후 트레이로 최소화
                    mainForm.Load += (sender, e) =>
                    {
                        mainForm.HideToTray();
                    };
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

        private static bool IsRunningInSession0()
        {
            try
            {
                uint processId = GetCurrentProcessId();
                if (ProcessIdToSessionId(processId, out uint sessionId))
                {
                    uint activeSessionId = WTSGetActiveConsoleSessionId();
                    return sessionId == 0 && activeSessionId != 0;
                }
            }
            catch
            {
                // API 호출 실패 시 안전하게 false 반환
            }
            return false;
        }

        private static void RestartInUserSession(string[] args)
        {
            try
            {
                var exePath = Application.ExecutablePath;
                var arguments = string.Join(" ", args);

                // 사용자 세션에서 새 프로세스 시작
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                // 재시작 실패 시에도 로그만 남기고 계속 진행
                try
                {
                    string errorLogPath = Path.Combine(AppContext.BaseDirectory, "session_error_log.txt");
                    File.AppendAllText(errorLogPath,
                        $"[{DateTime.Now}] Session restart failed: {ex}\n");
                }
                catch
                {
                    // 로그 실패도 무시
                }
            }
        }

        private static void BringExistingInstanceToFront()
        {
            try
            {
                // 창 제목으로 기존 인스턴스 찾기
                IntPtr hWnd = FindWindow(null, WindowTitle);

                if (hWnd == IntPtr.Zero)
                {
                    // 창이 숨겨져 있을 수 있으므로 프로세스명으로 찾기
                    var processes = Process.GetProcessesByName("CBWatchdog");
                    if (processes.Length == 0)
                    {
                        processes = Process.GetProcessesByName("EnhancedWatchdog");
                    }

                    foreach (var process in processes)
                    {
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            hWnd = process.MainWindowHandle;
                            break;
                        }
                    }
                }

                if (hWnd != IntPtr.Zero)
                {
                    // 기존 창을 포그라운드로 가져오기
                    if (IsIconic(hWnd))
                    {
                        ShowWindow(hWnd, SW_RESTORE);
                    }
                    SetForegroundWindow(hWnd);
                }
                else
                {
                    // 창을 찾을 수 없으면 메시지만 표시
                    MessageBox.Show("Enhanced Watchdog is already running in the background.\nCheck the system tray.",
                        "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // 기존 인스턴스 활성화 실패 시 메시지 표시
                MessageBox.Show($"Enhanced Watchdog is already running.\nError bringing to front: {ex.Message}",
                    "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                var settingsPath = Path.Combine(AppContext.BaseDirectory, "watchdog_settings.json");
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
                // 오류 로그 기록
                string errorLogPath = Path.Combine(AppContext.BaseDirectory, "error_log.txt");
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