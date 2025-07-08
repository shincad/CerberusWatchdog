using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Principal;
using System.ComponentModel;

namespace EnhancedWatchdog
{
    public class SystemRestartService : IDisposable
    {
        private Timer restartTimer;
        private WatchdogSettings settings;
        private readonly object lockObject = new object();
        private volatile bool isRunning = false;
        private DateTime? lastRestartCheck = null;

        public event EventHandler<LogMessageEventArgs> LogMessage;

        public bool IsRunning => isRunning;

        public SystemRestartService(WatchdogSettings settings)
        {
            this.settings = settings;
        }

        public void Start()
        {
            lock (lockObject)
            {
                if (isRunning) return;

                isRunning = true;

                if (settings.EnableSystemRestart)
                {
                    // 권한 체크
                    CheckSystemRestartPrivileges();

                    // 매분마다 체크 (60초 간격)
                    restartTimer = new Timer(CheckRestartTime, null, 0, 60000);
                    OnLogMessage($"System restart scheduler started. Type: {settings.SystemRestartType}, Time: {settings.SystemRestartHour:D2}:{settings.SystemRestartMinute:D2}");

                    if (settings.SystemRestartType == SystemRestartType.Weekly)
                    {
                        OnLogMessage($"Weekly restart day: {settings.SystemRestartDayOfWeek}");
                    }
                }
                else
                {
                    OnLogMessage("System restart is disabled.");
                }
            }
        }

        public void Stop()
        {
            lock (lockObject)
            {
                if (!isRunning) return;

                isRunning = false;
                restartTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                OnLogMessage("System restart scheduler stopped.");
            }
        }

        public void UpdateSettings(WatchdogSettings newSettings)
        {
            lock (lockObject)
            {
                settings = newSettings.Clone();
                OnLogMessage("System restart settings updated.");

                if (isRunning)
                {
                    // 재시작 서비스 다시 시작
                    Stop();
                    Start();
                }
            }
        }

