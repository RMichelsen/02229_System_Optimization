#define _ITERATOR_DEBUG_LEVEL 0

#include <ortools/base/logging.h>
#include <ortools/constraint_solver/constraint_solver.h>
//#include "ortools/sat/cp_model.h"
#include <pugixml.hpp>

#include "xmlReader.h"
#include "bfs.h"
#include <cstdlib>

using namespace operations_research;

constexpr int CYCLE_LENGTH = 12;

std::unordered_map<std::string, Edge> edges = {
	{ "ES1SW1", Edge(1, 1000, 10) },
	{ "SW1ES2", Edge(2, 1000, 10) },
	{ "ES1SW2", Edge(3, 1000, 10) },
	{ "SW2ES2", Edge(4, 1000, 10) },
	{ "SW2SW1", Edge(5, 1000, 10) }
};

std::unordered_map<std::string, Flow> flows = {
	{ "F1", Flow("F1", "ES1", "ES2", 300, 1000, 1000) },
	{ "F2", Flow("F2", "ES1", "ES2", 400, 2000, 2000) },
	{ "F3", Flow("F3", "ES1", "ES2", 500, 4000, 4000) },
	{ "F4", Flow("F4", "ES1", "ES2", 300, 8000, 4000) },
	{ "F5", Flow("F5", "ES2", "ES1", 400, 1000, 1000) },
	{ "F6", Flow("F6", "ES2", "ES1", 500, 2000, 2000) },
	{ "F7", Flow("F7", "ES2", "ES1", 300, 4000, 4000) },
	{ "F8", Flow("F8", "ES1", "ES2", 400, 8000, 4000) }
};

std::unordered_map<std::string, std::vector<std::vector<std::string>>> flow_paths = {
 	{ "F1", { { "ES1SW1", "SW1ES2" }, { "ES1SW1", "SW2SW1", "SW2ES2" }, { "ES1SW2", "SW2ES2" }, { "ES1SW2", "SW2SW1", "SW1ES2" } } },
	{ "F2", { { "ES1SW1", "SW1ES2" }, { "ES1SW1", "SW2SW1", "SW2ES2" }, { "ES1SW2", "SW2ES2" }, { "ES1SW2", "SW2SW1", "SW1ES2" } } },
	{ "F3", { { "ES1SW1", "SW1ES2" }, { "ES1SW1", "SW2SW1", "SW2ES2" }, { "ES1SW2", "SW2ES2" }, { "ES1SW2", "SW2SW1", "SW1ES2" } } },
	{ "F4", { { "ES1SW1", "SW1ES2" }, { "ES1SW1", "SW2SW1", "SW2ES2" }, { "ES1SW2", "SW2ES2" }, { "ES1SW2", "SW2SW1", "SW1ES2" } } },
	{ "F5", { { "SW1ES2", "ES1SW1" }, { "SW1ES2", "SW2SW1", "ES1SW2" }, { "SW2ES2", "ES1SW2" }, { "SW2ES2", "SW2SW1", "ES1SW1" } } },
	{ "F6", { { "SW1ES2", "ES1SW1" }, { "SW1ES2", "SW2SW1", "ES1SW2" }, { "SW2ES2", "ES1SW2" }, { "SW2ES2", "SW2SW1", "ES1SW1" } } },
	{ "F7", { { "SW1ES2", "ES1SW1" }, { "SW1ES2", "SW2SW1", "ES1SW2" }, { "SW2ES2", "ES1SW2" }, { "SW2ES2", "SW2SW1", "ES1SW1" } } },
	{ "F8", { { "ES1SW1", "SW1ES2" }, { "ES1SW1", "SW2SW1", "SW2ES2" }, { "ES1SW2", "SW2ES2" }, { "ES1SW2", "SW2SW1", "SW1ES2" } } }
};

