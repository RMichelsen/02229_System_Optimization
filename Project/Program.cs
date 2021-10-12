using Project;
using Project.Models;
using Project.Solvers;

class Program
{
    static void Main(string[] args)
    {
        Architecture architecture;
        Application application;
        XMLReader.Read(TestCase.TC1, out architecture, out application);

        TSNConstraintSolver solver = new TSNConstraintSolver(architecture, application);
    }
}

