#pragma once

#include <pugixml.hpp>
#include <unordered_map>

using namespace std;

enum TestCase {
	example,
	TC1,
	TC2,
	TC3,
	TC4,
	TC5,
	TC6,
	TC7,
	TC8,
	TC9
};

struct Edge {
	Edge() = default;
	Edge(int id, int bandwidth, int propagation_delay, string src, string dest) :
		id(id),
		bandwidth(bandwidth),
		propagation_delay(propagation_delay),
		src(src),
		dest(dest) {}
	Edge(int id, int bandwidth, int propagation_delay) :
		id(id),
		bandwidth(bandwidth),
		propagation_delay(propagation_delay) {}
	int id;
	string src;
	string dest;
	int bandwidth;
	int propagation_delay;
};

struct Flow {
	Flow() = default;
	Flow(string name, string source, string destination, int size, int period, int deadline) :
		name(name),
		source(source),
		destination(destination),
		size(size),
		period(period),
		deadline(deadline) {}
	string name;
	string source;
	string destination;
	int size;
	int period;
	int deadline;
};

bool loadTestCase(TestCase tc, unordered_map<string, Edge>& edges, unordered_map<string, Flow>& flows);


