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
    # print("Objective: " % self.ObjectiveValue())
    for flow_variables in self.all_variables.flow_variables:
      print('  Path:', end = ' ')
      print(' => '.join(flow_variables.paths[self.Value(flow_variables.path_choice)]))

      for _, (edge, q) in enumerate(flow_variables.queue_numbers.items()):
        try:
          print("  %s: %s = %i" % (edge, q, self.Value(q)))
        except Exception as e:
          print(e)

      print("    %s = %i" % (flow_variables.e2e_delay, self.Value(flow_variables.e2e_delay)))
      for _, (_, A) in enumerate(flow_variables.arrival_patterns.items()):
        try:
          print("    %s = %i" % (A, self.Value(A)))
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
        all_arrival_patterns[(v1, v2)] = [model.NewConstant(0)] * len(paths)
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

      all_arrival_patterns[(v1, v2)][p] = model.NewIntVar(0, infinity, "bandwidth_%s" % edge_identifier)

      arrival_pattern_tmp = model.NewIntVar(0, infinity, "arrival_pattern_tmp1_%s" % edge_identifier)
      model.Add(arrival_pattern_tmp == ((flow.size) - alpha) * cycle_length)
      arrival_pattern_tmp2 = model.NewIntVar(0, infinity, "arrival_pattern_tmp2_%s" % edge_identifier)
      model.AddModuloEquality(arrival_pattern_tmp2, arrival_pattern_tmp, flow.period)

      b = model.NewBoolVar('intermediate_boolean')
      model.Add(arrival_pattern_tmp2 == 0).OnlyEnforceIf(b)
      model.Add(arrival_pattern_tmp2 != 0).OnlyEnforceIf(b.Not())

      arrival_pattern = model.NewIntVar(0, infinity, "arrival_pattern_%s" % edge_identifier)
      model.Add(arrival_pattern == flow.size).OnlyEnforceIf(b)
      model.Add(arrival_pattern == 0).OnlyEnforceIf(b.Not())

      model.Add(all_arrival_patterns[(v1, v2)][p] == arrival_pattern)

      # Element of sum, rij.e.D + rij.q
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

    arrival_patterns[index] = model.NewIntVar(0, infinity, "flow_%s_A_%s%s" % (flow.name, edge[0], edge[1]))
    model.AddElement(path_choice, all_arrival_patterns[index], arrival_patterns[index])

    queue_numbers[index] = model.NewIntVar(0, infinity, "flow_%s_q_%s%s" % (flow.name, edge[0], edge[1]))
    model.AddElement(path_choice, all_queue_numbers[index], queue_numbers[index])

  return FlowVariables(e2e_delay, queue_numbers, arrival_patterns, path_choice, paths)

def invoke_constraint_solver(architecture, application):
  model = cp_model.CpModel()
  graph = create_graph(architecture)

  flow_variables = []
  for flow in application.flows:
    flow_variables.append(add_flow_variables(model, graph, flow))

  for (v1, v2) in graph.edges:
    edge = next(x for x in architecture.edges if (x.source == v1 and x.destination == v2) or (x.destination == v1 and x.source == v2))
    index = (v1, v2)

    arrival_patterns = []
    for flow_vars in flow_variables:
      if index not in flow_vars.arrival_patterns:
        index = (index[1], index[0])
      arrival_patterns.append(flow_vars.arrival_patterns[index])

    edge_bandwidth = model.NewIntVar(0, infinity, "bandwidth%s%s" % (v1, v2))
    model.Add(edge_bandwidth == sum(arrival_patterns))
    model.Add(edge_bandwidth <= edge.bandwidth)

  solution_printer = SolutionPrinter(AllVariables(flow_variables, []))

  solver = cp_model.CpSolver()
  solver.parameters.log_search_progress = True
  solver.parameters.enumerate_all_solutions = True
  solver.SolveWithSolutionCallback(model, solution_printer)

  
if __name__ == "__main__":
  architecture, application = parse_xml(TestCase.TC1)
  invoke_constraint_solver(architecture, application)

