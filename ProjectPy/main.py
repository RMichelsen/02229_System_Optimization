import itertools
import random
from enum import Enum
import xml.etree.ElementTree as ET
import networkx as nx
from networkx import convert
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

  def __hash__(self):
    return hash(self.name)

class Application:
  def __init__(self, flows):
    self.flows = flows

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

class SolutionPrinter(cp_model.CpSolverSolutionCallback):
  def __init__(self, variables):
    cp_model.CpSolverSolutionCallback.__init__(self)
    self.variables = variables
    self.solution_count = 0

  def OnSolutionCallback(self):
    self.solution_count += 1
    print("Solution %i:" % self.solution_count)
    print("  Objective: %s" % self.ObjectiveValue())
    for var in self.variables:
        print("  %s = %i" % (var, self.Value(var)))

infinity = 2**32
cycle_length = 12

def create_graph(architecture):
  G = nx.Graph()
  for edge in architecture.edges:
    G.add_edge(edge.source, edge.destination)
  return G

def get_edge(architecture, edge):
  return next(x for x in architecture.edges if (x.source == edge[0] and x.destination == edge[1]) or (x.destination == edge[0] and x.source == edge[1]))

# Converts paths from the networkx graph into paths containing edges
def get_flow_paths(graph, flow):
  paths = []
  for path in list(nx.all_simple_paths(graph, flow.source, flow.destination, cutoff = 20)):
    converted_path = []
    for v1, v2 in pairwise(path):
      edge_identifier = tuple(sorted((v1, v2)))
      converted_path.append(edge_identifier)
    paths.append(converted_path)
  return paths

if __name__ == "__main__":
  architecture, application = parse_xml(TestCase.TC1)
  graph = create_graph(architecture)
  model = cp_model.CpModel()

  # Calculate the cycle count by first calculating the hyperperiod (in microseconds) and then diving it by the cycle length
  cycle_count = int(sum(flow.period for flow in application.flows) / cycle_length)

  # This array contains variables to be printed out in the solution callback
  debug_variables = []

  # Paths for each flow
  paths = {}
  for flow in application.flows:
    paths[flow] = get_flow_paths(graph, flow)

  # Path choice for each flow (at random for now...)
  path_choices = {}
  for flow in application.flows:
    path_choices[flow] = random.randint(0, len(paths[flow]) - 1)
    # model.NewIntVar(0, len(paths[flow]), "path_choice_%s" % flow.name)

  # q choices for each edge, for each path, in each flow
  q_choices = {}
  for flow in application.flows:
    q_choices[flow] = {}
    for edge in paths[flow][path_choices[flow]]:
      q_choices[flow][edge] = model.NewIntVar(1, 3, "q_%s_p_%i_%s" % (edge, path_choices[flow], flow.name))

  arrival_patterns = {}
  for flow in application.flows:
    print("Processing flow %s" % flow.name)
    e2e_delay_parts = []
    for edge in paths[flow][path_choices[flow]]:
      e2e_delay = model.NewIntVar(0, infinity, "e2e_delay_%s_%s" % (edge, flow.name))
      model.Add(e2e_delay == (get_edge(architecture, edge).propagation_delay + (q_choices[flow][edge] * cycle_length)))
      e2e_delay_parts.append(e2e_delay)
      alpha = model.NewIntVar(0, infinity, "alpha_%s_%s" % (edge, flow.name))
      model.Add(alpha == sum(e2e_delay_parts))
      for c in range(cycle_count):
        if edge not in arrival_patterns:
          arrival_patterns[edge] = [[]] * cycle_count
        arrival_pattern_tmp = model.NewIntVar(0, infinity, "tmp")
        model.Add(arrival_pattern_tmp == (c * cycle_length) + alpha)

        # arrival_pattern_tmp2 = arrival_pattern_tmp % flow.period
        arrival_pattern_tmp2 = model.NewIntVar(0, infinity, "tmp")
        model.AddModuloEquality(arrival_pattern_tmp2, arrival_pattern_tmp, flow.period)

        b = model.NewBoolVar('intermediate_boolean_%i' % c)
        model.Add(arrival_pattern_tmp2 == 0).OnlyEnforceIf(b)
        model.Add(arrival_pattern_tmp2 != 0).OnlyEnforceIf(b.Not())

        arrival_pattern = model.NewIntVar(0, infinity, "A_%s_%s_%i" % (edge, flow.name, c))
        model.Add(arrival_pattern == flow.size).OnlyEnforceIf(b)
        model.Add(arrival_pattern == 0).OnlyEnforceIf(b.Not())

        arrival_patterns[edge][c].append(arrival_pattern)

    e2e_delay_sum = model.NewIntVar(0, infinity, "e2e_delay_sum_%s" % flow.name)
    model.Add(e2e_delay_sum == sum(e2e_delay_parts))
    model.Add(e2e_delay_sum <= flow.deadline)
    debug_variables.append(e2e_delay_sum)

  edge_bandwidths = []
  for edge in architecture.edges:
    edge_index = tuple(sorted((edge.source, edge.destination)))
    if edge_index in arrival_patterns:
      # cycle_bandwidths = []
      # for c in range(cycle_count):
        # cycle_bandwidth = model.NewIntVar(0, infinity, "B_%s_%i" % (edge_index, c))
        # model.Add(cycle_bandwidth == sum(arrival_patterns[edge_index]))
        # model.Add(sum(arrival_patterns[edge_index][c]) <= edge.bandwidth)
        # cycle_bandwidths.append(cycle_bandwidth)

      aps = []
      for x in arrival_patterns[edge_index]:
        aps.extend(x)

      # max_edge_bandwidths = max(edge_bandwidths_cs[index][c])
      max_edge_bandwidths = model.NewIntVar(0, infinity, "max_edge_bandwidth")
      model.AddMaxEquality(max_edge_bandwidths, sum(aps))

      # left_side = max_edge_bandwidths / S
      left_side = model.NewIntVar(0, infinity, "left_side_tmp")
      model.AddDivisionEquality(left_side, max_edge_bandwidths, edge.bandwidth * cycle_length)

      # edge_bandwidth = left_side * 1000
      edge_bandwidth = model.NewIntVar(0, infinity, "edge_bandwidth")
      model.AddMultiplicationEquality(edge_bandwidth, [left_side, 1000])

      edge_bandwidths.append(edge_bandwidth)

  sum_bandwidths = model.NewIntVar(0, infinity, "sum_bandwidth") 
  model.Add(sum_bandwidths == sum(arrival_patterns.values()))

  OMEGA = model.NewIntVar(0, infinity, "OMEGA") 
  model.AddDivisionEquality(OMEGA, sum_bandwidths, len(architecture.edges))

  model.Minimize(OMEGA)

  debug_variables.append(sum_bandwidths)
  debug_variables.append(OMEGA)

  solution_printer = SolutionPrinter(debug_variables)
  solver = cp_model.CpSolver()
  #solver.parameters.log_search_progress = True
  solver.parameters.enumerate_all_solutions = True
  solver.SolveWithSolutionCallback(model, solution_printer)

