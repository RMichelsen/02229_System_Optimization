using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Project.Models {
    class Report {
        
        private Solution solution {get; set;}
        private List<Message> messages {get; set;}
        public Report() {
            solution = new Solution();
            messages = new List<Message>();
        }
    }

    class Solution {
        private float Runtime {get;}
        private int MeanE2E {get;}
        private int MeanBW {get;} 
        public Solution(float runtime, int meanE2E, int meanBW) {
            Runtime = runtime;
            MeanE2E = meanE2E;
            MeanBW = meanBW;
        }
    }

    class Message {
        private String Name {get;}
        private int MaxE2E {get; set;}
        private List<Link> Links {get; set;}
        public Message(String name) {
            Name = Name;
            Links = new List<Link>();
        }
    }

    class Link {
        private Vertex Source {get;}
        private Vertex Destination {get;}
        private int Qnumber {get;}
        public Link(Vertex source, Vertex destination, int qnumber) {
            Source = source;
            Destination = destination;
            Qnumber = qnumber;
        }
    }
}