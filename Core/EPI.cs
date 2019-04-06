using System;

namespace Epicoin.Core {

	public interface IEpicore {

		///<summary>Retrieves the solver core component.</summary>
		ISolver GetSolver();

		///<summary>Retrieves the validator core component.</summary>
		IValidator GetValidator();

		///<summary>Retrieves the network manager/maestro core component.</summary>
		INet GetNetworkManager();

		/// <summary>
		/// Starts Epicore. Parallel, non-blocking - Epicore creates and manages all threads it requires automatically; returns as soon as all async components are bootstrapped (initialization is also asynchronous).
		/// </summary>
		void Start();

		/// <summary>
		/// Stops Epicore. Blocking - blocks until all Epicore components have stopped.
		/// </summary>
		void Stop();

		/// <summary>
		/// Stops Epicore. Async, non-blocking - task is completed when all components have stopped.
		/// </summary>
		System.Threading.Tasks.Task StopNB();

	}

	public interface ISolver {

	}

	public interface IValidator {
		
	}

	public interface INet {
		
	}

}