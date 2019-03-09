using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;

using Newtonsoft.Json;

[assembly: InternalsVisibleTo("Core.Tests")]
namespace Epicoin {

	/// <summary>
	/// The Epic Free and Open Blockchain of Epicness itself, in all its' structural glory.
	/// </summary>
	public class EFOBE {

		private readonly Dictionary<string, Block> blockTree = new Dictionary<string, Block>();

		private readonly List<Block> bedrocks;

		public EFOBE(List<Block> bedrocks){
			this.bedrocks = new List<Block>(bedrocks);
		}


		/// <summary>
		/// Appends the block to the end of the EFOBE.
		/// </summary>
		internal void addBlock(string problem, string pars, string sol, string hash, string precedingHash){
			if(blockTree.ContainsKey(precedingHash)){
				Block next = new Block(problem, pars, sol, hash, precedingHash);
				blockTree.Add(hash, next);
				blockTree[precedingHash].append(next);
			}
		}

		/// <summary>
		/// Creates a read-only <i>view</i> of the entire EFOBE.
		/// </summary>
		internal ReadOnlyCollection<Block> blocksV() => new ReadOnlyCollection<Block>(blocks);

		//public override string ToString() => "EFOBE{" + String.Join("=-", blocks) + "}";

		public class Block {

			public readonly string problem, parameters, solution;
			
			public readonly string hash;

			private readonly string precedingHash;
			private readonly List<string> next;

			internal Block(string problem, string pars, string sol, string hash, string precedingHash){
				this.problem = problem;
				this.parameters = pars;
				this.solution = sol;
				this.hash = hash;

				this.next = new List<string>(1); //Default assumption - stable linear network
			}

			internal void append(Block block){
				next.Add(block.hash);
			}

			//public override string ToString() => $"[{problem} @ {hash}]";

		}
	}

	/// <summary>
	/// Validator, main component, responsible for validating solved problems and modyfying local EFOBE and/or notyfying the network. (Also) Fully manages local EFOBE.
	/// </summary>
	internal class Validator : MainComponent<Validator.ITM> {

		internal readonly static log4net.ILog LOG = log4net.LogManager.GetLogger("Epicoin", "Epicore-Validator");

		protected EFOBE efobe;
		internal EFOBE eeffoobbee { get => efobe; }

		HashAlgorithm hasher = SHA256.Create();

		public Validator(Epicore core) : base(core) {}

		protected ImmutableDictionary<string, NPcProblemWrapper> problemsRegistry;

		internal override void InitAndRun(){
			init();
			while(!core.stop) keepChecking();
			cleanup();
		}

		internal void init(){
			var cachedE = new FileInfo(EFOBEfile);
			if (cachedE.Exists) efobe = loadEFOBE(cachedE);
			else core.sendITM2Net(new Epinet.ITM.IWantAFullEFOBE());
			problemsRegistry = waitForITMessageOfType<ITM.GetProblemsRegistry>().problemsRegistry;
			LOG.Info("Received problems registry.");
		}

		internal void keepChecking(){
			{
				var m = itc.readMessageOrDefault();
				if(m is ITM.HeresYourEFOBE){
					var receivedEFOBELoc = (m as ITM.HeresYourEFOBE).tmpCacheLoc;
					var recEFOBE = loadEFOBE(receivedEFOBELoc);
					EFOBE.Block prev = default(EFOBE.Block);
					foreach(var nb in recEFOBE.blocksV()) if(!validateBlock(prev, prev = nb)) goto finaly;
					this.efobe = recEFOBE;
					finaly: receivedEFOBELoc.Delete();
				} else
				if(m is ITM.ISolvedAProblem){
					var sol = m as ITM.ISolvedAProblem;
					if(validateSolution(sol.problem, sol.parms, sol.solution)){
						var blok = hashBlock(efobe.TopBlock(), sol.problem, sol.parms, sol.solution);
						efobe.addBlock(blok);
						core.sendITM2Net(new Epinet.ITM.TellEveryoneIKnowHowToMeth(sol.problem, sol.parms, sol.solution, blok.hash));
					}
				} else
				if(m is ITM.SomeoneSolvedAProblem){
					var ssa = m as ITM.SomeoneSolvedAProblem;
					var blok = new EFOBE.Block(ssa.problem, ssa.parms, ssa.solution, ssa.hash);
					if(validateBlock(efobe.TopBlock(), blok)){
						core.sendITM2Solver(new Solver.ITM.StahpSolvingUSlowpoke(ssa.problem, ssa.parms));
						efobe.addBlock(blok);
					}
				} else {
					Thread.Yield();
					Thread.Sleep(10); //Nuffin to do
				}
			}
		}

		internal void cleanup(){
			saveEFOBE(efobe, new FileInfo(EFOBEfile));
			hasher.Dispose();
		}

		internal const string EFOBEfile = "EFOBE.json";

		internal EFOBE loadEFOBE(FileInfo file) => JsonConvert.DeserializeObject<EFOBE>(File.ReadAllText(file.FullName));

		internal void saveEFOBE(EFOBE efobe, FileInfo file) => File.WriteAllText(file.FullName, JsonConvert.SerializeObject(efobe));

		internal string computeHash(EFOBE.Block preceding, string problem, string parms, string sol){
			Encoding enc = Encoding.ASCII;
			byte[] prevHash = Convert.FromBase64String(preceding.hash == null ? "dGltZSB0aGVyZSBpcyBubw==" : preceding.hash);
			int p, r, s;
			byte[] preHash = new byte[(s = (r = (p = prevHash.Length) + enc.GetByteCount(problem)) + enc.GetByteCount(parms)) + enc.GetByteCount(sol)];
			Array.Copy(prevHash, preHash, prevHash.Length);
			enc.GetBytes(problem, 0, problem.Length, preHash, p);
			enc.GetBytes(parms, 0, parms.Length, preHash, r);
			enc.GetBytes(sol, 0, sol.Length, preHash, s);
			return Convert.ToBase64String(hasher.ComputeHash(preHash));
		}
		protected EFOBE.Block hashBlock(EFOBE.Block preceding, string problem, string parms, string sol) => new EFOBE.Block(problem, parms, sol, computeHash(preceding, problem, parms, sol));

		protected bool validateSolution(string problem, string parms, string solution) => problemsRegistry[problem].check(parms, solution);
		protected bool validateBlock(EFOBE.Block preceding, EFOBE.Block v) => validateSolution(v.problem, v.parameters, v.solution) && computeHash(preceding, v.problem, v.parameters, v.solution) == v.hash;


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