using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using GasExtractionQC.Core;
using GasExtractionQC.Config;

namespace GasExtractionQC.UI
{
    public class MonitoringTab : UserControl
    {
        private readonly DecisionEngine _decisionEngine;
        
        private Panel _statusPanel;
        private Label _statusLabel;
        private DataGridView _parametersGrid;
        private RichTextBox _recommendationsBox;

        public MonitoringTab(DecisionEngine decisionEngine)
        {
            _decisionEngine = decisionEngine;
            InitializeUI();
        }

        private void InitializeUI()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10, 10, 10, 10)
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

            // Status indicator
            _statusPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkTheme.StatusGreen,
                BorderStyle = BorderStyle.None
            };

            _statusLabel = new Label
            {
                Text = "● QC STATUS: GREEN",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            _statusPanel.Controls.Add(_statusLabel);

            // Parameters grid
            var parametersGroup = CreateGroupBox("Real-Time Parameters");
            _parametersGrid = CreateParametersGrid();
            parametersGroup.Controls.Add(_parametersGrid);

            // Recommendations
            var recommendationsGroup = CreateGroupBox("Diagnostics & Recommendations");
            _recommendationsBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                BackColor = DarkTheme.Surface,
                ForeColor = DarkTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                Text = "No issues detected. System operating normally."
            };
            recommendationsGroup.Controls.Add(_recommendationsBox);

            mainLayout.Controls.Add(_statusPanel, 0, 0);
            mainLayout.Controls.Add(parametersGroup, 0, 1);
            mainLayout.Controls.Add(recommendationsGroup, 0, 2);

            this.Controls.Add(mainLayout);
        }

        private GroupBox CreateGroupBox(string title)
        {
            return new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = DarkTheme.TextSecondary,
                Padding = new Padding(10)
            };
        }

        private DataGridView CreateParametersGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 35 }
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Parameter", HeaderText = "Parameter", Width = 200 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Current Value", Width = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Min", HeaderText = "Min", Width = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Max", HeaderText = "Max", Width = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 120 });

            // Style status column
            grid.Columns["Status"].DefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            grid.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            return grid;
        }

        public void UpdateStatus(SystemStatus status)
        {
            // Update status indicator
            switch (status.CurrentQC)
            {
                case QCStatus.GREEN:
                    _statusPanel.BackColor = DarkTheme.StatusGreen;
                    _statusLabel.Text = "● QC STATUS: GREEN";
                    _statusLabel.ForeColor = Color.White;
                    break;
                case QCStatus.YELLOW:
                    _statusPanel.BackColor = DarkTheme.StatusYellow;
                    _statusLabel.Text = "⚠ QC STATUS: YELLOW";
                    // Black text reads better on yellow background
                    _statusLabel.ForeColor = Color.Black;
                    break;
                case QCStatus.RED:
                    _statusPanel.BackColor = DarkTheme.StatusRed;
                    _statusLabel.Text = "✖ QC STATUS: RED";
                    _statusLabel.ForeColor = Color.White;
                    break;
            }

            // Update parameters grid
            _parametersGrid.Rows.Clear();
            foreach (var param in status.ParameterStatuses)
            {
                var ps = param.Value;
                string statusText = "●";
                Color statusColor = DarkTheme.StatusGray;

                if (ps.Available && ps.Status.HasValue)
                {
                    statusText = ps.Status.Value switch
                    {
                        QCStatus.GREEN => "● OK",
                        QCStatus.YELLOW => "⚠ WARNING",
                        QCStatus.RED => "✖ ALARM",
                        _ => "●"
                    };

                    statusColor = ps.Status.Value switch
                    {
                        QCStatus.GREEN => DarkTheme.StatusGreen,
                        QCStatus.YELLOW => DarkTheme.StatusYellow,
                        QCStatus.RED => DarkTheme.StatusRed,
                        _ => DarkTheme.StatusGray
                    };
                }
                else
                {
                    statusText = "N/A";
                }

                int rowIndex = _parametersGrid.Rows.Add(
                    Settings.Instance.Parameters[param.Key].DisplayName,
                    ps.Available ? $"{ps.Value:F2}" : "N/A",
                    ps.MinOk.HasValue ? $"{ps.MinOk:F2}" : "-",
                    ps.MaxOk.HasValue ? $"{ps.MaxOk:F2}" : "-",
                    statusText
                );

                _parametersGrid.Rows[rowIndex].Cells["Status"].Style.ForeColor = statusColor;
            }

            // Update recommendations
            UpdateRecommendations(status.Recommendations);
        }

        private void UpdateRecommendations(List<Recommendation> recommendations)
        {
            _recommendationsBox.Clear();

            if (recommendations.Count == 0)
            {
                _recommendationsBox.SelectionFont = new Font("Segoe UI", 11);
                _recommendationsBox.SelectionColor = DarkTheme.StatusGreen;
                _recommendationsBox.AppendText("✓ No issues detected. System operating normally.\n");
                return;
            }

            foreach (var rec in recommendations)
            {
                // Problem header
                _recommendationsBox.SelectionFont = new Font("Segoe UI", 12, FontStyle.Bold);
                _recommendationsBox.SelectionColor = DarkTheme.StatusRed;
                _recommendationsBox.AppendText($"⚠ PROBLEM DETECTED\n");

                _recommendationsBox.SelectionFont = new Font("Segoe UI", 11);
                _recommendationsBox.SelectionColor = DarkTheme.TextPrimary;
                _recommendationsBox.AppendText($"\n{rec.ProblemDescription}\n");
                _recommendationsBox.AppendText($"Confidence: {rec.Confidence:P0} | Rule: {rec.RuleName}\n\n");

                // Solutions
                _recommendationsBox.SelectionFont = new Font("Segoe UI", 11, FontStyle.Bold);
                _recommendationsBox.SelectionColor = DarkTheme.Accent;
                _recommendationsBox.AppendText("RECOMMENDED ACTIONS:\n\n");

                int actionNum = 1;
                foreach (var solution in rec.Solutions)
                {
                    _recommendationsBox.SelectionFont = new Font("Segoe UI", 10);
                    _recommendationsBox.SelectionColor = DarkTheme.TextPrimary;
                    _recommendationsBox.AppendText($"  {actionNum}. {solution.Action}\n");
                    _recommendationsBox.SelectionColor = DarkTheme.TextSecondary;
                    _recommendationsBox.AppendText($"     Estimated time: {solution.EstimatedTimeMinutes} minutes\n\n");
                    actionNum++;
                }

                _recommendationsBox.AppendText("\n────────────────────────────────────────\n\n");
            }
        }
    }
}