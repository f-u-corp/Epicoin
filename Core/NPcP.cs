using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

namespace Epicoin {

	public interface INPcProblem<P, S> {
		//Temporary public interface for DLLs

		S solve(P parms);

		bool check(P parms, S solution);

	}

	///<summary>
	///Internal future-fixed wrapper class for in-dev-mutable problems and solutions represented publically.
	///</summary>
	internal struct NPcProblemWrapper {
		//TODO wrap

		///<summary>
		///Solves the problem given the parameters (with string representations - in any consistent way the problem may like).
		///</summary>
		///<param name="parms">Parameters to find the solution for.</param>
		///<returns>The solution to the problem, represented as string (in any consitent way the problem may like).</returns>
		string solve(string parms){
			return "";
		}

		///<summary>
		///Checks the solution to the problem for given parameters (with string representations - in any consistent way the problem may like).
		///</summary>
		///<param name="parms">Parameters to check with.</param>
		///<param name="solution">Solution to check. </param>
		///<returns>Whether the solution is correct.</returns>
		bool check(string parms, string solution){
			return false;
		}

	}

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