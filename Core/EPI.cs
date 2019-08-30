using System;

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

		///<summary>Subscribe to Epicore events! Free entry upon presentation of a ticket!</summary>
		EpicoreEvents Events { get; }

		/// <summary>
		/// Starts Epicore. Parallel, non-blocking - Epicore creates and manages all threads it requires automatically; returns as soon as all async components are bootstrapped (initialization is also asynchronous).
		/// </summary>
		void Start();

		/// <summary>
		/// Requests Epicoin to solve given problem - adds it to local queue (if solving is enabled) and tells everyone around as wel.
		/// </summary>
		void SolveAProblem(string problem, string parameters);

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
	/// Public events happening in the core. Allows to react to certain events taking place anywhere in the epicore.<br/>
	/// Undefined source - for any and all event, source location / thread / ... is not defined and can take place from anywhere in epicore.<br/>
	/// Asynchronous - all events are fired and processed asynchronously from all epicore and your threads, [thus] heavy computations can be potentially performed directly in the event handler.
	/// </summary>
	public interface EpicoreEvents {

		event Action<ISolver> OnSolverInitialized;
		event Action<IValidator> OnValidatorInitialized;
		event Action<INet> OnNetworkingInitialized;

		event Action<EFOBE> OnEFOBEAcquired;

		event Action<(string Problem, string Parameters)> OnStartedSolvingProblem;
		event Action<(string Problem, string Parameters, string Solution)> OnProblemSolved;
	}

	/// <summary>
	/// Public events taking place in the EFOBE. Follow the same async execution as Epicore events
	/// </summary>
	public interface EFOBEEvents {
		event Action<(string Problem, string Parameters, string Solution, string Parent, string Hash)> OnBlockAdded;
		event Action<(string Problem, string Parameters, string Solution, string Hash)> OnBlockImmortalized;
		event Action<(string Problem, string Parameters, string Solution, string Hash)> OnLCAChanged;
		event Action<(string Problem, string Parameters, string Solution, string OldParent, string OldHash, string NewParent, string NewHash)> OnBranchRebased;
	}

	/// <summary>
	/// Solver component of the Core. Responsible for loading problems (on initialization) and solving them when given the parameters, if solving problems was enabled.
	/// </summary>
	public interface ISolver {
		
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