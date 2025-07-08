using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
                
                // 10초 카운트다운
                for (int i = 10; i >= 1; i--)
                {
                    OnLogMessage($"System will restart in {i} seconds...");
                    Thread.Sleep(1000);
                }

                OnLogMessage("Executing system restart now.");

                // Windows 시스템 재시작 명령 실행
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "shutdown",
                    Arguments = "/r /t 0 /c \"Scheduled restart by Enhanced Watchdog\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                OnLogMessage($"Failed to execute system restart: {ex.Message}");
                OnLogMessage("Please check if the program has sufficient privileges to restart the system.");
            }
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
