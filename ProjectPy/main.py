import itertools
from enum import Enum
import xml.etree.ElementTree as ET
import networkx as nx
from ortools.sat.python import cp_model

def pairwise(iterable):
    a, b = itertools.tee(iterable)
    next(b, None)
    return zip(a, b)

class TestCase(Enum):
  TC1 = "TC1"
  TC2 = "TC2"
  TC3 = "TC3"
  TC4 = "TC4"
  TC5 = "TC5"
  TC6 = "TC6"
  TC7 = "TC7"
  TC8 = "TC8"
  TC9 = "TC9"

class Vertex:
  def __init__(self, name):
    self.name = name

class Edge:
  def __init__(self, id, source, destination, bandwidth, propagation_delay):
    self.id = id
    self.source = source
    self.destination = destination
    self.bandwidth = int(bandwidth)
    self.propagation_delay = int(propagation_delay)

class Architecture:
  def __init__(self, vertices, edges):
    self.vertices = vertices
    self.edges = edges

class Flow:
  def __init__(self, name, source, destination, size, period, deadline):
    self.name = name
    self.source = source
    self.destination = destination
    self.size = int(size)
    self.period = int(period)
    self.deadline = int(deadline)

class Application:
  def __init__(self, flows):
    self.flows = flows

class FlowVariables:
  def __init__(self, e2e_delay, queue_numbers, arrival_patterns, path_choice, paths):
    self.e2e_delay = e2e_delay
    self.queue_numbers = queue_numbers
    self.arrival_patterns = arrival_patterns
    self.path_choice = path_choice
    self.paths = paths

class AllVariables:
  def __init__(self, flow_variables, global_variables):
    self.flow_variables = flow_variables
    self.global_variables = global_variables

def parse_xml(test_case):
  architecture_root = ET.parse('Tests/' + test_case.value + '/Input/config.xml').getroot()
  vertices = []
  for vertex in architecture_root.findall('Vertex'):
    vertices.append(Vertex(vertex.get('Name')))
  edges = []
  for edge in architecture_root.findall('Edge'):
    edges.append(Edge(edge.get('Id'), edge.get('Source'), edge.get('Destination'), edge.get('BW'), edge.get('PropDelay')))

  application_root = ET.parse('Tests/' + test_case.value + '/Input/apps.xml').getroot()
  flows = []
  for flow in application_root.findall('Message'):
    flows.append(Flow(flow.get('Name'), flow.get('Source'), flow.get('Destination'), flow.get('Size'), flow.get('Period'), flow.get('Deadline')))

  return Architecture(vertices, edges), Application(flows)

def create_graph(architecture):
  G = nx.Graph()
  for edge in architecture.edges:
    G.add_edge(edge.source, edge.destination)

  return G

class SolutionPrinter(cp_model.CpSolverSolutionCallback):
  def __init__(self, all_variables):
    cp_model.CpSolverSolutionCallback.__init__(self)
    self.all_variables = all_variables
    self.solution_count = 0

  def OnSolutionCallback(self):
    self.solution_count += 1
    print("Solution %i:" % self.solution_count)

    print("  Objective: %s" % self.ObjectiveValue())
    for var in self.all_variables.global_variables:
        print("  %s = %i" % (var, self.Value(var)))

    for flow_variables in self.all_variables.flow_variables:
      print('  Path:', end = ' ')
      print(' => '.join(flow_variables.paths[self.Value(flow_variables.path_choice)]))

      # for _, (edge, q) in enumerate(flow_variables.queue_numbers.items()):
      #   try:
      #     print("  %s: %s = %i" % (edge, q, self.Value(q)))
      #   except Exception as e:
      #     print(e)

      print("    %s = %i" % (flow_variables.e2e_delay, self.Value(flow_variables.e2e_delay)))
      for _, (_, A) in enumerate(flow_variables.arrival_patterns.items()):
        try:
          print("    %s = %i" % (A[0], self.Value(A[0])))
          print("    %s = %i" % (A[1], self.Value(A[1])))
          print("    %s = %i" % (A[2], self.Value(A[2])))
        except Exception as e:
          print(e)


