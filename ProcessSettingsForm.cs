using System;
using System.Drawing;
using System.Windows.Forms;

namespace EnhancedWatchdog
{
    public partial class ProcessSettingsForm : Form
    {
        private Label processNameLabel;
        private Label processPathLabel;
        
        // 자동 재시작 활성화 설정
        private CheckBox enableAutoRestartCheckBox;
        
        // 개별 체크 간격 설정
        private CheckBox useGlobalCheckIntervalCheckBox;
        private Label checkIntervalLabel;
        private NumericUpDown checkIntervalNumeric;
        private Label globalCheckIntervalLabel;
        
        // 개별 부팅 지연시간 설정
        private CheckBox useGlobalBootDelayCheckBox;
        private Label bootDelayLabel;
        private NumericUpDown bootDelayNumeric;
        private Label globalBootDelayLabel;
        
        // 재시작 지연시간 설정
        private CheckBox useGlobalRestartDelayCheckBox;
        private Label restartDelayLabel;
        private NumericUpDown restartDelayNumeric;
        private Label globalRestartDelayLabel;
        
        // 응답없음 체크 설정
        private CheckBox useGlobalHangingCheckCheckBox;
        private CheckBox individualHangingCheckBox;
        private Label globalHangingCheckLabel;
        
        // 최대 재시작 횟수 설정
        private CheckBox useGlobalMaxRestartsCheckBox;
        private Label maxRestartsLabel;
        private NumericUpDown maxRestartsNumeric;
        private Label globalMaxRestartsLabel;
        
        private Button saveButton;
        private Button cancelButton;

        private ProcessInfo processInfo;
        private WatchdogSettings globalSettings;

