using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using GasExtractionQC.Models;
using GasExtractionQC.Config;


namespace GasExtractionQC.Core
{
    public enum QCStatus
    {
        GREEN,   // All parameters within safe range
        YELLOW,  // One or more in warning zone
        RED      // One or more out of range
    }

    public class ParameterStatus
    {
        public string Name { get; set; } = "";
        public float Value { get; set; }
        public bool Available { get; set; } = true;
        public QCStatus? Status { get; set; }
        public float? MinOk { get; set; }
        public float? MaxOk { get; set; }
        public string Note { get; set; } = "";
    }

    public class ThresholdConfig
    {
        public string DisplayName { get; set; } = "";
        public float Min { get; set; }
        public float Max { get; set; }
        public string Unit { get; set; } = "";
        public float WarningMargin { get; set; } = 0.1f;
    }

    public class QCMonitor
    {
        private readonly string _thresholdPath;
        private Dictionary<string, ThresholdConfig> _thresholds;
        private Dictionary<string, ParameterStatus> _parameterStatus;
        private QCStatus _overallStatus;
        private readonly string _incidentLogPath;

        public QCStatus OverallStatus => _overallStatus;

        public QCMonitor(string? thresholdConfigPath = null)
        {
            var settings = Config.Settings.Instance;
            _thresholdPath = thresholdConfigPath ?? Path.Combine(settings.ConfigDir, "thresholds.yaml");
            _thresholds = LoadThresholds(_thresholdPath);
            _parameterStatus = new Dictionary<string, ParameterStatus>();
            _overallStatus = QCStatus.GREEN;
            _incidentLogPath = Path.Combine(settings.AuditDir, "incidents.jsonl");

            // Ensure incident log exists
            if (!File.Exists(_incidentLogPath))
            {
                File.WriteAllText(_incidentLogPath, "");
            }

            Console.WriteLine($"QCMonitor initialized with {_thresholds.Count} thresholds");
        }

        private Dictionary<string, ThresholdConfig> LoadThresholds(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"Threshold file not found: {path}");
                    return new Dictionary<string, ThresholdConfig>();
                }

                var yaml = File.ReadAllText(path);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                var config = deserializer.Deserialize<Dictionary<string, object>>(yaml);
                
                if (config.TryGetValue("parameters", out var paramsObj))
                {
                    var paramsDict = paramsObj as Dictionary<object, object>;
                    if (paramsDict != null)
                    {
                        var result = new Dictionary<string, ThresholdConfig>();
                        
                        foreach (var kvp in paramsDict)
                        {
                            var paramName = kvp.Key.ToString() ?? "";
                            var paramConfig = kvp.Value as Dictionary<object, object>;
                            
                            if (paramConfig != null)
                            {
                                var threshold = new ThresholdConfig
                                {
                                    DisplayName = paramConfig.GetValueOrDefault("display_name", "")?.ToString() ?? "",
                                    Min = Convert.ToSingle(paramConfig.GetValueOrDefault("min", 0f)),
                                    Max = Convert.ToSingle(paramConfig.GetValueOrDefault("max", 100f)),
                                    Unit = paramConfig.GetValueOrDefault("unit", "")?.ToString() ?? "",
                                    WarningMargin = Convert.ToSingle(paramConfig.GetValueOrDefault("warning_margin", 0.1f))
                                };
                                
                                result[paramName] = threshold;
                            }
                        }
                        
                        return result;
                    }
                }

                return new Dictionary<string, ThresholdConfig>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load thresholds: {ex.Message}");
                return new Dictionary<string, ThresholdConfig>();
            }
        }



        public void SetThresholds(Dictionary<string, ThresholdConfig> newThresholds)
        {
            try {
                // Write full YAML structure
                var yaml = new { parameters = newThresholds };
                
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();
                
                var yamlText = serializer.Serialize(yaml);
                System.IO.File.WriteAllText(_thresholdPath, yamlText);
                
                // Reload into memory
                _thresholds = newThresholds;
                Console.WriteLine("Thresholds updated and persisted");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to persist thresholds: {ex}");
            }
        }
        public QCStatus Update(ParameterData parameterValues)
        {
            var settings = Config.Settings.Instance;
            _parameterStatus.Clear();

            foreach (var param in parameterValues.Values)
            {
                var paramName = param.Key;
                var value = param.Value;

                var status = new ParameterStatus
                {
                    Name = paramName,
                    Value = value
                };

                // Check if value is missing/unavailable
                if (float.IsNaN(value) || Math.Abs(value - settings.MissingValue) < 0.01f)
                {
                    status.Available = false;
                    status.Note = "N/A – No data / communication error";
                    status.Status = null;
                }
                else
                {
                    status.Available = true;

                    // Apply thresholds if available
                    if (_thresholds.TryGetValue(paramName, out var threshold))
                    {
                        status.MinOk = threshold.Min;
                        status.MaxOk = threshold.Max;

                        // Determine status
                        if (value < threshold.Min || value > threshold.Max)
                        {
                            status.Status = QCStatus.RED;
                        }
                        else
                        {
                            // Check warning margin
                            float range = threshold.Max - threshold.Min;
                            float lowWarn = threshold.Min + (threshold.WarningMargin * range);
                            float highWarn = threshold.Max - (threshold.WarningMargin * range);

                            if (value <= lowWarn || value >= highWarn)
                            {
                                status.Status = QCStatus.YELLOW;
                            }
                            else
                            {
                                status.Status = QCStatus.GREEN;
                            }
                        }
                    }
                    else
                    {
                        status.Status = QCStatus.GREEN; // No thresholds = assume green
                    }
                }

                _parameterStatus[paramName] = status;
            }

            var newStatus = CalculateOverallStatus();

            // Log incident if transitioning to RED
            if (newStatus == QCStatus.RED && _overallStatus != QCStatus.RED)
            {
                LogIncident(parameterValues);
            }

            _overallStatus = newStatus;
            return newStatus;
        }

        private QCStatus CalculateOverallStatus()
        {
            bool anyRed = false;
            bool anyYellow = false;

            foreach (var status in _parameterStatus.Values)
            {
                if (!status.Available)
                    continue;

                if (status.Status == QCStatus.RED)
                {
                    anyRed = true;
                    break;
                }

                if (status.Status == QCStatus.YELLOW)
                {
                    anyYellow = true;
                }
            }

            if (anyRed) return QCStatus.RED;
            if (anyYellow) return QCStatus.YELLOW;
            return QCStatus.GREEN;
        }

        private void LogIncident(ParameterData data)
        {
            try
            {
                var incident = new
                {
                    timestamp = DateTime.UtcNow,
                    raw_values = data.Values,
                    parameters = _parameterStatus.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new
                        {
                            value = kvp.Value.Value,
                            available = kvp.Value.Available,
                            status = kvp.Value.Status?.ToString(),
                            min_ok = kvp.Value.MinOk,
                            max_ok = kvp.Value.MaxOk,
                            note = kvp.Value.Note
                        }
                    )
                };

                var json = System.Text.Json.JsonSerializer.Serialize(incident);
                File.AppendAllText(_incidentLogPath, json + Environment.NewLine);
                Console.WriteLine($"Incident recorded to {_incidentLogPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log incident: {ex.Message}");
            }
        }

        public Dictionary<string, ParameterStatus> GetParameterStatuses()
        {
            return new Dictionary<string, ParameterStatus>(_parameterStatus);
        }

        public List<ParameterStatus> GetOutOfRangeParameters()
        {
            return _parameterStatus.Values
                .Where(p => p.Available && p.Status == QCStatus.RED)
                .ToList();
        }
    }
}