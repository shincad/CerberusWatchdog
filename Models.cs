using System;
using System.Collections.Generic;

namespace EnhancedWatchdog
{
    // 프로세스 정보 클래스
    public class ProcessInfo
    {
        public string Name { get; set; }
        public string ExecutablePath { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public bool EnableAutoRestart { get; set; }
        public bool IsRunning { get; set; }
        public int? ProcessId { get; set; }
        public DateTime? LastRestart { get; set; }
        public int RestartCount { get; set; }
        
        // 개별 프로세스 설정 (null이면 전역 설정 사용)
        public int? IndividualCheckInterval { get; set; }   // 개별 체크 간격 (초)
        public int? IndividualRestartDelay { get; set; }    // 개별 재시작 지연시간 (초)
        public int? IndividualBootDelay { get; set; }       // 개별 부팅 지연시간 (초)
        public bool? IndividualCheckForHanging { get; set; } // 개별 응답없음 체크 여부
        public int? IndividualMaxRestarts { get; set; }     // 개별 최대 재시작 횟수
        
        // 개별 체크 간격을 위한 내부 카운터
        public int CheckCounter { get; set; } = 0;

        public ProcessInfo()
        {
            EnableAutoRestart = true;
            IsRunning = false;
            RestartCount = 0;
        }

        public ProcessInfo(string name, string executablePath)
        {
            Name = name;
            ExecutablePath = executablePath;
            EnableAutoRestart = true;
            IsRunning = false;
            RestartCount = 0;
        }
    }

    // 시스템 재시작 타입 열거형
    public enum SystemRestartType
    {
        Daily = 0,    // 매일
        Weekly = 1    // 매주
    }

    // Watchdog 설정 클래스
    public class WatchdogSettings
    {
        public int CheckInterval { get; set; } = 5; // 초
        public int RestartDelay { get; set; } = 3; // 초
        public int BootDelay { get; set; } = 0; // 초 - 기본값을 0으로 변경
        public int MaxRestarts { get; set; } = 10;
        public bool CheckForHangingProcess { get; set; } = true;
        public bool AutoStart { get; set; } = true;  // 기본값을 true로 설정
        
        // 시스템 재시작 설정
        public bool EnableSystemRestart { get; set; } = false;  // 시스템 재시작 활성화
        public SystemRestartType SystemRestartType { get; set; } = SystemRestartType.Daily; // 재시작 타입
        public int SystemRestartHour { get; set; } = 23;   // 재시작 시간 (시)
        public int SystemRestartMinute { get; set; } = 30; // 재시작 시간 (분)
        public DayOfWeek SystemRestartDayOfWeek { get; set; } = DayOfWeek.Sunday; // 주간 재시작 요일

        public WatchdogSettings Clone()
        {
            return new WatchdogSettings
            {
                CheckInterval = this.CheckInterval,
                RestartDelay = this.RestartDelay,
                BootDelay = this.BootDelay,
                MaxRestarts = this.MaxRestarts,
                CheckForHangingProcess = this.CheckForHangingProcess,
                AutoStart = this.AutoStart,
                EnableSystemRestart = this.EnableSystemRestart,
                SystemRestartType = this.SystemRestartType,
                SystemRestartHour = this.SystemRestartHour,
                SystemRestartMinute = this.SystemRestartMinute,
                SystemRestartDayOfWeek = this.SystemRestartDayOfWeek
            };
        }
    }

    // 전체 데이터 저장용 클래스
    public class WatchdogData
    {
        public WatchdogSettings Settings { get; set; }
        public List<ProcessInfo> Processes { get; set; }

        public WatchdogData()
        {
            Settings = new WatchdogSettings();
            Processes = new List<ProcessInfo>();
        }
    }

    // 이벤트 인수 클래스들
    public class ProcessRestartedEventArgs : EventArgs
    {
        public string ProcessName { get; }
        public int NewProcessId { get; }

        public ProcessRestartedEventArgs(string processName, int newProcessId)
        {
            ProcessName = processName;
            NewProcessId = newProcessId;
        }
    }

    public class ProcessStatusChangedEventArgs : EventArgs
    {
        public string ProcessName { get; }
        public string Status { get; }

        public ProcessStatusChangedEventArgs(string processName, string status)
        {
            ProcessName = processName;
            Status = status;
        }
    }

    public class LogMessageEventArgs : EventArgs
    {
        public string Message { get; }
        public DateTime Timestamp { get; }

        public LogMessageEventArgs(string message)
        {
            Message = message;
            Timestamp = DateTime.Now;
        }
    }
}