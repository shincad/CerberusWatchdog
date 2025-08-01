using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.Win32;
using System.Threading;

namespace EnhancedWatchdog
{
    public partial class Form1 : Form
    {
        private WatchdogService watchdogService;
        private SystemRestartService systemRestartService;
        private NotifyIcon notifyIcon;
        private System.Windows.Forms.Timer uiUpdateTimer;
        private bool isMinimizedToTray = false;
        private bool isExiting = false;
        private bool shouldAutoStart = false;

        public Form1() : this(false)
        { 
        }
        public Form1(bool autoStart)
        {
            shouldAutoStart = autoStart;
            InitializeComponent(); // Basic control initialization
            InitializeControls(); // All control setup
            InitializeWatchdog(); // watchdogService 먼저 초기화
            SetupComplexUI(); // Complex UI setup (watchdogService 사용)
            InitializeTrayIcon();
            InitializeTimer();
            LoadSettings();
        }

        private void InitializeControls()
        {
            // Initialize all controls by name
            tabControl = new TabControl();
            processListView = new ListView();
            logTextBox = new TextBox();
            checkIntervalNumeric = new NumericUpDown();
            restartDelayNumeric = new NumericUpDown();
            bootDelayNumeric = new NumericUpDown();
            maxRestartsNumeric = new NumericUpDown();
            checkResponseCheckbox = new CheckBox();
            
            // 시스템 재시작 관련 컴트롤들 초기화
            enableSystemRestartCheckBox = new CheckBox();
            systemRestartTypeComboBox = new ComboBox();
            systemRestartHourNumeric = new NumericUpDown();
            systemRestartMinuteNumeric = new NumericUpDown();
            systemRestartDayComboBox = new ComboBox();
            nextRestartInfoLabel = new Label();

            // Set basic properties
            SetupControlProperties();
        }

        private void SetupControlProperties()
        {
            // TabControl setup - 메뉴 아래에 배치
            tabControl.Dock = DockStyle.Fill;
            
            // ListView setup
            processListView.View = View.Details;
            processListView.FullRowSelect = true;
            processListView.GridLines = true;

            // TextBox setup
            logTextBox.Multiline = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.ReadOnly = true;
            logTextBox.Font = new Font("Consolas", 9F);

            // NumericUpDown setup
            checkIntervalNumeric.Minimum = 1;
            checkIntervalNumeric.Maximum = 3600;
            checkIntervalNumeric.Value = 5;

            restartDelayNumeric.Minimum = 0;
            restartDelayNumeric.Maximum = 300;
            restartDelayNumeric.Value = 3;

            bootDelayNumeric.Minimum = 0;
            bootDelayNumeric.Maximum = 600;
            bootDelayNumeric.Value = 30;

            maxRestartsNumeric.Minimum = 1;
            maxRestartsNumeric.Maximum = 100;
            maxRestartsNumeric.Value = 10;

            // CheckBox setup
            checkResponseCheckbox.Text = "Check for unresponsive processes";
            checkResponseCheckbox.Checked = true;
        }

        private void SetupComplexUI()
        {
            try
            {
                // Add tab pages to tab control FIRST
                SetupTabPages();
                
                // Add tabControl to form FIRST
                this.Controls.Add(tabControl);
                
                // Setup menu strip AFTER
                SetupMenuStrip();
                
                // Confirm: UI setup completed
                this.Text = $" Cerberus Watchdog V1.0-Written by shincad )";
                
                // Ensure tab control is visible and properly sized
                tabControl.BringToFront();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"UI setup error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // 기본 UI를 설정하여 프로그램이 완전히 실패하지 않도록 함
                SetupMinimalUI();
            }
        }
        
