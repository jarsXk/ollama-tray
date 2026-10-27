using System.Globalization;

namespace OllamaTray;

internal sealed class SettingsForm : Form
{
    private readonly TextBox _modelsDirectoryTextBox = new();
    private readonly TextBox _hostTextBox = new();
    private readonly TextBox _contextLengthTextBox = new();
    private readonly TextBox _keepAliveTextBox = new();
    private readonly ComboBox _languageComboBox = new();
    private readonly CheckBox _startServerOnTrayLaunchCheckBox = new();
    private readonly CheckBox _stopServerOnTrayExitCheckBox = new();
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
        ClientSize = new Size(640, 500);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 11
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        for (var row = 3; row < 7; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        }
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        AddLanguageRow(layout, 0);
        AddOptionRow(layout, 1, "SettingsStartServerOnTrayLaunch", _startServerOnTrayLaunchCheckBox);
        AddOptionRow(layout, 2, "SettingsStopServerOnTrayExit", _stopServerOnTrayExitCheckBox);
        AddSettingRow(layout, 3, "SettingsModelsDirectory", "OLLAMA_MODELS", _modelsDirectoryTextBox);
        AddSettingRow(layout, 4, "SettingsHost", "OLLAMA_HOST", _hostTextBox);
        AddSettingRow(layout, 5, "SettingsContextLength", "OLLAMA_CONTEXT_LENGTH", _contextLengthTextBox);
        AddSettingRow(layout, 6, "SettingsKeepAlive", "OLLAMA_KEEP_ALIVE", _keepAliveTextBox);

        _applyNoticeLabel.Text = LocalizedText.Get("SettingsChangesApplyOnStart");
        _applyNoticeLabel.Dock = DockStyle.Fill;
        _applyNoticeLabel.TextAlign = ContentAlignment.MiddleLeft;
        layout.Controls.Add(_applyNoticeLabel, 0, 8);
        layout.SetColumnSpan(_applyNoticeLabel, 2);

        _validationLabel.Dock = DockStyle.Fill;
        _validationLabel.ForeColor = Color.Firebrick;
        layout.Controls.Add(_validationLabel, 0, 9);
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
        layout.Controls.Add(buttons, 0, 10);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        _modelsDirectoryTextBox.Text = Settings.ModelsDirectory;
        _hostTextBox.Text = Settings.Host;
        _contextLengthTextBox.Text = Settings.ContextLength.ToString(CultureInfo.InvariantCulture);
        _keepAliveTextBox.Text = Settings.KeepAlive;
        _startServerOnTrayLaunchCheckBox.Checked = Settings.StartServerOnTrayLaunch;
        _stopServerOnTrayExitCheckBox.Checked = Settings.StopServerOnTrayExit;
        _languageComboBox.SelectedIndex = (int)Settings.Language;
    }

    private void AddLanguageRow(TableLayoutPanel layout, int row)
    {
        var label = new Label
        {
            Text = LocalizedText.Get("SettingsLanguage"),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _languageComboBox.Dock = DockStyle.Fill;
        _languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageComboBox.Items.AddRange(
        [
            LocalizedText.Get("LanguageSystem"),
            LocalizedText.Get("LanguageEnglish"),
            LocalizedText.Get("LanguageRussian")
        ]);
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(_languageComboBox, 1, row);
    }

    private static void AddOptionRow(TableLayoutPanel layout, int row, string labelResource, CheckBox checkBox)
    {
        checkBox.Text = LocalizedText.Get(labelResource);
        checkBox.Dock = DockStyle.Fill;
        checkBox.AutoSize = false;
        layout.Controls.Add(checkBox, 0, row);
        layout.SetColumnSpan(checkBox, 2);
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
            KeepAlive = _keepAliveTextBox.Text.Trim(),
            StartServerOnTrayLaunch = _startServerOnTrayLaunchCheckBox.Checked,
            StopServerOnTrayExit = _stopServerOnTrayExitCheckBox.Checked,
            Language = (ApplicationLanguage)_languageComboBox.SelectedIndex
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}
