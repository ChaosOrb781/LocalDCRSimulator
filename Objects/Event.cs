using LocalDCRSimulator.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalDCRSimulator.Objects
{
    class Event
    {
        public string TraceID, ActivityID, Label;
        public DateTime Date;
        public Hashtable Columns = new Hashtable();
        public Event(List<string> columnLabels, List<string> columnValues, string traceIdColumn, string activityIdColumn, string labelColumn, string dateColumn)
        {
            if (columnLabels.Count != columnValues.Count) {
                throw new Exception("Invalid input, expected labels and values to be of equal size");
            }
            for (int i = 0; i < columnLabels.Count; i++) {
                if (columnLabels[i] == traceIdColumn)
                    TraceID = columnValues[i];
                if (columnLabels[i] == activityIdColumn)
                    ActivityID = columnValues[i];
                if (columnLabels[i] == labelColumn)
                    Label = columnValues[i];
                if (columnLabels[i] == dateColumn)
                    Date = Convert.ToDateTime(columnValues[i]);
                Columns.Add(columnLabels[i], columnValues[i]);
            }
        }

        public override string ToString()
        {
            return ((ActivityID ?? "No ID") + " : " + (Label ?? "No label"));
        }
    }
}
