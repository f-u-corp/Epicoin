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
			LOG.Info("Starting up...");
			core.Start();
			core.Events.OnValidatorInitialized += v => valInit = true;
			core.Events.OnSolverInitialized += s => solInit = true;
			core.Events.OnStartedSolvingProblem += p => LOG.Info($"Started solving: {p.Problem}?>{p.Parameters}");
			core.Events.OnProblemSolved += p => LOG.Info($"Problem solved: {p.Problem}?>{p.Parameters}-->{p.Solution}");
			core.Events.OnEFOBEAcquired += efobe => {
				efobe.OnBlockAdded += b => LOG.Info($"Local Block Added: [{b.Hash}] {b.Problem}?>{b.Parameters}-->{b.Solution}");
				efobe.OnBlockImmortalized += b => LOG.Info($"Block immortalized: [{b.Hash}]");
				efobe.OnLCAChanged += b => LOG.Info($"New LCA: [{b.Hash}] {b.Problem}?>{b.Parameters}");
				efobe.OnBranchRebased += br => LOG.Info($"Branch rebased: from {br.OldHash} to {br.NewHash} - {br.Problem}?>{br.Parameters}");
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