        private void SetupMinimalUI()
        {
            try
            {
                // 최소한의 UI만 설정
                if (tabControl == null)
                {
                    tabControl = new TabControl();
                }
                
                tabControl.Dock = DockStyle.Fill;
                
                // 기본 탭 추가
                var defaultTab = new TabPage("Monitoring");
                var label = new Label
                {
                    Text = "UI 초기화 오류가 발생했습니다. 프로그램을 다시 시작해주세요.",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                defaultTab.Controls.Add(label);
                tabControl.TabPages.Add(defaultTab);
                
                this.Controls.Add(tabControl);
                this.Text = "Enhanced Watchdog V1.0 - UI Error";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical UI error: {ex.Message}", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupMenuStrip()
        {
            var menuStrip = new MenuStrip();
            menuStrip.Dock = DockStyle.Top; // 메뉴를 상단에 고정
            
            var fileMenu = new ToolStripMenuItem("&File");
            var settingsMenu = new ToolStripMenuItem("&Settings");
            var helpMenu = new ToolStripMenuItem("&Help");

            var exitMenuItem = new ToolStripMenuItem("E&xit");
            exitMenuItem.Click += (s, e) => ExitApplication();
            fileMenu.DropDownItems.Add(exitMenuItem);

            var autoStartMenuItem = new ToolStripMenuItem("Enable &Auto Start");
            autoStartMenuItem.Click += AutoStartMenuItem_Click;
            var removeAutoStartMenuItem = new ToolStripMenuItem("&Remove Auto Start");
            removeAutoStartMenuItem.Click += RemoveAutoStartMenuItem_Click;
            settingsMenu.DropDownItems.AddRange(new[] { autoStartMenuItem, removeAutoStartMenuItem });

            var aboutMenuItem = new ToolStripMenuItem("&About");
            aboutMenuItem.Click += (s, e) => MessageBox.Show("Cerberus Watchdog v1.0\nProcess monitoring and auto restart tool\nWritten by shincad 2025", "About");
            helpMenu.DropDownItems.Add(aboutMenuItem);

            menuStrip.Items.AddRange(new[] { fileMenu, settingsMenu, helpMenu });
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void SetupTabPages()
        {
            // Monitoring tab
            var monitorTab = new TabPage("Monitoring");
            CreateMonitoringTab(monitorTab);

            // Settings tab
            var settingsTab = new TabPage("Settings");
            CreateSettingsTab(settingsTab);

            // Log tab
            var logTab = new TabPage("Log");
            CreateLogTab(logTab);

            tabControl.TabPages.AddRange(new[] { monitorTab, settingsTab, logTab });
        }

        private void CreateMonitoringTab(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // Process list group
            var processGroup = new GroupBox
            {
                Text = "Monitored Processes",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Setup ListView and add columns
            processListView.Dock = DockStyle.Fill;
            processListView.Columns.Clear();
            processListView.Columns.AddRange(new[] {
                new ColumnHeader { Text = "Process Name", Width = 150 },
                new ColumnHeader { Text = "Path", Width = 300 },
                new ColumnHeader { Text = "Status", Width = 80 },
                new ColumnHeader { Text = "PID", Width = 60 },
                new ColumnHeader { Text = "Last Restart", Width = 120 },
                new ColumnHeader { Text = "Restart Count", Width = 80 }
            });
            
            // Add process double-click event handler
            processListView.DoubleClick += ProcessListView_DoubleClick;

            processGroup.Controls.Add(processListView);

            // Button panel
            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };

            var addButton = new Button
            {
                Text = "Add Process",
                Size = new Size(100, 30),
                Location = new Point(10, 5)
            };
            addButton.Click += AddButton_Click_Enhanced;

            var removeButton = new Button
            {
                Text = "Remove Process",
                Size = new Size(100, 30),
                Location = new Point(120, 5)
            };
            removeButton.Click += RemoveButton_Click;

            var startButton = new Button
            {
                Text = "Start Monitoring",
                Size = new Size(100, 30),
                Location = new Point(230, 5)
            };
            startButton.Click += StartButton_Click;

            var stopButton = new Button
            {
                Text = "Stop Monitoring",
                Size = new Size(100, 30),
                Location = new Point(340, 5)
            };
            stopButton.Click += StopButton_Click;

            buttonPanel.Controls.AddRange(new Control[] { addButton, removeButton, startButton, stopButton });

            // Add controls to panel
            panel.Controls.Add(buttonPanel); // Add button panel first (Bottom)
            panel.Controls.Add(processGroup); // Add process group later (Fill)

            tab.Controls.Add(panel);
        }

        

        private void CreateLogTab(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // Setup logTextBox
            logTextBox.Dock = DockStyle.Fill;

            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };

            var clearLogButton = new Button
            {
                Text = "Clear Log",
                Size = new Size(100, 30),
                Location = new Point(10, 5)
            };
            clearLogButton.Click += (s, e) => logTextBox.Clear();

            var saveLogButton = new Button
            {
                Text = "Save Log",
                Size = new Size(100, 30),
                Location = new Point(120, 5)
            };
            saveLogButton.Click += SaveLogButton_Click;

            buttonPanel.Controls.AddRange(new Control[] { clearLogButton, saveLogButton });

            // Add controls to panel (order is important!)
            panel.Controls.Add(buttonPanel); // Add button panel first (Bottom)
            panel.Controls.Add(logTextBox);  // Add log textbox later (Fill)

            tab.Controls.Add(panel);
        }

        private void InitializeWatchdog()
        {
            watchdogService = new WatchdogService();
            watchdogService.ProcessRestarted += OnProcessRestarted;
            watchdogService.ProcessStatusChanged += OnProcessStatusChanged;
            watchdogService.LogMessage += OnLogMessage;
            
            // 시스템 재시작 서비스 초기화
            var settings = watchdogService.GetSettings();
            systemRestartService = new SystemRestartService(settings);
            systemRestartService.LogMessage += OnLogMessage;
        }

        private void InitializeTrayIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Application;
            notifyIcon.Text = "Enhanced Watchdog";
            notifyIcon.DoubleClick += (s, e) => ShowForm();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) => ShowForm());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => {
                ExitApplication();
            });
            notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void InitializeTimer()
        {
            uiUpdateTimer = new System.Windows.Forms.Timer();
            uiUpdateTimer.Interval = 1000;
            uiUpdateTimer.Tick += UpdateUI;
            uiUpdateTimer.Start();
        }

