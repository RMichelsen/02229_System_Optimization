using Project.Models;
using Google.OrTools.ConstraintSolver;
using QuikGraph.Algorithms.Observers;
using QuikGraph.Algorithms.Search;

namespace Project.Solvers
{
    class TSNConstraintSolver
    {
        public TSNConstraintSolver(Architecture architecture, Application application)
        {
            var graph = new QuikGraph.AdjacencyGraph<string, QuikGraph.Edge<string>>();

            foreach(var vertex in architecture.Vertices)
            {
                graph.AddVertex(vertex.Name);
            }
            foreach(var edge in architecture.Edges)
            {
                graph.AddEdge(new QuikGraph.Edge<string>(edge.Source, edge.Destination));
                // TODO: Bidirectional? Allpaths break
            }

            var algo = new EdgeDepthFirstSearchAlgorithm<string, QuikGraph.Edge<string>>(graph);
            var observer = new EdgePredecessorRecorderObserver<string, QuikGraph.Edge<string>>();
            
            var model = new Solver("ConstraintSolver");
            IntVarVector results = new IntVarVector();

            foreach(var flow in application.Flows)
            {
                using(observer.Attach(algo))
                {
                    algo.Compute(flow.Source);
                }

                // These are the routes we need to consider (and optimize for a given source -> destination)
                var candidateRoutes = observer.AllPaths().Where(x => x.Last().Target == flow.Destination).ToList();

                // TODO: Remove when bidirectional works...
                if(candidateRoutes.Count == 0)
                {
                    continue;
                }

                int longestRoute = 0;
                foreach(var route in candidateRoutes)
                {
                    if(route.Count > longestRoute) longestRoute = route.Count;
                }

                var queueNumbers = model.MakeIntVarMatrix(candidateRoutes.Count, longestRoute, new int[architecture.Edges.Count * longestRoute]);

                // End-to-end delay variable for each route
                IntVarVector allE2E = new IntVarVector();
                for(int r = 0; r < candidateRoutes.Count; ++r)
                {
                    IntVarVector routeE2E = new IntVarVector();
                    var routes = candidateRoutes[r].ToList();
                    for(int e = 0; e < candidateRoutes[r].Count; ++e)
                    {
                        var edge = routes[e];
                        var match = architecture.Edges.FindIndex(x => x.Source == edge.Source && x.Destination == edge.Target);
                        if(match != -1)
                        {
                            var q = model.MakeIntVar(1, 3);
                            var d = model.MakeIntConst(architecture.Edges[match].PropagationDelay);

                            // Element of sum, rij.e.D + rij.q
                            // TODO: Cycle length??
                            routeE2E.Add(model.MakeSum(d, q).Var());
                            queueNumbers[r, e] = q;
                        }
                    }

                    // Final end-to-end delay for a route
                    allE2E.Add(model.MakeSum(routeE2E).Var());
                }

                IntVar routeIndex = model.MakeIntVar(0, candidateRoutes.Count - 1);
                IntExpr E2E = model.MakeElement(allE2E, routeIndex);

                results.Add(routeIndex);
                results.Add(E2E.Var());
                results.AddRange(queueNumbers.Flatten());
            }

            DecisionBuilder db = model.MakePhase(results, Solver.CHOOSE_FIRST_UNBOUND, Solver.INT_VALUE_SIMPLE);
            model.Solve(db);

            while (model.NextSolution())
            {
                long e = 0;

            }

        }
    }
}
