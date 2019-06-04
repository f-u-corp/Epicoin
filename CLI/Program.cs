using System;
using System.Threading;

using Epicoin.Core;

using log4net;

namespace Epicoin.CLI {

	class Program {

		static bool valInit = false, solInit = false;

		static void Main(string[] args){
			IEpicore core = new Epicore();
			ILog LOG = LogManager.GetLogger("Epicoin", "CLI");
			void Info(string t, ConsoleColor c){
				Console.ForegroundColor = c;
				Console.WriteLine(t);
				Console.ForegroundColor = ConsoleColor.White;
			}
			LOG.Info("Starting up...");
			core.Start();
			core.Events.OnValidatorInitialized += v => valInit = true;
			core.Events.OnSolverInitialized += s => solInit = true;
			core.Events.OnStartedSolvingProblem += p => Info($"Started solving: {p.Problem}?>{p.Parameters}", ConsoleColor.DarkGreen);
			core.Events.OnProblemSolved += p => Info($"Problem solved: {p.Problem}?>\n	{p.Parameters}\n-->\n	{p.Solution}", ConsoleColor.Green);
			core.Events.OnEFOBEAcquired += efobe => {
				efobe.OnBlockAdded += b => Info($"Local Block Added: [{b.Hash}] {b.Problem}?>\n	{b.Parameters}\n-->\n	{b.Solution}", ConsoleColor.Magenta);
				efobe.OnBlockImmortalized += b => Info($"Block immortalized: [{b.Hash}]", ConsoleColor.DarkBlue);
				efobe.OnLCAChanged += b => Info($"New LCA: [{b.Hash}] {b.Problem}?>{b.Parameters}", ConsoleColor.Cyan);
				efobe.OnBranchRebased += br => Info($"Branch rebased: from {br.OldHash} to {br.NewHash} - {br.Problem}?>{br.Parameters}", ConsoleColor.DarkYellow);
			};
			LOG.Info("Start up successful.");
			while(!(valInit && solInit)) Thread.Yield();
			LOG.Info("CLI ready for user input.");
			CLI(LOG, core);
			LOG.Info("Press any key to exit.");
			while(!Console.KeyAvailable) Thread.Yield();
			LOG.Info("Shutting down...");
			core.Stop();
			LOG.Info("Shutdown complete.");
		}

		static void CLI(ILog LOG, IEpicore core){
			re: LOG.Info("Input 'another' to test with a different problem, or 'exit' to leave CLI problem testing");
			switch(Console.ReadLine().ToLower()){
				case "exit": case "quit": return;
				case "another": goto st;
				default: goto re;
			}
			st: LOG.Info("Input your problem!");
			var pr = Console.ReadLine();
			LOG.Info("Input parameters to solve for");
			string parms = Console.ReadLine();
			core.SolveAProblem(pr, parms);
			LOG.Info("Requested the problem to be solved. What do you want to do in the meantime?");
			goto re;
		}
	}
}