# def get_all_flow_paths(graph, flows):
#   flow_to_paths = {}
#   for flow in flows:
#     flow_to_paths[flow] = list(nx.all_simple_paths(graph, flow.source, flow.destination, cutoff = 20))
#   return flow_to_paths

# For all flows, for all paths precalculate the end to end delays
# def precalculate_end_to_end_delays(architecture, flows, flow_to_paths, max_paths, max_edges):
#   end_to_end_delays = {}
#   edge_capacities = {}
#   for flow in flows:
#     end_to_end_delays[flow.name] = [0] * max_paths * max_edges * 3
#     edge_capacities[flow.name] = [0] * max_paths * max_edges * 3
#     for p, path in enumerate(flow_to_paths[flow]):
#       path_offset = p * (max_edges * 3)
#       edge_offset = 0

#       global edge
#       for v1, v2 in pairwise(path):
#         edge = get_edge(architecture, v1, v2)
#         for q in range(1, 4):
#           idx = path_offset + (edge_offset * 3) + (q - 1) 
#           end_to_end_delays[flow.name][idx] = edge.propagation_delay + (q * cycle_length)
#           edge_capacities[flow.name][idx] = edge.bandwidth * cycle_length
#         edge_offset += 1

#       if edge_offset < max_edges:
#         for e in range(edge_offset, max_edges):
#           for q in range(1, 4):
#             idx = path_offset + (e * 3) + (q - 1) 
#             edge_capacities[flow.name][idx] = edge.bandwidth * cycle_length
#   return end_to_end_delays, edge_capacities

