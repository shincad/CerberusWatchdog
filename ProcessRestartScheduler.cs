using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EnhancedWatchdog
{
    /// <summary>
    /// 정해진 시간에 프로세스를 재시작하는 스케줄러 클래스
    /// 시스템 재시작과는 별개로 개별 프로세스의 스케줄된 재시작을 담당
    /// </summary>
    public class ProcessRestartScheduler : IDisposable
    {
        private Timer scheduleTimer;
        private WatchdogService watchdogService;
        private readonly object lockObject = new object();
        private volatile bool isRunning = false;
        private readonly Dictionary<string, DateTime?> lastScheduleCheck = new Dictionary<string, DateTime?>();

        public event EventHandler<LogMessageEventArgs> LogMessage;
        public event EventHandler<ProcessRestartedEventArgs> ProcessScheduledRestart;

        public bool IsRunning => isRunning;

        public ProcessRestartScheduler(WatchdogService watchdogService)
        {
            this.watchdogService = watchdogService ?? throw new ArgumentNullException(nameof(watchdogService));
        }

        public void Start()
        {
            lock (lockObject)
            {
                if (isRunning) return;

                isRunning = true;

                // 매분마다 체크 (60초 간격)
                scheduleTimer = new Timer(CheckScheduledRestarts, null, 0, 60000);
                OnLogMessage("Process restart scheduler started.");

                // 스케줄된 프로세스 목록 로깅
                LogScheduledProcesses();
            }
        }

        public void Stop()
        {
            lock (lockObject)
            {
                if (!isRunning) return;

                isRunning = false;
                scheduleTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                OnLogMessage("Process restart scheduler stopped.");
            }
        }

        private void LogScheduledProcesses()
        {
            try
            {
                var processes = watchdogService.GetProcesses();
                var scheduledProcesses = processes.Where(p => p.EnableScheduledRestart).ToList();

                if (scheduledProcesses.Any())
                {
                    OnLogMessage($"Found {scheduledProcesses.Count} process(es) with scheduled restart:");
                    foreach (var process in scheduledProcesses)
                    {
                        var nextRestartInfo = GetNextRestartInfo(process);
                        OnLogMessage($"  - {process.Name}: {nextRestartInfo}");
                    }
                }
                else
                {
                    OnLogMessage("No processes have scheduled restart enabled.");
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error logging scheduled processes: {ex.Message}");
            }
        }

        private void CheckScheduledRestarts(object state)
        {
            if (!isRunning) return;

            try
            {
                var processes = watchdogService.GetProcesses();
                var scheduledProcesses = processes.Where(p => p.EnableScheduledRestart).ToList();

                foreach (var processInfo in scheduledProcesses)
                {
                    CheckProcessSchedule(processInfo);
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error checking scheduled restarts: {ex.Message}");
            }
        }

        private void CheckProcessSchedule(ProcessInfo processInfo)
        {
            try
            {
                var now = DateTime.Now;
                var targetTime = new DateTime(now.Year, now.Month, now.Day, 
                    processInfo.ScheduledRestartHour, processInfo.ScheduledRestartMinute, 0);

                // 중복 실행 방지를 위한 체크
                var checkKey = $"{processInfo.Name}_{targetTime:yyyyMMddHHmm}";
                if (lastScheduleCheck.ContainsKey(checkKey) && 
                    lastScheduleCheck[checkKey].HasValue &&
                    (now - lastScheduleCheck[checkKey].Value).TotalMinutes < 2)
                {
                    return; // 이미 처리됨
                }

                bool shouldRestart = false;

                if (processInfo.ScheduledRestartType == ScheduledRestartType.Daily)
                {
                    // 매일: 설정된 시간과 현재 시간이 일치하는지 확인 (±1분 허용)
                    if (Math.Abs((now - targetTime).TotalMinutes) < 1)
                    {
                        shouldRestart = true;
                    }
                }
                else if (processInfo.ScheduledRestartType == ScheduledRestartType.Weekly)
                {
                    // 매주: 설정된 요일이고 설정된 시간과 현재 시간이 일치하는지 확인
                    if (now.DayOfWeek == processInfo.ScheduledRestartDayOfWeek &&
                        Math.Abs((now - targetTime).TotalMinutes) < 1)
                    {
                        shouldRestart = true;
                    }
                }

                if (shouldRestart)
                {
                    lastScheduleCheck[checkKey] = now;
                    ExecuteScheduledRestart(processInfo);
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error checking schedule for process {processInfo.Name}: {ex.Message}");
            }
        }

        private async void ExecuteScheduledRestart(ProcessInfo processInfo)
        {
            try
            {
                OnLogMessage($"=== SCHEDULED RESTART: {processInfo.Name} ===");
                OnLogMessage($"Scheduled restart time reached: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                // 현재 실행 중인 프로세스 찾기
                var currentProcess = GetProcessByName(processInfo.Name);
                
                if (currentProcess != null)
                {
                    OnLogMessage($"Terminating current process: {processInfo.Name} (PID: {currentProcess.Id})");
                    
                    try
                    {
                        // 프로세스 종료
                        currentProcess.Kill();
                        
                        // 프로세스가 완전히 종료될 때까지 대기 (최대 10초)
                        if (!currentProcess.WaitForExit(10000))
                        {
                            OnLogMessage($"Warning: Process {processInfo.Name} did not exit gracefully within 10 seconds");
                        }
                        else
                        {
                            OnLogMessage($"Process {processInfo.Name} terminated successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        OnLogMessage($"Error terminating process {processInfo.Name}: {ex.Message}");
                    }
                }
                else
                {
                    OnLogMessage($"Process {processInfo.Name} is not currently running");
                }

                // 프로세스 재시작을 위해 잠시 대기
                await Task.Delay(2000);

                // 프로세스 재시작
                bool restartSuccess = await RestartProcess(processInfo);
                
                if (restartSuccess)
                {
                    processInfo.LastScheduledRestart = DateTime.Now;
                    OnLogMessage($"Scheduled restart completed successfully: {processInfo.Name}");
                    OnProcessScheduledRestart(processInfo.Name, processInfo.ProcessId ?? 0);
                    
                    // 다음 스케줄 정보 로깅
                    var nextRestartInfo = GetNextRestartInfo(processInfo);
                    OnLogMessage($"Next scheduled restart for {processInfo.Name}: {nextRestartInfo}");
                }
                else
                {
                    OnLogMessage($"Scheduled restart failed: {processInfo.Name}");
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error executing scheduled restart for {processInfo.Name}: {ex.Message}");
            }
        }

        private async Task<bool> RestartProcess(ProcessInfo processInfo)
        {
            try
            {
                OnLogMessage($"Starting process: {processInfo.Name}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = processInfo.ExecutablePath,
                    Arguments = processInfo.Arguments ?? string.Empty,
                    WorkingDirectory = string.IsNullOrEmpty(processInfo.WorkingDirectory)
                        ? Path.GetDirectoryName(processInfo.ExecutablePath)
                        : processInfo.WorkingDirectory,
                    UseShellExecute = false
                };

                var newProcess = Process.Start(startInfo);
                if (newProcess != null)
                {
                    // 프로세스가 실제로 시작되었는지 확인
                    await Task.Delay(1000);
                    
                    if (!newProcess.HasExited)
                    {
                        processInfo.ProcessId = newProcess.Id;
                        processInfo.IsRunning = true;
                        OnLogMessage($"Process started successfully: {processInfo.Name} (PID: {newProcess.Id})");
                        return true;
                    }
                    else
                    {
                        OnLogMessage($"Process {processInfo.Name} started but exited immediately (Exit code: {newProcess.ExitCode})");
                        return false;
                    }
                }
                else
                {
                    OnLogMessage($"Failed to start process: {processInfo.Name}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error starting process {processInfo.Name}: {ex.Message}");
                return false;
            }
        }

        private Process GetProcessByName(string processName)
        {
            try
            {
                var processNameWithoutExtension = Path.GetFileNameWithoutExtension(processName);
                var processes = Process.GetProcessesByName(processNameWithoutExtension);
                return processes.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public string GetNextRestartInfo(ProcessInfo processInfo)
        {
            if (!processInfo.EnableScheduledRestart)
            {
                return "Scheduled restart is disabled";
            }

            try
            {
                var now = DateTime.Now;
                DateTime nextRestart;

                if (processInfo.ScheduledRestartType == ScheduledRestartType.Daily)
                {
                    // 매일: 오늘 또는 내일의 설정된 시간
                    nextRestart = new DateTime(now.Year, now.Month, now.Day, 
                        processInfo.ScheduledRestartHour, processInfo.ScheduledRestartMinute, 0);
                    if (nextRestart <= now)
                    {
                        nextRestart = nextRestart.AddDays(1);
                    }
                }
                else
                {
                    // 매주: 다음 설정된 요일의 설정된 시간
                    var daysUntilTarget = ((int)processInfo.ScheduledRestartDayOfWeek - (int)now.DayOfWeek + 7) % 7;
                    if (daysUntilTarget == 0)
                    {
                        // 오늘이 설정된 요일인 경우
                        nextRestart = new DateTime(now.Year, now.Month, now.Day, 
                            processInfo.ScheduledRestartHour, processInfo.ScheduledRestartMinute, 0);
                        if (nextRestart <= now)
                        {
                            daysUntilTarget = 7; // 다음 주
                        }
                    }

                    if (daysUntilTarget > 0)
                    {
                        nextRestart = now.AddDays(daysUntilTarget);
                        nextRestart = new DateTime(nextRestart.Year, nextRestart.Month, nextRestart.Day, 
                            processInfo.ScheduledRestartHour, processInfo.ScheduledRestartMinute, 0);
                    }
                    else
                    {
                        nextRestart = new DateTime(now.Year, now.Month, now.Day, 
                            processInfo.ScheduledRestartHour, processInfo.ScheduledRestartMinute, 0);
                    }
                }

                var timeSpan = nextRestart - now;
                return $"{nextRestart:yyyy-MM-dd HH:mm:ss} ({processInfo.ScheduledRestartType}) - in {timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
            }
            catch (Exception ex)
            {
                return $"Error calculating next restart: {ex.Message}";
            }
        }

        public string GetAllScheduledProcessesInfo()
        {
            try
            {
                var processes = watchdogService.GetProcesses();
                var scheduledProcesses = processes.Where(p => p.EnableScheduledRestart).ToList();

                if (!scheduledProcesses.Any())
                {
                    return "No processes have scheduled restart enabled.";
                }

                var info = $"Scheduled restarts ({scheduledProcesses.Count} processes):\n";
                foreach (var process in scheduledProcesses)
                {
                    var nextRestartInfo = GetNextRestartInfo(process);
                    info += $"• {process.Name}: {nextRestartInfo}\n";
                }

                return info.TrimEnd('\n');
            }
            catch (Exception ex)
            {
                return $"Error getting scheduled processes info: {ex.Message}";
            }
        }

        private void OnLogMessage(string message)
        {
            LogMessage?.Invoke(this, new LogMessageEventArgs(message));
        }

        private void OnProcessScheduledRestart(string processName, int newProcessId)
        {
            ProcessScheduledRestart?.Invoke(this, new ProcessRestartedEventArgs(processName, newProcessId));
        }

        public void Dispose()
        {
            Stop();
            scheduleTimer?.Dispose();
        }
    }
}
