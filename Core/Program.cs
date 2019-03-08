using System;
using System.Threading;

namespace Epicoin {
	class Program {
		static void Main(string[] args){
			Epicore core = new Epicore();
			Epicore.LOG.Info("Starting up...");
			core.Start();
			Epicore.LOG.Info("Start up successful.");
			Epicore.LOG.Info("Press any key to exit.");
			while(!Console.KeyAvailable) Thread.Yield();
			Epicore.LOG.Info("Shutting down...");
			core.Stop();
			Epicore.LOG.Info("Shutdown complete.");
		}
	}
}
