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

			public readonly string problem, parameters, solution;
			
			public readonly string hash;

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

		internal readonly static log4net.ILog LOG = log4net.LogManager.GetLogger("Epicoin", "Epicore-Validator");

		protected EFOBE efobe;

		public Validator(Epicore core) : base(core) {}

		protected ImmutableDictionary<string, NPcProblemWrapper> problemsRegistry;

		internal override void InitAndRun(){
			var cachedE = new FileInfo(EFOBEfile);
			if (cachedE.Exists) efobe = loadEFOBE(cachedE);
			else core.sendITM2Net(new Epinet.ITM.IWantAFullEFOBE());
			problemsRegistry = waitForITMessageOfType<ITM.GetProblemsRegistry>().problemsRegistry;
			LOG.Info("Received problems registry.");

			keepChecking();
		}

		internal void keepChecking(){
			while(!core.stop){
				var m = itc.readMessageOrDefault();
				if(m is ITM.HeresYourEFOBE){
					var receivedEFOBELoc = (m as ITM.HeresYourEFOBE).tmpCacheLoc;
					var recEFOBE = loadEFOBE(receivedEFOBELoc);
					//TODO Validate EFOBE
					this.efobe = recEFOBE;
					receivedEFOBELoc.Delete();
				}
				if(m is ITM.ISolvedAProblem){
					var sol = m as ITM.ISolvedAProblem;
					var blok = hashBlock(sol.problem, sol.parms, sol.solution);
					//TODO validate
					efobe.addBlock(blok);
					core.sendITM2Net(new Epinet.ITM.TellEveryoneIKnowHowToMeth(sol.problem, sol.parms, sol.solution, blok.hash));
				}
				if(m is ITM.SomeoneSolvedAProblem){
					var ssa = m as ITM.SomeoneSolvedAProblem;
					var blok = new EFOBE.Block(ssa.problem, ssa.parms, ssa.solution, ssa.hash);
					//TODO validate
					core.sendITM2Solver(new Solver.ITM.StahpSolvingUSlowpoke(ssa.problem, ssa.parms));
					efobe.addBlock(blok);
				}
			}
		}

		internal const string EFOBEfile = "EFOBE.json";

		internal EFOBE loadEFOBE(FileInfo file) => JsonConvert.DeserializeObject<EFOBE>(File.ReadAllText(file.FullName));

		internal void saveEFOBE(EFOBE efobe, FileInfo file) => File.WriteAllText(file.FullName, JsonConvert.SerializeObject(efobe));

		protected EFOBE.Block hashBlock(string problem, string parms, string sol){
			string hash = ""; //TODO Hash
			return new EFOBE.Block(problem, parms, sol, hash);
		}


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