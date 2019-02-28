using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

namespace Epicoin {

	public interface INPcProblem<P> {
		//Temporary public interface for DLLs
	}

	internal struct NPcProblemWrapper {
		//TODO wrap
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