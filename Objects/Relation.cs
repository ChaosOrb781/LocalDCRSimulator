using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LocalDCRSimulator.Objects {
    class Relation {
        public enum RelationType {
            Exclude,
            Include,
            MileStone,
            Condition,
            Response,
            Coresponse,
            Spawns,
            Unsupported
        }
        public RelationType Type  { get; private set; }
        private RelationType _type { get; set; }
        public string SourceID    { get; set; }
        private string _sourceID  { get; set; }
        public string TargetID    { get; set; }
        private string _targetID { get; set; }

        public static Relation FromXML(XElement xml) {
            Relation relation = new Relation();

            switch(xml.Name.ToString()) {
                case "exclude":
                    relation.Type = RelationType.Exclude;
                    break;
                case "include":
                    relation.Type = RelationType.Include;
                    break;
                case "milestone":
                    relation.Type = RelationType.MileStone;
                    break;
                case "condition":
                    relation.Type = RelationType.Condition;
                    break;
                case "response":
                    relation.Type = RelationType.Response;
                    break;
                case "coresponse":
                    relation.Type = RelationType.Coresponse;
                    break;
                case "spawn":
                    relation.Type = RelationType.Spawns;
                    break;
                default:
                    relation.Type = RelationType.Unsupported;
                    Diagnostics.Error("Unsupported relation type found");
                    break;
            }

            if ((relation.SourceID = xml.Attribute("sourceId")?.Value) == null)
                throw new Exception("No sourceID provided");
            if ((relation.TargetID = xml.Attribute("targetId")?.Value) == null)
                throw new Exception("No targetId provided");

            relation._type = relation.Type;
            relation._sourceID = relation.SourceID;
            relation._targetID = relation.TargetID;

            return relation;
        }

        public Relation Clone()
        {
            return new Relation() {
                Type = this.Type,
                SourceID = this.SourceID,
                TargetID = this.TargetID
            };
        }
    }
}
