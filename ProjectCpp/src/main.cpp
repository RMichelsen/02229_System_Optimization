#include <ortools/base/logging.h>
#include <ortools/constraint_solver/constraint_solver.h>

using namespace operations_research;

constexpr int CYCLE_LENGTH = 12;

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

std::unordered_map<std::string, Edge> edges = {
	{ "ES1SW1", Edge(1, 1000, 10) },
	{ "SW1ES2", Edge(2, 1000, 10) },
	{ "ES1SW2", Edge(3, 1000, 10) },
	{ "SW2ES2", Edge(4, 1000, 10) },
	{ "SW2SW1", Edge(5, 1000, 10) },

	// Repeated, lazy...
	{ "SW1ES1", Edge(1, 1000, 10) },
	{ "ES2SW1", Edge(2, 1000, 10) },
	{ "SW2ES1", Edge(3, 1000, 10) },
	{ "ES2SW2", Edge(4, 1000, 10) },
	{ "SW1SW2", Edge(5, 1000, 10) }
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
 	{ "F1", { { "ES1SW1", "SW1ES2" }, { "ES1SW1", "SW1SW2", "SW2ES2" }, { "ES1SW2", "SW2ES2" }, { "ES1SW2", "SW2SW1", "SW1ES2" } } },
	{ "F2", { { "ES1SW1", "SW1ES2" }, { "ES1SW1", "SW1SW2", "SW2ES2" }, { "ES1SW2", "SW2ES2" }, { "ES1SW2", "SW2SW1", "SW1ES2" } } },
	{ "F3", { { "ES1SW1", "SW1ES2" }, { "ES1SW1", "SW1SW2", "SW2ES2" }, { "ES1SW2", "SW2ES2" }, { "ES1SW2", "SW2SW1", "SW1ES2" } } },
	{ "F4", { { "ES1SW1", "SW1ES2" }, { "ES1SW1", "SW1SW2", "SW2ES2" }, { "ES1SW2", "SW2ES2" }, { "ES1SW2", "SW2SW1", "SW1ES2" } } },
	{ "F5", { { "ES2SW1", "SW1ES1" }, { "ES2SW1", "SW1SW2", "SW2ES1" }, { "ES2SW2", "SW2ES1" }, { "ES2SW2", "SW2SW1", "SW1ES1" } } },
	{ "F6", { { "ES2SW1", "SW1ES1" }, { "ES2SW1", "SW1SW2", "SW2ES1" }, { "ES2SW2", "SW2ES1" }, { "ES2SW2", "SW2SW1", "SW1ES1" } } },
	{ "F7", { { "ES2SW1", "SW1ES1" }, { "ES2SW1", "SW1SW2", "SW2ES1" }, { "ES2SW2", "SW2ES1" }, { "ES2SW2", "SW2SW1", "SW1ES1" } } },
	{ "F8", { { "ES1SW1", "SW1ES2" }, { "ES1SW1", "SW1SW2", "SW2ES2" }, { "ES1SW2", "SW2ES2" }, { "ES1SW2", "SW2SW1", "SW1ES2" } } }
};

int main(int argc, char **argv) {
	google::InitGoogleLogging(argv[0]);
	absl::SetFlag(&FLAGS_logtostderr, 1);

	Solver solver("ConstraintSolver");
	std::vector<IntVar *> all_variables;

	std::unordered_map<std::string, IntVar *> path_choices;
	for(const auto &[flow, paths] : flow_paths) {
		path_choices[flow] = solver.MakeIntVar(0, paths.size(), "path_choice" + flow);
	}

	std::unordered_map<std::string, IntVar *> q_choices;
	for(const auto &[flow, _] : flows) {
		for(const auto &edge : flow_paths[flow][0]) {
			q_choices[flow + "_" + edge] = solver.MakeIntVar(1, 3, "q_" + edge + "_p_0_" + flow);
		}
	}

	std::unordered_map<std::string, IntVar *> arrival_patterns;
	for(const auto &[flow_name, flow] : flows) {
		std::vector<IntVar *> e2e_delays;
		for(const auto &edge : flow_paths[flow_name][0]) {
			IntExpr *e2e_delay = solver.MakeSum(solver.MakeProd(q_choices[flow_name + "_" + edge], CYCLE_LENGTH), edges[edge].propagation_delay);
			e2e_delays.push_back(e2e_delay->Var());

			IntExpr *alpha = solver.MakeSum(e2e_delays);

			for(int c = 0; c < 2500; ++c) {
				IntExpr *A_input = solver.MakeModulo(solver.MakeSum(alpha, c * CYCLE_LENGTH), flow.period);
				IntVar *b = solver.MakeIsEqualCstVar(A_input, 0);
				IntVar *A = solver.MakeIntVar(0, std::numeric_limits<int32_t>::max());
				solver.AddConstraint(solver.MakeIfThenElseCt(b, solver.MakeIntConst(flow.size), solver.MakeIntConst(0), A));

				// A now contains the arrival pattern for cycle "c"
			}
      //   arrival_patterns[edge][c].append(arrival_pattern)
		}

		solver.AddConstraint(solver.MakeSumLessOrEqual(e2e_delays, flow.deadline));
		for(const auto &var : e2e_delays) all_variables.push_back(var);
	}

	LOG(INFO) << "Number of constraints: " << solver.constraints();

	for(const auto &[_, var] : path_choices) all_variables.push_back(var);
	for(const auto &[_, var] : q_choices) all_variables.push_back(var);
	for(const auto &[_, var] : q_choices) all_variables.push_back(var);

	DecisionBuilder* const db = solver.MakePhase(
	  all_variables, 
	  Solver::CHOOSE_FIRST_UNBOUND, 
	  Solver::ASSIGN_MIN_VALUE
	);

	solver.NewSearch(db);
	while (solver.NextSolution()) {
		LOG(INFO) << "Solution " << solver.solutions();
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

	return 0;
}