int ChooseMyQ(int f, int e) {
	switch(f) {
	case 0: return (e == 0) ? 2 : 1;
	case 1: return (e == 0) ? 1 : 3;
	case 2: return (e == 0) ? 3 : 3;
	case 3: return (e == 0) ? 1 : 3;
	case 4: return (e == 0) ? 2 : 2;
	case 5: return (e == 0) ? 2 : 1;
	case 6: return (e == 0) ? 3 : 2;
	case 7: return (e == 0) ? 2 : 1;
	default:
		assert(false);
		return 0;
	}
}

bool TrySolve() {
	int least_common_multiple = 1;
	for(const auto &[_, flow] : flows) {
		least_common_multiple = std::lcm(least_common_multiple, flow.period);
	}
	int cycle_count = std::ceil((float)least_common_multiple / (float)CYCLE_LENGTH);

	Solver solver("ConstraintSolver");
	std::vector<IntVar *> all_variables;

	std::unordered_map<std::string, IntVar *> path_choices;
	for(const auto &[flow, paths] : flow_paths) {
		path_choices[flow] = solver.MakeIntVar(0, paths.size() - 1, "path_choice" + flow);
		all_variables.push_back(path_choices[flow]);
	}

	size_t longest_path = 0;
	for(const auto &[flow, paths] : flow_paths) {
		for(const auto &path : paths) {
			longest_path = std::max(path.size(), longest_path);
		}
	}

	// This array will provide an integer in domain {0, 1}, 0 if the index is an edge which is part of path p of flow f.
	// edge_validity[f][e][p] - flow [f], edge [e] and path [p]
	std::vector<std::vector<std::vector<int>>> edge_validity;
	edge_validity.resize(flows.size());
	int f = 0;
	for(const auto &[flow, paths] : flow_paths) {
		edge_validity[f].resize(edges.size() + 1);
		for(int e = 0; e <= edges.size(); e++) {
			edge_validity[f][e].resize(paths.size(), 0);
		}

		f++;
	}

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
	std::vector<std::vector<std::vector<int>>> edge_ids(flows.size());
	std::vector<std::vector<std::vector<int>>> edge_bandwidths(flows.size());
	std::vector<std::vector<std::vector<int>>> edge_propagation_delays(flows.size());

	f = 0;
	for(const auto &[flow, paths] : flow_paths) {
		edge_ids[f].resize(longest_path);
		edge_bandwidths[f].resize(longest_path);
		edge_propagation_delays[f].resize(longest_path);
		for(int p = 0; p < paths.size(); p++) {
			const auto &path = paths[p];
			for(int e = 0; e < longest_path; e++) {
				edge_ids[f][e].resize(paths.size());
				edge_bandwidths[f][e].resize(paths.size());
				edge_propagation_delays[f][e].resize(paths.size());
				if(e < path.size()) {
					edge_ids[f][e][p] = edges[path[e]].id;
					edge_bandwidths[f][e][p] = edges[path[e]].bandwidth;
					edge_propagation_delays[f][e][p] = edges[path[e]].propagation_delay;
				}
				else {
					edge_ids[f][e][p] = 0;
					edge_bandwidths[f][e][p] = -1;
					edge_propagation_delays[f][e][p] = -1;
				}
			}
		}

		f++;
	}

	// arrival_patterns[c][f][e] - cycle [c], flow [f] and edges [e]
	std::vector<std::vector<std::vector<IntVar *>>> arrival_patterns;
	arrival_patterns.resize(cycle_count);
	for(int c = 0; c < cycle_count; c++) {
		arrival_patterns[c].resize(flows.size());
		for(int f = 0; f < flows.size(); f++) {
			arrival_patterns[c][f].resize(edges.size() + 1);
			arrival_patterns[c][f][0] = solver.MakeIntConst(0);
			for(int e = 1; e <= edges.size(); e++) {
				arrival_patterns[c][f][e] = solver.MakeIntVar(0, std::numeric_limits<int32_t>::max());
			}
		}
	}

	f = 0;
	for(const auto &[flow_name, flow] : flows) {
		std::vector<IntVar *> e2e_delays;
		IntVar *path_choice = path_choices[flow_name];

		for(int e = 0; e < longest_path; e++) {
			IntVar *edge_id = solver.MakeElement(edge_ids[f][e], path_choice)->Var();
			IntExpr *edge_propagation_delay = solver.MakeElement(edge_propagation_delays[f][e], path_choice);

			IntExpr *alpha = solver.MakeSum(e2e_delays);
			//all_variables.push_back(alpha->VarWithName(flow_name + "_e_" + std::to_string(e) + "_alpha"));

			// ceil(edge_propagation_delay / CYCLE_LENGTH) manually
			// -- there is no ceil function in the constraint solver library
			IntExpr *induced_delay = solver.MakeSum(solver.MakeDiv(solver.MakeSum(edge_propagation_delay, -1), CYCLE_LENGTH), 1);
			IntVar *q = solver.MakeIntVar(1, 3, flow_name + "_e_" + std::to_string(e) + "_q");

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
				IntVar *A_boolean = solver.MakeIsEqualCstVar(A_calc, 0);
				IntExpr *composite_boolean = solver.MakeProd(A_input_positive_check, A_boolean);

				// Calculate the arrival pattern, multiply by 0 if the edge is invalid (not part of solution)
				IntVar *A = solver.MakeIntVar(0, std::numeric_limits<int32_t>::max());
				solver.AddConstraint(solver.MakeIfThenElseCt(composite_boolean->Var(), solver.MakeIntConst(flow.size), solver.MakeIntConst(0), A));

				// Set the corresponding element of the arrival_patterns array, indexing with the edge ID integer variable.
				IntExpr *E = solver.MakeElement(arrival_patterns[c][f], edge_id);
				solver.AddConstraint(solver.MakeEquality(A, E));
			}
		}

		IntExpr *e2e_delay_sum = solver.MakeSum(e2e_delays);

		// TODO: Add division
		int acceptableDelay = std::ceil((float)flow.deadline / (float)CYCLE_LENGTH);
		solver.AddConstraint(solver.MakeSumLessOrEqual(e2e_delays, acceptableDelay));

		// TODO: Change after we know flow/edge input details
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

            // TODO: Make efficient
            int edge_capacity = 0;
            for(const auto &[_, edge] : edges) {
                if(edge.id == e) {
					edge_capacity = int(edge.bandwidth * 131072.0f * 0.000012f);
                }
            }

            solver.AddConstraint(solver.MakeLessOrEqual(cycle_edge_bandwidths[e][c], edge_capacity));
		}
	}

	// edge_consumed_bandwidths[e] - edge [e]
	std::vector<IntVar *> edge_consumed_bandwidths(edges.size() + 1);
	edge_consumed_bandwidths[0] = solver.MakeIntConst(0);
	for(int e = 1; e <= edges.size(); e++) {
		IntExpr *max_consumed_bandwidth = solver.MakeMax(cycle_edge_bandwidths[e]);

        // TODO: Make efficient
        int edge_capacity = 0;
        for(const auto &[_, edge] : edges) {
            if(edge.id == e) {
                edge_capacity = int(edge.bandwidth * 131072.0f * 0.000012f);
            }
        }

		edge_consumed_bandwidths[e] = solver.MakeDiv(solver.MakeProd(max_consumed_bandwidth, 1000), edge_capacity)->VarWithName("Edge_" + std::to_string(e) + "_Bandwidth");
	}

	IntVar *mean_bandwidth_usage = solver.MakeDiv(solver.MakeSum(edge_consumed_bandwidths), edges.size()*2)->VarWithName("mean_bandwidth_usage");
	all_variables.push_back(mean_bandwidth_usage);

	OptimizeVar *omega = solver.MakeMinimize(mean_bandwidth_usage, 5);

	LOG(INFO) << "Number of variables: " << all_variables.size();
	LOG(INFO) << "Number of constraints: " << solver.constraints();

	DecisionBuilder *const db = solver.MakePhase(
	  all_variables, 
	  Solver::CHOOSE_FIRST_UNBOUND, 
	  Solver::ASSIGN_MIN_VALUE
	);
	SearchMonitor *const search_log = solver.MakeSearchLog(100, omega);
	//SolutionCollector *const collector = solver.MakeFirstSolutionCollector();
	//collector->Add(all_variables);
	//solver.Solve(db, omega, search_log, collector);

	//LOG(INFO) << collector->solution(0)->DebugString();

	solver.NewSearch(db, search_log);
	while (solver.NextSolution()) {
		for(const auto &variable : all_variables) {
			LOG(INFO) << variable << " = " << variable->Value();

		}
	}
	solver.EndSearch();
	LOG(INFO) << "Number of solutions: " << solver.solutions();
	LOG(INFO) << "";
	LOG(INFO) << "Advanced usage:";
	LOG(INFO) << "Problem solved in " << solver.wall_time() << "ms";
	LOG(INFO) << "Memory usage: " << Solver::MemoryUsage() << " bytes";

	return solver.solutions() > 0;
}

