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

	public class Epicore {

		internal static readonly log4net.Repository.ILoggerRepository LOGREPO = LogManager.CreateRepository("Epicoin");
		internal static readonly ILog LOG = LogManager.GetLogger("Epicoin", "Epicore");
		private static void LogLoadConfig(System.IO.FileInfo config) => log4net.Config.XmlConfigurator.Configure(LOGREPO, config);

		static Epicore(){
			LogLoadConfig(new System.IO.FileInfo("log4net.config"));
		}

		internal Solver solver;
		internal Validator validator;

		internal Action<Solver.ITM> sendITM2Solver;
		internal Action<Validator.ITM> sendITM2Validator;
		internal Action sendITM2Net;

		internal bool stop { get; private set; }

		/// <summary>
		/// Creates new Epicore instance. Fast - all actual initialization happens async on startup (when Start is invoked).
		/// </summary>
		public Epicore(bool solverEnabled = true){
			st = new Thread((solver = new Solver(this, solverEnabled)).InitAndRun);
			vt = new Thread((validator = new Validator(this)).InitAndRun);
			nt = new Thread(() => {}); //TODO wire in network component

			sendITM2Solver = solver.sendITM;
			sendITM2Validator = validator.sendITM;
		}

		protected Thread vt, st, nt;

		/// <summary>
		/// Starts Epicore (and all related threads). Parallel, non-blocking - Epicore creates and manages all threads it requires automatically.
		/// </summary>
		public void Start(){
			vt.Start();
			nt.Start();
			st.Start();
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
		public Action<ITM> sendITM { get => itc.sendMessage; }

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

		public void sendMessage(M message) => messages.Enqueue(message);

		public M readMessageOrDefault() => messages.TryDequeue(out M m) ? m : default(M);

		protected ConcurrentQueue<M> stash = new ConcurrentQueue<M>();
		public void stashReadMessage(M message) => stash.Enqueue(message);

		public void readdStash(){
			while(stash.TryDequeue(out M m)) messages.Enqueue(m);
		}

	}

	internal interface ITCMessage {

	}

}