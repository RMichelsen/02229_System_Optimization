using System.Xml.Linq;
using projectSA.Models;
using System.Collections.Generic;
using System.Linq;

namespace projectSA
{
    enum TestCase
    {
        TC0,
        TC1,
        TC2,
        TC3,
        TC4,
        TC5,
        TC6,
        TC7,
        TC8,
        TC9,
        TC10,
        TC11,
        TC12,
    }

    enum InputFileType
    {
        Architecture,
        Application
    }

    static class XMLReader
    {
        public static void Read(TestCase testCase, out Architecture architecture, 
            out Application application)
        {
            var architectureXML = XDocument.Load(GetInputFilePath(testCase, InputFileType.Architecture));
            var vertexEntries = architectureXML.Root.Descendants("Vertex");
            var edgeEntries = architectureXML.Root.Descendants("Edge");

            var vertices = new List<Vertex>();
            foreach(var entry in vertexEntries)
            {
                vertices.Add(new Vertex((string)entry.Attribute("Name")));
            }
            var edges = new List<Edge>();
            foreach(var edge in edgeEntries)
            {
                edges.Add(new Edge(
                    (string)edge.Attribute("Id"),
                    (string)edge.Attribute("Source"),
                    (string)edge.Attribute("Destination"),
                    (int)edge.Attribute("BW"),
                    (int)edge.Attribute("PropDelay")
                ));

                edges.Add(new Edge(
                    ((int)edge.Attribute("Id")+edgeEntries.Count()).ToString(),
                    (string)edge.Attribute("Destination"),
                    (string)edge.Attribute("Source"),
                    (int)edge.Attribute("BW"),
                    (int)edge.Attribute("PropDelay")
                ));


            }
            architecture = new Architecture(vertices, edges);

            var applicationXML = XDocument.Load(GetInputFilePath(testCase, InputFileType.Application));
            var flowEntries = applicationXML.Root.Descendants("Message");

            var flows = new List<Flow>();
            foreach(var flow in flowEntries)
            {
                flows.Add(new Flow(
                    (string)flow.Attribute("Name"),
                    (string)flow.Attribute("Source"),
                    (string)flow.Attribute("Destination"),
                    (int)flow.Attribute("Size"),
                    (int)flow.Attribute("Period"),
                    (int)flow.Attribute("Deadline")
                ));
            }
            
            application = new Application(flows);
        }

        private static string GetInputFilePath(TestCase testCase, InputFileType inputFileType)
        {
            switch(inputFileType)
            {
                case InputFileType.Architecture:
                    return $"Tests/{testCase.ToString()}/Input/Config.xml";
                case InputFileType.Application:
                    return $"Tests/{testCase.ToString()}/Input/Apps.xml";
                default:
                    return "";
            }
        }
    }
}
