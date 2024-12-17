using StreamingService.Core.Interfaces;
using StreamingService.Core.Models;
using StreamingService.Streamer.Services;
using System.Windows.Forms;
using System.Drawing;

namespace StreamingService.Streamer.Forms
{
    public partial class MainForm : Form
    {
        private readonly IStreamingService _streamingService;
        private readonly StreamSettings _settings;

        // Добавляем объявления элементов управления
        private Button btnStartStream;
        private Button btnSettings;
        private Label lblStatus;
        private Label _micStatusLabel;
        private CheckBox _muteCheckBox;
        private ProgressBar _micLevelBar;

        public MainForm()
        {
            InitializeComponent();
            _settings = new StreamSettings();
            _streamingService = new StreamerService(_settings);
            InitializeControls();
            SetupEventHandlers();


        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Name = "MainForm";
            this.Text = "Streaming Service";
            this.ResumeLayout(false);
        }

        private void InitializeControls()
        {
            // Создаем элементы управления
            btnStartStream = new Button
            {
                Text = "Start Stream",
                Location = new Point(10, 10),
                Size = new Size(100, 30)
            };

            btnSettings = new Button
            {
                Text = "Settings",
                Location = new Point(120, 10),
                Size = new Size(100, 30)
            };

            lblStatus = new Label
            {
                Text = "Status: Ready",
                Location = new Point(10, 50),
                AutoSize = true
            };

            // Добавляем индикатор микрофона
            _muteCheckBox = new CheckBox
            {
                Text = "Mute Microphone",
                Location = new Point(230, 10),
                AutoSize = true
            };

            _micStatusLabel = new Label
            {
                Text = "Mic: Not active",
                Location = new Point(350, 10),
                AutoSize = true
            };

            _micLevelBar = new ProgressBar
            {
                Location = new Point(450, 10),
                Width = 100,
                Height = 20,
                Minimum = 0,
                Maximum = 100
            };

            // Добавляем элементы на форму
            Controls.Add(btnStartStream);
            Controls.Add(btnSettings);
            Controls.Add(lblStatus);
            Controls.Add(_muteCheckBox);
            Controls.Add(_micStatusLabel);
            Controls.Add(_micLevelBar);
        }

        private void SetupEventHandlers()
        {
            btnStartStream.Click += async (s, e) => await ToggleStreaming();
            btnSettings.Click += (s, e) => ShowSettings();

            _streamingService.StatusChanged += (s, status) =>
            {
                if (InvokeRequired)
                    Invoke(new Action(() => lblStatus.Text = $"Status: {status}"));
                else
                    lblStatus.Text = $"Status: {status}";
            };

            _streamingService.ErrorOccurred += (s, ex) =>
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            // Добавляем обработчики для микрофона
            _muteCheckBox.CheckedChanged += (s, e) =>
            {
                if (_streamingService is StreamerService streamerService)
                {
                    streamerService.IsMicrophoneMuted = _muteCheckBox.Checked;
                    _micStatusLabel.Text = _muteCheckBox.Checked ? "Mic: Muted" : "Mic: Active";
                }
            };

            if (_streamingService is StreamerService streamerService)
            {
                streamerService.AudioLevelChanged += (s, level) =>
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            _micLevelBar.Value = Math.Min(level, 100);
                        }));
                    }
                    else
                    {
                        _micLevelBar.Value = Math.Min(level, 100);
                    }
                };
            }
        }

        private async Task ToggleStreaming()
        {
            try
            {
                if (!_streamingService.IsStreaming)
                {
                    btnStartStream.Enabled = false;
                    await _streamingService.StartAsync();
                    btnStartStream.Text = "Stop Stream";
                    _micStatusLabel.Text = "Mic: Active";
                }
                else
                {
                    await _streamingService.StopAsync();
                    btnStartStream.Text = "Start Stream";
                    _micStatusLabel.Text = "Mic: Not active";
                    _micLevelBar.Value = 0;
                }
            }
            finally
            {
                btnStartStream.Enabled = true;
            }
        }

        private void ShowSettings()
        {
            using (var settingsForm = new SettingsForm(_settings))
            {
                settingsForm.ShowDialog();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_streamingService.IsStreaming)
            {
                _streamingService.StopAsync().Wait();
            }
            base.OnFormClosing(e);
        }
    }
}