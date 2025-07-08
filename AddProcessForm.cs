using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace EnhancedWatchdog
{
    public partial class AddProcessForm : Form
    {
        private TextBox nameTextBox;
        private TextBox pathTextBox;
        private TextBox argumentsTextBox;
        private TextBox workingDirectoryTextBox;
        private CheckBox autoRestartCheckBox;
        private Button browseButton;
        private Button browseWorkingDirButton;
        private Button okButton;
        private Button cancelButton;

        // 스케줄 재시작 관련 컨트롤들
        private CheckBox enableScheduledRestartCheckBox;
        private ComboBox scheduledRestartTypeComboBox;
        private NumericUpDown scheduledRestartHourNumeric;
        private NumericUpDown scheduledRestartMinuteNumeric;
        private ComboBox scheduledRestartDayComboBox;
        private Label nextRestartLabel;

        private ProcessInfo processInfo;

        public AddProcessForm(string executablePath = null)
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(executablePath))
            {
                pathTextBox.Text = executablePath;
                nameTextBox.Text = Path.GetFileNameWithoutExtension(executablePath);
                workingDirectoryTextBox.Text = Path.GetDirectoryName(executablePath);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 폼 설정
            this.Text = "프로세스 추가";
            this.Size = new Size(600, 550); // 크기 증가
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 프로세스명 레이블 및 텍스트박스
            var nameLabel = new Label
            {
                Text = "프로세스명:",
                Location = new Point(15, 15),
                Size = new Size(80, 20)
            };

            nameTextBox = new TextBox
            {
                Location = new Point(100, 13),
                Size = new Size(460, 20)
            };

            // 실행 파일 경로 레이블 및 텍스트박스
            var pathLabel = new Label
            {
                Text = "실행 파일:",
                Location = new Point(15, 45),
                Size = new Size(80, 20)
            };

            pathTextBox = new TextBox
            {
                Location = new Point(100, 43),
                Size = new Size(380, 20)
            };

            browseButton = new Button
            {
                Text = "찾아보기",
                Location = new Point(485, 42),
                Size = new Size(75, 23)
            };
            browseButton.Click += BrowseButton_Click;

            // 인수 레이블 및 텍스트박스
            var argumentsLabel = new Label
            {
                Text = "실행 인수:",
                Location = new Point(15, 75),
                Size = new Size(80, 20)
            };

            argumentsTextBox = new TextBox
            {
                Location = new Point(100, 73),
                Size = new Size(460, 20)
            };

            // 작업 디렉터리 레이블 및 텍스트박스
            var workingDirLabel = new Label
            {
                Text = "작업 디렉터리:",
                Location = new Point(15, 105),
                Size = new Size(80, 20)
            };

            workingDirectoryTextBox = new TextBox
            {
                Location = new Point(100, 103),
                Size = new Size(380, 20)
            };

            browseWorkingDirButton = new Button
            {
                Text = "찾아보기",
                Location = new Point(485, 102),
                Size = new Size(75, 23)
            };
            browseWorkingDirButton.Click += BrowseWorkingDirButton_Click;

            // 자동 재시작 체크박스
            autoRestartCheckBox = new CheckBox
            {
                Text = "자동 재시작 활성화",
                Location = new Point(100, 135),
                Size = new Size(200, 20),
                Checked = true
            };

            // 스케줄 재시작 그룹박스
            var scheduledRestartGroupBox = new GroupBox
            {
                Text = "스케줄 재시작 설정",
                Location = new Point(15, 170),
                Size = new Size(550, 200)
            };

            // 스케줄 재시작 활성화 체크박스
            enableScheduledRestartCheckBox = new CheckBox
            {
                Text = "스케줄 재시작 활성화",
                Location = new Point(15, 25),
                Size = new Size(200, 20),
                Checked = false
            };
            enableScheduledRestartCheckBox.CheckedChanged += EnableScheduledRestartCheckBox_CheckedChanged;

            // 재시작 타입 레이블 및 콤보박스
            var restartTypeLabel = new Label
            {
                Text = "재시작 타입:",
                Location = new Point(15, 55),
                Size = new Size(80, 20)
            };

            scheduledRestartTypeComboBox = new ComboBox
            {
                Location = new Point(100, 53),
                Size = new Size(100, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            scheduledRestartTypeComboBox.Items.AddRange(new[] { "Daily", "Weekly" });
            scheduledRestartTypeComboBox.SelectedIndex = 0;
            scheduledRestartTypeComboBox.SelectedIndexChanged += ScheduledRestartTypeComboBox_SelectedIndexChanged;

            // 시간 설정 레이블 및 컨트롤들
            var timeLabel = new Label
            {
                Text = "시간:",
                Location = new Point(15, 85),
                Size = new Size(40, 20)
            };

            scheduledRestartHourNumeric = new NumericUpDown
            {
                Location = new Point(60, 83),
                Size = new Size(50, 20),
                Minimum = 0,
                Maximum = 23,
                Value = 23
            };
            scheduledRestartHourNumeric.ValueChanged += ScheduledTimeChanged;

            var hourLabel = new Label
            {
                Text = ":",
                Location = new Point(115, 85),
                Size = new Size(10, 20)
            };

            scheduledRestartMinuteNumeric = new NumericUpDown
            {
                Location = new Point(130, 83),
                Size = new Size(50, 20),
                Minimum = 0,
                Maximum = 59,
                Value = 30
            };
            scheduledRestartMinuteNumeric.ValueChanged += ScheduledTimeChanged;

            var timeFormatLabel = new Label
            {
                Text = "(24시간 형식)",
                Location = new Point(190, 85),
                Size = new Size(80, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic)
            };

            // 요일 선택 (주간 재시작용)
            var dayLabel = new Label
            {
                Text = "요일:",
                Location = new Point(15, 115),
                Size = new Size(40, 20)
            };

            scheduledRestartDayComboBox = new ComboBox
            {
                Location = new Point(60, 113),
                Size = new Size(100, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            scheduledRestartDayComboBox.Items.AddRange(new[] {
                "일요일", "월요일", "화요일", "수요일",
                "목요일", "금요일", "토요일"
            });
            scheduledRestartDayComboBox.SelectedIndex = 0;
            scheduledRestartDayComboBox.Visible = false;
            scheduledRestartDayComboBox.SelectedIndexChanged += ScheduledTimeChanged;

            // 다음 재시작 정보 레이블
            nextRestartLabel = new Label
            {
                Location = new Point(15, 145),
                Size = new Size(520, 40),
                Text = "다음 재시작: 비활성화",
                ForeColor = Color.Blue,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // 스케줄 그룹박스에 컨트롤 추가
            scheduledRestartGroupBox.Controls.AddRange(new Control[] {
                enableScheduledRestartCheckBox,
                restartTypeLabel, scheduledRestartTypeComboBox,
                timeLabel, scheduledRestartHourNumeric, hourLabel, scheduledRestartMinuteNumeric, timeFormatLabel,
                dayLabel, scheduledRestartDayComboBox,
                nextRestartLabel
            });

            // 설명 레이블
            var descriptionLabel = new Label
            {
                Text = "프로세스가 종료되거나 응답하지 않을 때 자동으로 재시작됩니다.\n" +
                       "스케줄 재시작은 정해진 시간에 프로세스를 강제로 재시작합니다.",
                Location = new Point(15, 380),
                Size = new Size(550, 40),
                ForeColor = Color.Gray
            };

            // 버튼들
            okButton = new Button
            {
                Text = "확인",
                Location = new Point(380, 430),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "취소",
                Location = new Point(465, 430),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            // 컨트롤 추가
            this.Controls.AddRange(new Control[]
            {
                nameLabel, nameTextBox,
                pathLabel, pathTextBox, browseButton,
                argumentsLabel, argumentsTextBox,
                workingDirLabel, workingDirectoryTextBox, browseWorkingDirButton,
                autoRestartCheckBox,
                scheduledRestartGroupBox,
                descriptionLabel,
                okButton, cancelButton
            });

            // 초기 UI 상태 설정
            UpdateScheduledRestartUI();

            // 기본 버튼 설정
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;

            this.ResumeLayout();
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "실행 파일 선택",
                Filter = "실행 파일 (*.exe)|*.exe|모든 파일 (*.*)|*.*",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                pathTextBox.Text = openFileDialog.FileName;

                // 프로세스명이 비어있으면 자동으로 채움
                if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    nameTextBox.Text = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                }

                // 작업 디렉터리가 비어있으면 자동으로 채움
                if (string.IsNullOrWhiteSpace(workingDirectoryTextBox.Text))
                {
                    workingDirectoryTextBox.Text = Path.GetDirectoryName(openFileDialog.FileName);
                }
            }
        }

        private void BrowseWorkingDirButton_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "작업 디렉터리 선택",
                ShowNewFolderButton = false
            };

            if (!string.IsNullOrWhiteSpace(workingDirectoryTextBox.Text) && Directory.Exists(workingDirectoryTextBox.Text))
            {
                folderBrowserDialog.SelectedPath = workingDirectoryTextBox.Text;
            }

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                workingDirectoryTextBox.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            // 입력 검증
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("프로세스명을 입력해주세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(pathTextBox.Text))
            {
                MessageBox.Show("실행 파일 경로를 입력해주세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                pathTextBox.Focus();
                return;
            }

            if (!File.Exists(pathTextBox.Text))
            {
                MessageBox.Show("지정한 실행 파일이 존재하지 않습니다.", "파일 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                pathTextBox.Focus();
                return;
            }

            if (!string.IsNullOrWhiteSpace(workingDirectoryTextBox.Text) && !Directory.Exists(workingDirectoryTextBox.Text))
            {
                MessageBox.Show("지정한 작업 디렉터리가 존재하지 않습니다.", "디렉터리 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                workingDirectoryTextBox.Focus();
                return;
            }

            // ProcessInfo 객체 생성
            processInfo = new ProcessInfo
            {
                Name = nameTextBox.Text.Trim(),
                ExecutablePath = pathTextBox.Text.Trim(),
                Arguments = argumentsTextBox.Text.Trim(),
                WorkingDirectory = workingDirectoryTextBox.Text.Trim(),
                EnableAutoRestart = autoRestartCheckBox.Checked,
                
                // 스케줄 재시작 설정
                EnableScheduledRestart = enableScheduledRestartCheckBox.Checked,
                ScheduledRestartType = scheduledRestartTypeComboBox.SelectedItem?.ToString() == "Weekly" ? 
                    ScheduledRestartType.Weekly : ScheduledRestartType.Daily,
                ScheduledRestartHour = (int)scheduledRestartHourNumeric.Value,
                ScheduledRestartMinute = (int)scheduledRestartMinuteNumeric.Value,
                ScheduledRestartDayOfWeek = (DayOfWeek)scheduledRestartDayComboBox.SelectedIndex
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public ProcessInfo GetProcessInfo()
        {
            return processInfo;
        }

        // 스케줄 재시작 이벤트 핸들러들
        private void EnableScheduledRestartCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateScheduledRestartUI();
        }

        private void ScheduledRestartTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateScheduledRestartUI();
        }

        private void ScheduledTimeChanged(object sender, EventArgs e)
        {
            UpdateNextRestartInfo();
        }

        private void UpdateScheduledRestartUI()
        {
            bool isEnabled = enableScheduledRestartCheckBox.Checked;
            bool isWeekly = scheduledRestartTypeComboBox.SelectedItem?.ToString() == "Weekly";

            // 모든 관련 컨트롤들의 활성화 상태 설정
            scheduledRestartTypeComboBox.Enabled = isEnabled;
            scheduledRestartHourNumeric.Enabled = isEnabled;
            scheduledRestartMinuteNumeric.Enabled = isEnabled;
            scheduledRestartDayComboBox.Enabled = isEnabled && isWeekly;
            scheduledRestartDayComboBox.Visible = isWeekly;

            UpdateNextRestartInfo();
        }

        private void UpdateNextRestartInfo()
        {
            if (!enableScheduledRestartCheckBox.Checked)
            {
                nextRestartLabel.Text = "다음 재시작: 비활성화";
                nextRestartLabel.ForeColor = Color.Gray;
                return;
            }

            try
            {
                var isWeekly = scheduledRestartTypeComboBox.SelectedItem?.ToString() == "Weekly";
                var hour = (int)scheduledRestartHourNumeric.Value;
                var minute = (int)scheduledRestartMinuteNumeric.Value;
                var dayOfWeek = (DayOfWeek)scheduledRestartDayComboBox.SelectedIndex;

                var nextRestart = CalculateNextRestartTime(isWeekly, hour, minute, dayOfWeek);
                var timeSpan = nextRestart - DateTime.Now;
                
                string timeInfo = $"다음 재시작: {nextRestart:yyyy-MM-dd HH:mm}";
                if (timeSpan.TotalDays >= 1)
                {
                    timeInfo += $" ({(int)timeSpan.TotalDays}일 {timeSpan.Hours}시간 남음)";
                }
                else
                {
                    timeInfo += $" ({timeSpan.Hours}시간 {timeSpan.Minutes}분 남음)";
                }
                
                nextRestartLabel.Text = timeInfo;
                nextRestartLabel.ForeColor = Color.Blue;
            }
            catch (Exception ex)
            {
                nextRestartLabel.Text = $"다음 재시작 계산 오류: {ex.Message}";
                nextRestartLabel.ForeColor = Color.Red;
            }
        }

        private DateTime CalculateNextRestartTime(bool isWeekly, int hour, int minute, DayOfWeek dayOfWeek)
        {
            var now = DateTime.Now;
            var today = now.Date;
            var restartTime = today.AddHours(hour).AddMinutes(minute);

            if (!isWeekly)
            {
                // 일일 재시작
                if (restartTime <= now)
                {
                    restartTime = restartTime.AddDays(1);
                }
                return restartTime;
            }
            else // Weekly
            {
                // 주간 재시작
                var targetDayOfWeek = dayOfWeek;
                var daysUntilTarget = ((int)targetDayOfWeek - (int)now.DayOfWeek + 7) % 7;
                
                var targetDate = today.AddDays(daysUntilTarget);
                var targetDateTime = targetDate.AddHours(hour).AddMinutes(minute);
                
                // 오늘이 목표 요일이고 시간이 지났다면 다음 주로
                if (daysUntilTarget == 0 && targetDateTime <= now)
                {
                    targetDateTime = targetDateTime.AddDays(7);
                }
                
                return targetDateTime;
            }
        }
    }
}