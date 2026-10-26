using System.Globalization;

namespace OllamaTray;

internal sealed class SettingsForm : Form
{
    private readonly TextBox _modelsDirectoryTextBox = new();
    private readonly TextBox _hostTextBox = new();
    private readonly TextBox _contextLengthTextBox = new();
    private readonly TextBox _keepAliveTextBox = new();
    private readonly Label _applyNoticeLabel = new();
    private readonly Label _validationLabel = new();

    public OllamaSettings Settings { get; private set; }

    public SettingsForm(OllamaSettings settings)
    {
        Settings = settings.Copy();

        Text = LocalizedText.Get("SettingsTitle");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(620, 370);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 8
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < 4; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        }
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        AddSettingRow(layout, 0, "SettingsModelsDirectory", "OLLAMA_MODELS", _modelsDirectoryTextBox);
        AddSettingRow(layout, 1, "SettingsHost", "OLLAMA_HOST", _hostTextBox);
        AddSettingRow(layout, 2, "SettingsContextLength", "OLLAMA_CONTEXT_LENGTH", _contextLengthTextBox);
        AddSettingRow(layout, 3, "SettingsKeepAlive", "OLLAMA_KEEP_ALIVE", _keepAliveTextBox);

        _applyNoticeLabel.Text = LocalizedText.Get("SettingsChangesApplyOnStart");
        _applyNoticeLabel.Dock = DockStyle.Fill;
        _applyNoticeLabel.TextAlign = ContentAlignment.MiddleLeft;
        layout.Controls.Add(_applyNoticeLabel, 0, 5);
        layout.SetColumnSpan(_applyNoticeLabel, 2);

        _validationLabel.Dock = DockStyle.Fill;
        _validationLabel.ForeColor = Color.Firebrick;
        layout.Controls.Add(_validationLabel, 0, 6);
        layout.SetColumnSpan(_validationLabel, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        var saveButton = new Button
        {
            Text = LocalizedText.Get("SettingsSave"),
            AutoSize = true
        };
        saveButton.Click += SaveSettings;
        var cancelButton = new Button
        {
            Text = LocalizedText.Get("SettingsCancel"),
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        layout.Controls.Add(buttons, 0, 7);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        _modelsDirectoryTextBox.Text = Settings.ModelsDirectory;
        _hostTextBox.Text = Settings.Host;
        _contextLengthTextBox.Text = Settings.ContextLength.ToString(CultureInfo.InvariantCulture);
        _keepAliveTextBox.Text = Settings.KeepAlive;
    }

    private static void AddSettingRow(
        TableLayoutPanel layout,
        int row,
        string labelResource,
        string environmentVariable,
        TextBox textBox)
    {
        var label = new Label
        {
            Text = $"{LocalizedText.Get(labelResource)}{Environment.NewLine}{environmentVariable}",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        textBox.Dock = DockStyle.Fill;
        textBox.Margin = new Padding(3, 8, 3, 8);
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(textBox, 1, row);
    }

    private void SaveSettings(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_modelsDirectoryTextBox.Text))
        {
            _validationLabel.Text = LocalizedText.Get("ErrorModelsDirectoryRequired");
            _modelsDirectoryTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_hostTextBox.Text))
        {
            _validationLabel.Text = LocalizedText.Get("ErrorHostRequired");
            _hostTextBox.Focus();
            return;
        }

        if (!OllamaServerController.TryCreateApiBaseUri(_hostTextBox.Text, out _))
        {
            _validationLabel.Text = LocalizedText.Get("ErrorHostInvalid");
            _hostTextBox.Focus();
            _hostTextBox.SelectAll();
            return;
        }

        if (!int.TryParse(
                _contextLengthTextBox.Text,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var contextLength) ||
            contextLength <= 0)
        {
            _validationLabel.Text = LocalizedText.Get("ErrorContextLengthInvalid");
            _contextLengthTextBox.Focus();
            _contextLengthTextBox.SelectAll();
            return;
        }

        if (string.IsNullOrWhiteSpace(_keepAliveTextBox.Text))
        {
            _validationLabel.Text = LocalizedText.Get("ErrorKeepAliveRequired");
            _keepAliveTextBox.Focus();
            return;
        }

        Settings = new OllamaSettings
        {
            ModelsDirectory = _modelsDirectoryTextBox.Text.Trim(),
            Host = _hostTextBox.Text.Trim(),
            ContextLength = contextLength,
            KeepAlive = _keepAliveTextBox.Text.Trim()
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}
