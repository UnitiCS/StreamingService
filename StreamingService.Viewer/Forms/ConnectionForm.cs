using System.Windows.Forms;
using System.Drawing;

namespace StreamingService.Viewer.Forms
{
    public partial class ConnectionForm : Form
    {
        private TextBox _ipTextBox;
        private NumericUpDown _portNumeric;
        private Button _connectButton;
        private Button _cancelButton;

        public string ServerIP => _ipTextBox.Text;
        public int Port => (int)_portNumeric.Value;

        public ConnectionForm()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            var ipLabel = new Label
            {
                Text = "Server IP:",
                Location = new Point(10, 20),
                AutoSize = true
            };

            _ipTextBox = new TextBox
            {
                Location = new Point(10, 40),
                Width = 260,
                Text = "127.0.0.1"
            };

            var portLabel = new Label
            {
                Text = "Port:",
                Location = new Point(10, 70),
                AutoSize = true
            };

            _portNumeric = new NumericUpDown
            {
                Location = new Point(10, 90),
                Width = 260,
                Minimum = 1024,
                Maximum = 65535,
                Value = 8888
            };

            _connectButton = new Button
            {
                Text = "Connect",
                DialogResult = DialogResult.OK,
                Location = new Point(10, 120),
                Width = 120
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(140, 120),
                Width = 120
            };

            Controls.AddRange(new Control[]
            {
                ipLabel,
                _ipTextBox,
                portLabel,
                _portNumeric,
                _connectButton,
                _cancelButton
            });

            // Устанавливаем кнопку Cancel как кнопку отмены формы
            this.CancelButton = _cancelButton;
            // Устанавливаем кнопку Connect как кнопку по умолчанию
            this.AcceptButton = _connectButton;
        }
    }
}