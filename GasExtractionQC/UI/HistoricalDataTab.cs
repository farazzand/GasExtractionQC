using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using GasExtractionQC.Data;
using GasExtractionQC.Config;
using GasExtractionQC.Models;

namespace GasExtractionQC.UI
{
    public class HistoricalDataTab : UserControl
    {
        private readonly IParameterDataSource _dataSource;
        
        private CheckedListBox _parameterListBox;
        private DateTimePicker _startTimePicker;
        private DateTimePicker _endTimePicker;
        private Button _loadDataButton;
        private Panel _chartsPanel;
        private List<Chart> _charts = new();
        private CheckBox _autoScaleCheckBox;

        // Live mode controls
        private CheckBox _liveModeCheckBox;
        private ComboBox _timeframeComboBox;
        private Timer _liveTimer; 

        public HistoricalDataTab(IParameterDataSource dataSource)
        {
            _dataSource = dataSource;
            InitializeUI();
        }

        private void InitializeUI()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Left panel - Controls
            var controlsPanel = CreateControlsPanel();
            
            // Right panel - Charts
            _chartsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = DarkTheme.Background
            };

            mainLayout.Controls.Add(controlsPanel, 0, 0);
            mainLayout.Controls.Add(_chartsPanel, 1, 0);

            this.Controls.Add(mainLayout);
        }

        private Panel CreateControlsPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkTheme.Surface,
                Padding = new Padding(10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(5)
            };

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Title
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // Parameter list
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Time range label
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Start time
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // End time
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

            // Title
            var titleLabel = new Label
            {
                Text = "Historical Data Viewer",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = DarkTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 30
            };

            // Parameter selection
            var paramLabel = new Label
            {
                Text = "Select Parameters (Max 3):",
                Font = new Font("Segoe UI", 10),
                ForeColor = DarkTheme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 25
            };

            _parameterListBox = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                CheckOnClick = true
            };

            _parameterListBox.ItemCheck += ParameterListBox_ItemCheck;

            // Load parameters
            var settings = Settings.Instance;
            foreach (var param in settings.Parameters.Where(p => p.Value.Enabled))
            {
                _parameterListBox.Items.Add(param.Value.DisplayName, false);
            }

            // Time range
            var timeRangeLabel = new Label
            {
                Text = "Time Range:",
                Font = new Font("Segoe UI", 10),
                ForeColor = DarkTheme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 25
            };

            var startLabel = new Label
            {
                Text = "Start:",
                Font = new Font("Segoe UI", 9),
                ForeColor = DarkTheme.TextSecondary,
                Height = 20
            };

            _startTimePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm:ss",
                Value = DateTime.Now.AddHours(-1),
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9)
            };

            var endLabel = new Label
            {
                Text = "End:",
                Font = new Font("Segoe UI", 9),
                ForeColor = DarkTheme.TextSecondary,
                Height = 20,
                Margin = new Padding(0, 5, 0, 0)
            };

            _endTimePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm:ss",
                Value = DateTime.Now,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9)
            };

            // Auto-scale checkbox
            _autoScaleCheckBox = new CheckBox
            {
                Text = "Auto-scale Y axis",
                Checked = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = DarkTheme.TextPrimary,
                Height = 25,
                Margin = new Padding(0, 10, 0, 0)
            };

            // Load button
            _loadDataButton = new Button
            {
                Text = "📊 Load & Visualize",
                Height = 40,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = DarkTheme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 10, 0, 0)
            };

            _loadDataButton.Click += LoadDataButton_Click;

            // Add to layout
            var paramPanel = new Panel { Dock = DockStyle.Fill };
            paramPanel.Controls.Add(_parameterListBox);
            paramPanel.Controls.Add(paramLabel);

            var timePanel = new Panel { Dock = DockStyle.Fill, AutoSize = true };
            var timeLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, AutoSize = true };
            timeLayout.Controls.Add(startLabel);
            timeLayout.Controls.Add(_startTimePicker);
            timeLayout.Controls.Add(endLabel);
            timeLayout.Controls.Add(_endTimePicker);
            timePanel.Controls.Add(timeLayout);

            var buttonPanel = new Panel { Dock = DockStyle.Fill, AutoSize = true };
            var buttonLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, AutoSize = true };
            buttonLayout.Controls.Add(_liveModeCheckBox);
            buttonLayout.Controls.Add(_timeframeComboBox);
            buttonLayout.Controls.Add(_autoScaleCheckBox);
            buttonLayout.Controls.Add(_loadDataButton);
            buttonPanel.Controls.Add(buttonLayout);

            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(paramPanel, 0, 1);
            layout.Controls.Add(timeRangeLabel, 0, 2);
            layout.Controls.Add(timePanel, 0, 3);
            layout.Controls.Add(buttonPanel, 0, 5);

            panel.Controls.Add(layout);
            DarkTheme.ApplyTo(panel);

            return panel;
        }

        private void ParameterListBox_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            // Limit to 3 selections
            int checkedCount = _parameterListBox.CheckedItems.Count;
            
            if (e.NewValue == CheckState.Checked && checkedCount >= 3)
            {
                e.NewValue = CheckState.Unchecked;
                MessageBox.Show("You can select a maximum of 3 parameters.", 
                    "Selection Limit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LoadDataButton_Click(object? sender, EventArgs e)
        {
            if (_parameterListBox.CheckedItems.Count == 0)
            {
                MessageBox.Show("Please select at least one parameter.", 
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Get selected parameters
                var selectedParams = new List<string>();
                var settings = Settings.Instance;
                
                foreach (var item in _parameterListBox.CheckedItems)
                {
                    string displayName = item.ToString() ?? "";
                    var param = settings.Parameters.FirstOrDefault(p => p.Value.DisplayName == displayName);
                    if (!string.IsNullOrEmpty(param.Key))
                    {
                        selectedParams.Add(param.Key);
                    }
                }

                // Get historical data
                var historicalData = _dataSource.GetHistoricalRange(_startTimePicker.Value, _endTimePicker.Value);

                if (historicalData.Count == 0)
                {
                    MessageBox.Show("No data available for the selected time range.", 
                        "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Clear existing charts
                _chartsPanel.Controls.Clear();
                _charts.Clear();

                // Create charts
                int chartHeight = Math.Max(250, (_chartsPanel.Height - 20) / selectedParams.Count);

                foreach (var paramName in selectedParams)
                {
                    var chart = CreateChart(paramName, historicalData, chartHeight);
                    _charts.Add(chart);
                    _chartsPanel.Controls.Add(chart);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data:\n\n{ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Chart CreateChart(string parameterName, List<ParameterData> data, int height)
        {
            var settings = Settings.Instance;
            var paramConfig = settings.Parameters[parameterName];

            var chart = new Chart
            {
                Height = height,
                Dock = DockStyle.Top,
                BackColor = DarkTheme.Surface
            };

            // Chart area
            var chartArea = new ChartArea
            {
                Name = "MainArea",
                BackColor = DarkTheme.Surface,
                BorderColor = DarkTheme.Border,
                BorderWidth = 1
            };

            // Axis styling
            chartArea.AxisX.LabelStyle.ForeColor = DarkTheme.TextSecondary;
            chartArea.AxisX.LineColor = DarkTheme.Border;
            chartArea.AxisX.MajorGrid.LineColor = DarkTheme.Border;
            chartArea.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            chartArea.AxisX.Title = "Time";
            chartArea.AxisX.TitleForeColor = DarkTheme.TextPrimary;

            chartArea.AxisY.LabelStyle.ForeColor = DarkTheme.TextSecondary;
            chartArea.AxisY.LineColor = DarkTheme.Border;
            chartArea.AxisY.MajorGrid.LineColor = DarkTheme.Border;
            chartArea.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            chartArea.AxisY.Title = paramConfig.DisplayName;
            chartArea.AxisY.TitleForeColor = DarkTheme.TextPrimary;

            if (!_autoScaleCheckBox.Checked)
            {
                chartArea.AxisY.Minimum = paramConfig.DisplayMin;
                chartArea.AxisY.Maximum = paramConfig.DisplayMax;
            }

            chart.ChartAreas.Add(chartArea);

            // Series
            var series = new Series
            {
                Name = parameterName,
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                Color = DarkTheme.Accent
            };

            // Add data points
            foreach (var point in data)
            {
                float value = point.GetValue(parameterName);
                if (!float.IsNaN(value) && Math.Abs(value - settings.MissingValue) > 0.01f)
                {
                    series.Points.AddXY(point.Timestamp, value);
                }
            }

            chart.Series.Add(series);

            // Legend
            var legend = new Legend
            {
                Name = "Legend",
                Docking = Docking.Top,
                BackColor = Color.Transparent,
                ForeColor = DarkTheme.TextPrimary
            };
            chart.Legends.Add(legend);

            // Title
            var title = new Title
            {
                Text = paramConfig.DisplayName,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = DarkTheme.TextPrimary
            };
            chart.Titles.Add(title);

            return chart;
        }

        // --- Live mode helpers ---
        private void LiveModeCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (_liveModeCheckBox.Checked)
            {
                _startTimePicker.Enabled = false;
                _endTimePicker.Enabled = false;
                StartLiveMode();
            }
            else
            {
                StopLiveMode();
                _startTimePicker.Enabled = true;
                _endTimePicker.Enabled = true;
            }
        }

        private void StartLiveMode()
        {
            StopLiveMode();
            _liveTimer = new Timer();
            _liveTimer.Tick += LiveTimer_Tick;
            _liveTimer.Interval = GetLiveIntervalMs();
            _liveTimer.Start();
            FetchAndRenderLiveData();
        }

        private void StopLiveMode()
        {
            if (_liveTimer != null)
            {
                _liveTimer.Stop();
                _liveTimer.Tick -= LiveTimer_Tick;
                _liveTimer.Dispose();
                _liveTimer = null;
            }
        }

        private int GetLiveIntervalMs()
        {
            var sel = _timeframeComboBox.SelectedItem?.ToString() ?? "1m";
            return sel switch
            {
                "30s" => 30_000,
                "1m" => 60_000,
                "5m" => 5 * 60_000,
                "15m" => 15 * 60_000,
                "1h" => 60 * 60_000,
                _ => 60_000
            };
        }

        private TimeSpan GetTimeSpanFromSelection()
        {
            var sel = _timeframeComboBox.SelectedItem?.ToString() ?? "1m";
            return sel switch
            {
                "30s" => TimeSpan.FromSeconds(30),
                "1m" => TimeSpan.FromMinutes(1),
                "5m" => TimeSpan.FromMinutes(5),
                "15m" => TimeSpan.FromMinutes(15),
                "1h" => TimeSpan.FromHours(1),
                _ => TimeSpan.FromMinutes(1)
            };
        }

        private void LiveTimer_Tick(object? sender, EventArgs e)
        {
            if (_liveTimer != null)
            {
                _liveTimer.Interval = GetLiveIntervalMs();
            }

            FetchAndRenderLiveData();
        }

        private void FetchAndRenderLiveData()
        {
            try
            {
                if (_parameterListBox.CheckedItems.Count == 0)
                {
                    MessageBox.Show("Please select at least one parameter.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _liveModeCheckBox.Checked = false;
                    return;
                }

                var selectedParams = new List<string>();
                var settings = Settings.Instance;
                foreach (var item in _parameterListBox.CheckedItems)
                {
                    string displayName = item.ToString() ?? "";
                    var param = settings.Parameters.FirstOrDefault(p => p.Value.DisplayName == displayName);
                    if (!string.IsNullOrEmpty(param.Key))
                    {
                        selectedParams.Add(param.Key);
                    }
                }

                var end = DateTime.Now;
                var span = GetTimeSpanFromSelection();
                var historicalData = _dataSource.GetHistoricalRange(end - span, end);

                if (historicalData.Count == 0)
                {
                    // nothing to render yet
                    return;
                }

                // Clear existing charts and redraw for live window
                _chartsPanel.Controls.Clear();
                _charts.Clear();

                int chartHeight = Math.Max(250, (_chartsPanel.Height - 20) / selectedParams.Count);
                foreach (var paramName in selectedParams)
                {
                    var chart = CreateChart(paramName, historicalData, chartHeight);
                    _charts.Add(chart);
                    _chartsPanel.Controls.Add(chart);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            StopLiveMode();
            base.OnHandleDestroyed(e);
        }
    }
}