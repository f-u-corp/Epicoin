using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Cloo;
using Cloo.Extensions;

using Newtonsoft.Json;

[assembly: InternalsVisibleTo("Core.Tests")]
namespace Epicoin.Core {

	internal class INPcProblem<P, S> : INPcProblem where P : struct where S : struct {

		readonly ComputeProgram prog;
		readonly ComputeKernel slv, chck;

		public INPcProblem(ComputeProgram prog){
			this.prog = prog;
			slv = this.prog.CreateKernel("solve");
			chck = this.prog.CreateKernel("check");
		}

		public void Dispose(){
			slv.Dispose();
			chck.Dispose();
			prog.Dispose();
		}

		internal static Task<T[]> AsyncOpenCLWrap<T>(ComputeCommandQueue ccq, ComputeKernel kernel, ComputeBuffer<T> outputReadbuffer, CancellationToken cancel, long[][] globalLocalWorkSize = null) where T : struct {
			var olen = outputReadbuffer.Count;
			if(globalLocalWorkSize == null) globalLocalWorkSize = new []{new []{olen}, null}; //TODO: do a better job
			var task = new TaskCompletionSource<T[]>();
			var eve = new List<ComputeEventBase>();
			ccq.Execute(kernel, null, globalLocalWorkSize[0], globalLocalWorkSize[1], eve);
			ccq.Flush();
			eve[0].Completed += (s1,a1) => {
				if(cancel.IsCancellationRequested) task.SetCanceled();
				else {
					T[] ts = new T[olen];
					ccq.ReadFromBuffer(outputReadbuffer, ref ts, false, eve);
					eve[1].Completed += (s2,a2) => task.SetResult(ts);
					eve[1].Aborted += (s2,a2) => task.SetException(new Exception("OpenCL abnormal task termination occured when reading the output"));
				}
			};
			eve[0].Aborted += (s1,a1) => task.SetException(new Exception("OpenCL abnormal task termination occured during computation"));
			return task.Task;
		}

		internal async Task<S[]> solve(P[] parms, CancellationToken cancel){
			long Len = parms.LongLength;
			using(var parBuff = new ComputeBuffer<P>(prog.Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.UseHostPointer, parms))
			using(var solBuff = new ComputeBuffer<S>(prog.Context, ComputeMemoryFlags.WriteOnly, Len)){
				slv.SetMemoryArgument(0, parBuff);
				slv.SetMemoryArgument(1, solBuff);
				using(var ccq = new ComputeCommandQueue(prog.Context, prog.Devices[0], ComputeCommandQueueFlags.None)){
					return await AsyncOpenCLWrap(ccq, slv, solBuff, cancel);
				}
			}
		}
		public async Task<string> solve(string parms, CancellationToken cancel) => JsonConvert.SerializeObject(await solve(JsonConvert.DeserializeObject<P[]>(parms), cancel));

		internal async Task<bool> check(P[] parms, S[] solutions, CancellationToken cancel){
			long Len = parms.LongLength;
			if(Len != solutions.LongLength) throw new InvalidOperationException("Solutions must be as much as parameters!");
			using(var parBuff = new ComputeBuffer<P>(prog.Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.UseHostPointer, parms))
			using(var solBuff = new ComputeBuffer<S>(prog.Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.UseHostPointer, solutions))
			using(var valiBuff = new ComputeBuffer<short>(prog.Context, ComputeMemoryFlags.WriteOnly, Len)){
				chck.SetMemoryArgument(0, parBuff);
				chck.SetMemoryArgument(1, solBuff);
				chck.SetMemoryArgument(2, valiBuff);
				using(var ccq = new ComputeCommandQueue(prog.Context, prog.Devices[0], ComputeCommandQueueFlags.None)){
					return (await AsyncOpenCLWrap(ccq, chck, valiBuff, cancel)).All(s => s != 0);
				}
			}
		}
		public async Task<bool> check(string parms, string sols, CancellationToken cancel) => await check(JsonConvert.DeserializeObject<P[]>(parms), JsonConvert.DeserializeObject<S[]>(sols), cancel);

	}

	/// <summary>
	/// INTERNAL: DO NOT USE!!!
	/// </summary>
	internal interface INPcProblem : IDisposable {

		Task<string> solve(string parms, CancellationToken cancel);
		Task<bool> check(string parms, string sols, CancellationToken cancel);

	}

	/// <summary>
	/// Internal future-fixed wrapper class for in-dev-mutable problems and solutions represented publically.
	/// </summary>
	internal struct NPcProblemWrapper : IDisposable {

		private static readonly Type NPCPGTD = typeof(INPcProblem<,>);

		readonly INPcProblem deleg;
		readonly Type parmsT, solT;

		public NPcProblemWrapper(ComputeProgram prog, Type parameterT, Type solutionT){
			this.parmsT = parameterT;
			this.solT = solutionT;
			this.deleg = (INPcProblem) NPCPGTD.MakeGenericType(new []{parameterT, solutionT}).GetConstructor(new []{typeof(ComputeProgram)}).Invoke(new object[]{prog});
		}

