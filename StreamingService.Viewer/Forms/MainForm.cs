using StreamingService.Core.Models;
using StreamingService.Viewer.Services;
using System.Windows.Forms;
using System.Drawing;

namespace StreamingService.Viewer.Forms
{
    public partial class MainForm : Form
    {
        private readonly ViewerService _viewerService;
        private readonly PictureBox _displayBox;
        private readonly Label _statusLabel;
        private readonly Button _connectButton;
        private readonly Panel _controlPanel;

        // Добавляем элементы управления звуком
        private readonly TrackBar _volumeTrackBar;
        private readonly Label _volumeLabel;
        private readonly CheckBox _muteCheckBox;
        private readonly ProgressBar _audioLevelBar;

        public MainForm()
        {
            InitializeComponent();

            // Инициализация сервиса
            _viewerService = new ViewerService();

            // Создание панели управления
            _controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5)
            };

            // Создание кнопки подключения
            _connectButton = new Button
            {
                Text = "Connect to Stream",
                Dock = DockStyle.Left,
                Width = 120,
                Height = 30
            };

            // Создание метки статуса
            _statusLabel = new Label
            {
                Text = "Disconnected",
                Dock = DockStyle.Right,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(5)
            };

            // Создание области отображения видео
            _displayBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            // Создание панели управления звуком
            var audioPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Padding = new Padding(5)
            };

            _volumeLabel = new Label
            {
                Text = "Volume:",
                AutoSize = true,
                Location = new Point(10, 10)
            };

            _volumeTrackBar = new TrackBar
            {
                Location = new Point(70, 5),
                Width = 150,
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                TickFrequency = 10,
                TickStyle = TickStyle.Both
            };

            _muteCheckBox = new CheckBox
            {
                Text = "Mute",
                AutoSize = true,
                Location = new Point(230, 10)
            };

            _audioLevelBar = new ProgressBar
            {
                Location = new Point(300, 10),
                Width = 100,
                Height = 20,
                Minimum = 0,
                Maximum = 100
            };

            // Добавление элементов на панель управления звуком
            audioPanel.Controls.AddRange(new Control[]
            {
                _volumeLabel,
                _volumeTrackBar,
                _muteCheckBox,
                _audioLevelBar
            });

            // Добавление элементов управления на форму
            _controlPanel.Controls.Add(_connectButton);
            _controlPanel.Controls.Add(_statusLabel);
            Controls.Add(_controlPanel);
            Controls.Add(_displayBox);
            Controls.Add(audioPanel);

            // Настройка формы
            this.Text = "Stream Viewer";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(640, 480);

            // Привязка обработчиков событий
            _connectButton.Click += ConnectButton_Click;
            _viewerService.FrameReceived += ViewerService_FrameReceived;
            _viewerService.StatusChanged += ViewerService_StatusChanged;
            _viewerService.ErrorOccurred += ViewerService_ErrorOccurred;
            _viewerService.AudioLevelChanged += ViewerService_AudioLevelChanged;

            _volumeTrackBar.ValueChanged += (s, e) => _viewerService.SetVolume(_volumeTrackBar.Value / 100.0f);
            _muteCheckBox.CheckedChanged += (s, e) => _viewerService.IsMuted = _muteCheckBox.Checked;

            // Обработка закрытия формы
            this.FormClosing += MainForm_FormClosing;
        }

        private void ViewerService_AudioLevelChanged(object? sender, int level)
        {
            if (IsDisposed) return;

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => UpdateAudioLevel(level)));
                }
                else
                {
                    UpdateAudioLevel(level);
                }
            }
            catch (ObjectDisposedException)
            {
                // Форма могла быть закрыта
            }
        }

        private void UpdateAudioLevel(int level)
        {
            _audioLevelBar.Value = Math.Min(level, 100);
        }

        private async void ConnectButton_Click(object? sender, EventArgs e)
        {
            if (!_viewerService.IsConnected)
            {
                using (var connectionForm = new ConnectionForm())
                {
                    if (connectionForm.ShowDialog() == DialogResult.OK)
                    {
                        _connectButton.Enabled = false;
                        try
                        {
                            await _viewerService.ConnectAsync(connectionForm.ServerIP, connectionForm.Port);
                            _connectButton.Text = "Disconnect";
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Connection failed: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        finally
                        {
                            _connectButton.Enabled = true;
                        }
                    }
                }
            }
            else
            {
                await _viewerService.DisconnectAsync();
                _connectButton.Text = "Connect to Stream";
            }
        }

        private void UpdateDisplay(Bitmap frame)
        {
            try
            {
                if (frame == null) return;

                var oldImage = _displayBox.Image;
                _displayBox.Image = frame;
                oldImage?.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating display: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ViewerService_FrameReceived(object? sender, Bitmap frame)
        {
            if (IsDisposed) return;

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => UpdateDisplay(frame)));
                }
                else
                {
                    UpdateDisplay(frame);
                }
            }
            catch (ObjectDisposedException)
            {
                // Форма могла быть закрыта
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing frame: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ViewerService_ErrorOccurred(object? sender, Exception ex)
        {
            if (IsDisposed) return;

            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => ShowError(ex)));
                }
                else
                {
                    ShowError(ex);
                }
            }
            catch (ObjectDisposedException)
            {
                // Форма могла быть закрыта
            }
        }

        private void ViewerService_StatusChanged(object? sender, string status)
        {
            if (IsDisposed) return;

            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => UpdateStatus(status)));
                }
                else
                {
                    UpdateStatus(status);
                }
            }
            catch (ObjectDisposedException)
            {
                // Форма могла быть закрыта
            }
        }

        private void UpdateStatus(string status)
        {
            _statusLabel.Text = status;
        }

        private void ShowError(Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_viewerService.IsConnected)
            {
                await _viewerService.DisconnectAsync();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _viewerService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}