int main(int argc, char **argv) {
	google::InitGoogleLogging(argv[0]);
	absl::SetFlag(&FLAGS_logtostderr, 1);

	// Cycle count = least common multiple of flow periods
	pugi::xml_document doc;
	pugi::xml_parse_result result = doc.load_file("Tests/TC1/Input/Apps.xml");

	srand(time(NULL));

	std::unordered_map<std::string, Edge> edges2;
	std::unordered_map<std::string, Flow> flows2;

	loadTestCase(example, edges2, flows2);

	std::unordered_map<std::string, std::vector<std::vector<std::string>>> flow_paths;
	flow_paths = getFlowPaths((edgeMap)edges2, (flowMap)flows2);

	TrySolve();

	//for(int i = 0; i < 1000; ++i) {
	//	std::unordered_map<std::string, int> path_choices;
	//	// path_choices["F1"] = rand() % 4;
	//	// path_choices["F2"] = rand() % 4;
	//	// path_choices["F3"] = rand() % 4;
	//	// path_choices["F4"] = rand() % 4;
	//	// path_choices["F5"] = rand() % 4;
	//	// path_choices["F6"] = rand() % 4;
	//	// path_choices["F7"] = rand() % 4;
	//	// path_choices["F8"] = rand() % 4;

	//	path_choices["F1"] = 0;
	//	path_choices["F2"] = 2;
	//	path_choices["F3"] = 2;
	//	path_choices["F4"] = 0;
	//	path_choices["F5"] = 2;
	//	path_choices["F6"] = 2;
	//	path_choices["F7"] = 2;
	//	path_choices["F8"] = 2;

	//	if(TrySolve()) {
	//		for(const auto &[_, i] : path_choices) {
	//			std::cout << i << ' ';
	//		}

	//		break;
	//	}
	//}

	return 0;
}



