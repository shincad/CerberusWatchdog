English Manual

General Settings
- Check Interval (seconds)
Default: 5 seconds

Function: Sets how often the status of registered processes is checked.

Example: If set to 5 seconds, it will check every 5 seconds whether “notepad.exe” is running.

- Restart Delay (seconds)
Default: 3 seconds

Function: Sets how long to wait before restarting a process after detecting it has stopped.

Reason: Waiting briefly can prevent errors that may occur from an immediate restart.

- Boot Delay (seconds)
Default: 30 seconds

Function: Sets how long to wait before starting monitoring when the program first launches.

Reason: The system may be unstable right after booting, so it waits.

If set to 0: Monitoring starts immediately.

- Max Restarts
Default: 10 times

Function: Sets the maximum number of times to attempt restarting a single process.

Reason: Prevents infinite restart loops if a program has an issue.

- Check for unresponsive processes
Default: Enabled (checked)

Function: Also detects when a process is running but not responding.

Example: If a program freezes or enters a "Not Responding" state, it will still be restarted.

- Auto start monitoring on program launch
Default: Enabled (checked)

Function: Automatically starts monitoring when the Enhanced Watchdog program launches.

If checked: Monitoring starts as soon as the program is opened.

If unchecked: Monitoring must be started manually by clicking the "Start Monitoring" button.

- System Restart Settings
- Enable System Restart
Function: Automatically restarts the entire system at a scheduled time.

Use case: Useful for servers or computers that run 24/7 and require regular reboots.

- Restart Type
Daily: Restart at the same time every day.

Weekly: Restart at a specific time on a selected day each week.

- Time (24-hour format)
Function: Sets the time to perform the system restart (from 0:00 to 23:59).

Example: 03:00 = 3 AM

- Day (Visible only in Weekly mode)
Function: Selects the day of the week for the scheduled restart.

Example: Selecting Sunday will restart the system every Sunday at 3 AM.

- Next restart:
Function: Displays the scheduled time of the next system restart in real-time.

Example: "Next restart: 2025-01-09 03:00 (1d 5h remaining)"

한국어 설명

## - General Settings 옵션 설명

### - Check Interval (seconds)

- **기본값**: 5초
- **기능**: 등록된 프로세스들의 상태를 몇 초마다 체크할지 설정
- **예시**: 5초로 설정하면 5초마다 "notepad.exe가 실행 중인가?" 확인

###  - Restart Delay (seconds)

- **기본값**: 3초
- **기능**: 프로세스가 종료된 것을 감지한 후 몇 초 기다렸다가 재시작할지 설정
- **이유**: 즉시 재시작하면 오류가 날 수 있어서 잠깐 기다림

### - Boot Delay (seconds)

- **기본값**: 30초
- **기능**: **프로그램이 처음 시작될 때** 몇 초 기다렸다가 모니터링을 시작할지 설정
- **이유**: 윈도우 부팅 직후에는 시스템이 불안정할 수 있어서 기다림
- **0으로 설정**: 즉시 모니터링 시작

###  - Max Restarts

- **기본값**: 10번
- **기능**: 하나의 프로세스를 최대 몇 번까지 재시작 시도할지 설정
- **이유**: 무한 재시작 방지 (프로그램에 문제가 있을 때)

### - Check for unresponsive processes

- **기본값**: 체크됨
- **기능**: 프로세스가 실행 중이지만 응답하지 않는 경우도 감지
- **예시**: 프로그램이 멈춰있거나 "응답 없음" 상태일 때도 재시작

### - Auto start monitoring on program launch

- **기본값**: 체크됨
- **기능**: **Enhanced Watchdog 프로그램을 실행하면 자동으로 모니터링 시작**
- **체크된 경우**: 프로그램 열자마자 등록된 프로세스들 모니터링 시작
- **체크 해제**: 수동으로 "Start Monitoring" 버튼을 눌러야 모니터링 시작

##  - System Restart Settings 옵션 설명

### Enable System Restart

- **기능**: 정해진 시간에 컴퓨터 전체를 재시작
- **용도**: 서버나 24시간 돌아가는 컴퓨터의 정기적인 재부팅

### - Restart Type

- **Daily**: 매일 같은 시간에 재시작
- **Weekly**: 매주 특정 요일의 특정 시간에 재시작

### - Time (24-hour format)

- **기능**: 재시작할 시간 설정 (0:00 ~ 23:59)
- **예시**: 03:00 = 새벽 3시

###  - Day (Weekly 모드에서만 보임)

- **기능**: 주간 재시작시 어느 요일에 재시작할지 선택
- **예시**: Sunday 선택 → 매주 일요일 새벽 3시에 재시작

### - Next restart: 

- **기능**: 다음 재시작 예정 시간을 실시간으로 표시
- **예시**: "Next restart: 2025-01-09 03:00 (1d 5h remaining)"