# def invoke_constraint_solver(architecture, application):
#   # Calculate the cycle count by first calculating the hyperperiod (in microseconds) and then diving it by the cycle length
#   hyperperiod = max(flow.period for flow in application.flows)
#   cycle_count = int(hyperperiod / cycle_length)

#   graph = create_graph(architecture)
#   flow_to_paths = get_all_flow_paths(graph, application.flows)

#   max_paths = 0
#   max_edges = 0
#   for paths in flow_to_paths.values():
#     max_paths = max(max_paths, len(paths))
#     for path in paths:
#       max_edges = max(max_edges, len(path) - 1)

#   end_to_end_delays, edge_capacities = precalculate_end_to_end_delays(architecture, application.flows, flow_to_paths, max_paths, max_edges)

#   model = cp_model.CpModel()

#   all_vars = []

#   flow_edge_bandwidths = []

#   for flow in application.flows:
#     print("Processing flow")
#     e2e_delays = []
#     path_choice = model.NewIntVar(0, max_paths - 1, "flow_%s_patch_choice" % (flow.name))
#     all_vars.append(path_choice)

#     for e in range(max_edges):
#       alpha = model.NewIntVar(0, infinity, "flow_%s_alpha_%i" % (flow.name, e))
#       model.Add(alpha == sum(e2e_delays))

#       q = model.NewIntVar(1, 3, "flow_%s_q_%i" % (flow.name, e))
#       e2e_delays.append(model.NewIntVar(0, infinity, "flow_%s_e2e_%i" % (flow.name, e)))

#       index = path_choice * (max_edges * 3) + (e * 3) + (q - 1)

#       index_variable = model.NewIntVar(0, infinity, "flow_%s_e2e_%i_index" % (flow.name, e))
#       all_vars.append(index_variable)
#       model.Add(index_variable == index)
#       model.AddElement(index_variable, end_to_end_delays[flow.name], e2e_delays[e])

#       arrival_patterns = []
#       for c in range(cycle_count):
#         arrival_pattern_tmp = model.NewIntVar(0, infinity, "arrival_pattern_%s_tmp1_%i_%i" % (flow.name, c, e))
#         model.Add(arrival_pattern_tmp == (c * cycle_length) + alpha)

#         # arrival_pattern_tmp2 = arrival_pattern_tmp % flow.period
#         arrival_pattern_tmp2 = model.NewIntVar(0, infinity, "arrival_pattern_%s_tmp2_%i_%i" % (flow.name, c, e))
#         model.AddModuloEquality(arrival_pattern_tmp2, arrival_pattern_tmp, flow.period)

#         b = model.NewBoolVar('intermediate_boolean_%i' % c)
#         model.Add(arrival_pattern_tmp2 == 0).OnlyEnforceIf(b)
#         model.Add(arrival_pattern_tmp2 != 0).OnlyEnforceIf(b.Not())

#         arrival_pattern = model.NewIntVar(0, infinity, "arrival_pattern_%s_%i_%i" % (flow.name, c, e))
#         model.Add(arrival_pattern == flow.size).OnlyEnforceIf(b)
#         model.Add(arrival_pattern == 0).OnlyEnforceIf(b.Not())

#         arrival_patterns.append(arrival_pattern)

#       flow_edge_bandwidth = model.NewIntVar(0, infinity, "flow_%s_edge_%i_bandwidth" % (flow.name, e))
#       model.Add(flow_edge_bandwidth == sum(arrival_patterns))
#       edge_capacity = model.NewIntVar(0, infinity, "flow_%s_capacity" % flow.name)
#       model.AddElement(index_variable, edge_capacities[flow.name], edge_capacity)
#       model.Add(flow_edge_bandwidth <= edge_capacity)
#       flow_edge_bandwidths.append(flow_edge_bandwidth)

#       all_vars.append(edge_capacity)
#       all_vars.append(flow_edge_bandwidth)
#       all_vars.append(q)
#       all_vars.append(e2e_delays[e])

#     e2e_sum = model.NewIntVar(0, infinity, "flow_%s_e2e_sum" % flow.name)
#     model.Add(e2e_sum == sum(e2e_delays))
#     all_vars.append(e2e_sum)

#   solution_printer = SolutionPrinter(all_vars)

#   solver = cp_model.CpSolver()
#   #solver.parameters.log_search_progress = True
#   solver.parameters.enumerate_all_solutions = True
#   solver.SolveWithSolutionCallback(model, solution_printer)
  
