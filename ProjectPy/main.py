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


def create_graph(architecture, application):
	G = nx.Graph()
	for edge in architecture.edges:
		G.add_edge(edge.source, edge.destination)

	return G

class SolutionPrinter(cp_model.CpSolverSolutionCallback):
  def __init__(self, variables):
    cp_model.CpSolverSolutionCallback.__init__(self)
    self.variables = variables
    self.solution_count = 0

  def OnSolutionCallback(self):
    self.solution_count += 1
    print("Solution %i:" % self.solution_count)
    print("Objective: " % self.ObjectiveValue())
    for v in self.variables:
    	print('\t%s = %i' % (v, self.Value(v)))

infinity = 2**32
def add_flow_variables(model, graph, flow):
	paths = list(nx.all_simple_paths(graph, flow.source, flow.destination, cutoff = 20))

	if(len(paths) == 0):
		print(flow.source, flow.destination)

	queue_numbers = {}
	all_end_to_end_delays = []
	for p, path in enumerate(paths):
		path_identifier = "path%s" % str(p)
		path_end_to_end_delays = []

		for v1, v2 in pairwise(path):
			edge_identifier = path_identifier + v1 + v2
			# Find the edge
			edge = next(x for x in architecture.edges if (x.source == v1 and x.destination == v2) or (x.destination == v1 and x.source == v2))

			q = model.NewIntVar(1, 3, "queue_%s" % edge_identifier)
			d = model.NewConstant(edge.propagation_delay)
			edge_delay = model.NewIntVar(0, infinity, "edge_delay_%s" % edge_identifier)

			# Element of sum, rij.e.D + rij.q
			model.Add(q + d == edge_delay)
			path_end_to_end_delays.append(edge_delay)

			# Remember this paths queue number
			queue_numbers[(p, v1 + v2)] = q

		path_delay = model.NewIntVar(0, infinity, "path_delay_ %s" % path_identifier)
		model.Add(path_delay == sum(path_end_to_end_delays))
		all_end_to_end_delays.append(path_delay)

	end_to_end_delay = model.NewIntVar(0, infinity, "end_to_end_delay")
	model.AddElement(model.NewIntVar(0, len(paths) - 1, "path_choice"), all_end_to_end_delays, end_to_end_delay)
	model.Add(end_to_end_delay < flow.deadline)

	return end_to_end_delay


def invoke_constraint_solver(architecture, application):
	model = cp_model.CpModel()
	graph = create_graph(architecture, application)

	end_to_end_delay_variables = []
	for flow in application.flows:
		e2e_delay = add_flow_variables(model, graph, flow)
		end_to_end_delay_variables.append(e2e_delay)

	solution_printer = SolutionPrinter(end_to_end_delay_variables)

	solver = cp_model.CpSolver()
	# solver.parameters.log_search_progress = True
	# solver.parameters.enumerate_all_solutions = True
	solver.Solve(model, solution_printer)

	
if __name__ == "__main__":
	architecture, application = parse_xml(TestCase.TC1)
	invoke_constraint_solver(architecture, application)