//for(int e = 1; e < edges.size(); e++) {
//	std::vector<IntVar *> e_arrival_patterns;
//	for(int c = 0; c < cycle_count; c++) {
//		e_arrival_patterns.push_back(arrival_patterns[c][e]->Var());
//	}

//       IntExpr *sum = solver.MakeSum(arrival_patterns[c]);

   //	// TODO: Make efficient
   //	int edge_capacity = 0;
   //	for(const auto &[_, edge] : edges) {
   //		if(edge.id == e) {
   //			edge_capacity = int(edge.bandwidth * 131072) * 0.000012;
   //		}
   //	}

//       solver.AddConstraint(solver.MakeLessOrEqual(sum, edge_capacity));

//       cycle_bandwidths[edge].push_back(
//           sum->VarWithName("cycle_bandwidths" + edge + "_" + std::to_string(c))
//       );
   //}


//for(const auto &edge : flow_paths[flow_name][path_choices[flow_name]]) {
//	std::cout << "Processing edge: " << edge << " for flow: " << flow_name << std::endl;

//	// If the edge does not yet have an array of arrival patterns per cycle,
//	// initialize a vector that contains one vector of flow arrival patterns for each cycle. 
//	if(arrival_patterns.find(edge) == arrival_patterns.end()) {
//		arrival_patterns[edge] = std::vector<std::vector<IntVar *>>(cycle_count);
//	}

