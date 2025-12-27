using System;
using System.Collections.Generic;
using System.IO;

namespace GasExtractionQC.Config
{
    public class Settings
    {
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
            // Get the directory where the .exe is running
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // During development (dotnet run), we need to go up to project root
            // During production (.exe), configs should be next to executable
            
            // Check if we're in bin/Debug/net8.0-windows (development)
            if (baseDir.Contains(@"bin\Debug") || baseDir.Contains(@"bin\Release"))
            {
                // Go up to project root: bin/Debug/net8.0-windows -> project root
                RootDir = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName ?? baseDir;
            }
            else
            {
                // Production: configs are in same directory as exe
                RootDir = baseDir;
            }
            
            ConfigDir = Path.Combine(RootDir, "Config");
            DataDir = Path.Combine(RootDir, "Data");
            LogsDir = Path.Combine(RootDir, "Logs");
            AuditDir = Path.Combine(DataDir, "Audit");
            
            Directory.CreateDirectory(LogsDir);
            Directory.CreateDirectory(AuditDir);
            
            Console.WriteLine($"Base Dir: {baseDir}");
            Console.WriteLine($"Root Dir: {RootDir}");
            Console.WriteLine($"Config Dir: {ConfigDir}");
        }

        public string RootDir { get; }
        public string ConfigDir { get; }
        public string DataDir { get; }
        public string LogsDir { get; }
        public string AuditDir { get; }

        public string DataSourceType { get; set; } = "file";
        public string DataFilePath { get; set; } = "";
        public float PlaybackSpeed { get; set; } = 1.0f;

        public string SqlConnectionString { get; set; } = "";
        public string SqlTableName { get; set; } = "parameter_data";
        public int SqlPollInterval { get; set; } = 1;

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

        public TimestampConfig Timestamp { get; set; } = new()
        {
            SourceColumn = "Time(Sec)",
            Format = "seconds",
            Unit = "s"
        };

        public float MissingValue { get; set; } = -999.25f;
        public int UiUpdateInterval { get; set; } = 2000;
        
        public string LogLevel { get; set; } = "Information";
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
        public string Format { get; set; } = "seconds";
        public string? CustomFormat { get; set; } = null;
        public string Unit { get; set; } = "s";
    }
}