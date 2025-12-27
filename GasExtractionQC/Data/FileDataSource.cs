using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using GasExtractionQC.Models;
using GasExtractionQC.Config;

namespace GasExtractionQC.Data
{
    public class FileDataSource : IParameterDataSource
    {
        private readonly string _filePath;
        private readonly float _playbackSpeed;
        private List<ParameterData> _data;
        private int _currentIndex;
        private bool _isConnected;
        private List<Action<ParameterData>> _callbacks;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _playbackTask;

        public bool IsConnected => _isConnected;

        public FileDataSource(string filePath, float playbackSpeed = 1.0f)
        {
            _filePath = filePath;
            _playbackSpeed = playbackSpeed;
            _data = new List<ParameterData>();
            _currentIndex = 0;
            _isConnected = false;
            _callbacks = new List<Action<ParameterData>>();
        }

        public bool Connect()
        {
            try
            {
                Console.WriteLine($"Loading file: {_filePath}");

                if (!File.Exists(_filePath))
                {
                    Console.WriteLine($"File not found: {_filePath}");
                    return false;
                }

                // Read CSV file
                _data = ReadCsvFile(_filePath);

                if (_data.Count == 0)
                {
                    Console.WriteLine("No data loaded from file");
                    return false;
                }

                _isConnected = true;
                Console.WriteLine($"Loaded {_data.Count} records");
                Console.WriteLine($"Time range: {_data.First().Timestamp} to {_data.Last().Timestamp}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
                return false;
            }
        }

        private List<ParameterData> ReadCsvFile(string filePath)
        {
            var result = new List<ParameterData>();
            var settings = Settings.Instance;
            var lines = File.ReadAllLines(filePath);

            if (lines.Length < 2)
                return result;

            // Parse header
            var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();

            // Find column indices
            var timestampCol = Array.IndexOf(headers, settings.Timestamp.SourceColumn);
            if (timestampCol == -1)
            {
                Console.WriteLine($"Timestamp column '{settings.Timestamp.SourceColumn}' not found");
                return result;
            }

            // Map parameter names to column indices
            var parameterColumns = new Dictionary<string, int>();
            foreach (var param in settings.Parameters)
            {
                var config = param.Value;
                if (!config.Enabled)
                    continue;

                int colIndex = Array.IndexOf(headers, config.SourceColumn);
                if (colIndex != -1)
                {
                    parameterColumns[param.Key] = colIndex;
                }
            }

            // Parse data rows
            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var values = lines[i].Split(',');
                    
                    if (values.Length <= timestampCol)
                        continue;

                    // Parse timestamp
                    DateTime timestamp;
                    if (settings.Timestamp.Format == "seconds")
                    {
                        if (double.TryParse(values[timestampCol], NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
                        {
                            // Assuming Unix epoch or seconds since start
                            timestamp = new DateTime(2024, 1, 1).AddSeconds(seconds);
                        }
                        else
                            continue;
                    }
                    else
                    {
                        if (!DateTime.TryParse(values[timestampCol], out timestamp))
                            continue;
                    }

                    // Create parameter data
                    var data = new ParameterData { Timestamp = timestamp };

                    // Parse parameter values
                    foreach (var param in parameterColumns)
                    {
                        if (values.Length > param.Value)
                        {
                            if (float.TryParse(values[param.Value], NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                            {
                                data.SetValue(param.Key, val);
                            }
                            else
                            {
                                data.SetValue(param.Key, settings.MissingValue);
                            }
                        }
                    }

                    result.Add(data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing line {i}: {ex.Message}");
                }
            }

            return result;
        }

        public ParameterData GetCurrentValues()
        {
            if (!_isConnected || _data.Count == 0)
                throw new InvalidOperationException("Not connected to data source");

            if (_currentIndex >= _data.Count)
                _currentIndex = 0;

            return _data[_currentIndex];
        }

        public List<ParameterData> GetHistoricalRange(DateTime startTime, DateTime endTime)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to data source");

            return _data.Where(d => d.Timestamp >= startTime && d.Timestamp <= endTime).ToList();
        }

        public void SubscribeToUpdates(Action<ParameterData> callback)
        {
            _callbacks.Add(callback);
            Console.WriteLine($"Callback registered. Total callbacks: {_callbacks.Count}");

            // Start playback if not already running
            if (_playbackTask == null || _playbackTask.IsCompleted)
            {
                StartPlayback();
            }
        }

        private void StartPlayback()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            _playbackTask = Task.Run(async () =>
            {
                Console.WriteLine($"Playback started (speed: {_playbackSpeed}x)");

                while (!token.IsCancellationRequested && _isConnected)
                {
                    try
                    {
                        if (_currentIndex >= _data.Count)
                        {
                            Console.WriteLine("Reached end of file, restarting");
                            _currentIndex = 0;
                        }

                        var values = GetCurrentValues();

                        // Call all callbacks
                        foreach (var callback in _callbacks)
                        {
                            try
                            {
                                callback(values);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error in callback: {ex.Message}");
                            }
                        }

                        _currentIndex++;

                        // Sleep to simulate real-time
                        int sleepMs = (int)(1000 / _playbackSpeed);
                        await Task.Delay(sleepMs, token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in playback: {ex.Message}");
                        break;
                    }
                }

                Console.WriteLine("Playback stopped");
            }, token);
        }

        public void Disconnect()
        {
            Console.WriteLine("Disconnecting file source");

            _cancellationTokenSource?.Cancel();
            _playbackTask?.Wait(TimeSpan.FromSeconds(2));

            _isConnected = false;
            _data.Clear();
            _callbacks.Clear();

            Console.WriteLine("File source disconnected");
        }
    }
}