//	IntExpr *alpha = solver.MakeSum(e2e_delays);
//	all_variables.push_back(alpha->VarWithName(flow_name + "A" + edge));

//	int induced_delay = std::ceil((float)edges[edge].propagation_delay / (float)CYCLE_LENGTH);
//	//IntExpr *e2e_delay = solver.MakeSum(q_choices[flow_name + "_" + edge], induced_delay);
//	// e2e_delays.push_back(e2e_delay->Var());
//	IntVar *e2e_delay = solver.MakeIntConst(q_choices[flow_name + "_" + edge] + induced_delay);
//	e2e_delays.push_back(e2e_delay);
//	//all_variables.push_back(e2e_delay);

//	for(int c = 0; c < cycle_count; c++) {
//		// Modulo (c - a) * |c| % flow.period
//		IntExpr *A_input = solver.MakeDifference(c, alpha);
//		IntExpr *aMil = solver.MakeProd(A_input, CYCLE_LENGTH);
//		
//		// Modulo without modulo: a % b = a - (b * int(a / b))
//		// a = c * |c|
//		// b = flow.period
//		IntExpr *Ap = solver.MakeDifference(
//			aMil,
//			solver.MakeProd(solver.MakeDiv(aMil, flow.period), flow.period)
//		);

//		// TODO: We can check less than 12, since a cycle is 12 microseconds... But the modulo still seems off..
//		//IntVar *b = solver.MakeIsLessCstVar(A_input, CYCLE_LENGTH);
//		IntVar *bpos = solver.MakeIsGreaterOrEqualCstVar(A_input, 0);	//make sure input is positive
//		IntVar *b = solver.MakeIsEqualCstVar(Ap, 0);
//		IntVar *A = solver.MakeIntVar(0, std::numeric_limits<int32_t>::max(), flow_name + "_" + edge + "_" + std::to_string(c));
//		solver.AddConstraint(solver.MakeIfThenElseCt(solver.MakeProd(b, bpos)->Var(), solver.MakeIntConst(flow.size), solver.MakeIntConst(0), A));

//		// A now contains the arrival pattern for cycle "c"
//		arrival_patterns[edge][c].push_back(A);
//	}
//}



	//std::unordered_map<std::string, std::vector<IntVar *>> cycle_bandwidths;
	//for(const auto &[edge, As] : arrival_patterns) {
	//	for(int c = 0; c < cycle_count; c++) {
	//		IntExpr *sum = solver.MakeSum(As[c]);

	//		// Bandwidth in Mbps, to capacity in bytes per cycle
	//		int edge_capacity = int((edges[edge].bandwidth * 131072) * 0.000012);

	//		solver.AddConstraint(solver.MakeLessOrEqual(sum, edge_capacity));
	//		cycle_bandwidths[edge].push_back(
	//			sum->VarWithName("cycle_bandwidths" + edge + "_" + std::to_string(c))
	//		);
	//	}
	//}

	//std::vector<IntVar *> edge_bandwidthss;
	//for(const auto &[edge, _] : arrival_patterns) {
	//	// Bandwidth in Mbps, to capacity in bytes per cycle
	//	int edge_capacity = int((edges[edge].bandwidth * 131072) * 0.000012);

	//	IntExpr *bandwidth = solver.MakeDiv(solver.MakeProd(solver.MakeMax(cycle_bandwidths[edge]), 1000), edge_capacity);
	//	edge_bandwidthss.push_back(bandwidth->VarWithName("edge_bandwidth_" + edge));
	//}
	//for(const auto &var : edge_bandwidthss) all_variables.push_back(var);
