using System;
using System.Collections.Generic;

namespace GasExtractionQC.Models
{
    /// <summary>
    /// Represents a single set of parameter readings at a point in time
    /// </summary>
    public class ParameterData
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, float> Values { get; set; } = new();

        public float GetValue(string parameterName, float defaultValue = float.NaN)
        {
            if (Values.TryGetValue(parameterName, out float value))
                return value;
            return defaultValue;
        }

        public void SetValue(string parameterName, float value)
        {
            Values[parameterName] = value;
        }
    }
}