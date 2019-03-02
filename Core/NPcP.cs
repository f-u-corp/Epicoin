using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;
using System.Reflection;

namespace Epicoin {

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
	public interface INPcProblem {}

	/// <summary>
	/// Internal future-fixed wrapper class for in-dev-mutable problems and solutions represented publically.
	/// </summary>
	internal struct NPcProblemWrapper {

		private static P decodeParams<P>(string s){
			return default(P); //TODO - string -> P
		}

		private static string encodeSolution<S>(S sol){
			return ""; //TODO - S -> string
		}
		private static S decodeSolution<S>(string s){
			return default(S); //TODO - string -> S
		}



		readonly INPcProblem deleg;
		readonly Type parmsT, solT;

		public NPcProblemWrapper(INPcProblem problem){
			this.deleg = problem;
			var gir = problem.GetType().GetInterfaces().First(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(INPcProblem<,>)).GetGenericArguments();
			this.parmsT = gir[0];
			this.solT = gir[1];

			this.solvePi = typeof(NPcProblemWrapper).GetMethod("solveP").MakeGenericMethod(parmsT, solT);
			this.checkPi = typeof(NPcProblemWrapper).GetMethod("checkP").MakeGenericMethod(parmsT, solT);
		}

		/// <summary>
		/// Solves the problem given the parameters (with string representations - in any consistent way the problem may like).
		/// </summary>
		/// <param name="parms">Parameters to find the solution for.</param>
		/// <returns>The solution to the problem, represented as string (in any consitent way the problem may like).</returns>
		string solve(string parms) => (string) solvePi.Invoke(this, new object[]{parms});

		private readonly MethodInfo solvePi;
		private string solveP<P, S>(string p) => encodeSolution<S>((deleg as INPcProblem<P, S>).solve(decodeParams<P>(p)));

		/// <summary>
		/// Checks the solution to the problem for given parameters (with string representations - in any consistent way the problem may like).
		/// </summary>
		/// <param name="parms">Parameters to check with.</param>
		/// <param name="solution">Solution to check. </param>
		/// <returns>Whether the solution is correct.</returns>
		bool check(string parms, string solution) => (bool) checkPi.Invoke(this, new object[]{parms, solution});

		private readonly MethodInfo checkPi;
		private bool checkP<P, S>(string p, string s) => (deleg as INPcProblem<P, S>).check(decodeParams<P>(p), decodeSolution<S>(s));

	}

	/// <summary>
	/// Solver, main component, responsible for loading and solving problems.
	/// </summary>
	internal class Solver : MainComponent<Solver.ITM> {

		public Solver(Epicore core) : base(core){}

		protected ImmutableDictionary<string, NPcProblemWrapper> problemsRegistry;

		internal override void InitAndRun(){
			LoadProblems();
			core.sendITM2Validator(new Validator.ITM.GetProblemsRegistry(problemsRegistry));
		}

		protected void LoadProblems(){
			var reg = new Dictionary<string, NPcProblemWrapper>();
			//TODO load dlls
			this.problemsRegistry = ImmutableDictionary.ToImmutableDictionary(reg);
		}

		
		/*
		 * ITC
		 */

		internal class ITM : ITCMessage {

		}

	}

}