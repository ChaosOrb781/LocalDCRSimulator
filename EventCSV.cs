using LocalDCRSimulator.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalDCRSimulator {
    class EventCSV {
        public string TraceIDColumn { get; private set; }
        public string ActivityIDColumn { get; private set; }
        public string LabelColumn { get; private set; }
        public string DateColumn { get; private set; }
        public List<string> Columns { get; private set; } = new List<string>();
        public List<Event> Events { get; set; } = new List<Event>();

        public static List<FileInfo> FindCSVs(int maxDepth) {
            Diagnostics.Log("Finding all csv's within a depth of " + maxDepth);
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            List<FileInfo> files = new List<FileInfo>();
            for (int i = 0; i < maxDepth; i++) {
                foreach (FileInfo file in dir.GetFiles()) {
                    if (file.Extension == ".csv") {
                        Diagnostics.Log("Found csv: " + file.Name);
                        files.Add(file);
                    }
                }
                dir = new DirectoryInfo(Path.Combine(dir.FullName, ".."));
            }
            if (files.Count == 0) {
                Diagnostics.Error("No csv files found");
                return null;
            }
            return files;
        }

        public static EventCSV ExtractEventsFromCSV(FileInfo fi, string TraceIDcolumn, string ActivityIDcolumn, string LabelColumn, string DateColumn) {
            EventCSV csv = new EventCSV();
            try {
                StreamReader stream = new StreamReader(fi.FullName);
                bool first = true;
                while (!stream.EndOfStream) {
                    string line = stream.ReadLine();
                    List<string> values = line.Split(';').ToList();
                    if (!first) {
                        Event ev = new Event(csv.Columns, values, TraceIDcolumn, ActivityIDcolumn, LabelColumn, DateColumn);
                        csv.Events.Add(ev);
                    } else {
                        csv.Columns.AddRange(values);
                        first = false;
                    }
                }
                stream.Close();

                csv.TraceIDColumn = TraceIDcolumn;
                csv.ActivityIDColumn = ActivityIDcolumn;
                csv.LabelColumn = LabelColumn;
                csv.DateColumn = DateColumn;
                return csv;
            } catch (Exception e) {
                Diagnostics.Error(e.Message);
                return null;
            }
        }

        public bool WriteToCSV(FileInfo outputPath)
        {
            try
            {
                StreamWriter sw = new StreamWriter(outputPath.FullName);
                sw.WriteLine(this.Columns.Aggregate((left, right) => left + ";" + right));
                foreach (Event e in this.Events)
                {
                    string line = "";
                    foreach (string column in Columns)
                    {
                        line += e.Columns[column] + ";";
                    }
                    line.Substring(0, line.Length - 1);
                    sw.WriteLine(line);
                }
                sw.Close();
                Diagnostics.Log("Successfully wrote all events to new csv");
                return true;
            } catch (Exception e)
            {
                Diagnostics.Error(e.Message);
                return false;
            }
        }
    }
}
