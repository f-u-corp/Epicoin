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

	/// <summary>
	/// Internal future-fixed wrapper class for in-dev-mutable problems and solutions represented publically.
	/// </summary>
	internal struct NPcProblemWrapper : IDisposable {

		private static P decodeParams<P>(string s){
			return JsonConvert.DeserializeObject<OFW<P>>(s).o;
		}

		private static string encodeSolution<S>(S sol){
			return JsonConvert.SerializeObject(new OFW<S>(sol));
		}
		private static S decodeSolution<S>(string s){
			return JsonConvert.DeserializeObject<OFW<S>>(s).o;
		}

		private class OFW<T> {
			public T o;
			public OFW(T t) => o = t;
		}



		readonly ComputeProgram prog;
		readonly ComputeKernel slv, chck;
		readonly Type parameterT, solutionT;

		public NPcProblemWrapper(ComputeProgram prog, Type parameterT, Type solutionT){
			this.prog = prog;
			slv = this.prog.CreateKernel("solve");
			chck = this.prog.CreateKernel("check");
			this.parameterT = parameterT;
			this.solutionT = solutionT;
		}

		public void Dispose(){
			slv.Dispose();
			chck.Dispose();
			prog.Dispose();
		}

		/// <summary>
		/// Solves the problem given the parameters (with string representations - in any consistent way the problem may like).
		/// </summary>
		/// <param name="parms">Parameters to find the solution for.</param>
		/// <returns>The solution to the problem, represented as string (in any consitent way the problem may like).</returns>
		public string solve(string parms) => null;

		/// <summary>
		/// Checks the solution to the problem for given parameters (with string representations - in any consistent way the problem may like).
		/// </summary>
		/// <param name="parms">Parameters to check with.</param>
		/// <param name="solution">Solution to check. </param>
		/// <returns>Whether the solution is correct.</returns>
		public bool check(string parms, string solution) => false;

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