# def add_flow_variables(model, graph, flow, cycle_count):
#   # TODO: Change cutoff
#   paths = list(nx.all_simple_paths(graph, flow.source, flow.destination, cutoff = 20))

#   all_arrival_patterns = {}
#   all_queue_numbers = {}

#   all_e2e_delays = []
#   for p, path in enumerate(paths):
#     path_identifier = "path%s" % str(p)
#     path_e2e_delays = []

#     for v1, v2 in pairwise(path):
#       if (v1, v2) not in all_arrival_patterns:
#         all_arrival_patterns[(v1, v2)] = [[model.NewConstant(0)] * len(paths)] * cycle_count
#       if (v1, v2) not in all_queue_numbers:
#         all_queue_numbers[(v1, v2)] = [model.NewConstant(0)] * len(paths)

#       edge_identifier = "%s%s%s" % (path_identifier, v1, v2)
#       # Find the edge
#       edge = next(x for x in architecture.edges if (x.source == v1 and x.destination == v2) or (x.destination == v1 and x.source == v2))

#       q = model.NewIntVar(1, 3, "queue_%s" % edge_identifier)
#       d = model.NewConstant(edge.propagation_delay)
#       edge_delay = model.NewIntVar(0, infinity, "edge_delay_%s" % edge_identifier)

#       # Link capacity constraint
#       alpha = model.NewIntVar(0, infinity, "alpha_%s" % edge_identifier)
#       model.Add(alpha == sum(path_e2e_delays))

#       for c in range(cycle_count):
#         all_arrival_patterns[(v1, v2)][c][p] = model.NewIntVar(0, infinity, "arrival_pattern_%i_%s" % (c, edge_identifier))

#         arrival_pattern_tmp = model.NewIntVar(0, infinity, "arrival_pattern_tmp1_%i_%s" % (c, edge_identifier))
#         model.Add(arrival_pattern_tmp == (c * cycle_length) + alpha)

#         # arrival_pattern_tmp2 = arrival_pattern_tmp % flow.period
#         arrival_pattern_tmp2 = model.NewIntVar(0, infinity, "arrival_pattern_tmp2_%i_%s" % (c, edge_identifier))
#         model.AddModuloEquality(arrival_pattern_tmp2, arrival_pattern_tmp, flow.period)

#         b = model.NewBoolVar('intermediate_boolean_%i' % c)
#         model.Add(arrival_pattern_tmp2 == 0).OnlyEnforceIf(b)
#         model.Add(arrival_pattern_tmp2 != 0).OnlyEnforceIf(b.Not())

#         arrival_pattern = model.NewIntVar(0, infinity, "arrival_pattern_%i_%s" % (c, edge_identifier))
#         model.Add(arrival_pattern == flow.size).OnlyEnforceIf(b)
#         model.Add(arrival_pattern == 0).OnlyEnforceIf(b.Not())

#         model.Add(all_arrival_patterns[(v1, v2)][c][p] == arrival_pattern)

#       # Element of sum, rij.e.D + rij.q
#       # d is propagation delay in microseconds
#       # q is queue number (cycles) multiplied by the cycle length to get microseconds
#       model.Add(d + (q * cycle_length) == edge_delay)
#       path_e2e_delays.append(edge_delay)

#       # Remember this paths queue number
#       all_queue_numbers[(v1, v2)][p] = q

#     path_delay = model.NewIntVar(0, infinity, "path_delay_%s" % path_identifier)
#     model.Add(path_delay == sum(path_e2e_delays))
#     all_e2e_delays.append(path_delay)

#   path_choice = model.NewIntVar(0, len(paths) - 1, "path_choice")

#   e2e_delay = model.NewIntVar(0, infinity, "e2e_delay")
#   model.AddElement(path_choice, all_e2e_delays, e2e_delay)
#   model.Add(e2e_delay <= flow.deadline)
  
#   arrival_patterns = {}
#   queue_numbers = {}
#   for edge in graph.edges:
#     index = (edge[0], edge[1])
#     if index not in all_arrival_patterns:
#       index = (index[1], index[0])

#     if index not in arrival_patterns:
#       arrival_patterns[index] = [None] * cycle_count

#     for c in range(cycle_count):
#       # arrival_patterns[index][c] = all_arrival_patterns[index][c][path_choice]
#       arrival_patterns[index][c] = model.NewIntVar(-infinity, infinity, "flow_%s_A_%i_%s%s" % (flow.name, c, edge[0], edge[1]))
#       model.AddElement(path_choice, all_arrival_patterns[index][c], arrival_patterns[index][c])

