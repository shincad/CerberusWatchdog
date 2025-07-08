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
            this.Size = new Size(600, 400);
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

            // 설명 레이블
            var descriptionLabel = new Label
            {
                Text = "프로세스가 종료되거나 응답하지 않을 때 자동으로 재시작됩니다.\n" +
                       "프로세스명은 중복될 수 없습니다.",
                Location = new Point(15, 170),
                Size = new Size(450, 40),
                ForeColor = Color.Gray
            };

            // 버튼들
            okButton = new Button
            {
                Text = "확인",
                Location = new Point(300, 230),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "취소",
                Location = new Point(385, 230),
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
                descriptionLabel,
                okButton, cancelButton
            });

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
                EnableAutoRestart = autoRestartCheckBox.Checked
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public ProcessInfo GetProcessInfo()
        {
            return processInfo;
        }
    }
}