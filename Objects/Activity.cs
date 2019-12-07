using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LocalDCRSimulator.Objects {
    class Activity : IRecoverable {
        public string ID    { get; private set; }
        public string Label { get; private set; }
        public bool IsExecuted, IsPending, IsIncluded;
        public bool IsEnabled { 
            get {
                Diagnostics.Log("Checking if enabled through conditions and milestones");
                foreach (Relation r in IngoingRelations)
                {
                    if (r.Type == Relation.RelationType.Condition)
                    {
                        Activity source = Parent.Activities[r.SourceID];
                        if (source.IsIncluded && !source.IsExecuted)
                        {
                            return false;
                        }
                    }
                    else if (r.Type == Relation.RelationType.MileStone)
                    {
                        Activity source = Parent.Activities[r.SourceID];
                        if (source.IsIncluded && source.IsPending)
                        {
                            return false;
                        }
                    }
                }
                return IsIncluded;
            } 
        }
        public List<Activity> NestedActivities = new List<Activity>();

        public List<Relation> OutgoingRelations = new List<Relation>();
        public List<Relation> IngoingRelations = new List<Relation>();

        public DCRGraph Parent { get; private set; } = null;

        //Recovery values
        private bool _isExecuted, _isPending, _isIncluded;

        public static Activity FromXML(DCRGraph parent, XElement labelMapping, List<XElement> markings) {
            Activity activity = new Activity();
            activity.ID = labelMapping.Attribute("eventId").Value;
            activity.Label = labelMapping.Attribute("labelId").Value;
            foreach (XElement mark in markings)
            {
                List<XElement> temp = null;
                switch (mark.Name.ToString())
                {
                    case "executed":
                        if ((temp = mark.Elements("event").ToList()) != null)
                            foreach(XElement eventExecuted in temp)
                                if (eventExecuted.Attribute("id").Value == activity.ID)
                                {
                                    activity.IsExecuted = true;
                                    break;
                                }
                        break;
                    case "included":
                        if ((temp = mark.Elements("event").ToList()) != null)
                            foreach (XElement eventExecuted in temp)
                                if (eventExecuted.Attribute("id").Value == activity.ID)
                                {
                                    activity.IsIncluded = true;
                                    break;
                                }
                        break;
                    case "pendingResponses":
                        if ((temp = mark.Elements("event").ToList()) != null)
                            foreach (XElement eventExecuted in temp)
                                if (eventExecuted.Attribute("id").Value == activity.ID) 
                                {
                                    activity.IsPending = true;
                                    break;
                                }
                        break;
                    case "globalStore":
                        if ((temp = mark.Elements("event").ToList()) != null)
                            foreach (XElement eventExecuted in temp)
                                if (eventExecuted.Attribute("id").Value == activity.ID)
                                {
                                    Diagnostics.Warning("Unsupported marking found");
                                    break;
                                }
                        break;
                }
            }
            activity._isIncluded = activity.IsIncluded;
            activity._isExecuted = activity.IsExecuted;
            activity._isPending = activity.IsPending;
            activity.Parent = parent;
            return activity;
        }

        public bool AddRelation(Relation r)
        {
            switch (r.Type)
            {
                case Relation.RelationType.Condition:
                case Relation.RelationType.MileStone:
                    if (r.TargetID != this.ID)
                    {
                        Diagnostics.Error("Attempt at adding invalid relation of type " + r.Type + " to " + this.ID);
                        return false;
                    }
                    IngoingRelations.Add(r);
                    break;
                case Relation.RelationType.Response:
                case Relation.RelationType.Include:
                case Relation.RelationType.Exclude:
                case Relation.RelationType.Coresponse:
                case Relation.RelationType.Spawns:
                    if (r.SourceID != this.ID)
                    {
                        Diagnostics.Error("Attempt at adding invalid relation of type " + r.Type + " to " + this.ID);
                        return false;
                    }
                    OutgoingRelations.Add(r);
                    break;
            }
            return true;
        }

        public void Recover(bool collapse = true)
        {
            IsExecuted = _isExecuted;
            IsIncluded = _isIncluded;
            IsPending = _isPending;
            if (!collapse) return;
            foreach (Activity child in this.NestedActivities)
            {
                child.Recover(collapse);
            }
        }
        public Activity Clone()
        {
            return new Activity()
            {
                ID = this.ID,
                Label = this.Label,
                IsExecuted = this.IsExecuted,
                IsIncluded = this.IsIncluded,
                IsPending = this.IsPending
            };
        }
    }
}
