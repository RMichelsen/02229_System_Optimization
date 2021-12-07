#define _ITERATOR_DEBUG_LEVEL 0

#include <ortools/base/logging.h>
#include <ortools/constraint_solver/constraint_solver.h>
#include <pugixml.hpp>

#include "xmlReader.h"
#include "graph.h"
#include <cstdlib>
#include <filesystem>
#include <fstream>

using namespace operations_research;

constexpr int CYCLE_LENGTH = 12;
int printFile = 1;
string subfolder = "";

TestCase oldTestCase;
edge_map_t oldEdges;
flow_map_t oldFlows;
flow_paths_t oldFlow_paths;


string getStrategyString(Solver::IntValueStrategy strat) {
	switch (strat)
	{
	case operations_research::Solver::INT_VALUE_DEFAULT:
	case operations_research::Solver::INT_VALUE_SIMPLE:
	case operations_research::Solver::ASSIGN_MIN_VALUE:
	case operations_research::Solver::SPLIT_LOWER_HALF:
		return "min";
	case operations_research::Solver::ASSIGN_MAX_VALUE:
	case operations_research::Solver::SPLIT_UPPER_HALF:
		return "max";
	case operations_research::Solver::ASSIGN_RANDOM_VALUE:
		return "rand";
	case operations_research::Solver::ASSIGN_CENTER_VALUE:
		return "center";
	default:
		return "unknown";
	}
}