#     # queue_numbers[index] = all_queue_numbers[index][path_choice]
#     queue_numbers[index] = model.NewIntVar(0, infinity, "flow_%s_q_%s%s" % (flow.name, edge[0], edge[1]))
#     model.AddElement(path_choice, all_queue_numbers[index], queue_numbers[index])

#   return FlowVariables(e2e_delay, queue_numbers, arrival_patterns, path_choice, paths)

# def invoke_constraint_solver(architecture, application):
#   # Calculate the cycle count by first calculating the hyperperiod (in microseconds) and then diving it by the cycle length
#   cycle_count = int(max(flow.period for flow in application.flows) / cycle_length)

#   model = cp_model.CpModel()
#   graph = create_graph(architecture)

#   all_vars = []

#   edge_bandwidth_cs = {}
#   flow_variables = []
#   for flow in application.flows:
#     print("Processing flow %s" % flow.name)
#     flow_variables.append(add_flow_variables(model, graph, flow, cycle_count))

#     for (v1, v2) in graph.edges:
#       edge = next(x for x in architecture.edges if (x.source == v1 and x.destination == v2) or (x.destination == v1 and x.source == v2))
#       index = (v1, v2)

#       for c in range(cycle_count):
#         arrival_patterns = []
#         for flow_vars in flow_variables:
#           if index not in flow_vars.arrival_patterns:
#             index = (index[1], index[0])

#           if index not in edge_bandwidth_cs:
#               edge_bandwidth_cs[index] = [[] for _ in range(cycle_count)]

#           arrival_patterns.append(flow_vars.arrival_patterns[index][c])

#         edge_bandwidth_c = model.NewIntVar(0, infinity, "bandwidth%i_%s%s" % (c, v1, v2)) 
#         model.Add(edge_bandwidth_c == sum(arrival_patterns))
#         model.Add(edge_bandwidth_c <= edge.bandwidth)
#         edge_bandwidth_cs[index][c].append(edge_bandwidth_c)

#     for f_vars in flow_variables:
#       all_vars.extend(f_vars.queue_numbers)
#       all_vars.extend(f_vars.arrival_patterns)

#   all_bandwidths = []
#   for (v1, v2) in graph.edges:
#     edge = next(x for x in architecture.edges if (x.source == v1 and x.destination == v2) or (x.destination == v1 and x.source == v2))
#     index = (v1, v2)

#     if index not in edge_bandwidth_cs:
#       edge_bandwidth_cs[index] = [[] for _ in range(cycle_count)]

#     for c in range(cycle_count):
#       # max_edge_bandwidths = max(edge_bandwidths_cs[index][c])
#       max_edge_bandwidths = model.NewIntVar(0, infinity, "max_edge_bandwidth%s%s" % (v1, v2))
#       model.AddMaxEquality(max_edge_bandwidths, edge_bandwidth_cs[index][c])

#       # left_side = max_edge_bandwidths / S
#       left_side = model.NewIntVar(0, infinity, "left_side_tmp%s%s" % (v1, v2))
#       model.AddDivisionEquality(left_side, max_edge_bandwidths, edge.bandwidth * cycle_length)

#       # edge_bandwidth = left_side * 1000
#       edge_bandwidth = model.NewIntVar(0, infinity, "edge_bandwidth%s%s" % (v1, v2))
#       model.AddMultiplicationEquality(edge_bandwidth, [left_side, 1000])

#       all_bandwidths.append(edge_bandwidth)

#   sum_bandwidths = model.NewIntVar(0, infinity, "sum_bandwidth") 
#   model.Add(sum_bandwidths == sum(all_bandwidths))

#   OMEGA = model.NewIntVar(0, infinity, "OMEGA") 
#   model.AddDivisionEquality(OMEGA, sum_bandwidths, len(architecture.edges))

#   model.Minimize(OMEGA)

#   all_vars.append(sum_bandwidths)
#   all_vars.append(OMEGA)

#   solution_printer = SolutionPrinter(all_vars)

#   solver = cp_model.CpSolver()
#   #solver.parameters.log_search_progress = True
#   solver.parameters.enumerate_all_solutions = True
#   solver.SolveWithSolutionCallback(model, solution_printer)
