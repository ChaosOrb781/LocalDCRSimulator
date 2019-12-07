using LocalDCRSimulator.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static LocalDCRSimulator.Objects.DCRGraph;

namespace LocalDCRSimulator {
    class Program {
        static void Main(string[] args) {
            List<FileInfo> csvfiles = EventCSV.FindCSVs(4);
            if (csvfiles == null) 
            {
                Interface.WriteLine("No csv files found at a depth of 3 from .exe");
                Interface.Wait();
                return;
            }

            Interface.WriteLine("Found " + csvfiles.Count + " csv file" + (csvfiles.Count > 1 ? "s" : "") +":", ConsoleColor.Green);
            foreach (FileInfo fi in csvfiles) 
            {
                Interface.WriteLine("  - " + fi.FullName);
            }

            Tuple<string, string> credentials = GetCredentials();
            List<string> graphs = GetGraphIDs(credentials.Item1, credentials.Item2);

            foreach (FileInfo logfile in csvfiles)
            {
                Interface.WriteLine("Running over " + logfile.FullName, ConsoleColor.Yellow);
                EventCSV log = EventCSV.ExtractEventsFromCSV(logfile, "ID", "Event", "Title", "Date");
                foreach (string graphID in graphs)
                {
                    Interface.WriteLine("Executing on graph " + graphID, ConsoleColor.Yellow);
                    DCRGraph graph = DCRGraph.GetGraphFromServer(credentials.Item1, credentials.Item2, graphID);
                    graph.LogProgress += WriteProgress;
                    DCRGraph.ExecuteResult results = graph.ExecuteTracesInCSV(log, 5);
                    graph.LogProgress -= WriteProgress;
                    graph.WriteResult(results, logfile);
                    graph.Recover(true);

                }
                log.Events = log.Events.OrderBy(e => e.TraceID).ThenBy(e => e.Date).ToList();
                Diagnostics.Log("Writing a sorted csv back to same folder the log was found");
                log.WriteToCSV(new FileInfo(logfile.FullName.Substring(0, logfile.FullName.Length - 4) + ".scsv"));
            }
            Interface.WriteLine("Done!");
            Interface.Wait();
        }

        static Tuple<string, string> GetCredentials()
        {
            string username = null, password = null;
            bool passed = false;
            while (!passed)
            {
                Interface.WriteLine("\nEnter credentials, if left blank, use default user");
                username = Interface.GetStringInput("Username: ");
                if (string.IsNullOrEmpty(username))
                    username = "DefaultUser";

                password = Interface.GetStringInput("Password: ");
                if (string.IsNullOrEmpty(password))
                    password = "DefaultUser123";

                passed = ValidateLogin(username, password);
                if (!passed)
                    Interface.Warning("Incorrect password/username");
            }
            Interface.WriteLine("Successfully logged in", ConsoleColor.Green);
            return Tuple.Create(username, password);
        }

        static bool ValidateLogin(string username, string password)
        {
            bool isAccess = false;
            string GetXMLURL = "https://repository.dcrgraphs.net/api/graphs";
            HttpWebRequest request = WebRequest.Create(GetXMLURL) as HttpWebRequest;
            request.Method = "GET";
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(string.Format("{0}:{1}", username, password)));
            try
            {
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                if (response.StatusCode == HttpStatusCode.OK)
                    isAccess = true;
            }
            catch (Exception ex)
            {
                if (((System.Net.WebException)ex).Response != null)
                    Diagnostics.Error(((System.Net.WebException)ex).Message);
                else
                    Diagnostics.Error("Unable to connect to DCR , please contact Admin");
            }
            return isAccess;
        }

        static List<string> GetGraphIDs(string username, string password)
        {
            List<string> graphIDs = new List<string>();
            bool passed = false;
            Interface.WriteLine("\nWrite one or more graphs that will be executed:");
            while (!passed)
            {
                string graphID = Interface.GetStringInput("Graph ID: ");

                //dcr.ConnectToGraph(graphID);
                passed = ValidateGraph(username, password, graphID);
                if (!passed)
                    Interface.Warning("Cound not load given graph");
                else
                {
                    Interface.WriteLine("Added graph succesfully", ConsoleColor.Green);
                    graphIDs.Add(graphID);
                    string continue_loop = "";
                    while (continue_loop.ToLower() != "y" && continue_loop != "n")
                    {
                        continue_loop = Interface.GetStringInput("Add more graphs?(Y/N): ", ConsoleColor.Yellow);
                    }
                    if (continue_loop == "y")
                    {
                        passed = false;
                    }
                }
            }
            return graphIDs;
        }

        static bool ValidateGraph(string username, string password, string id)
        {
            string PosTinitURL = "https://repository.dcrgraphs.net/api/graphs/" + id + "/sims";
            HttpWebRequest request = WebRequest.Create(PosTinitURL) as HttpWebRequest;
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(string.Format("{0}:{1}", username, password)));
            request.Method = "POST";
            request.ContentLength = 0;
            try
            {
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                return true;
            }
            catch (Exception ex)
            {
                Diagnostics.Error(ex.Message);
                return false;
            }
        }

        private static void WriteProgress(object sender, ProgressArgs e)
        {
            Console.WriteLine("Progress: " + e.Percent + "%");
        }
    }
}