int Solve(TestCase test_case, Solver::IntValueStrategy q_choice_strat, Solver::IntValueStrategy path_choice_strat, int millis, string fileTag = "") {
	edge_map_t edges;
	flow_map_t flows;
	flow_paths_t flow_paths;
	if (test_case == oldTestCase && oldEdges.size() != 0) {
		edges = oldEdges;
		flows = oldFlows;
		flow_paths = oldFlow_paths;
	}
	else {
		loadTestCase(test_case, edges, flows);
		flow_paths = getFlowPaths(edges, flows);
		
		oldEdges = edges;
		oldFlows = flows;
		oldFlow_paths = flow_paths;
		oldTestCase = test_case;
	}


	int least_common_multiple = 1;
	for(const auto &[_, flow] : flows) {
		least_common_multiple = std::lcm(least_common_multiple, flow.period);
	}
	int cycle_count = static_cast<int>(std::ceil(static_cast<float>(least_common_multiple) / static_cast<float>(CYCLE_LENGTH)));

	Solver solver("ConstraintSolver");
	std::vector<IntVar *> path_variables;
	std::vector<IntVar *> q_variables;

	std::unordered_map<std::string, IntVar *> path_choices;
	for(const auto &[flow, paths] : flow_paths) {
		path_choices[flow] = solver.MakeIntVar(0, paths.size() - 1, "path_choice" + flow);
		path_variables.push_back(path_choices[flow]);
	}

	size_t longest_path = 0;
	for(const auto &[flow, paths] : flow_paths) {
		for(const auto &path : paths) {
			longest_path = std::max(path.size(), longest_path);
		}
	}

	// This array will provide an integer in domain {0, 1}, 0 if the index is an edge which is part of path p of flow f.
	// edge_validity[f][e][p] - flow [f], edge [e] and path [p]
	std::vector<std::vector<std::vector<int>>> edge_validity(flows.size());

	// Set all edges in the array to 0 (invalid)
	int f = 0;
	for(const auto &[flow, paths] : flow_paths) {
		edge_validity[f].resize(edges.size() + 1);
		for(int e = 0; e <= edges.size(); e++) {
			edge_validity[f][e].resize(paths.size(), 0);
		}

		f++;
	}

	// Set all potentially valid edges to 1
	f = 0;
	for(const auto &[flow, paths] : flow_paths) {
		for(int e = 0; e <= edges.size(); e++) {
			for(int p = 0; p < paths.size(); p++) {
				for(const auto &edge_name : paths[p]) {
					for(const auto &[name, edge] : edges) {
						if(edge_name == name) {
							edge_validity[f][edge.id][p] = 1;
						}
					}
				}
			}
		}

		f++;
	}

	// Pack edge values in int vectors
	// edge_X[f][e][p] - flow [f], edge [e] and path [p]
	std::vector<std::vector<std::vector<int>>> edge_ids(flows.size(), std::vector<std::vector<int>>(longest_path));
	std::vector<std::vector<std::vector<int>>> edge_bandwidths(flows.size(), std::vector<std::vector<int>>(longest_path));
	std::vector<std::vector<std::vector<int>>> edge_propagation_delays(flows.size(), std::vector<std::vector<int>>(longest_path));

	f = 0;
	for(const auto &[flow, paths] : flow_paths) {
		for(int e = 0; e < longest_path; e++) {
            edge_ids[f][e].resize(paths.size());
            edge_bandwidths[f][e].resize(paths.size());
            edge_propagation_delays[f][e].resize(paths.size());
			for(int p = 0; p < paths.size(); p++) {
				const auto &path = paths[p];
				bool valid_edge = e < path.size();
				edge_ids[f][e][p] = valid_edge ? edges[path[e]].id : 0;
				edge_bandwidths[f][e][p] = valid_edge ? edges[path[e]].bandwidth : -1;
				edge_propagation_delays[f][e][p] = valid_edge ? edges[path[e]].propagation_delay : -1;
			}
		}

		f++;
	}

	// arrival_patterns[c][f][e] - cycle [c], flow [f] and edges [e]
	std::vector<std::vector<std::vector<IntVar *>>> arrival_patterns;
	arrival_patterns.resize(cycle_count);
	for(int c = 0; c < cycle_count; c++) {
		arrival_patterns[c].resize(flows.size());
		for(f = 0; f < flows.size(); f++) {
			arrival_patterns[c][f].resize(edges.size() + 1);
			arrival_patterns[c][f][0] = solver.MakeIntConst(0);
			for(int e = 1; e <= edges.size(); e++) {
				arrival_patterns[c][f][e] = solver.MakeIntVar(0, std::numeric_limits<int32_t>::max());
			}
		}
	}

	std::vector<IntVar *> max_e2e_delays;
	f = 0;
	for(const auto &[flow_name, flow] : flows) {
		std::vector<IntVar *> e2e_delays;
		IntVar *path_choice = path_choices[flow_name];

		for(int e = 0; e < longest_path; e++) {
			IntVar *edge_id = solver.MakeElement(edge_ids[f][e], path_choice)->Var();
			IntExpr *edge_propagation_delay = solver.MakeElement(edge_propagation_delays[f][e], path_choice);

			// ceil(edge_propagation_delay / CYCLE_LENGTH) manually
			// -- there is no ceil function in the constraint solver library
			IntExpr *induced_delay = solver.MakeSum(solver.MakeDiv(solver.MakeSum(edge_propagation_delay, -1), CYCLE_LENGTH), 1);
			IntVar *q = solver.MakeIntVar(1, 3, flow_name + "_e_" + std::to_string(e) + "_q");
			q_variables.push_back(q);

			IntExpr *alpha = solver.MakeSum(q, solver.MakeSum(e2e_delays));

			// Calculate an e2e delay candidate, multiply by 0 if the edge is invalid (not part of solution)
			IntExpr *e2e_delay_candidate = solver.MakeSum(q, induced_delay);
			IntExpr *is_valid_edge = solver.MakeIsDifferentCstVar(edge_id, 0);

			IntExpr *e2e_delay = solver.MakeProd(e2e_delay_candidate, is_valid_edge);
			e2e_delays.push_back(e2e_delay->VarWithName(flow_name + "_e_" + std::to_string(e) + "_delay"));

			for(int c = 0; c < cycle_count; c++) {
				// Modulo (c - a) * |c| % flow.period
				// Modulo without modulo: a % b = a - (b * int(a / b))
				// a = c * |c|
				// b = flow.period
				IntExpr *A_input = solver.MakeDifference(c, alpha);
				IntExpr *alpha_ms = solver.MakeProd(A_input, CYCLE_LENGTH);
				IntExpr *A_calc = solver.MakeDifference(
					alpha_ms,
					solver.MakeProd(solver.MakeDiv(alpha_ms, flow.period), flow.period)
				);

				// Boolean variable used in the arrival pattern functions, 
				// also an extra boolean to set the output to 0 if the input is negative
				IntVar *A_input_positive_check = solver.MakeIsGreaterOrEqualCstVar(A_input, 0);
				IntVar *A_boolean = solver.MakeIsLessCstVar(A_calc, CYCLE_LENGTH);
				IntExpr *composite_boolean = solver.MakeProd(A_input_positive_check, A_boolean);

				// Calculate the arrival pattern, multiply by 0 if the edge is invalid (not part of solution)
				//IntVar *A_candidate = solver.MakeIntVar(0, std::numeric_limits<int32_t>::max());
				//solver.AddConstraint(solver.MakeIfThenElseCt(composite_boolean->Var(), solver.MakeIntConst(flow.size), solver.MakeIntConst(0), A_candidate));
				//IntVar *A = solver.MakeProd(A_candidate, is_valid_edge)->Var();
				IntVar* A = solver.MakeProd(solver.MakeProd(composite_boolean, flow.size), is_valid_edge)->Var();

				// Set the corresponding element of the arrival_patterns array, indexing with the edge ID integer variable.
				IntExpr *E = solver.MakeElement(arrival_patterns[c][f], edge_id);
				solver.AddConstraint(solver.MakeEquality(A, E));
			}
		}

		IntExpr *e2e_delay_sum = solver.MakeSum(e2e_delays);
		max_e2e_delays.push_back(e2e_delay_sum->Var());

		int acceptableDelay = static_cast<int>(std::ceil(static_cast<float>(flow.deadline) / static_cast<float>(CYCLE_LENGTH)));
		solver.AddConstraint(solver.MakeSumLessOrEqual(e2e_delays, acceptableDelay));

		f++;
	}

	// cycle_edge_bandwidths[e][c] - edge [e] and cycle [c] 
	std::vector<std::vector<IntVar *>> cycle_edge_bandwidths;
	cycle_edge_bandwidths.resize(edges.size() + 1);
	for(int e = 1; e <= edges.size(); e++) {
		cycle_edge_bandwidths[e].resize(cycle_count);
		for(int c = 0; c < cycle_count; c++) {
			std::vector<IntVar *> tmp_sum;

			for(int f = 0; f < flows.size(); f++) {
				// TODO: Make efficient
				auto item = flows.begin();
				std::advance(item, f);
				IntVar *path_choice = path_choices[item->second.name];
				IntExpr *arrival_pattern = arrival_patterns[c][f][e];
				IntExpr *is_valid_edge = solver.MakeElement(edge_validity[f][e], path_choice);
				// Add partial arrival pattern sum, multiply by 0 if the edge is invalid (not part of solution)
				tmp_sum.push_back(solver.MakeProd(arrival_pattern, is_valid_edge)->Var());
			}

			cycle_edge_bandwidths[e][c] = solver.MakeSum(tmp_sum)->Var();

			Edge edge = (*std::find_if(edges.begin(), edges.end(), [=](const auto &kv) { return kv.second.id == e; })).second;
			int edge_capacity = int(edge.bandwidth * 131072.0f * 0.000012f);

			solver.AddConstraint(solver.MakeLessOrEqual(cycle_edge_bandwidths[e][c], edge_capacity));
		}
	}

	// edge_consumed_bandwidths[e] - edge [e]
	std::vector<IntVar *> edge_consumed_bandwidths(edges.size() + 1);
	edge_consumed_bandwidths[0] = solver.MakeIntConst(0);
	for(int e = 1; e <= edges.size(); e++) {
		IntExpr *max_consumed_bandwidth = solver.MakeMax(cycle_edge_bandwidths[e]);

		Edge edge = (*std::find_if(edges.begin(), edges.end(), [=](const auto &kv) { return kv.second.id == e; })).second;
        int edge_capacity = int(edge.bandwidth * 131072.0f * 0.000012f);

		edge_consumed_bandwidths[e] = solver.MakeDiv(solver.MakeProd(max_consumed_bandwidth, 1000), edge_capacity)->VarWithName("Edge_" + std::to_string(e) + "_Bandwidth");
	}

	IntVar *mean_bandwidth_usage = solver.MakeDiv(solver.MakeSum(edge_consumed_bandwidths), edges.size())->VarWithName("mean_bandwidth_usage");

	OptimizeVar *omega = solver.MakeMinimize(mean_bandwidth_usage, 10);

	LOG(INFO) << "Number of constraints: " << solver.constraints();

	DecisionBuilder *const db1 = solver.MakePhase(
		path_variables,
		Solver::CHOOSE_FIRST_UNBOUND,
		path_choice_strat
	);
	DecisionBuilder *const db2 = solver.MakePhase(
		q_variables,
		Solver::CHOOSE_FIRST_UNBOUND,
		q_choice_strat
	);
	DecisionBuilder *const db = solver.Compose(db1, db2);
	SearchMonitor *const search_log = solver.MakeSearchLog(100000, omega);
	RegularLimit *const time_limit = solver.MakeTimeLimit(millis);
	SolutionCollector *const collector = solver.MakeLastSolutionCollector();

	collector->Add(path_variables);
	collector->Add(q_variables);
	collector->Add(max_e2e_delays);
	collector->AddObjective(mean_bandwidth_usage);
	solver.Solve(db, omega, search_log, time_limit, collector);

	if (collector->solution_count() == 0) {
		cout << "Could not find solution within given time constraint.." << endl << endl;
		return -1;
	}

	if (printFile == 0) {
		return collector->objective_value(0);
	}

	LOG(INFO) << collector->solution(0)->DebugString();
	cout << endl;

	pugi::xml_document result_xml;
	auto report = result_xml.append_child("Report");
	auto solution = report.append_child("Solution");
	solution.append_attribute("Runtime") = static_cast<float>(collector->wall_time(0)) / 1000.0f;
	int mean_e2e = 0;
	for(f = 0; f < flows.size(); f++) { mean_e2e += collector->Value(0, max_e2e_delays[f]); }
	solution.append_attribute("MeanE2E") = mean_e2e / flows.size();
	solution.append_attribute("MeanBW") = collector->objective_value(0);

	f = 0;
	for(const auto &[flow_name, flow] : flows) {
		auto message = report.append_child("Message");
		message.append_attribute("Name") = flow_name.c_str();
		message.append_attribute("maxE2E") = collector->Value(0, max_e2e_delays[f]);
		int chosen_path = collector->Value(0, path_variables[f]);

		int e = 0;
		for(const auto &edge : flow_paths[flow_name][chosen_path]) {
			auto link = message.append_child("Link");
			link.append_attribute("Source") = edge.substr(0, 3).c_str();
			link.append_attribute("Destination") = edge.substr(3, 3).c_str();
			IntVar *q_var = *std::find_if(q_variables.begin(), q_variables.end(), [=](const auto &s) { 
				return s->name() == flow_name + "_e_" + std::to_string(e) + "_q"; 
			});
			link.append_attribute("Qnumber") = collector->Value(0, q_var);

			e++;
		}

		f++;
	}

	string arguments = "";
	arguments += "_q" + getStrategyString(q_choice_strat);
	arguments += "_p" + getStrategyString(path_choice_strat);
	arguments += "_" + to_string(millis);
	
	string path = getTestCasePath(test_case) + "Output";
	string fileName = "Report" + arguments + fileTag +".xml";
	std::filesystem::create_directory(path + "\\" + subfolder);
	auto b = result_xml.save_file((path + "\\" + subfolder + "\\" + fileName).c_str());

	return collector->objective_value(0);
}

