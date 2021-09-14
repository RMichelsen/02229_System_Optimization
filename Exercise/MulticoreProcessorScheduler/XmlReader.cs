using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using MulticoreProcessorScheduler.Models;

namespace MulticoreProcessorScheduler
{
    class XmlReader
    {
        public static void Read(ImportFileSize importFileSize, out List<Task> tasks, out List<Processor> processors) {
            tasks = new List<Task>();
            processors = new List<Processor>();

            //load file
            string inputfilePath = GetInputFilePath(importFileSize);
            var xml = XDocument.Load(inputfilePath);
            
            //get xml lists from file
            var mcps = xml.Root.Descendants("Platform").Descendants("MCP");
            var taskElements = xml.Root.Descendants("Application").Descendants("Task");

            //load in mcps
            foreach (var mcp in mcps)
            {
                string mcpId = (string)mcp.Attribute("Id");
                var processor = new Processor(mcpId);

                var cores = mcp.Descendants("Core")
                    .Select(core => {
                        string id = (string)core.Attribute("Id");
                        decimal wcetFactor = ((decimal)core.Attribute("WCETFactor"));

                        return new Core(id, mcpId, wcetFactor);
                    }).ToList();

                processor.Cores = cores;
                processors.Add(processor);
            }

            //load tasks
            foreach (var taskElem in taskElements)
            {
                string id = (string)taskElem.Attribute("Id");
                int deadLine = ((int)taskElem.Attribute("Deadline"));
                int period = ((int)taskElem.Attribute("Period"));
                int wcet = ((int)taskElem.Attribute("WCET"));

                tasks.Add(new Task(id, deadLine, period, wcet));
            }
        }

        private static string GetInputFilePath(ImportFileSize importFileSize)
        {
            // string path = @"..\..\";
            string path = @"test_cases/";
            switch(importFileSize) {
                case ImportFileSize.Small:
                    return $"{path}small.xml";
                case ImportFileSize.Medium:
                    return $"{path}medium.xml";
                case ImportFileSize.Large:
                    return $"{path}large.xml";
                default:
                    throw new ArgumentException("Unknown type");
            }
        }
    }

    enum ImportFileSize
    {
        Small,
        Medium,
        Large
    }
}