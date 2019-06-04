using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

using log4net;

[assembly: InternalsVisibleTo("Core.Tests")]
namespace Epicoin.Core {

	public class Epicore : IEpicore {

		internal static readonly log4net.Repository.ILoggerRepository LOGREPO = LogManager.CreateRepository("Epicoin");
		internal static readonly ILog LOG = LogManager.GetLogger("Epicoin", "Epicore");
		private static void LogLoadConfig(System.IO.FileInfo config) => log4net.Config.XmlConfigurator.Configure(LOGREPO, config);

		static Epicore(){
			LogLoadConfig(new System.IO.FileInfo("log4net.config"));
		}

		internal Solver solver;
		internal Validator validator;
		internal NetworkMaestro maestro;

		internal Action<Solver.ITM> sendITM2Solver;
		internal Action<Validator.ITM> sendITM2Validator;
		internal Action<Epicoin.Core.Net.ITM> sendITM2Net;

		internal AsyncEventsManager events = new AsyncEventsManager();
		public EpicoreEvents Events => events;

		internal bool stop { get; private set; }

		/// <summary>
		/// Creates new Epicore instance. Fast - all actual initialization happens async on startup (when Start is invoked).
		/// </summary>
		/// <param name="solverEnabled">Whether [local] problem solving is enabled.</param>
		public Epicore(bool solverEnabled = true){
			st = new Thread((solver = new Solver(this, solverEnabled)).InitAndRun);
			vt = new Thread((validator = new Validator(this)).InitAndRun);
			nt = new Thread((maestro = new NetworkMaestro(this)).InitAndRun);

			st.Name = "Solver Thread";
			vt.Name = "Validator Thread";
			nt.Name = "Networking Thread";

			sendITM2Solver = solver.sendITM;
			sendITM2Validator = validator.sendITM;
			sendITM2Net = maestro.sendITM;
		}

		///<summary>Retrieves the solver core component.</summary>
		public ISolver GetSolver() => solver;
		///<summary>Retrieves the validator core component.</summary>
		public IValidator GetValidator() => validator;
		///<summary>Retrieves the network manager/maestro core component.</summary>
		public INet GetNetworkManager() => maestro;

		protected Thread vt, st, nt;

		/// <summary>
		/// Starts Epicore (and all related threads). Parallel, non-blocking - Epicore creates and manages all threads it requires automatically.
		/// </summary>
		public void Start(){
			vt.Start();
			nt.Start();
			st.Start();
		}

		public void SolveAProblem(string problem, string parameters){
			sendITM2Net(new Epicoin.Core.Net.ITM.ProblemToSolve(problem, parameters));
			sendITM2Solver(new Solver.ITM.ProblemToBeSolved(problem, parameters));
		}

		/// <summary>
		/// Stops Epicore (and all related threads). Blocking - blocks until all Epicore components have stopped.
		/// </summary>
		public void Stop(){
			stop = true;
			while(vt.IsAlive || st.IsAlive || nt.IsAlive) Thread.Yield();
		}

		/// <summary>
		/// Stops Epicore (and all related threads). Non-blocking - task is marked as completed when all components have stopped.
		/// </summary>
		public async Task StopNB(){
			await Task.Run((Action) Stop);
		}

	}
	
	internal sealed class AsyncEventsManager : EpicoreEvents {

		private static void RAINN(object nn, Action a){ if(nn != null) Task.Run(a); }
		public static void FireAsync<T>(Action<T> eve, T param) => RAINN(eve, () => eve(param));
		public static void FireAsync<T1,T2>(Action<T1,T2> eve, T1 param1, T2 param2) => RAINN(eve, () => eve(param1, param2));
		public static void FireAsync<T1,T2,T3>(Action<T1,T2,T3> eve, T1 param1, T2 param2, T3 param3) => RAINN(eve, () => eve(param1, param2, param3));
		public static void FireAsync<T1,T2,T3,T4>(Action<T1,T2,T3,T4> eve, T1 param1, T2 param2, T3 param3, T4 param4) => RAINN(eve, () => eve(param1, param2, param3, param4));
		public static void FireAsync<T1,T2,T3,T4,T5>(Action<T1,T2,T3,T4,T5> eve, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5) => RAINN(eve, () => eve(param1, param2, param3, param4, param5));

