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

		protected Dictionary<string, NPcProblemWrapper> problemsRegistry;

		internal override void InitAndRun(){
			LoadProblems();
		}

		protected void LoadProblems(){
			problemsRegistry = new Dictionary<string, NPcProblemWrapper>();
			//TODO load dlls
		}

		
		/*
		 * ITC
		 */

		internal class ITM : ITCMessage {

		}

	}

}