/*
	method ideas:
		- repeat x times and saves all files to subfolder, output objective value statistics
		- generate min/rand mix files
		- 
*/

void testHelper1(TestCase tc, Solver::IntValueStrategy q_choice, Solver::IntValueStrategy path_choice, int millis, int repeats, string pathToResultFile = "")
{
	string arguments;
	string tcStr = tc == example? "example" : getTestCasePath(tc).substr(6, 3);
	arguments += tcStr;
	arguments += "_q" + getStrategyString(q_choice);
	arguments += "_p" + getStrategyString(path_choice);
	arguments += "_" + to_string(millis);
	arguments += "_" + to_string(repeats) + "repeats";
	
	printFile = 0;

	vector<int> obj_values;
	int obj_val;
	for (int i = 0; i < repeats; i++)
	{
		obj_val = Solve(tc, q_choice, path_choice, millis);
		if (obj_val != -1) 
			obj_values.push_back(obj_val);
	}

	cout << endl << "Arguments: " << arguments << endl << endl;
	cout << "Objective values: " << endl;

	cout << "\t[";
	for (int val : obj_values) {
		cout << to_string(val) << ",";
	}
	cout << "]" << endl;

	int obj_sum = std::accumulate(obj_values.begin(), obj_values.end(), 0);
	int avgObj = obj_values.size() != 0 ? obj_sum / obj_values.size() : 0;
	cout << "Avg. obj. value: " << to_string(avgObj) << endl << endl;

	if (pathToResultFile != "")
	{
		string path = getTestCasePath(tc) + "Output";
		ofstream result_file(path + "\\" + pathToResultFile, std::ios_base::app);
		if (result_file.is_open())
		{
			result_file.seekp(0, ios::end);
			result_file << endl << "Arguments: " << arguments << endl << endl;
			result_file << "Objective values: " << endl;

			result_file << "\t[";
			for (int val : obj_values) {
				result_file << to_string(val) << ",";
			}
			result_file << "]" << endl;

			result_file << "Avg. obj. value: " << to_string(avgObj) << endl << endl;
		}
	}

}