		public event Action<ISolver> OnSolverInitialized;
		public void FireOnSolverInitialized(Solver solver) => FireAsync(OnSolverInitialized, solver);
		public event Action<IValidator> OnValidatorInitialized;
		public void FireOnValidatorInitialized(Validator validator) => FireAsync(OnValidatorInitialized, validator);
		public event Action<INet> OnNetworkingInitialized;
		public void FireOnNetworkingInitialized(INet net) => FireAsync(OnNetworkingInitialized, net);

		public event Action<EFOBE> OnEFOBEAcquired;
		public void FireOneEFOBEAcquired(EFOBE efobe) => FireAsync(OnEFOBEAcquired, efobe);

		public event Action<(string, string)> OnStartedSolvingProblem;
		public void FireOnStartedSolvingProblem(string problem, string parms) => FireAsync(OnStartedSolvingProblem, (problem, parms));
		public event Action<(string, string, string)> OnProblemSolved;
		public void FireOnProblemSolved(string problem, string parms, string sol) => FireAsync(OnProblemSolved, (problem, parms, sol));

	}

	/// <summary>
	/// Internal base class for any Epicoin main component - component significant enough to be run on dedicated thread(s).
	/// Provides base methods for execution, as well as Inter-Thread-Comms structure and higher-level ITC utils.
	/// </summary>
	internal abstract class MainComponent<ITM> where ITM : ITCMessage {

		protected readonly Epicore core;

		protected MainComponent(Epicore core) => this.core = core;

		internal abstract void InitAndRun();

		/*
		 * ITC
		 */
		
		protected InterThreadComms<ITM> itc = new InterThreadComms<ITM>();
		public virtual Action<ITM> sendITM { get => itc.sendMessage; }

		protected void readITInbox(Action<ITM> readMessage){
			ITM m;
			while((m = itc.readMessageOrDefault()) != null) readMessage(m);
		}

		protected ITM waitForITMessage(Predicate<ITM> p){
			ITM m;
			while((m = itc.readMessageOrDefault()) == null || !p(m)) itc.stashReadMessage(m);
			itc.readdStash();
			return m;
		}

		protected SITM waitForITMessageOfType<SITM>() where SITM : ITM => (SITM) waitForITMessage(m => m is SITM);

	}

	/// <summary>
	/// Thread-safe "mailbox" - used for safe inter-thread communications on the static structure exchange basis [aka immutable messages].
	/// This class in particular is receiver's message box - any thread can safely send messages to this box, and the thread owner the box will [eventually] process them.
	/// </summary>
	/// <typeparam name="M">Base type of all messages received by this box. <b>All messages must be inherently immutable.</b></typeparam>
	/// <remarks>
	/// In theory, all consumer operations (except ones using stashing) are thread safe as well, meaning, <i>in theory</i>, the owner of the box can be multithreaded.
	/// </remarks>
	internal class InterThreadComms<M> where M : ITCMessage  {

		protected ConcurrentQueue<M> messages = new ConcurrentQueue<M>();

		public void sendMessage(M message){
			if(message != null) messages.Enqueue(message);
		}

		public M readMessageOrDefault() => messages.TryDequeue(out M m) ? m : default(M);

		protected ConcurrentQueue<M> stash = new ConcurrentQueue<M>();
		public void stashReadMessage(M message){
			if(message != null) stash.Enqueue(message);
		}

		public void readdStash(){
			while(stash.TryDequeue(out M m)) messages.Enqueue(m);
		}

	}

	internal interface ITCMessage {

	}

}