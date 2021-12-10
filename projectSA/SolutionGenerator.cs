using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml; 
using projectSA;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.Observers;
using QuikGraph.Algorithms.Search;
using QuikGraph.Algorithms.ShortestPath;
using QuikGraph.Algorithms.RankedShortestPath;
using projectSA.Models;


namespace projectSA
{
    class SolutionGenerator{
    static Random rnd = new Random();
    public static Dictionary<string,Flow> flows;
    public static Dictionary<string, List<List<Edge>>> flow_paths {get; set;}


 	
    public SolutionGenerator(Dictionary<string,Flow> Flows, Dictionary<string, List<List<Edge>>> Flow_paths){
        flows = Flows;
        flow_paths = Flow_paths;
    }
    public Report GetInititalSolution(){
             
        var report = new Report();

        foreach(var flowName in flow_paths.Keys){
            //Console.WriteLine(flowName);
            var msg = new Message(flowName);
            var index = rnd.Next(flow_paths[flowName].Count());
            var path = flow_paths[flowName][index];
            msg.pathIndex = index;

            foreach(var edge in path){
                var link = new Link(edge,rnd.Next(1,4)); //q can be either 1,2 or 3 but Random.Next starts 0 normally, so we get a random int between 0+1 and 3+1.
                msg.Links.Add(link);
            }
            report.messages.Add(msg);
            msg.flow = flows[flowName];
            
            
        }
        return report;
        
    }

    public Report GenerateExampleReport(){
        Edge ES1SW1 = new Edge("1","ES1","SW1", 1000, 10);
        Edge SW1ES2 = new Edge("2","SW1","ES2", 1000, 10);
        Edge ES1SW2 = new Edge("3","ES1","SW2", 1000, 10);
        Edge SW2ES2 = new Edge("4","SW2","ES2", 1000, 10);
        Edge SW2SW1 = new Edge("5","SW2","SW1", 1000, 10);

        flows = new Dictionary<string, Flow>{
            { "F1", new Flow("F1", "ES1", "ES2", 300, 1000, 1000) },
            { "F2", new Flow("F2", "ES1", "ES2", 400, 2000, 2000) },
            { "F3", new Flow("F3", "ES1", "ES2", 500, 4000, 4000) },
            { "F4", new Flow("F4", "ES1", "ES2", 300, 8000, 4000) },
            { "F5", new Flow("F5", "ES2", "ES1", 400, 1000, 1000) },
            { "F6", new Flow("F6", "ES2", "ES1", 500, 2000, 2000) },
            { "F7", new Flow("F7", "ES2", "ES1", 300, 4000, 4000) },
            { "F8", new Flow("F8", "ES1", "ES2", 400, 8000, 4000) },
        };


        Report example = new Report();

        List<Message> msgList = new List<Message>();
       
        var msg = new Message("F1");
        msg.flow = flows[msg.Name];
        msg.Links.Add(new Link(ES1SW1,2));
        msg.Links.Add(new Link(SW1ES2,1));
        msgList.Add(msg);

        msg = new Message("F2");
        msg.flow = flows[msg.Name];
        msg.Links.Add(new Link(ES1SW2,1));
        msg.Links.Add(new Link(SW2ES2,3));
        msgList.Add(msg);


        msg = new Message("F3");
        msg.flow = flows[msg.Name];
        msg.Links.Add(new Link(ES1SW2,3));
        msg.Links.Add(new Link(SW2ES2,3));
        msgList.Add(msg);

        msg = new Message("F4");
        msg.flow = flows[msg.Name];
        msg.Links.Add(new Link(ES1SW1,1));
        msg.Links.Add(new Link(SW1ES2,3));
        msgList.Add(msg);

        msg = new Message("F5");
        msg.flow = flows[msg.Name];
        msg.Links.Add(new Link(SW2ES2,2));
        msg.Links.Add(new Link(ES1SW2,2));
        msgList.Add(msg);

        msg = new Message("F6");
        msg.flow = flows[msg.Name];
        msg.Links.Add(new Link(SW2ES2,2));
        msg.Links.Add(new Link(ES1SW2,1));
        msgList.Add(msg);

        msg = new Message("F7");
        msg.flow = flows[msg.Name];
        msg.Links.Add(new Link(SW2ES2,3));
        msg.Links.Add(new Link(ES1SW2,2));
        msgList.Add(msg);

        msg = new Message("F8");
        msg.flow = flows[msg.Name];
        msg.Links.Add(new Link(ES1SW2,2));
        msg.Links.Add(new Link(SW2ES2,1));
        msgList.Add(msg);

        foreach(var m in msgList){
            example.messages.Add(m);
        }

        return example;
    }

    public Report GenerateNeighbour(Report report) {
            if(rnd.NextDouble() > 0.7) {
				return changeFlowPath(report);
            }
			else {
				return swapQ(report);
            }
    }

    public Report swapQ(Report report)
    {
        var neighbour = new Report();
        neighbour = report.Copy();
        var msg = neighbour.messages[rnd.Next(neighbour.messages.Count())];
        var link = msg.Links[rnd.Next(msg.Links.Count())];
        var newQ = 0;
        do{
            newQ = rnd.Next(1,4);
        }while(newQ == link.Qnumber);

        link.Qnumber = newQ;
        
        return neighbour;
    }
   
    public Report changeFlowPath(Report report)
    {
        var neighbour = new Report();
        neighbour = report.Copy();
        var msg = neighbour.messages[rnd.Next(neighbour.messages.Count())];

        int pathIndex;
        List<Edge> newPath;

        do{
            pathIndex = rnd.Next(flow_paths[msg.Name].Count());
            newPath = flow_paths[msg.Name][pathIndex];
        }while(pathIndex == msg.pathIndex);
        
        msg.Links = new List<Link>();
        foreach(var edge in newPath){
            msg.Links.Add(new Link(edge,rnd.Next(1,4)));
        }
        
        return neighbour;
    }

    }   
 }