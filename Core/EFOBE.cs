using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;

using Newtonsoft.Json;

namespace Epicoin {

	/// <summary>
	/// The Epic Free and Open Blockchain of Epicness itself, in all its' structural glory.
	/// </summary>
	public class EFOBE {

		private List<Block> blocks;

		public EFOBE(List<Block> blocks){
			this.blocks = new List<Block>(blocks);
		}

		public Block TopBlock() => blocks.Last();

		internal void addBlock(Block block){
			blocks.Add(block);
		}

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
	internal class Validator : MainComponent<Validator.ITM> {

		protected EFOBE efobe;

		public Validator(Epicore core) : base(core) {}

		protected ImmutableDictionary<string, NPcProblemWrapper> problemsRegistry;

		internal override void InitAndRun(){
			var cachedE = new FileInfo(EFOBEfile);
			if(cachedE.Exists) efobe = loadEFOBE(cachedE);
			//else TODO Request EFOBE from network
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

		}

	}

}