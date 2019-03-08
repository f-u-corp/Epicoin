using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Newtonsoft.Json;

namespace Epicoin {

	/// <summary>
	/// The Epic Free and Open Blockchain of Epicness itself, in all its' structural glory.
	/// </summary>
	public class EFOBE {

		private readonly List<Block> blocks;

		public EFOBE(List<Block> blocks){
			this.blocks = new List<Block>(blocks);
		}

		internal void addBlock(Block block){
			blocks.Add(block);
		}

		public struct Block {

			readonly string problem, parameters, solution;
			
			readonly string hash;

			public Block(string problem, string pars, string sol, string hash){
				this.problem = problem;
				this.parameters = pars;
				this.solution = sol;
				this.hash = hash;
			}

		}
	}

	/// <summary>
	/// Validator, main component, responsible for validating solved problems and modyfying local EFOBE and/or notyfying the network. (Also) Fully manages local EFOBE.
	/// </summary>
	internal class Validator : MainComponent<Validator.ITM> {

		protected EFOBE efobe;

		public Validator(Epicore core) : base(core) {}

		protected ImmutableDictionary<string, NPcProblemWrapper> problemsRegistry;

		internal override void InitAndRun(){
			var cachedE = new FileInfo(EFOBEfile);
			if(cachedE.Exists) efobe = loadEFOBE(cachedE);
			else {
				core.sendITM2Net(new Epinet.ITM.IWantAFullEFOBE());
				var receivedEFOBELoc = waitForITMessageOfType<ITM.HeresYourEFOBE>().tmpCacheLoc;
				var recEFOBE = loadEFOBE(receivedEFOBELoc);
				//Validate EFOBE
				this.efobe = recEFOBE;
				receivedEFOBELoc.Delete();
			}
			problemsRegistry = waitForITMessageOfType<ITM.GetProblemsRegistry>().problemsRegistry;
		}

		internal const string EFOBEfile = "EFOBE.json";

		internal EFOBE loadEFOBE(FileInfo file) => JsonConvert.DeserializeObject<EFOBE>(File.ReadAllText(file.FullName));

		internal void saveEFOBE(EFOBE efobe, FileInfo file) => File.WriteAllText(file.FullName, JsonConvert.SerializeObject(efobe));


		/*
		 * ITC
		 */

		internal class ITM : ITCMessage {

			internal class GetProblemsRegistry : ITM {

				public readonly ImmutableDictionary<string, NPcProblemWrapper> problemsRegistry;

				public GetProblemsRegistry(IDictionary<string, NPcProblemWrapper> reg) => problemsRegistry = reg is ImmutableDictionary<string, NPcProblemWrapper> ? reg as ImmutableDictionary<string, NPcProblemWrapper> : ImmutableDictionary.ToImmutableDictionary(reg);

			}

			internal class ISolvedAProblem : ITM {

				public readonly string problem, parms, solution;

				public ISolvedAProblem(string problem, string parms, string sol){
					this.problem = problem;
					this.parms = parms;
					this.solution = sol;
				}

			}

			internal class HeresYourEFOBE : ITM {

				public readonly FileInfo tmpCacheLoc;

				public HeresYourEFOBE(FileInfo tmpCacheLoc){
					this.tmpCacheLoc = tmpCacheLoc;
				}

			}

			internal class SomeoneSolvedAProblem : ITM {

				public readonly string problem, parms, solution, hash;

				public SomeoneSolvedAProblem(string problem, string parms, string sol, string hash){
					this.problem = problem;
					this.parms = parms;
					this.solution = sol;
					this.hash = hash;
				}

			}

		}

	}

}