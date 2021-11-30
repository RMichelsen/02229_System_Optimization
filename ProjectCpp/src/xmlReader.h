#pragma once

#include <pugixml.hpp>
#include <unordered_map>
#include <cstdlib>

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
	Edge(int id, int bandwidth, int propagation_delay) :
		id(id),
		bandwidth(bandwidth),
		propagation_delay(propagation_delay) {}
	int id;
	int bandwidth;
	int propagation_delay;
};

struct Flow {
	Flow() = default;
	Flow(std::string name, std::string source, std::string destination, int size, int period, int deadline) :
		name(name),
		source(source),
		destination(destination),
		size(size),
		period(period),
		deadline(deadline) {}
	std::string name;
	std::string source;
	std::string destination;
	int size;
	int period;
	int deadline;
};

bool loadTestCase(TestCase tc, std::unordered_map<std::string, Edge>& edges, std::unordered_map<std::string, Flow>& flows);


