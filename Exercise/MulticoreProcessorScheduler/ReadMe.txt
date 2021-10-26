Hi, 
Here is a short readMe regarding running our simulated annealing program.

Our program is written in C# and can be run with dotnet.
We used Visual Studio Code to write our program and run it, the link below is a simple guide is needed.
https://code.visualstudio.com/docs/languages/dotnet
Alternatively the program can be run in the terminal with the .NET Core SDK.

Run the program int the terminal:
1. Navigate to the folder where 'MulticoreProcessorScheduler.csproj' is located
2. In the terminal, run 'dotnet run small'
	2a. Simulated Annealing is now run on the small test file
	2b. The program prints statistics of the run
			and saves the solution in a .xml file in the 'solutions' folder

The input 'small' can be switched out with 'medium' or 'large' to run SA on the other test cases.
The files in the 'solutions' folder are generated from earlier runs and represent our results.
