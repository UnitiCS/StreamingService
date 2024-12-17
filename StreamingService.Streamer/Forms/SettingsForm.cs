using StreamingService.Core.Models;
using System.Windows.Forms;
using System.Drawing;

namespace StreamingService.Streamer.Forms
{
    public partial class SettingsForm : Form
    {
        private readonly StreamSettings _settings;

        public SettingsForm(StreamSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            InitializeControls();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(300, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Stream Settings";
            this.ResumeLayout(false);
        }

        private void InitializeControls()
        {
            // Создаем элементы управления для настроек
            var lblQuality = new Label
            {
                Text = "Video Quality:",
                Location = new Point(10, 10),
                AutoSize = true
            };

            var numQuality = new NumericUpDown
            {
                Location = new Point(120, 10),
                Minimum = 1,
                Maximum = 100,
                Value = _settings.VideoQuality
            };

            var lblFPS = new Label
            {
                Text = "FPS:",
                Location = new Point(10, 40),
                AutoSize = true
            };

            var numFPS = new NumericUpDown
            {
                Location = new Point(120, 40),
                Minimum = 1,
                Maximum = 60,
                Value = _settings.FPS
            };

            var lblPort = new Label
            {
                Text = "Port:",
                Location = new Point(10, 70),
                AutoSize = true
            };

            var numPort = new NumericUpDown
            {
                Location = new Point(120, 70),
                Minimum = 1024,
                Maximum = 65535,
                Value = _settings.Port
            };

            var btnSave = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Location = new Point(10, 120),
                Width = 120
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(140, 120),
                Width = 120
            };

            btnSave.Click += (s, e) =>
            {
                _settings.VideoQuality = (int)numQuality.Value;
                _settings.FPS = (int)numFPS.Value;
                _settings.Port = (int)numPort.Value;
                Close();
            };

            // Добавляем элементы на форму
            Controls.AddRange(new Control[]
            {
                lblQuality, numQuality,
                lblFPS, numFPS,
                lblPort, numPort,
                btnSave, btnCancel
            });
        }
    }
}