infinity = 2**32
cycle_length = 12

def add_flow_variables(model, graph, flow):
  # TODO: Change cutoff
  paths = list(nx.all_simple_paths(graph, flow.source, flow.destination, cutoff = 20))

  all_arrival_patterns = {}
  all_queue_numbers = {}

  all_e2e_delays = []
  for p, path in enumerate(paths):
    path_identifier = "path%s" % str(p)
    path_e2e_delays = []

    for v1, v2 in pairwise(path):
      if (v1, v2) not in all_arrival_patterns:
        all_arrival_patterns[(v1, v2)] = [[model.NewConstant(0)] * 3] * len(paths)
      if (v1, v2) not in all_queue_numbers:
        all_queue_numbers[(v1, v2)] = [model.NewConstant(0)] * len(paths)

      edge_identifier = "%s%s%s" % (path_identifier, v1, v2)
      # Find the edge
      edge = next(x for x in architecture.edges if (x.source == v1 and x.destination == v2) or (x.destination == v1 and x.source == v2))

      q = model.NewIntVar(1, 3, "queue_%s" % edge_identifier)
      d = model.NewConstant(edge.propagation_delay)
      edge_delay = model.NewIntVar(0, infinity, "edge_delay_%s" % edge_identifier)

      # Link capacity constraint
      alpha = model.NewIntVar(0, infinity, "alpha_%s" % edge_identifier)
      model.Add(alpha == sum(path_e2e_delays))

      for c in range(3):
        all_arrival_patterns[(v1, v2)][p][c] = model.NewIntVar(-infinity, infinity, "arrival_pattern_%i_%s" % (c, edge_identifier))

        arrival_pattern_tmp = model.NewIntVar(-infinity, infinity, "arrival_pattern_tmp1_%i_%s" % (c, edge_identifier))
        model.Add(arrival_pattern_tmp == (c * cycle_length) - alpha)

        # arrival_pattern_tmp2 = arrival_pattern_tmp % flow.period
        arrival_pattern_tmp2 = model.NewIntVar(-infinity, infinity, "arrival_pattern_tmp2_%i_%s" % (c, edge_identifier))
        model.AddModuloEquality(arrival_pattern_tmp2, arrival_pattern_tmp, flow.period)

        b = model.NewBoolVar('intermediate_boolean_%i' % c)
        model.Add(arrival_pattern_tmp2 == 0).OnlyEnforceIf(b)
        model.Add(arrival_pattern_tmp2 != 0).OnlyEnforceIf(b.Not())

        arrival_pattern = model.NewIntVar(0, infinity, "arrival_pattern_%i_%s" % (c, edge_identifier))
        model.Add(arrival_pattern == flow.size).OnlyEnforceIf(b)
        model.Add(arrival_pattern == 0).OnlyEnforceIf(b.Not())

        model.Add(all_arrival_patterns[(v1, v2)][p][c] == arrival_pattern)

      # Element of sum, rij.e.D + rij.q
      # d is propagation delay in microseconds
      # q is queue number (cycles) multiplied by the cycle length to get microseconds
      model.Add(d + (q * cycle_length) == edge_delay)
      path_e2e_delays.append(edge_delay)

      # Remember this paths queue number
      all_queue_numbers[(v1, v2)][p] = q

    path_delay = model.NewIntVar(0, infinity, "path_delay_%s" % path_identifier)
    model.Add(path_delay == sum(path_e2e_delays))
    all_e2e_delays.append(path_delay)

  path_choice = model.NewIntVar(0, len(paths) - 1, "path_choice")

  e2e_delay = model.NewIntVar(0, infinity, "e2e_delay")
  model.AddElement(path_choice, all_e2e_delays, e2e_delay)
  model.Add(e2e_delay <= flow.deadline)
  
  arrival_patterns = {}
  queue_numbers = {}
  for edge in graph.edges:
    index = (edge[0], edge[1])
    if index not in all_arrival_patterns:
      index = (index[1], index[0])

    if index not in arrival_patterns:
      arrival_patterns[index] = [None] * 3

    for c in range(3):
      # arrival_patterns[index][c] = all_arrival_patterns[index][path_choice]
      arrival_patterns[index][c] = model.NewIntVar(-infinity, infinity, "flow_%s_A_%i_%s%s" % (flow.name, c, edge[0], edge[1]))
      model.AddElement(path_choice, all_arrival_patterns[index][c], arrival_patterns[index][c])

    # queue_numbers[index] = all_queue_numbers[index][path_choice]
    queue_numbers[index] = model.NewIntVar(0, infinity, "flow_%s_q_%s%s" % (flow.name, edge[0], edge[1]))
    model.AddElement(path_choice, all_queue_numbers[index], queue_numbers[index])

  return FlowVariables(e2e_delay, queue_numbers, arrival_patterns, path_choice, paths)