        public void StartWatchdogService()
        {
            try
            {
                if (watchdogService.IsRunning)
                {
                    LogMessage("Monitoring is already running.");
                    return;
                }
                
                watchdogService.Start();
                systemRestartService.Start(); // 시스템 재시작 서비스도 시작
                LogMessage("Monitoring has been started.");
                
                // UI 업데이트를 위해 프로세스 리스트 새로고침
                UpdateProcessList();
                
                // 다음 재시작 정보 로그
                var nextRestartInfo = systemRestartService.GetNextRestartInfo();
                LogMessage(nextRestartInfo);
            }
            catch (Exception ex)
            {
                LogMessage($"Monitoring start error: {ex.Message}");
                throw;
            }
        }

        // Event handlers
        private void AddButton_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select executable file to monitor",
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var addProcessForm = new AddProcessForm(openFileDialog.FileName);
                if (addProcessForm.ShowDialog() == DialogResult.OK)
                {
                    var processInfo = addProcessForm.GetProcessInfo();
                    watchdogService.AddProcess(processInfo);
                    SaveSettings();
                    UpdateProcessList();
                    LogMessage($"Process added: {processInfo.Name}");
                }
            }
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (processListView?.SelectedItems.Count > 0)
            {
                var selectedItem = processListView.SelectedItems[0];
                var processName = selectedItem.Text;

                if (MessageBox.Show($"Do you want to remove '{processName}' from monitoring?",
                    "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    watchdogService.RemoveProcess(processName);
                    SaveSettings();
                    UpdateProcessList();
                    LogMessage($"Process removed: {processName}");
                }
            }
            else
            {
                MessageBox.Show("Please select a process to remove.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            try
            {
                watchdogService.Start();
                LogMessage("Monitoring has been started.");
                MessageBox.Show("Monitoring has been started.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Monitoring start error: {ex.Message}");
                MessageBox.Show($"Failed to start monitoring: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            try
            {
                watchdogService.Stop();
                systemRestartService.Stop(); // 시스템 재시작 서비스도 중지
                LogMessage("Monitoring has been stopped.");
                MessageBox.Show("Monitoring has been stopped.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Monitoring stop error: {ex.Message}");
                MessageBox.Show($"Failed to stop monitoring: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void CreateSettingsTab(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // General settings group
            var generalGroup = new GroupBox
            {
                Text = "General Settings",
                Size = new Size(450, 250),
                Location = new Point(10, 10)
            };

            // Check interval setting
            var checkIntervalLabel = new Label
            {
                Text = "Check Interval (seconds):",
                Location = new Point(10, 25),
                Size = new Size(150, 20)
            };
            checkIntervalNumeric.Location = new Point(170, 23);
            checkIntervalNumeric.Size = new Size(60, 20);

            // Restart delay setting
            var restartDelayLabel = new Label
            {
                Text = "Restart Delay (seconds):",
                Location = new Point(10, 55),
                Size = new Size(150, 20)
            };
            restartDelayNumeric.Location = new Point(170, 53);
            restartDelayNumeric.Size = new Size(60, 20);

            // Boot delay setting
            var bootDelayLabel = new Label
            {
                Text = "Boot Delay (seconds):",
                Location = new Point(10, 85),
                Size = new Size(150, 20)
            };
            bootDelayNumeric.Location = new Point(170, 83);
            bootDelayNumeric.Size = new Size(60, 20);
            
            var bootDelayDescLabel = new Label
            {
                Text = "Delay before starting monitoring when program first launches (0 = immediate)",
                Location = new Point(240, 85),
                Size = new Size(200, 15),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 7F, FontStyle.Italic)
            };

            // Max restarts setting
            var maxRestartsLabel = new Label
            {
                Text = "Max Restarts:",
                Location = new Point(10, 115),
                Size = new Size(150, 20)
            };
            maxRestartsNumeric.Location = new Point(170, 113);
            maxRestartsNumeric.Size = new Size(60, 20);

            // Check response checkbox
            checkResponseCheckbox.Location = new Point(10, 145);
            checkResponseCheckbox.Size = new Size(300, 20);

            // Auto start option
            var autoStartCheckbox = new CheckBox
            {
                Text = "Auto start monitoring on program launch",
                Location = new Point(10, 175),
                Size = new Size(300, 20)
            };
            
            var autoStartDescLabel = new Label
            {
                Text = "When enabled, monitoring will start automatically when the program opens.",
                Location = new Point(30, 195),
                Size = new Size(350, 15),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic)
            };
            
            try
            {
                if (watchdogService != null)
                {
                    var settings = watchdogService.GetSettings();
                    autoStartCheckbox.Checked = settings?.AutoStart ?? true;
                }
                else
                {
                    autoStartCheckbox.Checked = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings load error in CreateSettingsTab: {ex.Message}");
                autoStartCheckbox.Checked = true;
            }

            generalGroup.Controls.AddRange(new Control[] {
                checkIntervalLabel, checkIntervalNumeric,
                restartDelayLabel, restartDelayNumeric,
                bootDelayLabel, bootDelayNumeric, bootDelayDescLabel,
                maxRestartsLabel, maxRestartsNumeric,
                checkResponseCheckbox,
                autoStartCheckbox,
                autoStartDescLabel
            });

            // System Restart Settings Group
            var systemRestartGroup = new GroupBox
            {
                Text = "System Restart Settings",
                Size = new Size(450, 200),
                Location = new Point(480, 10) // General Settings 옆으로 이동
            };

            // Enable system restart checkbox
            enableSystemRestartCheckBox.Text = "Enable System Restart";
            enableSystemRestartCheckBox.Location = new Point(10, 25);
            enableSystemRestartCheckBox.Size = new Size(200, 20);
            enableSystemRestartCheckBox.CheckedChanged += EnableSystemRestartCheckBox_CheckedChanged;

            // Restart type label and combobox
            var restartTypeLabel = new Label
            {
                Text = "Restart Type:",
                Location = new Point(10, 55),
                Size = new Size(80, 20)
            };
            
            systemRestartTypeComboBox.Location = new Point(100, 53);
            systemRestartTypeComboBox.Size = new Size(120, 20);
            systemRestartTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            systemRestartTypeComboBox.Items.AddRange(new[] { "Daily", "Weekly" });
            systemRestartTypeComboBox.SelectedIndex = 0;
            systemRestartTypeComboBox.SelectedIndexChanged += SystemRestartTypeComboBox_SelectedIndexChanged;

            // Time setting labels and controls
            var timeLabel = new Label
            {
                Text = "Time:",
                Location = new Point(10, 85),
                Size = new Size(40, 20)
            };

            systemRestartHourNumeric.Location = new Point(55, 83);
            systemRestartHourNumeric.Size = new Size(50, 20);
            systemRestartHourNumeric.Minimum = 0;
            systemRestartHourNumeric.Maximum = 23;
            systemRestartHourNumeric.Value = 3;

            var hourLabel = new Label
            {
                Text = ":",
                Location = new Point(110, 85),
                Size = new Size(10, 20)
            };

            systemRestartMinuteNumeric.Location = new Point(125, 83);
            systemRestartMinuteNumeric.Size = new Size(50, 20);
            systemRestartMinuteNumeric.Minimum = 0;
            systemRestartMinuteNumeric.Maximum = 59;
            systemRestartMinuteNumeric.Value = 0;

            var timeFormatLabel = new Label
            {
                Text = "(24-hour format)",
                Location = new Point(185, 85),
                Size = new Size(100, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic)
            };

            // Day selection for weekly restart
            var dayLabel = new Label
            {
                Text = "Day:",
                Location = new Point(10, 115),
                Size = new Size(40, 20)
            };

            systemRestartDayComboBox.Location = new Point(55, 113);
            systemRestartDayComboBox.Size = new Size(100, 20);
            systemRestartDayComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            systemRestartDayComboBox.Items.AddRange(new[] { 
                "Sunday", "Monday", "Tuesday", "Wednesday", 
                "Thursday", "Friday", "Saturday" 
            });
            systemRestartDayComboBox.SelectedIndex = 0;
            systemRestartDayComboBox.Visible = false;

            // Next restart info label
            nextRestartInfoLabel.Location = new Point(10, 145);
            nextRestartInfoLabel.Size = new Size(430, 40);
            nextRestartInfoLabel.Text = "Next restart: Not scheduled";
            nextRestartInfoLabel.ForeColor = Color.Blue;
            nextRestartInfoLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            // Add all system restart controls to group
            systemRestartGroup.Controls.AddRange(new Control[] {
                enableSystemRestartCheckBox,
                restartTypeLabel, systemRestartTypeComboBox,
                timeLabel, systemRestartHourNumeric, hourLabel, systemRestartMinuteNumeric, timeFormatLabel,
                dayLabel, systemRestartDayComboBox,
                nextRestartInfoLabel
            });

            // Save button (위치 조정)
            var saveButton = new Button
            {
                Text = "Save Settings",
                Size = new Size(100, 30),
                Location = new Point(10, 270) // General Settings 아래로 이동
            };
            saveButton.Click += SaveSettingsButton_Click;

            // Add all controls to main panel
            panel.Controls.AddRange(new Control[] { generalGroup, systemRestartGroup, saveButton });
            tab.Controls.Add(panel);

            // 초기 설정 로드
            LoadSystemRestartSettings();
            UpdateSystemRestartUI();
        }
        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                var settings = watchdogService.GetSettings();
                settings.CheckInterval = (int)checkIntervalNumeric.Value;
                settings.RestartDelay = (int)restartDelayNumeric.Value;
                settings.BootDelay = (int)bootDelayNumeric.Value;
                settings.MaxRestarts = (int)maxRestartsNumeric.Value;
                settings.CheckForHangingProcess = checkResponseCheckbox.Checked;
                settings.AutoStart = true; // Always save auto start as true

                watchdogService.UpdateSettings(settings);
                SaveSettings();
                LogMessage("Settings have been saved.");
                MessageBox.Show("Settings have been saved.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Settings save error: {ex.Message}");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveLogButton_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save log file",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"WatchdogLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, logTextBox.Text, Encoding.UTF8);
                    MessageBox.Show("Log has been saved.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save log: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AutoStartMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var exePath = Application.ExecutablePath;
                var registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                registryKey?.SetValue("EnhancedWatchdog", $"\"{exePath}\" /minimized");
                registryKey?.Close();

                LogMessage("Auto startup has been registered.");
                MessageBox.Show("Auto startup has been registered.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Auto startup registration error: {ex.Message}");
                MessageBox.Show($"Failed to register auto startup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveAutoStartMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                registryKey?.DeleteValue("EnhancedWatchdog", false);
                registryKey?.Close();

                LogMessage("Auto startup has been removed.");
                MessageBox.Show("Auto startup has been removed.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Auto startup removal error: {ex.Message}");
                MessageBox.Show($"Failed to remove auto startup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnProcessRestarted(object sender, ProcessRestartedEventArgs e)
        {
            if (this.IsHandleCreated && !this.IsDisposed && !isExiting)
            {
                try
                {
                    this.Invoke(new Action(() =>
                    {
                        LogMessage($"Process restarted: {e.ProcessName} (PID: {e.NewProcessId})");
                        UpdateProcessList();
                    }));
                }
                catch (ObjectDisposedException)
                {
                    // 폼이 이미 disposed된 경우 무시
                }
                catch (InvalidOperationException)
                {
                    // 창 핸들 관련 오류 발생 시 무시
                }
            }
        }

        private void OnProcessStatusChanged(object sender, ProcessStatusChangedEventArgs e)
        {
            if (this.IsHandleCreated && !this.IsDisposed && !isExiting)
            {
                try
                {
                    this.Invoke(new Action(() =>
                    {
                        LogMessage($"Process status changed: {e.ProcessName} - {e.Status}");
                        UpdateProcessList();
                    }));
                }
                catch (ObjectDisposedException)
                {
                    // 폼이 이미 disposed된 경우 무시
                }
                catch (InvalidOperationException)
                {
                    // 창 핸들 관련 오류 발생 시 무시
                }
            }
        }

        private void OnLogMessage(object sender, LogMessageEventArgs e)
        {
            // 창 핸들이 생성되었고 폼이 종료 중이 아닐 때만 Invoke 호출
            if (this.IsHandleCreated && !this.IsDisposed && !isExiting)
            {
                try
                {
                    this.Invoke(new Action(() =>
                    {
                        LogMessage(e.Message);
                    }));
                }
                catch (ObjectDisposedException)
                {
                    // 폼이 이미 disposed된 경우 무시
                }
                catch (InvalidOperationException)
                {
                    // 창 핸들 관련 오류 발생 시 무시
                }
            }
        }

        private void UpdateUI(object sender, EventArgs e)
        {
            UpdateProcessList();
        }

        private void UpdateProcessList()
        {
            if (processListView == null || processListView.IsDisposed) return;
            if (watchdogService == null) return;

            try
            {
                processListView.BeginUpdate();
                processListView.Items.Clear();

                var processes = watchdogService.GetProcesses();
                if (processes == null) return;

                foreach (var processInfo in processes)
                {
                    if (processInfo == null) continue;
                    
                    var item = new ListViewItem(processInfo.Name ?? "Unknown");
                    item.SubItems.Add(processInfo.ExecutablePath ?? "N/A");
                    item.SubItems.Add(processInfo.IsRunning ? "Running" : "Stopped");
                    item.SubItems.Add(processInfo.ProcessId?.ToString() ?? "N/A");
                    item.SubItems.Add(processInfo.LastRestart?.ToString("MM/dd HH:mm:ss") ?? "Never");
                    item.SubItems.Add(processInfo.RestartCount.ToString());

                    if (!processInfo.IsRunning)
                    {
                        item.BackColor = Color.LightCoral;
                    }
                    else if (processInfo.RestartCount > 0)
                    {
                        item.BackColor = Color.LightYellow;
                    }

                    processListView.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateProcessList error: {ex.Message}");
                // 오류가 발생해도 UI를 마비시키지 않도록 하기 위해 예외만 로그
            }
            finally
            {
                try
                {
                    processListView.EndUpdate();
                }
                catch
                {
                    // EndUpdate 실패도 무시
                }
            }
        }

        private void LogMessage(string message)
        {
            if (logTextBox == null) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logEntry = $"[{timestamp}] {message}{Environment.NewLine}";

            logTextBox.AppendText(logEntry);
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();
        }

        private void LoadSettings()
        {
            try
            {
                var settings = watchdogService.LoadSettings();

                if (checkIntervalNumeric != null) checkIntervalNumeric.Value = settings.CheckInterval;
                if (restartDelayNumeric != null) restartDelayNumeric.Value = settings.RestartDelay;
                if (bootDelayNumeric != null) bootDelayNumeric.Value = settings.BootDelay;
                if (maxRestartsNumeric != null) maxRestartsNumeric.Value = settings.MaxRestarts;
                if (checkResponseCheckbox != null) checkResponseCheckbox.Checked = settings.CheckForHangingProcess;

                UpdateProcessList();
                LogMessage("Settings have been loaded.");
            }
            catch (Exception ex)
            {
                LogMessage($"Settings load error: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                watchdogService.SaveSettings();
            }
            catch (Exception ex)
            {
                LogMessage($"Settings save error: {ex.Message}");
            }
        }

        private void ShowForm()
        {
            if (isExiting) return;

            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            isMinimizedToTray = false;
            notifyIcon.Visible = false;
        }

        private void HideToTray()
        {
            if (isExiting) return;

            this.Hide();
            isMinimizedToTray = true;
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(3000, "Enhanced Watchdog", "Program has been minimized to system tray.", ToolTipIcon.Info);
        }

        // Process settings double-click handler
        private void ProcessListView_DoubleClick(object sender, EventArgs e)
        {
            if (processListView?.SelectedItems.Count > 0)
            {
                var selectedItem = processListView.SelectedItems[0];
                var processName = selectedItem.Text;
                
                var processInfo = watchdogService.GetProcesses()
                    .FirstOrDefault(p => p.Name.Equals(processName, StringComparison.OrdinalIgnoreCase));
                    
                if (processInfo != null)
                {
                    var globalSettings = watchdogService.GetSettings();
                    var settingsForm = new ProcessSettingsForm(processInfo, globalSettings);
                    
                    if (settingsForm.ShowDialog() == DialogResult.OK)
                    {
                        var updatedProcessInfo = settingsForm.GetUpdatedProcessInfo();
                        SaveSettings();
                        UpdateProcessList();
                        LogMessage($"Process settings updated: {processInfo.Name}");
                    }
                }
            }
        }
        
        // Enhanced process addition method - automatically starts monitoring after adding
        private void AddButton_Click_Enhanced(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select executable file to monitor",
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var addProcessForm = new AddProcessForm(openFileDialog.FileName);
                if (addProcessForm.ShowDialog() == DialogResult.OK)
                {
                    var processInfo = addProcessForm.GetProcessInfo();
                    watchdogService.AddProcess(processInfo);
                    
                    // Automatically start monitoring after adding process if not already started
                    if (!watchdogService.IsRunning)
                    {
                        try
                        {
                            watchdogService.Start();
                            LogMessage("Monitoring started automatically due to process addition.");
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Auto monitoring start failed: {ex.Message}");
                        }
                    }
                    
                    SaveSettings();
                    UpdateProcessList();
                    LogMessage($"Process added: {processInfo.Name}");
                }
            }
        }

        private void ExitApplication()
        {
            if (isExiting) return;
            isExiting = true;

            try
            {
                watchdogService?.Stop();
                systemRestartService?.Stop(); // 시스템 재시작 서비스도 중지
                uiUpdateTimer?.Stop();

                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                }

                // Ensure the form is visible for closing
                if (!this.Visible)
                {
                    this.Show();
                    this.WindowState = FormWindowState.Normal;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exit error: {ex.Message}");
            }
            finally
            {
                Application.Exit();
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            if (isExiting || this.Created)
            {
                base.SetVisibleCore(value);
                return;
            }

            if (!value && !isMinimizedToTray && !this.Created)
            {
                base.SetVisibleCore(false);
                HideToTray();
            }
            else
            {
                base.SetVisibleCore(value);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // 폼이 완전히 로드된 후 자동 시작 처리
            // 1. 명령줄에서 지정된 자동 시작
            // 2. 설정에서 "Auto start monitoring on program launch" 옵션이 체크된 경우
            bool shouldStart = shouldAutoStart;
            
            if (!shouldStart && watchdogService != null)
            {
                try
                {
                    var settings = watchdogService.GetSettings();
                    shouldStart = settings?.AutoStart ?? false;
                }
                catch (Exception ex)
                {
                    LogMessage($"Auto start setting check failed: {ex.Message}");
                }
            }
            
            if (shouldStart)
            {
                try
                {
                    StartWatchdogService();
                    LogMessage("Auto start: Monitoring started automatically.");
                }
                catch (Exception ex)
                {
                    LogMessage($"Auto start failed: {ex.Message}");
                }
            }
            else
            {
                LogMessage("Auto start is disabled. Click 'Start Monitoring' to begin.");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (isExiting)
            {
                base.OnFormClosing(e);
                return;
            }

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
            }
            else
            {
                ExitApplication();
            }
        }

        // 시스템 재시작 체크박스 이벤트 핸들러
        private void EnableSystemRestartCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateSystemRestartUI();
            UpdateNextRestartInfo();
        }

        // 시스템 재시작 타입 변경 이벤트 핸들러
        private void SystemRestartTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSystemRestartUI();
            UpdateNextRestartInfo();
        }

        // 시스템 재시작 UI 업데이트
        private void UpdateSystemRestartUI()
        {
            bool isEnabled = enableSystemRestartCheckBox.Checked;
            bool isWeekly = systemRestartTypeComboBox.SelectedItem?.ToString() == "Weekly";

            // 모든 관련 컨트롤들의 활성화 상태 설정
            systemRestartTypeComboBox.Enabled = isEnabled;
            systemRestartHourNumeric.Enabled = isEnabled;
            systemRestartMinuteNumeric.Enabled = isEnabled;
            systemRestartDayComboBox.Enabled = isEnabled && isWeekly;
            systemRestartDayComboBox.Visible = isWeekly;

            // 다음 재시작 정보 레이블 업데이트
            if (isEnabled)
            {
                nextRestartInfoLabel.ForeColor = Color.Blue;
            }
            else
            {
                nextRestartInfoLabel.ForeColor = Color.Gray;
                nextRestartInfoLabel.Text = "System restart is disabled";
            }
        }

        // 다음 재시작 정보 업데이트
        private void UpdateNextRestartInfo()
        {
            if (!enableSystemRestartCheckBox.Checked)
            {
                nextRestartInfoLabel.Text = "System restart is disabled";
                return;
            }

            try
            {
                var isWeekly = systemRestartTypeComboBox.SelectedItem?.ToString() == "Weekly";
                var hour = (int)systemRestartHourNumeric.Value;
                var minute = (int)systemRestartMinuteNumeric.Value;
                var dayOfWeek = (DayOfWeek)systemRestartDayComboBox.SelectedIndex;

                var nextRestart = CalculateNextRestartTime(isWeekly, hour, minute, dayOfWeek);
                var timeSpan = nextRestart - DateTime.Now;
                
                string timeInfo = $"Next restart: {nextRestart:yyyy-MM-dd HH:mm}";
                if (timeSpan.TotalDays >= 1)
                {
                    timeInfo += $" ({(int)timeSpan.TotalDays}d {timeSpan.Hours}h remaining)";
                }
                else
                {
                    timeInfo += $" ({timeSpan.Hours}h {timeSpan.Minutes}m remaining)";
                }
                
                nextRestartInfoLabel.Text = timeInfo;
            }
            catch (Exception ex)
            {
                nextRestartInfoLabel.Text = $"Error calculating next restart: {ex.Message}";
                nextRestartInfoLabel.ForeColor = Color.Red;
            }
        }

        // 다음 재시작 시간 계산
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

        // 시스템 재시작 설정 저장 버튼 클릭 이벤트
        private void SaveSettingsButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (watchdogService == null)
                {
                    MessageBox.Show("Watchdog service is not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                var settings = watchdogService.GetSettings();
                if (settings == null)
                {
                    MessageBox.Show("Settings could not be loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // 기본 설정 저장
                settings.CheckInterval = (int)checkIntervalNumeric.Value;
                settings.RestartDelay = (int)restartDelayNumeric.Value;
                settings.BootDelay = (int)bootDelayNumeric.Value;
                settings.MaxRestarts = (int)maxRestartsNumeric.Value;
                settings.CheckForHangingProcess = checkResponseCheckbox.Checked;
                
                // 시스템 재시작 설정 저장
                settings.EnableSystemRestart = enableSystemRestartCheckBox.Checked;
                settings.SystemRestartType = systemRestartTypeComboBox.SelectedItem?.ToString() == "Weekly" ? 
                    SystemRestartType.Weekly : SystemRestartType.Daily;
                settings.SystemRestartHour = (int)systemRestartHourNumeric.Value;
                settings.SystemRestartMinute = (int)systemRestartMinuteNumeric.Value;
                settings.SystemRestartDayOfWeek = (DayOfWeek)systemRestartDayComboBox.SelectedIndex;

                watchdogService.UpdateSettings(settings);
                
                // 시스템 재시작 서비스 업데이트
                if (systemRestartService != null)
                {
                    systemRestartService.UpdateSettings(settings);
                    if (settings.EnableSystemRestart)
                    {
                        systemRestartService.Start();
                        LogMessage("System restart schedule updated and enabled.");
                    }
                    else
                    {
                        systemRestartService.Stop();
                        LogMessage("System restart schedule disabled.");
                    }
                }
                
                SaveSettings();
                UpdateNextRestartInfo();
                LogMessage("Settings have been saved.");
                MessageBox.Show("Settings have been saved.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Settings save error: {ex.Message}");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 시스템 재시작 설정 로드
        private void LoadSystemRestartSettings()
        {
            try
            {
                if (watchdogService != null)
                {
                    var settings = watchdogService.GetSettings();
                    if (settings != null)
                    {
                        enableSystemRestartCheckBox.Checked = settings.EnableSystemRestart;
                        systemRestartTypeComboBox.SelectedItem = settings.SystemRestartType == SystemRestartType.Weekly ? "Weekly" : "Daily";
                        systemRestartHourNumeric.Value = settings.SystemRestartHour;
                        systemRestartMinuteNumeric.Value = settings.SystemRestartMinute;
                        systemRestartDayComboBox.SelectedIndex = (int)settings.SystemRestartDayOfWeek;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"System restart settings load error: {ex.Message}");
            }
        }
    }
}