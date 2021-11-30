using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;


namespace projectSA.Models {
    class Report {
        
        //private Solution solution {get; set;}
        public List<Message> messages {get; set;}
        public Solution solution {get;set;}
        public Report() {
            solution = new Solution(0.0f,1111,1111);
            messages = new List<Message>();
        }

        public Report Copy()
		{	
			Report copy = new Report();
            
            foreach(var m in this.messages){
                var copyMsg = new Message(m.Name);
                foreach(var link in m.Links){
                    copyMsg.Links.Add(new Link(link.Edge,link.Qnumber));
                }
                copyMsg.flow = m.flow;
                copyMsg.MaxE2E = m.MaxE2E;
                copy.messages.Add(copyMsg);

            }
			return copy;
		}


    public string toXML(string nameprefix)
	{
		string path = @"solutions/";
		string fullFilePath = path +nameprefix+ "solution.xml";    
		XmlWriterSettings settings = new XmlWriterSettings();
		settings.OmitXmlDeclaration = true;
    	settings.CloseOutput = false;
			
		using (XmlWriter xmlWriter = XmlWriter.Create(fullFilePath, settings))
		{
			xmlWriter.WriteStartElement("Report");
			xmlWriter.WriteRaw("\n");
            writeSolution(xmlWriter);

			messages.ForEach(m => {
                writeMessage(m,xmlWriter);
            });
			xmlWriter.WriteEndElement();  
    		xmlWriter.Flush();
		}
		return fullFilePath;
	}

    public void writeMessage(Message m, XmlWriter xmlWriter){
                xmlWriter.WriteRaw("\t");
                xmlWriter.WriteStartElement("Message");
                xmlWriter.WriteAttributeString("Name",m.Name);
                xmlWriter.WriteAttributeString("maxE2E",m.MaxE2E.ToString());
                xmlWriter.WriteRaw("\n");
                m.Links.ForEach(l =>{
                    writeLink(l,xmlWriter);
                });
                xmlWriter.WriteRaw("\t");
                xmlWriter.WriteEndElement();
                xmlWriter.WriteRaw("\n");
    }
    public void writeLink(Link l, XmlWriter xmlWriter){
        xmlWriter.WriteRaw("\t\t");
        xmlWriter.WriteStartElement("Link");
        xmlWriter.WriteAttributeString("Source",l.Source);
        xmlWriter.WriteAttributeString("Destination",l.Destination);
        xmlWriter.WriteAttributeString("Qnumber",l.Qnumber.ToString());
        xmlWriter.WriteRaw("\n");
        xmlWriter.WriteRaw("\t\t");
        xmlWriter.WriteEndElement();
        xmlWriter.WriteRaw("\n");
    }

    public void writeSolution(XmlWriter xmlWriter){
        xmlWriter.WriteRaw("\t");
        xmlWriter.WriteStartElement("Solution");
        xmlWriter.WriteAttributeString("Runtime",solution.Runtime.ToString());
        xmlWriter.WriteAttributeString("MeanE2E",solution.MeanE2E.ToString());
        xmlWriter.WriteAttributeString("MeanBW",solution.MeanBW.ToString());
        xmlWriter.WriteRaw("\n");
        xmlWriter.WriteEndElement();
        xmlWriter.WriteRaw("\n");
    }


    }

    class Solution {
        public float Runtime {get; set;}
        public int MeanE2E {get; set;}
        public int MeanBW {get; set;}
        public Solution(float runtime, int meanE2E, int meanBW){
            Runtime = runtime;
            MeanE2E = meanE2E;
            MeanBW = meanBW;
        }
    }

    class Message {
        
        private Flow _flow;
		public Flow flow { 
			get { return _flow; } 
			set {
				_flow = value;
				//MaxE2E = _flow.Deadline;
			} 
		}
        public string Name {get;}
        public int MaxE2E {get; set;}
        public List<Link> Links {get; set;}
        
        public Message(string name) {
            Name = name;
            Links = new List<Link>();
            MaxE2E = 0;
        }
    }

    class Link {
        public string Source {get;}
        public string Destination {get;}
        public Edge Edge {get;}
        public int PropagationDelay {get;}
        public int Qnumber {get; set;}
        public Link(Edge edge, int qnumber) {
            Edge = edge;
            //Source = source;
            //Destination = destination;
            Qnumber = qnumber;
            Source = edge.Source;
            Destination = edge.Destination;
            PropagationDelay = edge.PropagationDelay;
        }
    }

}