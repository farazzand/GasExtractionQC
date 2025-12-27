using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using GasExtractionQC.Core;
using GasExtractionQC.Data;
using GasExtractionQC.Models;
using GasExtractionQC.Config;

namespace GasExtractionQC.UI
{
    public class MainForm : Form
    {
        private readonly IParameterDataSource _dataSource;
        private readonly DecisionEngine _decisionEngine;
        private readonly QCMonitor _qcMonitor;

        // UI Components
        private TabControl _tabControl;
        private MonitoringTab _monitoringTab;
        private ParameterConfigTab _parameterConfigTab;
        private HistoricalDataTab _historicalDataTab;

        public MainForm(IParameterDataSource dataSource, DecisionEngine decisionEngine, QCMonitor qcMonitor)
        {
            _dataSource = dataSource;
            _decisionEngine = decisionEngine;
            _qcMonitor = qcMonitor;

            InitializeUI();
            SetupDataSubscription();
        }

        private void InitializeUI()
        {
            // Window settings
            this.Text = "Gas Extraction QC Monitor";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1200, 700);

            // Tab control
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                BackColor = DarkTheme.Background,
                Padding = new Point(0, 0)
            };

            // Remove default margins to avoid light gaps around tabs
            _tabControl.Margin = new Padding(0);
            _tabControl.Padding = new Point(0, 0);
            this.Padding = new Padding(0);

            // Create tabs
            _monitoringTab = new MonitoringTab(_decisionEngine);
            _parameterConfigTab = new ParameterConfigTab(_qcMonitor);
            _historicalDataTab = new HistoricalDataTab(_dataSource);

            _tabControl.TabPages.Add(CreateTabPage("📊 Monitoring", _monitoringTab));
            _tabControl.TabPages.Add(CreateTabPage("⚙️ Parameters", _parameterConfigTab));
            _tabControl.TabPages.Add(CreateTabPage("📈 Historical Data", _historicalDataTab));

            this.Controls.Add(_tabControl);

            // Apply dark theme
            DarkTheme.ApplyTo(this);
        }

        private TabPage CreateTabPage(string title, Control content)
        {
            var page = new TabPage(title)
            {
                Padding = new Padding(10),
                BackColor = DarkTheme.Background
            };
            content.Dock = DockStyle.Fill;
            page.Controls.Add(content);
            return page;
        }

        private void SetupDataSubscription()
        {
            _dataSource.SubscribeToUpdates(OnDataUpdate);
        }

        private void OnDataUpdate(ParameterData data)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ParameterData>(OnDataUpdate), data);
                return;
            }

            try
            {
                var status = _decisionEngine.ProcessUpdate(data);
                _monitoringTab.UpdateStatus(status);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating UI: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _dataSource.Disconnect();
            base.OnFormClosing(e);
        }
    }
}