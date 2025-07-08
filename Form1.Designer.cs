using System;

namespace EnhancedWatchdog
{
    partial class Form1
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // UI 컨트롤들
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.ListView processListView;
        private System.Windows.Forms.TextBox logTextBox;
        private System.Windows.Forms.NumericUpDown checkIntervalNumeric;
        private System.Windows.Forms.NumericUpDown restartDelayNumeric;
        private System.Windows.Forms.NumericUpDown bootDelayNumeric;
        private System.Windows.Forms.NumericUpDown maxRestartsNumeric;
        private System.Windows.Forms.CheckBox checkResponseCheckbox;
        
        // 시스템 재시작 관련 컨트롤들
        private System.Windows.Forms.CheckBox enableSystemRestartCheckBox;
        private System.Windows.Forms.ComboBox systemRestartTypeComboBox;
        private System.Windows.Forms.NumericUpDown systemRestartHourNumeric;
        private System.Windows.Forms.NumericUpDown systemRestartMinuteNumeric;
        private System.Windows.Forms.ComboBox systemRestartDayComboBox;
        private System.Windows.Forms.Label nextRestartInfoLabel;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    // 커스텀 리소스 정리
                    if (watchdogService != null)
                    {
                        watchdogService.Stop();
                        watchdogService.Dispose();
                        watchdogService = null;
                    }

                    if (uiUpdateTimer != null)
                    {
                        uiUpdateTimer.Stop();
                        uiUpdateTimer.Dispose();
                        uiUpdateTimer = null;
                    }

                    if (notifyIcon != null)
                    {
                        notifyIcon.Visible = false;
                        notifyIcon.Dispose();
                        notifyIcon = null;
                    }

                    // 표준 컴포넌트 정리
                    if (components != null)
                    {
                        components.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    // Dispose 중 오류는 무시
                    System.Diagnostics.Debug.WriteLine($"Dispose 중 오류: {ex.Message}");
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.MinimumSize = new System.Drawing.Size(1000, 400);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Enhanced Watchdog";
            this.ResumeLayout(false);
        }

        #endregion
    }
}