int main(int argc, char **argv) {
	google::InitGoogleLogging(argv[0]);
	absl::SetFlag(&FLAGS_logtostderr, 1);

	Solver::IntValueStrategy min = Solver::ASSIGN_MIN_VALUE;
	Solver::IntValueStrategy max = Solver::ASSIGN_MAX_VALUE;
	Solver::IntValueStrategy random = Solver::ASSIGN_RANDOM_VALUE;
	Solver::IntValueStrategy center = Solver::ASSIGN_CENTER_VALUE;

	string resultfile = "crossTest.txt";
	TestCase tc = TC1;
	int millis = 1000;
	int repeats = 10;

	testHelper1(tc, min, min, millis, repeats, resultfile);
	testHelper1(tc, min, max, millis, repeats, resultfile);
	testHelper1(tc, min, center, millis, repeats, resultfile);
	testHelper1(tc, min, random, millis, repeats, resultfile);
	testHelper1(tc, max, min, millis, repeats, resultfile);
	testHelper1(tc, max, max, millis, repeats, resultfile);
	testHelper1(tc, max, center, millis, repeats, resultfile);
	testHelper1(tc, max, random, millis, repeats, resultfile);
	testHelper1(tc, center, min, millis, repeats, resultfile);
	testHelper1(tc, center, max, millis, repeats, resultfile);
	testHelper1(tc, center, center, millis, repeats, resultfile);
	testHelper1(tc, center, random, millis, repeats, resultfile);
	testHelper1(tc, random, min, millis, repeats, resultfile);
	testHelper1(tc, random, max, millis, repeats, resultfile);
	testHelper1(tc, random, center, millis, repeats, resultfile);
	testHelper1(tc, random, random, millis, repeats, resultfile);
	
	millis = 10000;
	tc = TC5;

	testHelper1(tc, min, min, millis, repeats, resultfile);
	testHelper1(tc, min, max, millis, repeats, resultfile);
	testHelper1(tc, min, center, millis, repeats, resultfile);
	testHelper1(tc, min, random, millis, repeats, resultfile);
	testHelper1(tc, max, min, millis, repeats, resultfile);
	testHelper1(tc, max, max, millis, repeats, resultfile);
	testHelper1(tc, max, center, millis, repeats, resultfile);
	testHelper1(tc, max, random, millis, repeats, resultfile);
	testHelper1(tc, center, min, millis, repeats, resultfile);
	testHelper1(tc, center, max, millis, repeats, resultfile);
	testHelper1(tc, center, center, millis, repeats, resultfile);
	testHelper1(tc, center, random, millis, repeats, resultfile);
	testHelper1(tc, random, min, millis, repeats, resultfile);
	testHelper1(tc, random, max, millis, repeats, resultfile);
	testHelper1(tc, random, center, millis, repeats, resultfile);
	testHelper1(tc, random, random, millis, repeats, resultfile);

	millis = 20000;
	tc = TC9;

	testHelper1(tc, min, min, millis, repeats, resultfile);
	testHelper1(tc, min, max, millis, repeats, resultfile);
	testHelper1(tc, min, center, millis, repeats, resultfile);
	testHelper1(tc, min, random, millis, repeats, resultfile);
	testHelper1(tc, max, min, millis, repeats, resultfile);
	testHelper1(tc, max, max, millis, repeats, resultfile);
	testHelper1(tc, max, center, millis, repeats, resultfile);
	testHelper1(tc, max, random, millis, repeats, resultfile);
	testHelper1(tc, center, min, millis, repeats, resultfile);
	testHelper1(tc, center, max, millis, repeats, resultfile);
	testHelper1(tc, center, center, millis, repeats, resultfile);
	testHelper1(tc, center, random, millis, repeats, resultfile);
	testHelper1(tc, random, min, millis, repeats, resultfile);
	testHelper1(tc, random, max, millis, repeats, resultfile);
	testHelper1(tc, random, center, millis, repeats, resultfile);
	testHelper1(tc, random, random, millis, repeats, resultfile);

	return 0;
}