        private void CheckSystemRestartPrivileges()
        {
            try
            {
                // 현재 사용자의 권한 확인
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                bool hasShutdownPrivilege = principal.IsInRole(WindowsBuiltInRole.Administrator);

                if (!hasShutdownPrivilege)
                {
                    OnLogMessage("Warning: Current user may not have sufficient privileges to restart the system.");
                    OnLogMessage("For reliable system restart, run the application as Administrator.");
                }
                else
                {
                    OnLogMessage("System restart privileges confirmed.");
                }

                // Session 정보 확인
                var sessionId = Process.GetCurrentProcess().SessionId;
                OnLogMessage($"Running in session ID: {sessionId}");

                if (sessionId == 0)
                {
                    OnLogMessage("Note: Running in Session 0 (service session). System restart should work reliably.");
                }
                else
                {
                    OnLogMessage("Note: Running in user session. System restart may require elevated privileges.");
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Could not check system restart privileges: {ex.Message}");
            }
        }

        private void CheckRestartTime(object state)
        {
            if (!isRunning || !settings.EnableSystemRestart) return;

            try
            {
                var now = DateTime.Now;
                var targetTime = new DateTime(now.Year, now.Month, now.Day, settings.SystemRestartHour, settings.SystemRestartMinute, 0);

                // 이미 오늘 체크했는지 확인 (중복 실행 방지)
                if (lastRestartCheck.HasValue &&
                    lastRestartCheck.Value.Date == now.Date &&
                    Math.Abs((lastRestartCheck.Value - targetTime).TotalMinutes) < 1)
                {
                    return; // 이미 처리됨
                }

                bool shouldRestart = false;

                if (settings.SystemRestartType == SystemRestartType.Daily)
                {
                    // 매일: 설정된 시간과 현재 시간이 일치하는지 확인 (±1분 허용)
                    if (Math.Abs((now - targetTime).TotalMinutes) < 1)
                    {
                        shouldRestart = true;
                    }
                }
                else if (settings.SystemRestartType == SystemRestartType.Weekly)
                {
                    // 매주: 설정된 요일이고 설정된 시간과 현재 시간이 일치하는지 확인
                    if (now.DayOfWeek == settings.SystemRestartDayOfWeek &&
                        Math.Abs((now - targetTime).TotalMinutes) < 1)
                    {
                        shouldRestart = true;
                    }
                }

                if (shouldRestart)
                {
                    lastRestartCheck = now;
                    ExecuteSystemRestart();
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error checking restart time: {ex.Message}");
            }
        }

        private void ExecuteSystemRestart()
        {
            try
            {
                OnLogMessage("=== SYSTEM RESTART INITIATED ===");
                OnLogMessage($"Scheduled restart time reached: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                // 여러 방법으로 시스템 재시작 시도
                bool restartSuccess = false;

                // 방법 1: 표준 shutdown 명령 (관리자 권한이 있을 때)
                if (!restartSuccess)
                {
                    restartSuccess = TryStandardShutdown();
                }

                // 방법 2: 강제 재시작 (긴급 상황용)
                if (!restartSuccess)
                {
                    restartSuccess = TryForceShutdown();
                }

                // 방법 3: WMI를 통한 재시작 시도
                if (!restartSuccess)
                {
                    restartSuccess = TryWmiShutdown();
                }

                if (!restartSuccess)
                {
                    OnLogMessage("All restart methods failed. Please check system permissions and try running as Administrator.");
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Failed to execute system restart: {ex.Message}");
                OnLogMessage("Please check if the program has sufficient privileges to restart the system.");
            }
        }

        private bool TryStandardShutdown()
        {
            try
            {
                OnLogMessage("Attempting standard shutdown method...");

                // 30초 카운트다운으로 변경 (로그 저장 시간 확보)
                for (int i = 30; i >= 1; i--)
                {
                    if (i <= 10 || i % 5 == 0) // 마지막 10초와 5초 단위로만 로그
                    {
                        OnLogMessage($"System will restart in {i} seconds...");
                    }
                    Thread.Sleep(1000);
                }

                OnLogMessage("Executing system restart now (standard method).");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "shutdown",
                    Arguments = "/r /t 30 /c \"Scheduled restart by Enhanced Watchdog\" /f",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // 관리자 권한 요청
                };

                var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    process.WaitForExit(5000); // 5초 대기
                    if (process.ExitCode == 0)
                    {
                        OnLogMessage("Standard shutdown command executed successfully.");
                        return true;
                    }
                    else
                    {
                        OnLogMessage($"Standard shutdown failed with exit code: {process.ExitCode}");
                    }
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) // 사용자가 UAC를 취소한 경우
            {
                OnLogMessage("Standard shutdown cancelled by user (UAC declined).");
            }
            catch (Exception ex)
            {
                OnLogMessage($"Standard shutdown method failed: {ex.Message}");
            }

            return false;
        }

        private bool TryForceShutdown()
        {
            try
            {
                OnLogMessage("Attempting force shutdown method...");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "shutdown",
                    Arguments = "/r /t 5 /f", // 5초 후 강제 재시작
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    OnLogMessage("Force shutdown command executed.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Force shutdown method failed: {ex.Message}");
            }

            return false;
        }

        private bool TryWmiShutdown()
        {
            try
            {
                OnLogMessage("Attempting WMI shutdown method...");

                // WMI를 통한 시스템 재시작 (System.Management 참조 필요)
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "os where Primary=TRUE call Reboot",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    process.WaitForExit(10000); // 10초 대기
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();

                    if (process.ExitCode == 0 || output.Contains("ReturnValue = 0"))
                    {
                        OnLogMessage("WMI shutdown command executed successfully.");
                        return true;
                    }
                    else
                    {
                        OnLogMessage($"WMI shutdown failed. Output: {output}, Error: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"WMI shutdown method failed: {ex.Message}");
            }

            return false;
        }

        public string GetNextRestartInfo()
        {
            if (!settings.EnableSystemRestart)
            {
                return "System restart is disabled.";
            }

            var now = DateTime.Now;
            DateTime nextRestart;

            if (settings.SystemRestartType == SystemRestartType.Daily)
            {
                // 매일: 오늘 또는 내일의 설정된 시간
                nextRestart = new DateTime(now.Year, now.Month, now.Day, settings.SystemRestartHour, settings.SystemRestartMinute, 0);
                if (nextRestart <= now)
                {
                    nextRestart = nextRestart.AddDays(1);
                }
            }
            else
            {
                // 매주: 다음 설정된 요일의 설정된 시간
                var daysUntilTarget = ((int)settings.SystemRestartDayOfWeek - (int)now.DayOfWeek + 7) % 7;
                if (daysUntilTarget == 0)
                {
                    // 오늘이 설정된 요일인 경우
                    nextRestart = new DateTime(now.Year, now.Month, now.Day, settings.SystemRestartHour, settings.SystemRestartMinute, 0);
                    if (nextRestart <= now)
                    {
                        daysUntilTarget = 7; // 다음 주
                    }
                }

                if (daysUntilTarget > 0)
                {
                    nextRestart = now.AddDays(daysUntilTarget);
                    nextRestart = new DateTime(nextRestart.Year, nextRestart.Month, nextRestart.Day, settings.SystemRestartHour, settings.SystemRestartMinute, 0);
                }
                else
                {
                    nextRestart = new DateTime(now.Year, now.Month, now.Day, settings.SystemRestartHour, settings.SystemRestartMinute, 0);
                }
            }

            var timeSpan = nextRestart - now;
            return $"Next system restart: {nextRestart:yyyy-MM-dd HH:mm:ss} ({settings.SystemRestartType}) - in {timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        }

        private void OnLogMessage(string message)
        {
            LogMessage?.Invoke(this, new LogMessageEventArgs(message));
        }

        public void Dispose()
        {
            Stop();
            restartTimer?.Dispose();
        }
    }
}