		public void Dispose() => deleg.Dispose();

		/// <summary>
		/// Solves the problem given the parameters (with string representations - in any consistent way the problem may like).
		/// </summary>
		/// <param name="parms">Parameters to find the solution for.</param>
		/// <param name="cancel">Cancellation token to (attempt) to terminate the computation.</param>
		/// <returns>The solution to the problem, represented as string (in any consitent way the problem may like).</returns>
		public Task<string> solve(string parms, CancellationToken cancel) => deleg.solve(parms, cancel);

		/// <summary>
		/// Checks the solution to the problem for given parameters (with string representations - in any consistent way the problem may like).
		/// </summary>
		/// <param name="parms">Parameters to check with.</param>
		/// <param name="solution">Solution to check. </param>
		/// <param name="cancel">Cancellation token to (attempt) to terminate the computation.</param>
		/// <returns>Whether the solution is correct.</returns>
		public Task<bool> check(string parms, string solution, CancellationToken cancel) => deleg.check(parms, solution, cancel);

	}

	/// <summary>
	/// Solver, main component, responsible for loading and solving problems.
	/// </summary>
	internal class Solver : MainComponent<Solver.ITM>, ISolver {

		internal readonly static log4net.ILog LOG = log4net.LogManager.GetLogger("Epicoin", "Epicore-Solver");

		protected bool doSolve;
		public Solver(Epicore core, bool doSolve) : base(core){
			this.doSolve = doSolve;
		}

		public bool SolvingEnabled() => doSolve;

		protected ImmutableDictionary<string, NPcProblemWrapper> problemsRegistry;
		protected HashSet<string> problemsICanSolve;

		public IEnumerable<string> GetProblems() => problemsRegistry.Keys;
		public bool SolvingEnabled(string problem) => problemsICanSolve.Contains(problem);
		public void SetSolvingEnabled(string problem, bool doSolve){
			if(problemsRegistry.ContainsKey(problem)){
				if(doSolve) problemsICanSolve.Add(problem);
				else problemsICanSolve.Remove(problem);
			}
		}

		internal override void InitAndRun(){
			InitOpenCL();
			LoadProblems();
			core.sendITM2Validator(new Validator.ITM.GetProblemsRegistry(problemsRegistry));
			core.events.FireOnSolverInitialized(this);
			AsyncCLIProblemTesting();
			TakeCareOfStuffFromTimeToTime();
			CleanupOpenCL();
		}

		private void AsyncCLIProblemTesting(){
			Action homeUI = null, solveUI = null;
			Action<string, string, string> validateUI = null;
			void wannaContinue(){
				re: LOG.Info("Input 'another' to test with a different problem, or 'exit' to leave CLI problem testing");
				switch(Console.ReadLine().ToLower()){
					case "exit": case "quit": break;
					case "another":
						solveUI();
						break;
					default: goto re;
				}
			};
			void solveInput(){
				st: LOG.Info("Input your problem!");
				var pr = Console.ReadLine();
				if(!problemsRegistry.ContainsKey(pr)) goto st;
				LOG.Info("Input parameters to solve for");
				string parms = Console.ReadLine();
				StartSolving(pr, parms);
				var solt = currentlySolving;
				solt.Wait();
				if(solt.IsCompletedSuccessfully){
					string sol = solt.Result;
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine(sol);
					Console.ForegroundColor = ConsoleColor.White;
					LOG.Info("Press a key to validate found solution");
					Console.ReadKey();
					validateUI(pr, parms, sol);
				} else {
					Console.WriteLine("Task cancelled or errored :(");
					wannaContinue();
				}
			};
			void validateInput(string problem, string parms, string sol){
				var valt = problemsRegistry[problem].check(parms, sol, CancellationToken.None);
				valt.Wait();
				bool val = valt.Result;
				Console.ForegroundColor = val ? ConsoleColor.Green : ConsoleColor.Red;
				Console.WriteLine("Solution valid: " + val);
				Console.ForegroundColor = ConsoleColor.White;
				wannaContinue();
			}
			(homeUI, solveUI, validateUI) = (wannaContinue, solveInput, validateInput);
			LOG.Info("Welcome to CLI problem testing!");
			Task.Run(homeUI);
		}

		protected ComputeDevice clDevice;
		protected ComputeContext clContext;

		protected void InitOpenCL(string device = null){
			clDevice = ComputePlatform.Platforms.SelectMany(p => p.Devices).First(d => device == null || $"{d.Name} {d.DriverVersion}" == device);
			clContext = new ComputeContext(new[]{clDevice}, new ComputeContextPropertyList(clDevice.Platform), (info, data, size, ud) => LOG.Error("OpenCL Error: " + info), IntPtr.Zero);
		}

		protected void CleanupOpenCL(){
			foreach(var p in problemsRegistry.Values) p.Dispose();
			clContext.Dispose();
		}

