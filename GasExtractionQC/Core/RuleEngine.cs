using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using GasExtractionQC.Models;

namespace GasExtractionQC.Core
{
    public enum Trend
    {
        INCREASING,
        DECREASING,
        STABLE,
        ANY
    }

    public class Solution
    {
        public string Id { get; set; } = "";
        public string Action { get; set; } = "";
        public int Priority { get; set; }
        public int EstimatedTimeMinutes { get; set; }
    }

    public class Recommendation
    {
        public string RuleId { get; set; } = "";
        public string RuleName { get; set; } = "";
        public string ProblemCategory { get; set; } = "";
        public string ProblemDescription { get; set; } = "";
        public List<Solution> Solutions { get; set; } = new();
        public float Confidence { get; set; }
        public List<string> MatchingConditions { get; set; } = new();
    }

    public class RuleCondition
    {
        public string Parameter { get; set; } = "";
        public string Trend { get; set; } = "any";
        public bool ThresholdBreach { get; set; }
    }

    public class RuleSolution
    {
        public string Id { get; set; } = "";
        public string Action { get; set; } = "";
        public int Priority { get; set; }
        public int EstimatedTimeMinutes { get; set; }
    }

    public class Rule
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<RuleCondition> Conditions { get; set; } = new();
        public string ProblemCategory { get; set; } = "";
        public string ProblemDescription { get; set; } = "";
        public List<RuleSolution> Solutions { get; set; } = new();
        public float BaseConfidence { get; set; }
        public float HistoricalSuccessRate { get; set; }
    }

    public class RuleEngine
    {
        private List<Rule> _rules;
        private Dictionary<string, List<float>> _parameterHistory;

        public RuleEngine(string? rulesConfigPath = null)
        {
            var settings = Config.Settings.Instance;
            rulesConfigPath ??= Path.Combine(settings.ConfigDir, "rules.yaml");

            _rules = LoadRules(rulesConfigPath);
            _parameterHistory = new Dictionary<string, List<float>>();

            Console.WriteLine($"RuleEngine initialized with {_rules.Count} rules");
        }

        private List<Rule> LoadRules(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"Rules file not found: {path}");
                    return new List<Rule>();
                }

                var yaml = File.ReadAllText(path);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var config = deserializer.Deserialize<Dictionary<string, List<Rule>>>(yaml);
                
                if (config.TryGetValue("rules", out var rules))
                {
                    Console.WriteLine($"Loaded {rules.Count} rules");
                    return rules;
                }

                return new List<Rule>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load rules: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return new List<Rule>();
            }
        }

        public void UpdateHistory(Dictionary<string, float> parameterValues)
        {
            foreach (var param in parameterValues)
            {
                if (param.Key == "timestamp")
                    continue;

                if (!_parameterHistory.ContainsKey(param.Key))
                {
                    _parameterHistory[param.Key] = new List<float>();
                }

                _parameterHistory[param.Key].Add(param.Value);

                // Keep only last 120 values (2 minutes at 1 Hz)
                if (_parameterHistory[param.Key].Count > 120)
                {
                    _parameterHistory[param.Key].RemoveAt(0);
                }
            }
        }

        public Trend CalculateTrend(string parameterName, int window = 20)
        {
            if (!_parameterHistory.ContainsKey(parameterName))
                return Trend.STABLE;

            var history = _parameterHistory[parameterName];

            if (history.Count < window)
                return Trend.STABLE;

            // Get recent values
            var recent = history.Skip(history.Count - window).ToArray();

            // Calculate simple linear regression slope
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            int n = recent.Length;

            for (int i = 0; i < n; i++)
            {
                sumX += i;
                sumY += recent[i];
                sumXY += i * recent[i];
                sumX2 += i * i;
            }

            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);

            // Normalize slope by value range
            double valueRange = recent.Max() - recent.Min();
            if (valueRange == 0)
                return Trend.STABLE;

            double normalizedSlope = slope / valueRange;

            // Threshold for trend detection
            const double threshold = 0.02; // 2% change over window

            if (normalizedSlope > threshold)
                return Trend.INCREASING;
            else if (normalizedSlope < -threshold)
                return Trend.DECREASING;
            else
                return Trend.STABLE;
        }

        public List<Recommendation> Diagnose(
            List<ParameterStatus> outOfRangeParameters,
            Dictionary<string, float> currentValues)
        {
            // Update history for trend calculation
            UpdateHistory(currentValues);

            var recommendations = new List<Recommendation>();

            // Check each rule
            foreach (var rule in _rules)
            {
                var matchingConditions = CheckRule(rule, outOfRangeParameters);

                if (matchingConditions != null && matchingConditions.Count > 0)
                {
                    // Rule matches!
                    var recommendation = new Recommendation
                    {
                        RuleId = rule.Id,
                        RuleName = rule.Name,
                        ProblemCategory = rule.ProblemCategory,
                        ProblemDescription = rule.ProblemDescription,
                        Solutions = rule.Solutions
                            .OrderBy(s => s.Priority)
                            .Select(s => new Solution
                            {
                                Id = s.Id,
                                Action = s.Action,
                                Priority = s.Priority,
                                EstimatedTimeMinutes = s.EstimatedTimeMinutes
                            })
                            .ToList(),
                        Confidence = rule.BaseConfidence,
                        MatchingConditions = matchingConditions
                    };

                    recommendations.Add(recommendation);
                    Console.WriteLine($"Rule matched: {rule.Name} (confidence: {rule.BaseConfidence:P0})");
                }
            }

            // Sort by confidence (highest first)
            recommendations = recommendations.OrderByDescending(r => r.Confidence).ToList();

            if (recommendations.Count == 0)
            {
                Console.WriteLine("No rules matched current conditions");
            }

            return recommendations;
        }

        private List<string>? CheckRule(Rule rule, List<ParameterStatus> outOfRangeParams)
        {
            var matchingConditions = new List<string>();

            foreach (var condition in rule.Conditions)
            {
                var paramName = condition.Parameter;
                
                if (!Enum.TryParse<Trend>(condition.Trend.ToUpper(), out var requiredTrend))
                {
                    requiredTrend = Trend.ANY;
                }

                bool requiresBreach = condition.ThresholdBreach;

                // Check if parameter is out of range (if required)
                bool paramBreached = outOfRangeParams.Any(p => p.Name == paramName);

                if (requiresBreach && !paramBreached)
                {
                    // Required condition not met
                    return null;
                }

                // Check trend (if not "any")
                if (requiredTrend != Trend.ANY)
                {
                    var actualTrend = CalculateTrend(paramName);

                    if (actualTrend != requiredTrend)
                    {
                        // Trend doesn't match
                        return null;
                    }

                    matchingConditions.Add($"{paramName} is {actualTrend.ToString().ToLower()}");
                }
                else
                {
                    matchingConditions.Add($"{paramName} is out of range");
                }
            }

            // All conditions met!
            return matchingConditions.Count > 0 ? matchingConditions : null;
        }
    }
}