using System;
using System.Collections.Generic;
using GasExtractionQC.Models;

namespace GasExtractionQC.Data
{
    /// <summary>
    /// Interface for all data sources (file or SQL)
    /// </summary>
    public interface IParameterDataSource
    {
        bool Connect();
        ParameterData GetCurrentValues();
        List<ParameterData> GetHistoricalRange(DateTime startTime, DateTime endTime);
        void SubscribeToUpdates(Action<ParameterData> callback);
        void Disconnect();
        bool IsConnected { get; }
    }
}