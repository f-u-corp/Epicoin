using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;
using System.Reflection;

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

		internal S[] solve(P[] parms){
			long Len = parms.LongLength;
			using(var parBuff = new ComputeBuffer<P>(prog.Context, ComputeMemoryFlags.ReadOnly, parms))
			using(var solBuff = new ComputeBuffer<S>(prog.Context, ComputeMemoryFlags.WriteOnly, Len)){
				slv.SetMemoryArgument(0, parBuff);
				slv.SetMemoryArgument(1, solBuff);
				using(var ccq = new ComputeCommandQueue(prog.Context, prog.Devices[0], ComputeCommandQueueFlags.None)){
					ccq.Execute(slv, null, new []{Len}, null, null);
					S[] sols = new S[Len];
					ccq.Finish();
					ccq.ReadFromBuffer(solBuff, ref sols, true, null);
					return sols;
				}
			}
		}
		public string solve(string parms) => JsonConvert.SerializeObject(solve(JsonConvert.DeserializeObject<P[]>(parms)));

		internal bool check(P[] parms, S[] solutions){
			long Len = parms.LongLength;
			if(Len != solutions.LongLength) throw new InvalidOperationException("Solutions must be as much as parameters!");
			using(var parBuff = new ComputeBuffer<P>(prog.Context, ComputeMemoryFlags.ReadOnly, parms))
			using(var solBuff = new ComputeBuffer<S>(prog.Context, ComputeMemoryFlags.ReadOnly, solutions))
			using(var valiBuff = new ComputeBuffer<short>(prog.Context, ComputeMemoryFlags.WriteOnly, Len)){
				chck.SetMemoryArgument(0, parBuff);
				chck.SetMemoryArgument(1, solBuff);
				chck.SetMemoryArgument(2, valiBuff);
				using(var ccq = new ComputeCommandQueue(prog.Context, prog.Devices[0], ComputeCommandQueueFlags.None)){
					ccq.Execute(chck, null, new []{Len}, null, null);
					short[] val = new short[Len];
					ccq.Finish();
					ccq.ReadFromBuffer(valiBuff, ref val, true, null);
					return val.All(s => s != 0);
				}
			}
		}
		public bool check(string parms, string sols) => check(JsonConvert.DeserializeObject<P[]>(parms), JsonConvert.DeserializeObject<S[]>(sols));

	}

	/// <summary>
	/// INTERNAL: DO NOT USE!!!
	/// </summary>
	internal interface INPcProblem : IDisposable {

		string solve(string parms);
		bool check(string parms, string sols);

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
		/// <returns>The solution to the problem, represented as string (in any consitent way the problem may like).</returns>
		public string solve(string parms) => deleg.solve(parms);

		/// <summary>
		/// Checks the solution to the problem for given parameters (with string representations - in any consistent way the problem may like).
		/// </summary>
		/// <param name="parms">Parameters to check with.</param>
		/// <param name="solution">Solution to check. </param>
		/// <returns>Whether the solution is correct.</returns>
		public bool check(string parms, string solution) => deleg.check(parms, solution);

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
			CleanupOpenCL();
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
			foreach(var tr in pdir.GetFiles("*.cl").Select(f => (name: f.Name.Substring(f.Name.Length-3), cl: f, str: new FileInfo(f.FullName.Substring(0, f.FullName.Length-2) + "json"))).Select(tr => (name: tr.name, cl: new ComputeProgram(clContext, File.ReadAllText(tr.cl.FullName)), str: JsonStructCreator.CreateStructs(tr.name, File.ReadAllText(tr.str.FullName))))) reg.Add(tr.name, new NPcProblemWrapper(tr.cl, tr.str["Parameter"], tr.str["Solution"]));
			this.problemsRegistry = ImmutableDictionary.ToImmutableDictionary(reg);
			this.problemsICanSolve = new HashSet<string>(problemsRegistry.Keys); //TODO persistent config? I'd say it does not belong to core...
			LOG.Info($"Successfuly loaded problems - {problemsRegistry.Count} ({String.Join(", ", problemsRegistry.Keys)})");
		}

		
		/*
		 * ITC
		 */

		internal class ITM : ITCMessage {

		}

	}

}