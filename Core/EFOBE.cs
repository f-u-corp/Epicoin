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
namespace Epicoin.Core {

	/// <summary>
	/// The Epic Free and Open Blockchain of Epicness itself, in all its' structural glory.
	/// </summary>
	public class EFOBE {

		private readonly List<Block> blocks;

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

			public readonly string problem, parameters, solution;
			
			public readonly string hash;

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

		HashAlgorithm hasher = SHA256.Create();

		public Validator(Epicore core) : base(core) {}

		public EFOBE GetLocalEFOBE() => efobe;

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

			internal class EFOBEReqReply : ITM {
				public readonly FileInfo cachedEFOBE;

				public EFOBEReqReply(FileInfo cache){
					this.cachedEFOBE = cache;
				}
			}

		}

	}

}