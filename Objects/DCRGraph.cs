using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LocalDCRSimulator.Objects {
    class DCRGraph : IRecoverable {
        public string Title { get; private set; }
        public Dictionary<string, Activity> Activities = new Dictionary<string, Activity>();
        //List<DCRGraph> Proceses = new List<DCRGraph>();

        private DCRGraph() { }

        public static DCRGraph FromXML(XDocument xml) {
            DCRGraph graph = new DCRGraph();
            graph.Title = xml.Element("dcrgraph").Attribute("title").Value;
            Diagnostics.Log("Generating graph \"" + graph.Title + "\"");

            List<XElement> relationsXML = xml
                .Element("dcrgraph")
                .Element("specification")
                .Element("constraints")
                .Descendants().ToList();

            Diagnostics.Log("Generating relations");
            List<Relation> relations = new List<Relation>();
            foreach (XElement relation in relationsXML)
            {
                foreach (XElement type in relation.Descendants())
                {
                    relations.Add(Relation.FromXML(type));
                }
            }

            List<XElement> activitiesInfo = xml
                .Element("dcrgraph")
                .Element("specification")
                .Element("resources")
                .Element("events")
                .Elements("event").ToList();
            List<XElement> activitiesLabels = xml
                .Element("dcrgraph")
                .Element("specification")
                .Element("resources")
                .Element("labelMappings")
                .Elements("labelMapping").ToList();
            List<XElement> activitiesMarking = xml
                .Element("dcrgraph")
                .Element("runtime")
                .Element("marking")
                .Elements().ToList();

            Diagnostics.Log("Generating activities");
            foreach (XElement activitiesLabel in activitiesLabels)
            {
                string id = activitiesLabel.Attribute("eventId").Value;
                Activity activity = Activity.FromXML(graph, activitiesLabel, activitiesMarking);
                graph.Activities.Add(id, activity);
            }

            Diagnostics.Log("Adding nested activities");
            foreach (XElement info in activitiesInfo) {
                if (info.Attribute("type")?.Value == "nesting") {
                    string id = info.Attribute("id").Value;
                    foreach (XElement nestedInfo in info.Elements("event")) {
                        string nestedid = nestedInfo.Attribute("id").Value;
                        graph.Activities[id].NestedActivities.Add(graph.Activities[nestedid]);
                    }
                }
            }

            Diagnostics.Log("Adding relations to the appropriate activities");
            foreach (Relation r in relations)
            {
                //To relations
                switch (r.Type)
                {
                    case Relation.RelationType.Condition:
                    case Relation.RelationType.MileStone:
                        Activity target = graph.Activities[r.TargetID];
                        if (target.NestedActivities.Count > 0) {
                            Queue<Activity> allNested = new Queue<Activity>();
                            foreach (Activity a in target.NestedActivities)
                            {
                                allNested.Enqueue(a);
                            }
                            while (allNested.Count > 0)
                            {
                                Activity nestedActivity = allNested.Dequeue();
                                if (nestedActivity.NestedActivities.Count > 0)
                                {
                                    foreach (Activity a in nestedActivity.NestedActivities)
                                        allNested.Enqueue(a);
                                } 
                                else
                                {
                                    Relation r_copy = r.Clone();
                                    r_copy.TargetID = nestedActivity.ID;
                                    nestedActivity.AddRelation(r_copy);
                                }
                            }
                        }
                        else
                        {
                            target.AddRelation(r);
                        }
                        break;
                }
                //From relations
                switch (r.Type) {
                    case Relation.RelationType.Response:
                    case Relation.RelationType.Include:
                    case Relation.RelationType.Exclude:
                    case Relation.RelationType.Coresponse:
                    case Relation.RelationType.Spawns:
                        Activity source = graph.Activities[r.SourceID];
                        if (source.NestedActivities.Count > 0)
                        {
                            Queue<Activity> allNested = new Queue<Activity>();
                            foreach (Activity a in source.NestedActivities)
                            {
                                allNested.Enqueue(a);
                            }
                            while (allNested.Count > 0)
                            {
                                Activity nestedActivity = allNested.Dequeue();
                                if (nestedActivity.NestedActivities.Count > 0)
                                {
                                    foreach (Activity a in nestedActivity.NestedActivities)
                                        allNested.Enqueue(a);
                                }
                                else
                                {
                                    Relation r_copy = r.Clone();
                                    r_copy.SourceID = nestedActivity.ID;
                                    nestedActivity.AddRelation(r_copy);
                                }
                            }
                        }
                        else
                        {
                            source.AddRelation(r);
                        }
                        break;
                }
            }

            return graph;
        }

        public void WriteResult(ExecuteResult results, FileInfo logfile)
        {
            string filename = string.Format("Results_{0}_{1}_{2}hour_{3}minute_{4}day_{5}month.txt", logfile.Name, Title.Trim(new char[] {'\\','/',':','?','"','<','>','l'}), DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Day, DateTime.Now.Month);
            string path = Path.Combine(logfile.DirectoryName, filename);
            while (File.Exists(path))
            {
                path = path.Substring(0, path.Length - 4) + "_dub.txt";
            }
            StreamWriter wr = new StreamWriter(path);
            wr.WriteLine("Successes: " + results.SuccessIDs.Count);
            wr.WriteLine("Fails: " + results.FailedIDs.Count);
            wr.WriteLine("Successful traces:");
            foreach(string id in results.SuccessIDs)
            {
                wr.WriteLine(id);
            }
            wr.WriteLine("Failed traces:");
            foreach (string id in results.FailedIDs)
            {
                wr.WriteLine(id);
            }
            wr.Close();
        }

        public static DCRGraph GetGraphFromServer(string username, string password, string ID) {
            string result = string.Empty;
            string GetXMLURL = string.Format("https://repository.dcrgraphs.net/api/graphs/{0}", ID);
            Diagnostics.Log("Loading xml with ID: " + ID);
            // Create the web request  
            HttpWebRequest request = WebRequest.Create(GetXMLURL) as HttpWebRequest;
            request.Method = "GET";
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(string.Format("{0}:{1}", username, password)));
            // Get response  
            try {
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse) {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    result = reader.ReadToEnd();
                }
            } catch (Exception ex) {
                Diagnostics.Error("Could not load graph: " + ex.Message);
                return null;
            }
            Diagnostics.Log("Successfully loaded xml with ID: " + ID);
            return FromXML(XDocument.Parse(result));
        }

        public class ExecuteResult {
            public List<string> SuccessIDs = new List<string>();
            public List<string> FailedIDs = new List<string>();
        }

        public bool ExecuteEvent(Event ev, bool ignoreMissing = true) {
            try
            {
                Diagnostics.Log("Executing trace: " + ev.TraceID + " with activity: " + ev.ActivityID + "/" + ev.Label);
                Activity activity = null;
                //If id does not match, use label instead (since setting up id might be tedious)
                Activities.TryGetValue(ev.ActivityID, out activity);
                if ((activity = Activities.Values.Where(a => ev.Label == a.Label).FirstOrDefault()) == null)
                {
                    Diagnostics.Warning("No valid activity on either event id and label found");
                    return ignoreMissing;
                }

                //"e is included"
                if (!activity.IsEnabled)
                {
                    Diagnostics.Warning("Event's activity was not enabled");
                    return false;
                }

                Diagnostics.Log("Event has passed as possible to execute");

                //Note: ordering is important for sematics, what if is a response to itself?
                activity.IsPending = false;

                //All relations where our event is the source
                foreach (Relation r in activity.OutgoingRelations.Where(r => r.Type == Relation.RelationType.Response))
                    Activities[r.TargetID].IsPending = true;

                //Order important, we want to exclude first, then include as it may include itself
                foreach (Relation r in activity.OutgoingRelations.Where(r => r.Type == Relation.RelationType.Exclude))
                    Activities[r.TargetID].IsIncluded = false;

                foreach (Relation r in activity.OutgoingRelations.Where(r => r.Type == Relation.RelationType.Include))
                    Activities[r.TargetID].IsIncluded = true;

                //Do only allow nests to be considered "IsExecuted" if all nested activities are
                activity.IsExecuted = true;
                return true;
            } 
            catch (Exception e)
            {
                Diagnostics.Error(e.Message);
                return false;
            }
        }

        public ExecuteResult ExecuteTracesInCSV(EventCSV csv, int displayProgressForEach = 0) {
            ExecuteResult ret = new ExecuteResult();
            int percentFeedback = displayProgressForEach;
            int percent = 0;
            List<string> uniqueTraces = csv.Events.Select(e => e.TraceID).Distinct().ToList();
            for (int i = 0; i < uniqueTraces.Count; i++)
            {
                string traceid = uniqueTraces[i];
                List<Event> trace = csv.Events.Where(e => e.TraceID == traceid).OrderBy(e => e.Date).ToList();
                bool accepted = true;
                foreach (Event ev in trace)
                {
                    if (!ExecuteEvent(ev))
                    {
                        ret.FailedIDs.Add(traceid);
                        accepted = false;
                        break;
                    }
                }

                //If any activity remains pending after execution, fail the trace
                foreach (Activity activity in Activities.Values)
                    if (activity.IsEnabled && activity.IsIncluded && activity.IsPending)
                    {
                        ret.FailedIDs.Add(traceid);
                        accepted = false;
                        break;
                    }

                if (accepted)
                    ret.SuccessIDs.Add(traceid);

                this.Recover(true);

                percent = (int)(((float)i + 1) / ((float)uniqueTraces.Count) * 100);
                if (displayProgressForEach > 0 && percent >= percentFeedback)
                {
                    percentFeedback += displayProgressForEach;
                    LogProgress?.Invoke(this, new ProgressArgs(percent));
                }
            }
            return ret;
        }

        public class ProgressArgs : EventArgs
        {
            public int Percent { get; private set; }
            public ProgressArgs(int percent)
            {
                Percent = percent;
            }
        }
        public event EventHandler<ProgressArgs> LogProgress;

        public void Recover(bool collapse)
        {
            //List contains all activities, therefore recover is not needed
            foreach (Activity a in Activities.Values)
                a.Recover(false);
        }
    }
}
