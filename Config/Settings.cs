using System;
using System.Collections.Generic;
using System.IO;

namespace GasExtractionQC.Config
{
    public class Settings
    {
        // Singleton pattern - only one instance exists
        private static Settings? _instance;
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Settings();
                return _instance;
            }
        }

        private Settings()
        {
            // Initialize paths relative to executable
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            RootDir = Directory.GetParent(baseDir)?.Parent?.Parent?.FullName ?? baseDir;
            
            ConfigDir = Path.Combine(RootDir, "Config");
            DataDir = Path.Combine(RootDir, "Data");
            LogsDir = Path.Combine(RootDir, "Logs");
            AuditDir = Path.Combine(DataDir, "Audit");
            
            // Create directories if they don't exist
            Directory.CreateDirectory(LogsDir);
            Directory.CreateDirectory(AuditDir);
        }

        // ===== PATHS =====
        public string RootDir { get; }
        public string ConfigDir { get; }
        public string DataDir { get; }
        public string LogsDir { get; }
        public string AuditDir { get; }

        // ===== DATA SOURCE =====
        public string DataSourceType { get; set; } = "file"; // "file" or "sql"
        public string DataFilePath { get; set; } = "";
        public float PlaybackSpeed { get; set; } = 1.0f;

        // ===== SQL SETTINGS =====
        public string SqlConnectionString { get; set; } = "";
        public string SqlTableName { get; set; } = "parameter_data";
        public int SqlPollInterval { get; set; } = 1; // seconds

        // ===== PARAMETERS =====
        public Dictionary<string, ParameterConfig> Parameters { get; set; } = new()
        {
            { "mud_level", new ParameterConfig
                {
                    SourceColumn = "MudLevelOUT (cm3/min)",
                    DisplayName = "Mud Level",
                    Enabled = true,
                    DisplayMin = -1,
                    DisplayMax = 2
                }
            },
            { "tdegasser", new ParameterConfig
                {
                    SourceColumn = "TdegasserOUT (degC)",
                    DisplayName = "T Degasser",
                    Enabled = true,
                    DisplayMin = 0,
                    DisplayMax = 150
                }
            },
            { "qmud", new ParameterConfig
                {
                    SourceColumn = "QmudOUT (cm3/min)",
                    DisplayName = "Q Mud",
                    Enabled = true,
                    DisplayMin = 0,
                    DisplayMax = 400
                }
            },
            { "pgasline", new ParameterConfig
                {
                    SourceColumn = "PgaslineOUT (mbar)",
                    DisplayName = "P Gas Line",
                    Enabled = true,
                    DisplayMin = 0,
                    DisplayMax = 400
                }
            },
            { "ppump", new ParameterConfig
                {
                    SourceColumn = "PpumpOUT (mbar)",
                    DisplayName = "P Pump",
                    Enabled = true,
                    DisplayMin = 0,
                    DisplayMax = 150
                }
            },
            { "qpump", new ParameterConfig
                {
                    SourceColumn = "QpumpOUT (cm3/min)",
                    DisplayName = "Q Pump",
                    Enabled = true,
                    DisplayMin = 0,
                    DisplayMax = 700
                }
            }
        };

        // ===== TIMESTAMP CONFIG =====
        public TimestampConfig Timestamp { get; set; } = new()
        {
            SourceColumn = "Time(Sec)",
            Format = "seconds",
            Unit = "s"
        };

        // ===== MONITORING =====
        public float MissingValue { get; set; } = -999.25f;
        public int UiUpdateInterval { get; set; } = 2000; // milliseconds
        
        // ===== LOGGING =====
        public string LogLevel { get; set; } = "Information"; // Debug, Information, Warning, Error
        public string LogFilePath => Path.Combine(LogsDir, "app.log");
    }

    public class ParameterConfig
    {
        public string SourceColumn { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public float DisplayMin { get; set; } = 0;
        public float DisplayMax { get; set; } = 100;
    }

    public class TimestampConfig
    {
        public string SourceColumn { get; set; } = "";
        public string Format { get; set; } = "seconds"; // "seconds", "datetime", "custom"
        public string? CustomFormat { get; set; } = null;
        public string Unit { get; set; } = "s"; // "s" or "ms"
    }
}