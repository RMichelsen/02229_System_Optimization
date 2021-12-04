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

inline string getTestCasePath(TestCase tc) {
	string res = "Tests\\";
	switch(tc) {
	case example:
		res += "example\\";
		break;
	case TC1:
		res += "TC1\\";
		break;
	case TC2:
		res += "TC2\\";
		break;
	case TC3:
		res += "TC3\\";
		break;
	case TC4:
		res += "TC4\\";
		break;
	case TC5:
		res += "TC5\\";
		break;
	case TC6:
		res += "TC6\\";
		break;
	case TC7:
		res += "TC7\\";
		break;
	case TC8:
		res += "TC8\\";
		break;
	case TC9:
		res += "TC9\\";
		break;
	}

	return res;
}

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