        public ProcessSettingsForm(ProcessInfo processInfo, WatchdogSettings globalSettings)
        {
            this.processInfo = processInfo;
            this.globalSettings = globalSettings;
            
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 폼 설정
            this.Text = "프로세스 개별 설정";
            this.Size = new Size(500, 750);  // 폼 크기를 더 크게 조정
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 프로세스 정보 표시
            var processInfoGroup = new GroupBox
            {
                Text = "프로세스 정보",
                Location = new Point(15, 15),
                Size = new Size(450, 80)
            };

            processNameLabel = new Label
            {
                Location = new Point(10, 25),
                Size = new Size(430, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            processPathLabel = new Label
            {
                Location = new Point(10, 45),
                Size = new Size(430, 20),
                ForeColor = Color.Gray
            };

            processInfoGroup.Controls.AddRange(new Control[] { processNameLabel, processPathLabel });

            // 자동 재시작 활성화 설정 그룹
            var autoRestartGroup = new GroupBox
            {
                Text = "자동 재시작 설정",
                Location = new Point(15, 105),
                Size = new Size(450, 60)
            };

            enableAutoRestartCheckBox = new CheckBox
            {
                Text = "자동 재시작 활성화",
                Location = new Point(10, 25),
                Size = new Size(200, 20),
                Checked = true
            };

            var autoRestartDescLabel = new Label
            {
                Text = "체크 해제 시 이 프로세스는 감시되지만 재시작되지 않습니다.",
                Location = new Point(220, 25),
                Size = new Size(220, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic)
            };

            autoRestartGroup.Controls.AddRange(new Control[] { enableAutoRestartCheckBox, autoRestartDescLabel });

            // 개별 체크 간격 설정 그룹
            var checkIntervalGroup = new GroupBox
            {
                Text = "체크 간격 설정",
                Location = new Point(15, 175),
                Size = new Size(450, 90)
            };

            useGlobalCheckIntervalCheckBox = new CheckBox
            {
                Text = "전역 설정 사용",
                Location = new Point(10, 25),
                Size = new Size(120, 20),
                Checked = true
            };
            useGlobalCheckIntervalCheckBox.CheckedChanged += UseGlobalCheckIntervalCheckBox_CheckedChanged;

            globalCheckIntervalLabel = new Label
            {
                Location = new Point(140, 25),
                Size = new Size(200, 20),
                ForeColor = Color.Gray
            };

            checkIntervalLabel = new Label
            {
                Text = "개별 체크 간격 (초):",
                Location = new Point(10, 55),
                Size = new Size(120, 20)
            };

            checkIntervalNumeric = new NumericUpDown
            {
                Location = new Point(140, 53),
                Size = new Size(80, 20),
                Minimum = 1,
                Maximum = 3600,
                Value = 5,
                Enabled = false
            };

            checkIntervalGroup.Controls.AddRange(new Control[] {
                useGlobalCheckIntervalCheckBox, globalCheckIntervalLabel,
                checkIntervalLabel, checkIntervalNumeric
            });

            // 개별 부팅 지연시간 설정 그룹
            var bootDelayGroup = new GroupBox
            {
                Text = "부팅 지연시간 설정",
                Location = new Point(15, 275),
                Size = new Size(450, 90)
            };

            useGlobalBootDelayCheckBox = new CheckBox
            {
                Text = "전역 설정 사용",
                Location = new Point(10, 25),
                Size = new Size(120, 20),
                Checked = true
            };
            useGlobalBootDelayCheckBox.CheckedChanged += UseGlobalBootDelayCheckBox_CheckedChanged;

            globalBootDelayLabel = new Label
            {
                Location = new Point(140, 25),
                Size = new Size(200, 20),
                ForeColor = Color.Gray
            };

            bootDelayLabel = new Label
            {
                Text = "개별 부팅 지연 (초):",
                Location = new Point(10, 55),
                Size = new Size(120, 20)
            };

            bootDelayNumeric = new NumericUpDown
            {
                Location = new Point(140, 53),
                Size = new Size(80, 20),
                Minimum = 0,
                Maximum = 600,
                Value = 0,
                Enabled = false
            };

            bootDelayGroup.Controls.AddRange(new Control[] {
                useGlobalBootDelayCheckBox, globalBootDelayLabel,
                bootDelayLabel, bootDelayNumeric
            });

            // 재시작 지연시간 설정 그룹
            var restartDelayGroup = new GroupBox
            {
                Text = "재시작 지연시간 설정",
                Location = new Point(15, 375),
                Size = new Size(450, 90)
            };

            useGlobalRestartDelayCheckBox = new CheckBox
            {
                Text = "전역 설정 사용",
                Location = new Point(10, 25),
                Size = new Size(120, 20),
                Checked = true
            };
            useGlobalRestartDelayCheckBox.CheckedChanged += UseGlobalRestartDelayCheckBox_CheckedChanged;

            globalRestartDelayLabel = new Label
            {
                Location = new Point(140, 25),
                Size = new Size(200, 20),
                ForeColor = Color.Gray
            };

            restartDelayLabel = new Label
            {
                Text = "개별 지연시간 (초):",
                Location = new Point(10, 55),
                Size = new Size(120, 20)
            };

            restartDelayNumeric = new NumericUpDown
            {
                Location = new Point(140, 53),
                Size = new Size(80, 20),
                Minimum = 0,
                Maximum = 300,
                Value = 3,
                Enabled = false
            };

            restartDelayGroup.Controls.AddRange(new Control[] {
                useGlobalRestartDelayCheckBox, globalRestartDelayLabel,
                restartDelayLabel, restartDelayNumeric
            });

            // 응답없음 체크 설정 그룹
            var hangingCheckGroup = new GroupBox
            {
                Text = "응답없음 체크 설정",
                Location = new Point(15, 475),
                Size = new Size(450, 90)
            };

            useGlobalHangingCheckCheckBox = new CheckBox
            {
                Text = "전역 설정 사용",
                Location = new Point(10, 25),
                Size = new Size(120, 20),
                Checked = true
            };
            useGlobalHangingCheckCheckBox.CheckedChanged += UseGlobalHangingCheckCheckBox_CheckedChanged;

            globalHangingCheckLabel = new Label
            {
                Location = new Point(140, 25),
                Size = new Size(200, 20),
                ForeColor = Color.Gray
            };

            individualHangingCheckBox = new CheckBox
            {
                Text = "응답없음 프로세스 재시작",
                Location = new Point(10, 55),
                Size = new Size(200, 20),
                Enabled = false
            };

            hangingCheckGroup.Controls.AddRange(new Control[] {
                useGlobalHangingCheckCheckBox, globalHangingCheckLabel,
                individualHangingCheckBox
            });

            // 최대 재시작 횟수 설정 그룹
            var maxRestartsGroup = new GroupBox
            {
                Text = "최대 재시작 횟수 설정",
                Location = new Point(15, 575),
                Size = new Size(450, 90)
            };

            useGlobalMaxRestartsCheckBox = new CheckBox
            {
                Text = "전역 설정 사용",
                Location = new Point(10, 25),
                Size = new Size(120, 20),
                Checked = true
            };
            useGlobalMaxRestartsCheckBox.CheckedChanged += UseGlobalMaxRestartsCheckBox_CheckedChanged;

            globalMaxRestartsLabel = new Label
            {
                Location = new Point(140, 25),
                Size = new Size(200, 20),
                ForeColor = Color.Gray
            };

            maxRestartsLabel = new Label
            {
                Text = "개별 최대 횟수:",
                Location = new Point(10, 55),
                Size = new Size(120, 20)
            };

            maxRestartsNumeric = new NumericUpDown
            {
                Location = new Point(140, 53),
                Size = new Size(80, 20),
                Minimum = 1,
                Maximum = 100,
                Value = 10,
                Enabled = false
            };

            maxRestartsGroup.Controls.AddRange(new Control[] {
                useGlobalMaxRestartsCheckBox, globalMaxRestartsLabel,
                maxRestartsLabel, maxRestartsNumeric
            });

            // 버튼 패널 생성 (하단에 고정)
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.LightGray
            };

            // 버튼들 - "확인"을 "저장"으로 변경
            saveButton = new Button
            {
                Text = "💾 저장",  // 아이콘 추가
                Location = new Point(300, 10),
                Size = new Size(80, 35),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(100, 150, 255),  // 더 눈에 띄는 파란색
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            saveButton.Click += SaveButton_Click;

            cancelButton = new Button
            {
                Text = "❌ 취소",  // 아이콘 추가
                Location = new Point(390, 10),
                Size = new Size(80, 35),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                UseVisualStyleBackColor = false
            };

            // 버튼들을 패널에 추가
            buttonPanel.Controls.AddRange(new Control[] { saveButton, cancelButton });

            // 컨트롤 추가
            this.Controls.AddRange(new Control[]
            {
                processInfoGroup,
                autoRestartGroup,
                checkIntervalGroup,
                bootDelayGroup,
                restartDelayGroup,
                hangingCheckGroup,
                maxRestartsGroup,
                buttonPanel  // 버튼 패널 추가
            });

            // 기본 버튼 설정
            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;

            this.ResumeLayout();
        }

        private void LoadCurrentSettings()
        {
            // 프로세스 정보 표시
            processNameLabel.Text = $"프로세스명: {processInfo.Name}";
            processPathLabel.Text = $"실행 파일: {processInfo.ExecutablePath}";

            // 자동 재시작 설정 로드
            enableAutoRestartCheckBox.Checked = processInfo.EnableAutoRestart;

            // 전역 설정 표시
            globalCheckIntervalLabel.Text = $"(전역: {globalSettings.CheckInterval}초)";
            globalBootDelayLabel.Text = $"(전역: {globalSettings.BootDelay}초)";
            globalRestartDelayLabel.Text = $"(전역: {globalSettings.RestartDelay}초)";
            globalHangingCheckLabel.Text = $"(전역: {(globalSettings.CheckForHangingProcess ? "활성화" : "비활성화")})";
            globalMaxRestartsLabel.Text = $"(전역: {globalSettings.MaxRestarts}회)";

            // 현재 개별 설정 로드
            if (processInfo.IndividualCheckInterval.HasValue)
            {
                useGlobalCheckIntervalCheckBox.Checked = false;
                checkIntervalNumeric.Value = processInfo.IndividualCheckInterval.Value;
            }
            else
            {
                checkIntervalNumeric.Value = globalSettings.CheckInterval;
            }

            if (processInfo.IndividualBootDelay.HasValue)
            {
                useGlobalBootDelayCheckBox.Checked = false;
                bootDelayNumeric.Value = processInfo.IndividualBootDelay.Value;
            }
            else
            {
                bootDelayNumeric.Value = globalSettings.BootDelay;
            }

            if (processInfo.IndividualRestartDelay.HasValue)
            {
                useGlobalRestartDelayCheckBox.Checked = false;
                restartDelayNumeric.Value = processInfo.IndividualRestartDelay.Value;
            }
            else
            {
                restartDelayNumeric.Value = globalSettings.RestartDelay;
            }

            if (processInfo.IndividualCheckForHanging.HasValue)
            {
                useGlobalHangingCheckCheckBox.Checked = false;
                individualHangingCheckBox.Checked = processInfo.IndividualCheckForHanging.Value;
            }
            else
            {
                individualHangingCheckBox.Checked = globalSettings.CheckForHangingProcess;
            }

            if (processInfo.IndividualMaxRestarts.HasValue)
            {
                useGlobalMaxRestartsCheckBox.Checked = false;
                maxRestartsNumeric.Value = processInfo.IndividualMaxRestarts.Value;
            }
            else
            {
                maxRestartsNumeric.Value = globalSettings.MaxRestarts;
            }
        }

        private void UseGlobalCheckIntervalCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            checkIntervalNumeric.Enabled = !useGlobalCheckIntervalCheckBox.Checked;
            if (useGlobalCheckIntervalCheckBox.Checked)
            {
                checkIntervalNumeric.Value = globalSettings.CheckInterval;
            }
        }

        private void UseGlobalBootDelayCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            bootDelayNumeric.Enabled = !useGlobalBootDelayCheckBox.Checked;
            if (useGlobalBootDelayCheckBox.Checked)
            {
                bootDelayNumeric.Value = globalSettings.BootDelay;
            }
        }

        private void UseGlobalRestartDelayCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            restartDelayNumeric.Enabled = !useGlobalRestartDelayCheckBox.Checked;
            if (useGlobalRestartDelayCheckBox.Checked)
            {
                restartDelayNumeric.Value = globalSettings.RestartDelay;
            }
        }

        private void UseGlobalHangingCheckCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            individualHangingCheckBox.Enabled = !useGlobalHangingCheckCheckBox.Checked;
            if (useGlobalHangingCheckCheckBox.Checked)
            {
                individualHangingCheckBox.Checked = globalSettings.CheckForHangingProcess;
            }
        }

        private void UseGlobalMaxRestartsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            maxRestartsNumeric.Enabled = !useGlobalMaxRestartsCheckBox.Checked;
            if (useGlobalMaxRestartsCheckBox.Checked)
            {
                maxRestartsNumeric.Value = globalSettings.MaxRestarts;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 자동 재시작 설정 적용
                processInfo.EnableAutoRestart = enableAutoRestartCheckBox.Checked;

                // 개별 체크 간격 설정 적용
                if (useGlobalCheckIntervalCheckBox.Checked)
                {
                    processInfo.IndividualCheckInterval = null;
                }
                else
                {
                    processInfo.IndividualCheckInterval = (int)checkIntervalNumeric.Value;
                }

                // 개별 부팅 지연시간 설정 적용
                if (useGlobalBootDelayCheckBox.Checked)
                {
                    processInfo.IndividualBootDelay = null;
                }
                else
                {
                    processInfo.IndividualBootDelay = (int)bootDelayNumeric.Value;
                }

                // 재시작 지연시간 설정 적용
                if (useGlobalRestartDelayCheckBox.Checked)
                {
                    processInfo.IndividualRestartDelay = null;
                }
                else
                {
                    processInfo.IndividualRestartDelay = (int)restartDelayNumeric.Value;
                }

                if (useGlobalHangingCheckCheckBox.Checked)
                {
                    processInfo.IndividualCheckForHanging = null;
                }
                else
                {
                    processInfo.IndividualCheckForHanging = individualHangingCheckBox.Checked;
                }

                if (useGlobalMaxRestartsCheckBox.Checked)
                {
                    processInfo.IndividualMaxRestarts = null;
                }
                else
                {
                    processInfo.IndividualMaxRestarts = (int)maxRestartsNumeric.Value;
                }

                // 저장 성공 메시지 표시
                MessageBox.Show($"'{processInfo.Name}' 프로세스의 개별 설정이 저장되었습니다.", 
                               "설정 저장 완료", 
                               MessageBoxButtons.OK, 
                               MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                // 저장 실패 시 오류 메시지 표시
                MessageBox.Show($"설정 저장 중 오류가 발생했습니다:\n{ex.Message}", 
                               "저장 오류", 
                               MessageBoxButtons.OK, 
                               MessageBoxIcon.Error);
            }
        }

        public ProcessInfo GetUpdatedProcessInfo()
        {
            return processInfo;
        }
    }
}