def invoke_constraint_solver(architecture, application):
  model = cp_model.CpModel()
  graph = create_graph(architecture)

  all_vars = []

  edge_bandwidth_cs = {}
  flow_variables = []
  for flow in application.flows:
    flow_variables.append(add_flow_variables(model, graph, flow))

    for (v1, v2) in graph.edges:
      edge = next(x for x in architecture.edges if (x.source == v1 and x.destination == v2) or (x.destination == v1 and x.source == v2))
      index = (v1, v2)

      for c in range(3):
        arrival_patterns = []
        for flow_vars in flow_variables:
          if index not in flow_vars.arrival_patterns:
            index = (index[1], index[0])

          if index not in edge_bandwidth_cs:
              edge_bandwidth_cs[index] = [[] for _ in range(3)]

          arrival_patterns.append(flow_vars.arrival_patterns[index][c])

        edge_bandwidth_c = model.NewIntVar(0, infinity, "bandwidth%i_%s%s" % (c, v1, v2)) 
        model.Add(edge_bandwidth_c == sum(arrival_patterns))
        model.Add(edge_bandwidth_c <= edge.bandwidth)
        edge_bandwidth_cs[index][c].append(edge_bandwidth_c)

  all_bandwidths = []
  for (v1, v2) in graph.edges:
    edge = next(x for x in architecture.edges if (x.source == v1 and x.destination == v2) or (x.destination == v1 and x.source == v2))
    index = (v1, v2)

    if index not in edge_bandwidth_cs:
      edge_bandwidth_cs[index] = [[] for _ in range(3)]

    for c in range(3):
      # max_edge_bandwidths = max(edge_bandwidths_cs[index][c])
      max_edge_bandwidths = model.NewIntVar(0, infinity, "max_edge_bandwidth%s%s" % (v1, v2))
      model.AddMaxEquality(max_edge_bandwidths, edge_bandwidth_cs[index][c])

      # left_side = max_edge_bandwidths / S
      left_side = model.NewIntVar(0, infinity, "left_side_tmp%s%s" % (v1, v2))
      model.AddDivisionEquality(left_side, max_edge_bandwidths, edge.bandwidth * cycle_length)

      # edge_bandwidth = left_side * 1000
      edge_bandwidth = model.NewIntVar(0, infinity, "edge_bandwidth%s%s" % (v1, v2))
      model.AddMultiplicationEquality(edge_bandwidth, [left_side, 1000])

      all_bandwidths.append(edge_bandwidth)

  sum_bandwidths = model.NewIntVar(0, infinity, "sum_bandwidth") 
  model.Add(sum_bandwidths == sum(all_bandwidths))

  OMEGA = model.NewIntVar(0, infinity, "OMEGA") 
  model.AddDivisionEquality(OMEGA, sum_bandwidths, len(architecture.edges))

  model.Minimize(OMEGA)

  all_vars.append(sum_bandwidths)
  all_vars.append(OMEGA)

  solution_printer = SolutionPrinter(AllVariables(flow_variables, all_vars))

  solver = cp_model.CpSolver()
  #solver.parameters.log_search_progress = True
  solver.parameters.enumerate_all_solutions = True
  solver.SolveWithSolutionCallback(model, solution_printer)

  
if __name__ == "__main__":
  architecture, application = parse_xml(TestCase.TC1)
  invoke_constraint_solver(architecture, application)