		protected void LoadProblems(){
			LOG.Info("Loading problems...");
			var reg = new Dictionary<string, NPcProblemWrapper>();
			var pdir = new DirectoryInfo("npocl");
			pdir.Create();
			ComputeProgram compile(FileInfo f){
				var p = new ComputeProgram(clContext, File.ReadAllText(f.FullName));
				p.Build(new[]{p.Devices[0]}, null, null, IntPtr.Zero);
				ComputeProgramBuildStatus status() => p.GetBuildStatus(p.Devices[0]);
				if(status() == ComputeProgramBuildStatus.Success){
					Solver.LOG.Info("Successfully Compiled program: " + f.Name);
				} else {
					Solver.LOG.Error("Program compilation failed: " + f.Name);
					Solver.LOG.Error(p.GetBuildLog(p.Devices[0]));
				}
				return p;
			}
			foreach(var tr in pdir.GetFiles("*.cl").Select(f => (name: f.Name.Substring(0, f.Name.Length-3), cl: f, str: new FileInfo(f.FullName.Substring(0, f.FullName.Length-2) + "json"))).Select(tr => (name: tr.name, cl: compile(tr.cl), str: JsonStructCreator.CreateStructs(tr.name, File.ReadAllText(tr.str.FullName))))) reg.Add(tr.name, new NPcProblemWrapper(tr.cl, tr.str["Parameter"], tr.str["Solution"]));
			this.problemsRegistry = ImmutableDictionary.ToImmutableDictionary(reg);
			this.problemsICanSolve = new HashSet<string>(problemsRegistry.Keys); //TODO persistent config? I'd say it does not belong to core...
			LOG.Info($"Successfuly loaded problems - {problemsRegistry.Count} ({String.Join(", ", problemsRegistry.Keys)})");
		}

		//Running

		protected Queue<(string problem, string parms)> toBeSolvedQueue = new Queue<(string problem, string parms)>();
		protected (string problem, string parms) currentlySolvingData;
		protected Task<string> currentlySolving;
		protected CancellationTokenSource currentlySolvingCancellor;

		public bool IsSolving() => currentlySolving != null;

		protected virtual void TakeCareOfStuffFromTimeToTime(){
			while(!core.stop){
				var message = itc.readMessageOrDefault();
				if(message is ITM.ProblemToBeSolved){
					var ptbs = message as ITM.ProblemToBeSolved;
					toBeSolvedQueue.Enqueue((ptbs.Problem, ptbs.Parameters));
				}
				if(message is ITM.CancelPendingProblem){
					var cpp = message as ITM.CancelPendingProblem;
					if(currentlySolvingData.problem == cpp.Problem && currentlySolvingData.parms == cpp.Parameters) currentlySolvingCancellor.Cancel();
					else if(toBeSolvedQueue.Contains((cpp.Problem, cpp.Parameters))){
						var tbsl = toBeSolvedQueue.ToList();
						tbsl.Remove((cpp.Problem, cpp.Parameters));
						toBeSolvedQueue = new Queue<(string problem, string parms)>(tbsl);
					}
				}
				if(currentlySolving != null){
					if(currentlySolving.IsCompletedSuccessfully){
						if(!currentlySolvingCancellor.IsCancellationRequested) ProblemIHaveSolved(currentlySolvingData.problem, currentlySolvingData.parms, currentlySolving.Result);
						currentlySolvingData = (null, null);
						currentlySolving = null;
						currentlySolvingCancellor = null;
					} else if(currentlySolving.IsCompleted){
						currentlySolvingData = (null, null);
						currentlySolving = null;
						currentlySolvingCancellor = null;
					}
				}
				if(currentlySolving == null){
					if(toBeSolvedQueue.TryDequeue(out (string problem, string parms) next)) StartSolving(next.problem, next.parms);
					else Thread.Yield();
				}
			}
		}

		protected bool StartSolving(string problem, string parms){
			if(currentlySolving != null) return false;
			if(!doSolve) return false;
			if(!SolvingEnabled(problem)) return false;
			currentlySolvingData = (problem, parms);
			currentlySolving = problemsRegistry[problem].solve(parms, (currentlySolvingCancellor = new CancellationTokenSource()).Token);
			core.events.FireOnStartedSolvingProblem(problem, parms);
			return true;
		}

		protected void AttemptToCancelCurrentlySolving(){
			currentlySolvingCancellor.Cancel();
		}

		protected void ProblemIHaveSolved(string problem, string parms, string sol){
			core.sendITM2Validator(new Validator.ITM.ProblemSolved(problem, parms, sol));
			core.events.FireOnProblemSolved(problem, parms, sol);
		}

		/*
		 * ITC
		 */

		internal class ITM : ITCMessage {
			internal class ProblemToBeSolved : ITM { //From Network
				public readonly string Problem, Parameters;

				public ProblemToBeSolved(string problem, string parms){
					this.Problem = problem;
					this.Parameters = parms;
				}
			}
			internal class CancelPendingProblem : ITM { //From Validator
				public readonly string Problem, Parameters;

				public CancelPendingProblem(string problem, string parms){
					this.Problem = problem;
					this.Parameters = parms;
				}
			}
		}

	}

}