#pragma once

#include "xmlReader.h"
#include <unordered_map>
#include <cstdlib>

typedef std::unordered_map<std::string, Edge> edgeMap;
typedef std::unordered_map<std::string, Flow> flowMap;

std::unordered_map<std::string, std::vector<std::vector<std::string>>> getFlowPaths(edgeMap edges, flowMap flows);
