using System;
using Project;
using Project.Models;

class Program
{
    static void Main(string[] args)
    {
        Architecture architecture;
        Application application;
        XMLReader.Read(TestCase.TC1, out architecture, out application);
        
    }
}