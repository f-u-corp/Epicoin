using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;

using Newtonsoft.Json;

namespace Epicoin.Core {

	/// <summary>
	/// The Epic Free and Open Blockchain of Epicness itself, in all its' structural glory.
	/// </summary>
	public class EFOBE {

		private List<Block> blocks;

		public EFOBE(List<Block> blocks){
			this.blocks = new List<Block>(blocks);
		}

		/// <summary>
		/// Retrieves the latest block of the EFOBE.
		/// </summary>
		/// <returns>The latest block, or <c>default(Block)</c> if the EFOBE is empty</returns>
		public Block TopBlock() => blocks.Count > 0 ? blocks.Last() : default(Block);

		/// <summary>
		/// Appends the block to the end of the EFOBE.
		/// </summary>
		internal void addBlock(Block block){
			blocks.Add(block);
		}

		/// <summary>
		/// Creates a read-only <i>view</i> of the entire EFOBE.
		/// </summary>
		internal ReadOnlyCollection<Block> blocksV() => new ReadOnlyCollection<Block>(blocks);

		public override string ToString() => "EFOBE{" + String.Join("=-", blocks) + "}";

		public struct Block {

			string problem, parameters, solution;
			
			string hash;

			public Block(string problem, string pars, string sol, string hash){
				this.problem = problem;
				this.parameters = pars;
				this.solution = sol;
				this.hash = hash;
			}

			public override string ToString() => $"[{problem} @ {hash}]";

		}
	}

	/// <summary>
	/// Validator, main component, responsible for validating solved problems and modyfying local EFOBE and/or notyfying the network. (Also) Fully manages local EFOBE.
	/// </summary>
	internal class Validator : MainComponent<Validator.ITM>, IValidator {

		internal readonly static log4net.ILog LOG = log4net.LogManager.GetLogger("Epicoin", "Epicore-Validator");

		protected EFOBE efobe;

		public Validator(Epicore core) : base(core) {}

		protected ImmutableDictionary<string, NPcProblemWrapper> problemsRegistry;

		internal override void InitAndRun(){
			var cachedE = new FileInfo(EFOBEfile);
			if(cachedE.Exists) efobe = loadEFOBE(cachedE);
			//else TODO Request EFOBE from network
			problemsRegistry = waitForITMessageOfType<ITM.GetProblemsRegistry>().problemsRegistry;
			LOG.Info("Received problems registry.");
		}

		internal const string EFOBEfile = "EFOBE.json";

		internal EFOBE loadEFOBE(FileInfo file) => JsonConvert.DeserializeObject<EFOBE>(File.ReadAllText(file.FullName));

		internal void saveEFOBE(EFOBE efobe, FileInfo file) => File.WriteAllText(file.FullName, JsonConvert.SerializeObject(efobe));


		/*
		 * ITC
		 */

		internal class ITM : ITCMessage {

			internal class GetProblemsRegistry : ITM { //From Solver

				public readonly ImmutableDictionary<string, NPcProblemWrapper> problemsRegistry;

				public GetProblemsRegistry(IDictionary<string, NPcProblemWrapper> reg) => problemsRegistry = reg is ImmutableDictionary<string, NPcProblemWrapper> ? reg as ImmutableDictionary<string, NPcProblemWrapper> : ImmutableDictionary.ToImmutableDictionary(reg);

			}

			internal class ProblemSolved : ITM { //From Solver
				public readonly string Problem, Parameters, Solution;

				public ProblemSolved(string problem, string parms, string sol){
					this.Problem = problem;
					this.Parameters = parms;
					this.Solution = sol;
				}
			}

			internal class EFOBERemoteBlockAdded : ITM { //From Network
				public readonly string Problem, Parameters, Solution;
				public readonly string Parent, Hash;

				public EFOBERemoteBlockAdded(string problem, string parms, string sol, string parent, string hash){
					this.Problem = problem;
					this.Parameters = parms;
					this.Solution = sol;
					this.Parent = parent;
					this.Hash = hash;
				}
			}

			internal class EFOBERemoteBlockRebase : ITM { //From Network
				public readonly string Hash;
				public readonly string NewParent, NewHash;

				public EFOBERemoteBlockRebase(string hash, string newParent, string newHash){
					this.Hash = hash;
					this.NewParent = newParent;
					this.NewHash = newHash;
				}
			}

		}

	}

}