using System;
using System.Windows.Forms;
using System.IO;
using GasExtractionQC.Config;
using GasExtractionQC.Data;
using GasExtractionQC.Core;
using GasExtractionQC.UI;

namespace GasExtractionQC
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var settings = Config.Settings.Instance;

            Console.WriteLine("=== Gas Extraction QC Monitor ===");
            Console.WriteLine($"Root: {settings.RootDir}");
            Console.WriteLine($"Config: {settings.ConfigDir}");

            // Check config files
            string thresholdsPath = Path.Combine(settings.ConfigDir, "thresholds.yaml");
            string rulesPath = Path.Combine(settings.ConfigDir, "rules.yaml");

            if (!File.Exists(thresholdsPath) || !File.Exists(rulesPath))
            {
                MessageBox.Show("Missing config files!\n\nPlease create:\n- thresholds.yaml\n- rules.yaml\n\nin Config folder",
                    "Config Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Ask for data file
            string dataFilePath = "";
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select CSV Data File";
                openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                openFileDialog.InitialDirectory = Path.Combine(settings.RootDir, "Data");

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    dataFilePath = openFileDialog.FileName;
                }
                else
                {
                    MessageBox.Show("No data file selected. Exiting.",
                        "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                // Initialize components
                var dataSource = new FileDataSource(dataFilePath, playbackSpeed: 1.0f);
                
                if (!dataSource.Connect())
                {
                    MessageBox.Show("Failed to connect to data source",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var qcMonitor = new QCMonitor(thresholdsPath);
                var ruleEngine = new RuleEngine(rulesPath);
                var decisionEngine = new DecisionEngine(qcMonitor, ruleEngine);

                // Launch UI
                var mainForm = new MainForm(dataSource, decisionEngine, qcMonitor);
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}