using System;
using System.Threading;

using Epicoin.Core;

using log4net;

namespace Epicoin.CLI {

	class Program {
		static void Main(string[] args){
			IEpicore core = new Epicore();
			ILog LOG = LogManager.GetLogger("Epicoin", "CLI");
			LOG.Info("Starting up...");
			core.Start();
			LOG.Info("Start up successful.");
			LOG.Info("Press any key to exit.");
			while(!Console.KeyAvailable) Thread.Yield();
			LOG.Info("Shutting down...");
			core.Stop();
			LOG.Info("Shutdown complete.");
		}
	}
}
