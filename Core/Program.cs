using System;
using System.Threading;

namespace Epicoin {
	class Program {
		static void Main(string[] args){
			Epicore core = new Epicore();
			Console.WriteLine("Starting up...");
			core.Start();
			Console.WriteLine("Start up successful.");
			Console.WriteLine("Press any key to exit.");
			while(!Console.KeyAvailable) Thread.Yield();
			Console.WriteLine("Shutting down...");
			core.Stop();
			Console.WriteLine("Shutdown complete.");
		}
	}
}
