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
	internal struct NPcProblemWrapper {

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




		public NPcProblemWrapper(ComputeProgram prog){
			this.prog = prog;

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
			LoadProblems();
			core.sendITM2Validator(new Validator.ITM.GetProblemsRegistry(problemsRegistry));
		}

		protected void LoadProblems(){
			LOG.Info("Loading problems...");
			var reg = new Dictionary<string, NPcProblemWrapper>();
			new DirectoryInfo("npdlls").Create();
			new DirectoryInfo("npdlls").GetFiles("*.dll").ToList().ForEach(f => Assembly.LoadFile(f.FullName).GetExportedTypes().Where(typeof(INPcProblem).IsAssignableFrom).Select(Activator.CreateInstance).Cast<INPcProblem>().ToList().ForEach(p => reg.Add(p.getName(), new NPcProblemWrapper(p))));
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