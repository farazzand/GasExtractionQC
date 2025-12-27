using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using GasExtractionQC.Core;
using GasExtractionQC.Config;

namespace GasExtractionQC.UI
{
    public class ParameterConfigTab : UserControl
    {
        private readonly QCMonitor _qcMonitor;
        
        private DataGridView _parametersGrid;
        private Button _addParameterButton;
        private Button _saveButton;
        private Button _deleteParameterButton;

        public ParameterConfigTab(QCMonitor qcMonitor)
        {
            _qcMonitor = qcMonitor;
            InitializeUI();
            LoadParameters();
        }

        private void InitializeUI()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            // Parameters grid
            var parametersGroup = new GroupBox
            {
                Text = "Parameter Configuration",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = DarkTheme.TextSecondary,
                Padding = new Padding(10)
            };

            _parametersGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 35 }
            };

            // Define columns
            _parametersGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "InternalName", 
                HeaderText = "Internal Name",
                ReadOnly = true,
                Width = 150
            });

            _parametersGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "DisplayName", 
                HeaderText = "Display Name",
                Width = 150
            });

            _parametersGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "SourceColumn", 
                HeaderText = "Source Column",
                Width = 180
            });

            _parametersGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Min", 
                HeaderText = "Min Threshold",
                Width = 120
            });

            _parametersGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Max", 
                HeaderText = "Max Threshold",
                Width = 120
            });

            _parametersGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "WarningMargin", 
                HeaderText = "Warning Margin",
                Width = 130
            });

            _parametersGrid.Columns.Add(new DataGridViewCheckBoxColumn 
            { 
                Name = "Enabled", 
                HeaderText = "Enabled",
                Width = 80
            });

            parametersGroup.Controls.Add(_parametersGrid);

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10)
            };

            _addParameterButton = CreateButton("➕ Add Parameter", AddParameter_Click);
            _deleteParameterButton = CreateButton("🗑️ Delete Selected", DeleteParameter_Click);
            _saveButton = CreateButton("💾 Save Changes", SaveChanges_Click);
            _saveButton.BackColor = DarkTheme.Accent;

            buttonPanel.Controls.Add(_addParameterButton);
            buttonPanel.Controls.Add(_deleteParameterButton);
            buttonPanel.Controls.Add(_saveButton);

            mainLayout.Controls.Add(parametersGroup, 0, 0);
            mainLayout.Controls.Add(buttonPanel, 0, 1);

            this.Controls.Add(mainLayout);

            DarkTheme.ApplyTo(this);
        }

        private Button CreateButton(string text, EventHandler clickHandler)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(180, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(5)
            };
            button.Click += clickHandler;
            return button;
        }

        private void LoadParameters()
        {
            _parametersGrid.Rows.Clear();
            var settings = Settings.Instance;

            foreach (var param in settings.Parameters)
            {
                var config = param.Value;
                
                // Get threshold if exists
                var thresholds = _qcMonitor.GetType()
                    .GetField("_thresholds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .GetValue(_qcMonitor) as Dictionary<string, ThresholdConfig>;

                float min = 0, max = 100, warningMargin = 0.1f;
                
                if (thresholds != null && thresholds.ContainsKey(param.Key))
                {
                    var threshold = thresholds[param.Key];
                    min = threshold.Min;
                    max = threshold.Max;
                    warningMargin = threshold.WarningMargin;
                }

                _parametersGrid.Rows.Add(
                    param.Key,
                    config.DisplayName,
                    config.SourceColumn,
                    min,
                    max,
                    warningMargin,
                    config.Enabled
                );
            }
        }

        private void AddParameter_Click(object? sender, EventArgs e)
        {
            using (var dialog = new AddParameterDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var paramData = dialog.GetParameterData();
                    
                    _parametersGrid.Rows.Add(
                        paramData["InternalName"],
                        paramData["DisplayName"],
                        paramData["SourceColumn"],
                        paramData["Min"],
                        paramData["Max"],
                        paramData["WarningMargin"],
                        true
                    );

                    MessageBox.Show(
                        "Parameter added to grid.\n\nClick 'Save Changes' to persist.",
                        "Parameter Added",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
        }

        private void DeleteParameter_Click(object? sender, EventArgs e)
        {
            if (_parametersGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a parameter to delete.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Are you sure you want to delete this parameter?\n\nThis will take effect after saving.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                _parametersGrid.Rows.Remove(_parametersGrid.SelectedRows[0]);
            }
        }

        private void SaveChanges_Click(object? sender, EventArgs e)
        {
            try
            {
                var settings = Settings.Instance;
                var newParameters = new Dictionary<string, ParameterConfig>();
                var newThresholds = new Dictionary<string, ThresholdConfig>();

                foreach (DataGridViewRow row in _parametersGrid.Rows)
                {
                    if (row.IsNewRow) continue;

                    string internalName = row.Cells["InternalName"].Value?.ToString() ?? "";
                    string displayName = row.Cells["DisplayName"].Value?.ToString() ?? "";
                    string sourceColumn = row.Cells["SourceColumn"].Value?.ToString() ?? "";
                    
                    float.TryParse(row.Cells["Min"].Value?.ToString(), out float min);
                    float.TryParse(row.Cells["Max"].Value?.ToString(), out float max);
                    float.TryParse(row.Cells["WarningMargin"].Value?.ToString(), out float warningMargin);
                    
                    bool enabled = row.Cells["Enabled"].Value is bool b && b;

                    // Add to parameters
                    newParameters[internalName] = new ParameterConfig
                    {
                        SourceColumn = sourceColumn,
                        DisplayName = displayName,
                        Enabled = enabled,
                        DisplayMin = 0,
                        DisplayMax = max * 1.2f // Auto-calculate display range
                    };

                    // Add to thresholds
                    newThresholds[internalName] = new ThresholdConfig
                    {
                        DisplayName = displayName,
                        Min = min,
                        Max = max,
                        WarningMargin = warningMargin,
                        Unit = ""
                    };
                }

                // Update settings
                settings.Parameters = newParameters;

                // Save thresholds to file
                _qcMonitor.GetType()
                    .GetMethod("SetThresholds", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?
                    .Invoke(_qcMonitor, new object[] { newThresholds });

                MessageBox.Show(
                    "Configuration saved successfully!\n\nRestart the application for all changes to take effect.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving configuration:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }

    // Add Parameter Dialog
    public class AddParameterDialog : Form
    {
        private TextBox _internalNameBox;
        private TextBox _displayNameBox;
        private TextBox _sourceColumnBox;
        private NumericUpDown _minBox;
        private NumericUpDown _maxBox;
        private NumericUpDown _warningMarginBox;
        private Button _okButton;
        private Button _cancelButton;

        public AddParameterDialog()
        {
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            this.Text = "Add New Parameter";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(20)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Internal Name
            layout.Controls.Add(CreateLabel("Internal Name:"), 0, 0);
            _internalNameBox = CreateTextBox("e.g., new_parameter");
            layout.Controls.Add(_internalNameBox, 1, 0);

            // Display Name
            layout.Controls.Add(CreateLabel("Display Name:"), 0, 1);
            _displayNameBox = CreateTextBox("e.g., New Parameter");
            layout.Controls.Add(_displayNameBox, 1, 1);

            // Source Column
            layout.Controls.Add(CreateLabel("Source Column:"), 0, 2);
            _sourceColumnBox = CreateTextBox("e.g., NewParamOUT (unit)");
            layout.Controls.Add(_sourceColumnBox, 1, 2);

            // Autocomplete suggestions from existing configuration
            var suggestions = Settings.Instance.Parameters
                .SelectMany(p => new[] { p.Key, p.Value.DisplayName, p.Value.SourceColumn })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToArray();

            var ac = new AutoCompleteStringCollection();
            ac.AddRange(suggestions);

            _internalNameBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            _internalNameBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            _internalNameBox.AutoCompleteCustomSource = ac;

            _displayNameBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            _displayNameBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            _displayNameBox.AutoCompleteCustomSource = ac;

            _sourceColumnBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            _sourceColumnBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            _sourceColumnBox.AutoCompleteCustomSource = ac;

            // Min Threshold
            layout.Controls.Add(CreateLabel("Min Threshold:"), 0, 3);
            _minBox = CreateNumericUpDown(0, 10000, 0);
            layout.Controls.Add(_minBox, 1, 3);

            // Max Threshold
            layout.Controls.Add(CreateLabel("Max Threshold:"), 0, 4);
            _maxBox = CreateNumericUpDown(0, 10000, 100);
            layout.Controls.Add(_maxBox, 1, 4);

            // Warning Margin
            layout.Controls.Add(CreateLabel("Warning Margin:"), 0, 5);
            _warningMarginBox = CreateNumericUpDown(0, 1, 0.1m, 0.01m);
            layout.Controls.Add(_warningMarginBox, 1, 5);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill
            };

            _okButton = new Button { Text = "Add", Size = new Size(100, 35), DialogResult = DialogResult.OK };
            _cancelButton = new Button { Text = "Cancel", Size = new Size(100, 35), DialogResult = DialogResult.Cancel };

            _okButton.Click += (s, e) => ValidateAndClose();

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_okButton);

            layout.Controls.Add(buttonPanel, 1, 6);

            this.Controls.Add(layout);
            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;

            DarkTheme.ApplyTo(this);
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = new Font("Segoe UI", 10)
            };
        }

        private TextBox CreateTextBox(string placeholder)
        {
            return new TextBox
            {
                PlaceholderText = placeholder,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };
        }

        private NumericUpDown CreateNumericUpDown(decimal min, decimal max, decimal value, decimal increment = 1)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                Increment = increment,
                DecimalPlaces = increment < 1 ? 2 : 0,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };
        }

        private void ValidateAndClose()
        {
            if (string.IsNullOrWhiteSpace(_internalNameBox.Text))
            {
                MessageBox.Show("Internal name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(_displayNameBox.Text))
            {
                MessageBox.Show("Display name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(_sourceColumnBox.Text))
            {
                MessageBox.Show("Source column is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (_minBox.Value >= _maxBox.Value)
            {
                MessageBox.Show("Max threshold must be greater than min threshold.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }
        }

        public Dictionary<string, object> GetParameterData()
        {
            return new Dictionary<string, object>
            {
                { "InternalName", _internalNameBox.Text.Trim() },
                { "DisplayName", _displayNameBox.Text.Trim() },
                { "SourceColumn", _sourceColumnBox.Text.Trim() },
                { "Min", (float)_minBox.Value },
                { "Max", (float)_maxBox.Value },
                { "WarningMargin", (float)_warningMarginBox.Value }
            };
        }
    }
}