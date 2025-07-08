using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EnhancedWatchdog
{
    public class WatchdogService : IDisposable
    {
        private readonly object lockObject = new object();
        private readonly ConcurrentDictionary<string, ProcessInfo> monitoredProcesses;
        private Timer monitorTimer;
        private WatchdogSettings settings;
        private volatile bool isRunning = false;
        private volatile bool isMonitoringActive = false;
        private bool isInitialStartup = true;
        private readonly string settingsFilePath;
        private CancellationTokenSource monitoringCts;
        private ManualResetEventSlim monitoringFinishedEvent = new ManualResetEventSlim(true);

        public event EventHandler<ProcessRestartedEventArgs> ProcessRestarted;
        public event EventHandler<ProcessStatusChangedEventArgs> ProcessStatusChanged;
        public event EventHandler<LogMessageEventArgs> LogMessage;

        public bool IsRunning => isRunning;

        public WatchdogService(bool autoStart = false)
        {
            monitoredProcesses = new ConcurrentDictionary<string, ProcessInfo>(StringComparer.OrdinalIgnoreCase);
            settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "watchdog_settings.json");
            settings = new WatchdogSettings();
            monitoringCts = new CancellationTokenSource();
            LoadSettings();

            // �ڵ� ���� �ɼ� �߰�
            if (autoStart)
            {
                Start();
            }
        }

        public void Start()
        {
            lock (lockObject)
            {
                if (isRunning) return;

                isRunning = true;
                OnLogMessage("Watchdog service has been started.");

                // Initial boot delay handling
                if (isInitialStartup && settings.BootDelay > 0)
                {
                    OnLogMessage($"Initial boot delay: waiting {settings.BootDelay} seconds... (You can change this in Settings tab)");
                    Task.Delay(settings.BootDelay * 1000).ContinueWith(t =>
                    {
                        StartMonitoring();
                    });
                }
                else
                {
                    StartMonitoring();
                }
            }
        }

        private void StartMonitoring()
        {
            if (isMonitoringActive) return;

            isInitialStartup = false;
            monitoringCts = new CancellationTokenSource();
            monitoringFinishedEvent.Reset();

            // Start periodic monitoring with 1-second interval for individual check intervals
            monitorTimer = new Timer(MonitorProcesses, null, 0, 1000);
            OnLogMessage("Process monitoring has been started.");
        }

        public void Stop()
        {
            lock (lockObject)
            {
                if (!isRunning) return;

                isRunning = false;
                isMonitoringActive = false;

                // Cancel ongoing monitoring
                monitoringCts?.Cancel();

                // Wait for current monitoring to complete
                if (monitoringFinishedEvent.Wait(3000))
                {
                    OnLogMessage("Monitoring tasks completed.");
                }
                else
                {
                    OnLogMessage("Monitoring tasks did not complete in time.");
                }

                monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                OnLogMessage("Watchdog service has been stopped.");
            }
        }

        public void AddProcess(ProcessInfo processInfo)
        {
            // 체크 카운터 초기화
            processInfo.CheckCounter = 0;
            
            monitoredProcesses[processInfo.Name] = processInfo;
            var effectiveCheckInterval = GetEffectiveCheckInterval(processInfo);
            OnLogMessage($"Process added: {processInfo.Name} (Check interval: {effectiveCheckInterval}s)");

            // Start monitoring immediately if service is running
            if (isRunning && !isMonitoringActive)
            {
                CheckAndStartProcess(processInfo);
            }
        }

        public void RemoveProcess(string processName)
        {
            if (monitoredProcesses.TryRemove(processName, out _))
            {
                OnLogMessage($"Process removed: {processName}");
            }
        }

        public IReadOnlyList<ProcessInfo> GetProcesses()
        {
            return monitoredProcesses.Values.ToList();
        }

        public WatchdogSettings GetSettings()
        {
            return settings.Clone();
        }

        public void UpdateSettings(WatchdogSettings newSettings)
        {
            lock (lockObject)
            {
                settings = newSettings.Clone();
                OnLogMessage("Settings have been updated.");
                
                // 개별 체크 간격이 도입되었으므로 전역 체크 간격 변경시에도 타이머는 1초로 유지
                // 각 프로세스의 카운터만 리셋하여 새로운 간격 적용
                if (isRunning)
                {
                    foreach (var processInfo in monitoredProcesses.Values)
                    {
                        processInfo.CheckCounter = 0; // 모든 프로세스의 카운터 리셋
                    }
                }
            }
        }

        // Helper methods to get effective settings
        private int GetEffectiveCheckInterval(ProcessInfo processInfo)
        {
            return processInfo.IndividualCheckInterval ?? settings.CheckInterval;
        }
        
        private int GetEffectiveRestartDelay(ProcessInfo processInfo)
        {
            return processInfo.IndividualRestartDelay ?? settings.RestartDelay;
        }
        
        private int GetEffectiveBootDelay(ProcessInfo processInfo)
        {
            return processInfo.IndividualBootDelay ?? settings.BootDelay;
        }

        private bool GetEffectiveCheckForHanging(ProcessInfo processInfo)
        {
            return processInfo.IndividualCheckForHanging ?? settings.CheckForHangingProcess;
        }

        private int GetEffectiveMaxRestarts(ProcessInfo processInfo)
        {
            return processInfo.IndividualMaxRestarts ?? settings.MaxRestarts;
        }

        private async void MonitorProcesses(object state)
        {
            if (!isRunning || isMonitoringActive) return;

            isMonitoringActive = true;
            var token = monitoringCts.Token;

            try
            {
                // Process each item with individual check intervals
                var tasks = new List<Task>();
                
                foreach (var processInfo in monitoredProcesses.Values)
                {
                    // 개별 체크 간격 처리
                    processInfo.CheckCounter++;
                    var effectiveCheckInterval = GetEffectiveCheckInterval(processInfo);
                    
                    if (processInfo.CheckCounter >= effectiveCheckInterval)
                    {
                        // 체크 간격에 도달했으면 실제 체크 수행
                        processInfo.CheckCounter = 0; // 카운터 리셋
                        tasks.Add(CheckProcessStatusAsync(processInfo, token));
                    }
                }

                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks);
                }
            }
            catch (OperationCanceledException)
            {
                // Monitoring was canceled
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error during monitoring: {ex.Message}");
            }
            finally
            {
                isMonitoringActive = false;
                monitoringFinishedEvent.Set();
            }
        }

        private async Task CheckProcessStatusAsync(ProcessInfo processInfo, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            try
            {
                var wasRunning = processInfo.IsRunning;
                var currentProcess = GetProcessByName(processInfo.Name);

                if (currentProcess != null)
                {
                    processInfo.ProcessId = currentProcess.Id;
                    processInfo.IsRunning = true;

                    // Check for hanging processes
                    if (GetEffectiveCheckForHanging(processInfo) && IsProcessHanging(currentProcess))
                    {
                        OnLogMessage($"Process is not responding: {processInfo.Name} (PID: {currentProcess.Id})");

                        try
                        {
                            currentProcess.Kill();
                            await Task.Delay(1000); // Give time for process to terminate
                            OnLogMessage($"Unresponsive process terminated: {processInfo.Name}");
                        }
                        catch (Exception ex)
                        {
                            OnLogMessage($"Failed to terminate process: {processInfo.Name} - {ex.Message}");
                        }

                        processInfo.IsRunning = false;
                        processInfo.ProcessId = null;
                    }
                }
                else
                {
                    processInfo.IsRunning = false;
                    processInfo.ProcessId = null;
                }

                // Fire status change event
                if (wasRunning != processInfo.IsRunning)
                {
                    OnProcessStatusChanged(processInfo.Name, processInfo.IsRunning ? "Running" : "Stopped");
                }

                // Attempt restart if process is stopped
                if (!processInfo.IsRunning && processInfo.EnableAutoRestart)
                {
                    var effectiveMaxRestarts = GetEffectiveMaxRestarts(processInfo);
                    if (processInfo.RestartCount < effectiveMaxRestarts)
                    {
                        await RestartProcessAsync(processInfo, token);
                    }
                    else
                    {
                        OnLogMessage($"Maximum restart count exceeded: {processInfo.Name} ({processInfo.RestartCount}/{effectiveMaxRestarts})");
                        processInfo.EnableAutoRestart = false; // Disable auto restart
                    }
                }
                else if (!processInfo.IsRunning && !processInfo.EnableAutoRestart)
                {
                    OnLogMessage($"Process {processInfo.Name} is stopped but auto restart is disabled.");
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error checking process status: {processInfo.Name} - {ex.Message}");
            }
        }

        private void CheckAndStartProcess(ProcessInfo processInfo)
        {
            var currentProcess = GetProcessByName(processInfo.Name);
            if (currentProcess == null && processInfo.EnableAutoRestart)
            {
                OnLogMessage($"Process is not running: {processInfo.Name}");
                
                // 개별 부팅 지연시간 적용
                var effectiveBootDelay = GetEffectiveBootDelay(processInfo);
                if (effectiveBootDelay > 0 && processInfo.RestartCount == 0)
                {
                    OnLogMessage($"Initial boot delay for {processInfo.Name}: waiting {effectiveBootDelay} seconds...");
                    Task.Delay(effectiveBootDelay * 1000).ContinueWith(t =>
                    {
                        _ = RestartProcessAsync(processInfo, CancellationToken.None);
                    });
                }
                else
                {
                    _ = RestartProcessAsync(processInfo, CancellationToken.None);
                }
            }
            else if (currentProcess != null)
            {
                processInfo.ProcessId = currentProcess.Id;
                processInfo.IsRunning = true;
                OnLogMessage($"Process is already running: {processInfo.Name} (PID: {currentProcess.Id})");
            }
        }

        private async Task RestartProcessAsync(ProcessInfo processInfo, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;
            if (!processInfo.EnableAutoRestart) 
            {
                OnLogMessage($"Auto restart is disabled for process: {processInfo.Name}");
                return;
            }

            try
            {
                OnLogMessage($"Attempting to restart process: {processInfo.Name}");

                // Restart delay
                var effectiveRestartDelay = GetEffectiveRestartDelay(processInfo);
                if (effectiveRestartDelay > 0)
                {
                    OnLogMessage($"Restart delay: waiting {effectiveRestartDelay} seconds...");
                    await Task.Delay(effectiveRestartDelay * 1000, token);
                }

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
                    processInfo.ProcessId = newProcess.Id;
                    processInfo.IsRunning = true;
                    processInfo.LastRestart = DateTime.Now;
                    processInfo.RestartCount++;

                    var effectiveMaxRestarts = GetEffectiveMaxRestarts(processInfo);
                    OnLogMessage($"Process restart successful: {processInfo.Name} (PID: {newProcess.Id}, restart count: {processInfo.RestartCount}/{effectiveMaxRestarts})");
                    OnProcessRestarted(processInfo.Name, newProcess.Id);
                }
                else
                {
                    OnLogMessage($"Process restart failed: {processInfo.Name}");
                }
            }
            catch (OperationCanceledException)
            {
                OnLogMessage($"Restart canceled: {processInfo.Name}");
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error during process restart: {processInfo.Name} - {ex.Message}");
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

        private bool IsProcessHanging(Process process)
        {
            try
            {
                // Check response status for GUI processes
                if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                {
                    return !process.Responding;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public WatchdogSettings LoadSettings()
        {
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    var json = File.ReadAllText(settingsFilePath);
                    var loadedData = JsonConvert.DeserializeObject<WatchdogData>(json);

                    settings = loadedData.Settings ?? new WatchdogSettings();

                    monitoredProcesses.Clear();
                    if (loadedData.Processes != null)
                    {
                        foreach (var process in loadedData.Processes)
                        {
                            // 기존 프로세스들의 체크 카운터 초기화
                            process.CheckCounter = 0;
                            monitoredProcesses[process.Name] = process;
                        }
                    }
                    
                    // Boot Delay 감지 및 안내
                    if (settings.BootDelay > 10)
                    {
                        OnLogMessage($"Note: Boot Delay is set to {settings.BootDelay} seconds. You can reduce this in Settings tab for faster startup.");
                    }

                    OnLogMessage("Settings file has been loaded.");
                }
                else
                {
                    settings = new WatchdogSettings();
                    OnLogMessage("Using default settings.");
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"Settings load failed: {ex.Message}");
                settings = new WatchdogSettings();
            }

            return settings;
        }

        public void SaveSettings()
        {
            try
            {
                var data = new WatchdogData
                {
                    Settings = settings,
                    Processes = monitoredProcesses.Values.ToList()
                };

                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(settingsFilePath, json);
                OnLogMessage("Settings have been saved.");
            }
            catch (Exception ex)
            {
                OnLogMessage($"Settings save failed: {ex.Message}");
                throw;
            }
        }

        private void OnProcessRestarted(string processName, int newProcessId)
        {
            ProcessRestarted?.Invoke(this, new ProcessRestartedEventArgs(processName, newProcessId));
        }

        private void OnProcessStatusChanged(string processName, string status)
        {
            ProcessStatusChanged?.Invoke(this, new ProcessStatusChangedEventArgs(processName, status));
        }

        private void OnLogMessage(string message)
        {
            LogMessage?.Invoke(this, new LogMessageEventArgs(message));
        }

        public void Dispose()
        {
            monitoringCts?.Cancel();
            Stop();
            monitorTimer?.Dispose();
            monitoringCts?.Dispose();
            monitoringFinishedEvent?.Dispose();
        }
    }
}