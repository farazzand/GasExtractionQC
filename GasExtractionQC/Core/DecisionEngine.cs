using System;
using System.Collections.Generic;
using GasExtractionQC.Models;

namespace GasExtractionQC.Core
{
    public class SystemStatus
    {
        public DateTime Timestamp { get; set; }
        public QCStatus CurrentQC { get; set; }
        public Dictionary<string, ParameterStatus> ParameterStatuses { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
    }

    public class DecisionEngine
    {
        private readonly QCMonitor _qcMonitor;
        private readonly RuleEngine _ruleEngine;

        public DecisionEngine(QCMonitor qcMonitor, RuleEngine ruleEngine)
        {
            _qcMonitor = qcMonitor;
            _ruleEngine = ruleEngine;

            Console.WriteLine("DecisionEngine initialized");
        }

        public SystemStatus ProcessUpdate(ParameterData currentValues)
        {
            var timestamp = currentValues.Timestamp;

            // 1. Check current QC status
            var currentQC = _qcMonitor.Update(currentValues);
            var parameterStatuses = _qcMonitor.GetParameterStatuses();

            // 2. Generate recommendations if needed (QC is RED)
            List<Recommendation> recommendations = new();

            if (currentQC == QCStatus.RED)
            {
                var outOfRange = _qcMonitor.GetOutOfRangeParameters();
                recommendations = _ruleEngine.Diagnose(outOfRange, currentValues.Values);
            }

            // 3. Create system status
            var status = new SystemStatus
            {
                Timestamp = timestamp,
                CurrentQC = currentQC,
                ParameterStatuses = parameterStatuses,
                Recommendations = recommendations
            };

            return status;
        }
    }
}