
#include "xmlReader.h"
#include <iostream>

using namespace std;

//unordered_map<string, Edge> edges = {
//	{ "ES1SW1", Edge(1, 1000, 10) },
//	{ "SW1ES2", Edge(2, 1000, 10) },
//	{ "ES1SW2", Edge(3, 1000, 10) },
//	{ "SW2ES2", Edge(4, 1000, 10) },
//	{ "SW2SW1", Edge(5, 1000, 10) }
//};
//
//unordered_map<string, Flow> flows = {
//	{ "F1", Flow("F1", "ES1", "ES2", 300, 1000, 1000) },
//	{ "F2", Flow("F2", "ES1", "ES2", 400, 2000, 2000) },
//	{ "F3", Flow("F3", "ES1", "ES2", 500, 4000, 4000) },
//	{ "F4", Flow("F4", "ES1", "ES2", 300, 8000, 4000) },
//	{ "F5", Flow("F5", "ES2", "ES1", 400, 1000, 1000) },
//	{ "F6", Flow("F6", "ES2", "ES1", 500, 2000, 2000) },
//	{ "F7", Flow("F7", "ES2", "ES1", 300, 4000, 4000) },
//	{ "F8", Flow("F8", "ES1", "ES2", 400, 8000, 4000) }
//};

bool loadTestCase(TestCase tc, unordered_map<string, Edge> &edges, unordered_map<string, Flow> &flows) {
	string path = getTestCasePath(tc) + "Input\\";

	string configPath = path + "Config.xml";
	string appsPath = path + "Apps.xml";

	pugi::xml_document configDoc;
	pugi::xml_parse_result configXml = configDoc.load_file(configPath.c_str());
	if (!configXml)
		return -1;

	pugi::xml_document appsDoc;
	pugi::xml_parse_result appsXml = appsDoc.load_file(appsPath.c_str());
	if (!appsXml)
		return -1;

	int numEdges = 0;
	for(pugi::xml_node edge : configDoc.child("Architecture").children("Edge")) {
		numEdges++;
	}

	string linkName;
	int id;
	int bw;
	int delay;
	for (pugi::xml_node edge : configDoc.child("Architecture").children("Edge"))
	{
		string source = (string) edge.attribute("Source").value();
		string destination = (string) edge.attribute("Destination").value();
		linkName = source + destination;
		id = (int) edge.attribute("Id").as_int();
		bw = (int) edge.attribute("BW").as_int();
		delay = (int) edge.attribute("PropDelay").as_int();
		edges[linkName] = Edge(id, bw, delay, source, destination);
		linkName = destination + source;
		edges[linkName] = Edge(id + numEdges, bw, delay, destination, source);
	}

	string flowName;
	string src;
	string dest;
	int size;
	int period;
	int deadLine;
	for (pugi::xml_node flow : appsDoc.child("Application").children("Message"))
	{
	
		flowName = (string)flow.attribute("Name").value();
		src = (string)flow.attribute("Source").value();
		dest = (string)flow.attribute("Destination").value();

		int size = (int)flow.attribute("Size").as_int();
		int period = (int)flow.attribute("Period").as_int();
		int deadLine = (int)flow.attribute("Deadline").as_int();

		flows[flowName] = *(new Flow(flowName, src, dest, size, period, deadLine));
	}

	return true;
}
