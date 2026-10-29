namespace WalkTimer;

internal sealed class ConfigurationForm : Form
{
    private readonly NumericUpDown seated;
    private readonly NumericUpDown walking;

    public ConfigurationForm(AppSettings settings, bool startsOnLogon, Action<AppSettings, bool> save)
    {
        Text = "Configure Walk Timer";
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        seated = DurationInput(settings.SeatedMinutes);
        walking = DurationInput(settings.WalkMinutes);
        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label { Text = "Max time seated (minutes)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(seated, 1, 0);
        layout.Controls.Add(new Label { Text = "Walk time (minutes)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(walking, 1, 1);

        var startup = new CheckBox
        {
            Text = "Start on user logon",
            Checked = startsOnLogon,
            AutoSize = true,
            Margin = new Padding(3, 12, 3, 3)
        };
        layout.Controls.Add(startup, 0, 2);
        layout.SetColumnSpan(startup, 2);
        var note = new Label
        {
            Text = "Saving keeps the current timer's elapsed time.",
            AutoSize = true,
            Margin = new Padding(3, 12, 3, 12)
        };
        layout.Controls.Add(note, 0, 3);
        layout.SetColumnSpan(note, 2);
        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        var saveButton = new Button { Text = "Save", AutoSize = true };
        saveButton.Click += (_, _) =>
        {
            try
            {
                save(new AppSettings((int)seated.Value, (int)walking.Value), startup.Checked);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                MessageBox.Show(this, $"Could not save all settings. Some changes may already be saved.\n\n{ex.Message}", "Walk Timer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(saveButton);
        layout.Controls.Add(buttons, 0, 4);
        layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
        AcceptButton = saveButton;
        CancelButton = cancel;
    }

    private static NumericUpDown DurationInput(int value) => new()
    {
        Minimum = 1,
        Maximum = AppSettings.MaximumMinutes,
        Value = value,
        Width = 90,
        Margin = new Padding(12, 4, 3, 4)
    };
}
