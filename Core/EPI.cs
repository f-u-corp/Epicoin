using System;
using System.Collections.Generic;

namespace Epicoin.Core {

	/// <summary>
	/// Core of the Core. Provides methods for starting/stopping the core, as well as accessing components; aka all methods that any self-respecting epicore [implementation] must implement.
	/// </summary>
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

	/// <summary>
	/// Solver component of the Core. Responsible for loading problems (on initialization) and solving them when given the parameters, if solving problems was enabled.
	/// </summary>
	public interface ISolver {

		/// <summary>
		/// Returns collection of all loaded problems.
		/// </summary>
		/// <returns>Collection of loaded problems.</returns>
		IEnumerable<string> GetProblems();

		/// <summary>
		/// Is the solver enabled.
		/// </summary>
		/// <returns>Whether problem solving is enabled.</returns>
		bool SolvingEnabled();

		/// <summary>
		/// Is solving problems with given ID/name enabled. Undefined if problem solving (overall) is disabled.
		/// </summary>
		/// <param name="problem">Problem ID/name</param>
		/// <returns>Whether solving given problem is enabled.</returns>
		bool SolvingEnabled(string problem);

		/// <summary>
		/// Marks solving problems with given ID/name as enabled/disabled. Result is not explicitly defined if problem solving (overall) is disabled.
		/// </summary>
		/// <param name="problem">Problem ID/name</param>
		/// <param name="doSolve">Whether to solve these problems</param>
		void SetSolvingEnabled(string problem, bool doSolve);

		bool IsSolving();

	}

	/// <summary>
	/// Validator component of the Core. Responsible for validating problems solutions (both by-self and received) as well as the EFOBE.
	/// </summary>
	public interface IValidator {
		
	}

	/// <summary>
	/// Networking component of the Core.
	/// </summary>
	public interface INet {
		
	}

}