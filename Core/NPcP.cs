using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;
using System.Reflection;

using Newtonsoft.Json;

[assembly: InternalsVisibleTo("Core.Tests")]
namespace Epicoin.Core {

	/// <summary>
	/// Temporary interface for dll-based problems externalization approach
	/// </summary>
	public interface INPcProblem<P, S> : INPcProblem {

		S solve(P parms);

		bool check(P parms, S solution);

	}

	/// <summary>
	/// INTERNAL: DO NOT USE!!!
	/// </summary>
	public interface INPcProblem {

		string getName();

	}

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



		readonly INPcProblem deleg;
		readonly Type parmsT, solT;

		public NPcProblemWrapper(INPcProblem problem){
			this.deleg = problem;
			var gir = problem.GetType().GetInterfaces().First(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(INPcProblem<,>)).GetGenericArguments();
			this.parmsT = gir[0];
			this.solT = gir[1];

			this.solvePi = typeof(NPcProblemWrapper).GetMethod("solveP", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(parmsT, solT);
			this.checkPi = typeof(NPcProblemWrapper).GetMethod("checkP", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(parmsT, solT);
		}

		/// <summary>
		/// Solves the problem given the parameters (with string representations - in any consistent way the problem may like).
		/// </summary>
		/// <param name="parms">Parameters to find the solution for.</param>
		/// <returns>The solution to the problem, represented as string (in any consitent way the problem may like).</returns>
		public string solve(string parms) => (string) solvePi.Invoke(this, new object[]{parms});

		private readonly MethodInfo solvePi;
		private string solveP<P, S>(string p) => encodeSolution<S>((deleg as INPcProblem<P, S>).solve(decodeParams<P>(p)));

		/// <summary>
		/// Checks the solution to the problem for given parameters (with string representations - in any consistent way the problem may like).
		/// </summary>
		/// <param name="parms">Parameters to check with.</param>
		/// <param name="solution">Solution to check. </param>
		/// <returns>Whether the solution is correct.</returns>
		public bool check(string parms, string solution) => (bool) checkPi.Invoke(this, new object[]{parms, solution});

		private readonly MethodInfo checkPi;
		private bool checkP<P, S>(string p, string s) => (deleg as INPcProblem<P, S>).check(decodeParams<P>(p), decodeSolution<S>(s));

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

		protected ImmutableDictionary<string, NPcProblemWrapper> problemsRegistry;

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
			LOG.Info($"Successfuly loaded problems - {problemsRegistry.Count} ({String.Join(", ", problemsRegistry.Keys)})");
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