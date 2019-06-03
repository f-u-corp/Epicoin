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
	public class EFOBE : EFOBEEvents {

		/// <summary>
		/// Serializes EFOBE into JSON string.
		/// </summary>
		/// <param name="efobe">EFOBE for serialization.</param>
		/// <returns>JSON string serialization of EFOBE.</returns>
		public static string Serialize(EFOBE efobe) => JsonConvert.SerializeObject(Decompile(efobe));

		/// <summary>
		/// Deserializes EFOBE from JSON string.
		/// </summary>
		/// <param name="efobe">JSON string for EFOBE deserialization.</param>
		/// <returns>Deserialized EFOBE from JSON string.</returns>
		public static EFOBE Deserialize(string efobe) => Compile(JsonConvert.DeserializeObject<List<(string problem, string parameters, string solution, string hash, string prevHash)>>(efobe));

		/// <summary>
		/// Decompiles EFOBE into more basic data structure.
		/// </summary>
		/// <param name="efobe">EFOBE for decompilation.</param>
		/// <returns>Decompiled basic efobe representation.</returns>
		public static List<(string problem, string parameters, string solution, string hash, string prevHash)> Decompile(EFOBE efobe){
			var bcol = new List<(string problem, string parameters, string solution, string hash, string prevHash)>();
			var prevHash = NullHash;
			{
				foreach(var b in efobe.bedrocks) if(b.hash != prevHash){
					bcol.Add((b.problem, b.parameters, b.solution, b.hash, prevHash));
					prevHash = b.hash;
				}
			}
			{
				var LCA = efobe.LCA;
				bcol.Add((LCA.problem, LCA.parameters, LCA.solution, LCA.hash, prevHash));
				prevHash = LCA.hash;
			}
			foreach(var b in efobe.blockTree.Values) if(b.hash != efobe.LCA.hash) bcol.Add((b.problem, b.parameters, b.solution, b.hash, b.precedingHash));
			return bcol;
		}

		/// <summary>
		/// Compiles EFOBE from basic data structure.
		/// </summary>
		/// <param name="raw">Decompiled EFOBE representation</param>
		/// <returns>Compiled EFOBE</returns>
		public static EFOBE Compile(List<(string problem, string parameters, string solution, string hash, string prevHash)> raw, Func<string, string, string, string, string> hasher){
			EFOBE efobe = new EFOBE(hasher);
			efobe.skipUpdateCheck = true;
			raw.ForEach(b => efobe.addBlock(b.problem, b.parameters, b.solution, b.hash, b.prevHash));
			efobe.skipUpdateCheck = false;
			efobe.updateCheckBranches(true);
			return efobe;
		}

		private const int BedrockDelta = 1024, BranchLengthDelta = 1024;
		private const string NullHash = "dGltZSB0aGVyZSBpcyBubw==";

		///<summary>Hashing function: (previousHash, problem, parameters, solution) -> hash</summary>
		private readonly Func<string, string, string, string, string> hasher;

		private readonly Dictionary<string, Block.UncertainBlock> blockTree = new Dictionary<string, Block.UncertainBlock>();
		///<summary>Locked Common Ancestor - transcending block, block whose certainty has been confirmed, yet he must exist in the tree awaiting arrival of the next chosen one.</summary>
		private Block.UncertainBlock LCA;

		private int longestBranch = 0;
		private readonly List<List<string>> branches;

		///<summary>Blocks whose existance is undeniable.</summary>
		private readonly List<Block> bedrocks;

		///<summary>Blocks whose existance is undeniable. [read-only view]</summary>
		public ReadOnlyCollection<Block> Bedrocks => bedrocks.AsReadOnly();

		/// <summary>
		/// Creates new empty EFOBE.
		/// </summary>
		public EFOBE(Func<string, string, string, string, string> hasher){
			this.hasher = hasher;
			branches = new List<List<string>>();
			LCA = new Block.UncertainBlock(null, null, null, NullHash, null);
			blockTree[LCA.hash] = LCA;
			bedrocks = new List<Block>{};
		}

		protected List<string> LongestBranch() => branches.OrderByDescending(br => br.Count).First();

		/// <summary>
		/// Returns the hash of the top-most block (last block on the longest branch).
		/// </summary>
		/// <returns>Hash of last block on the longest branch.</returns>
		public string TopBlock() => branches.Count == 0 ? LCA.hash : LongestBranch().Last();

		/// <summary>
		/// Checks whether a branch can grow or derive from block with given hash.
		/// </summary>
		/// <returns>Whether branching from given block can occur.</returns>
		public bool CanBranch(string hash) => hash == LCA.hash || blockTree.ContainsKey(hash);

		/// <summary>
		/// Adds block to the tree.
		/// </summary>
		internal void addBlock(string problem, string pars, string sol, string hash, string precedingHash){
			if(blockTree.ContainsKey(precedingHash)){
				Block.UncertainBlock next = new Block.UncertainBlock(problem, pars, sol, hash, precedingHash);
				blockTree.Add(hash, next);
				if(skipUpdateCheck) return;

				Block.UncertainBlock prev = blockTree[precedingHash];
				bool rb;
				if(prev == LCA){
					List<string> newBranch = new List<string>{hash};
					branches.Add(newBranch);
					next.branches.Add(newBranch);
					longestBranch = Math.Max(longestBranch, 1);
					rb = false;
				} else {
					rb = prev.append(next);
					if(!rb){
						var branch = prev.branches[0];
						branch.Add(next.hash);
						next.branches.Add(branch);
						longestBranch = Math.Max(longestBranch, branch.Count);
					} else {
						var branch = prev.branches[0].TakeWhile(h => h != prev.hash).ToList();
						branch.Add(prev.hash);
						branch.Add(next.hash);
						branches.Add(branch);
						foreach(var b in branch.Select(h => blockTree[h])) b.branches.Add(branch);
						rb = false;
					}
				}
				updateCheckBranches(rb);
			}
		}

		private bool skipUpdateCheck = false; //WARNING: After skipping update checks, and setting this back to false, updateCheckBranches(true) must be called!!!
		internal void updateCheckBranches(bool refresh = false){
			if(skipUpdateCheck) return;
			brecalc: if(refresh){
				//Full branch cash reconstruction requested
				branches.Clear();
				List<string> derive(List<string> branch){
					List<string> copy = new List<string>(branch);
					branches.Add(copy);
					return copy;
				}
				void followBranch(List<string> branch, Block.UncertainBlock next){
					next.branches.Clear();
					var brs = new List<List<string>>{branch};
					for(int i = 1; i < next.next.Count; i++) brs.Add(derive(branch));
					for(int i = 0; i < next.next.Count; i++){
						var br = brs[i];
						var nn = blockTree[next.next[i]];
						br.Add(nn.hash);
						followBranch(br, nn);
					}
				}
				foreach(var bs in blockTree.Values.Where(b => b.precedingHash == LCA.hash)) followBranch(derive(new List<string>()), bs);
				foreach(var br in branches) foreach(var b in br.Select(id => blockTree[id])) b.branches.Add(br);
			}
			//Remove short outdated branches
			List<List<string>> forRebase = branches.Where(br => longestBranch - br.Count >= BranchLengthDelta).ToList();
			foreach(var br in forRebase){
				var lb = LongestBranch();
				var firstDerivedFromLongest = br[0];
				for(int i = br.Count-1; i > 0; i--) if(lb.Contains(br[i-1])) firstDerivedFromLongest = br[i];
				if(rebase(firstDerivedFromLongest, lb.Last(), false)){
					refresh = true;
					goto brecalc;
				}
			}
			//Immortalize common ancestors until next derivation, or we reach outdate threshold
			while(longestBranch > BedrockDelta){
				var nca = branches[0][0];
				if(branches.All(br => br[0] == nca)){
					bedrocks.Add(new Block(LCA));
					blockTree.Remove(LCA.hash);
					var nLCA = blockTree[nca];
					LCA = new Block.UncertainBlock(nLCA.problem, nLCA.parameters, nLCA.solution, nLCA.hash, null);
					blockTree[LCA.hash] = LCA;
					branches.ForEach(b => b.RemoveAt(0));
					longestBranch--;
				} else break;
			}
		}

		/// <summary>
		/// Deprecated, use rebase instead
		/// Terminates existence of a branch, and all blocks existing [exclusively] on it.
		/// </summary>
		internal void destroyBranch(List<string> branch){
			branches.Remove(branch);
			foreach(var b in branch.Select(b => blockTree[b])){
				b.branches.Remove(branch);
				if(b.branches.Count == 0){
					blockTree.Remove(b.hash);
					if(blockTree.ContainsKey(b.precedingHash)) blockTree[b.precedingHash].next.Remove(b.hash);
				}
			}
		}

		/// <summary>
		/// Terminates existence of a branch, and all blocks existing [exclusively] on it.
		/// </summary>
		internal bool rebase(string hash, string newPrecedingHash, bool ucb = true){
			if(blockTree.ContainsKey(hash) && blockTree.ContainsKey(newPrecedingHash) && hash != LCA.hash){
				var newParent = blockTree[newPrecedingHash];
				var oldBlock = blockTree[hash];
				var oldParent = blockTree[oldBlock.precedingHash];
				oldParent?.next?.Remove(oldBlock.hash);
				var newBlock = new Block.UncertainBlock(oldBlock.problem, oldBlock.parameters, oldBlock.solution, hasher(newPrecedingHash, oldBlock.problem, oldBlock.parameters, oldBlock.solution), newPrecedingHash);
				newParent.append(newBlock);
				blockTree.Remove(hash);
				blockTree[hash] = newBlock;
				foreach(var childRebase in oldBlock.next) rebase(childRebase, newBlock.hash);
				if(ucb) updateCheckBranches(true);
				return true;
			}
			return false;
		}

		/*
		 * Testing
		 */
		
		internal int TotalBlockCount => bedrocks.Count + blockTree.Count;

		//public override string ToString() => "EFOBE{" + String.Join("=-", blocks) + "}";

		/// <summary>
		/// Everything a self-respecting block requires.
		/// </summary>
		public class Block {

			public readonly string problem, parameters, solution;
			
			public readonly string hash;

			internal Block(string problem, string pars, string sol, string hash){
				this.problem = problem;
				this.parameters = pars;
				this.solution = sol;
				this.hash = hash;
			}

			internal Block(Block b) : this(b.problem, b.parameters, b.solution, b.hash){}

			/// <summary>
			/// An uncertain block, whose existance may end any moment, requires additional information for survival.
			/// </summary>
			internal class UncertainBlock : Block {

				internal List<List<string>> branches = new List<List<string>>();

				internal readonly string precedingHash;
				internal readonly List<string> next;

				internal UncertainBlock(string problem, string pars, string sol, string hash, string precedingHash) : base(problem, pars, sol, hash){
					this.precedingHash = precedingHash;
					this.next = new List<string>(1); //Default assumption - stable linear network
				}

				internal bool append(Block block){
					next.Add(block.hash);
					return next.Count > 1;
				}

				//public override string ToString() => $"[{problem} @ {hash}]";

			}

		}

		//Events
		public event Action<(string Problem, string Parameters, string Solution, string Parent, string Hash)> OnBlockAdded;
		private void FireOnBlockAdded(string Problem, string Parameters, string Solution, string Parent, string Hash) => AsyncEventsManager.FireAsync(OnBlockAdded, (Problem, Parameters, Solution, Parent, Hash));
		public event Action<(string Problem, string Parameters, string Solution, string Hash)> OnBlockImmortalized;
		private void FireOnBlockImmortalized(string Problem, string Parameters, string Solution, string Hash) => AsyncEventsManager.FireAsync(OnBlockImmortalized, (Problem, Parameters, Solution, Hash));
		public event Action<(string Problem, string Parameters, string Solution, string Hash)> OnLCAChanged;
		private void FireOnLCAChanged(string Problem, string Parameters, string Solution, string Hash) => AsyncEventsManager.FireAsync(OnLCAChanged, (Problem, Parameters, Solution, Hash));
		public event Action<(string Problem, string Parameters, string Solution, string OldParent, string OldHash, string NewParent, string NewHash)> OnBranchRebased;
		private void FireOnBranchRebased(string Problem, string Parameters, string Solution, string OldParent, string OldHash, string NewParent, string NewHash) => AsyncEventsManager.FireAsync(OnBranchRebased, (Problem, Parameters, Solution, OldParent, OldHash, NewParent, NewHash));

	}

	/// <summary>
	/// Validator, main component, responsible for validating solved problems and modyfying local EFOBE and/or notyfying the network. (Also) Fully manages local EFOBE.
	/// </summary>
	internal class Validator : MainComponent<Validator.ITM>, IValidator {

		internal readonly static log4net.ILog LOG = log4net.LogManager.GetLogger("Epicoin", "Epicore-Validator");

		protected EFOBE efobe;

		HashAlgorithm hasher = SHA256.Create();

		public Validator(Epicore core) : base(core){
			efobe = new EFOBE(computeHash);
		}

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
			else core.sendITM2Net(new Epicoin.Core.Net.ITM.EFOBERequest());
			problemsRegistry = waitForITMessageOfType<ITM.GetProblemsRegistry>().problemsRegistry;
			LOG.Info("Received problems registry.");
		}

		internal void keepChecking(){
			{
				var m = itc.readMessageOrDefault();
				if(m is ITM.EFOBEReqReply){
					var receivedEFOBELoc = (m as ITM.EFOBEReqReply).cachedEFOBE;
					var decomp = JsonConvert.DeserializeObject<List<(string problem, string parameters, string solution, string hash, string prevHash)>>(File.ReadAllText(receivedEFOBELoc.FullName));
					if(!decomp.All(validateBlock)) goto finaly;
					this.efobe = EFOBE.Compile(decomp, computeHash);
					finaly: receivedEFOBELoc.Delete();
				} else
				if(m is ITM.ProblemSolved){
					var sol = m as ITM.ProblemSolved;
					if(validateSolution(sol.Problem, sol.Parameters, sol.Solution)){
						var prevHash = efobe.TopBlock();
						var hash = computeHash(prevHash, sol.Problem, sol.Parameters, sol.Solution);
						efobe.addBlock(sol.Problem, sol.Parameters, sol.Solution, hash, prevHash);
						core.sendITM2Net(new Epicoin.Core.Net.ITM.EFOBELocalBlockAdded(sol.Problem, sol.Parameters, sol.Solution, prevHash, hash));
					}
				} else
				if(m is ITM.EFOBERemoteBlockAdded){
					var ssa = m as ITM.EFOBERemoteBlockAdded;
					if(efobe.CanBranch(ssa.Parent) && validateBlock((ssa.Problem, ssa.Parameters, ssa.Solution, ssa.Hash, ssa.Parent))){
						core.sendITM2Solver(new Solver.ITM.CancelPendingProblem(ssa.Problem, ssa.Parameters));
						efobe.addBlock(ssa.Problem, ssa.Parameters, ssa.Solution, ssa.Hash, ssa.Parent);
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

		internal EFOBE loadEFOBE(FileInfo file) => EFOBE.Deserialize(File.ReadAllText(file.FullName));

		internal void saveEFOBE(EFOBE efobe, FileInfo file) => File.WriteAllText(file.FullName, EFOBE.Serialize(efobe));

		internal string computeHash(string prHash, string problem, string parms, string sol){
			Encoding enc = Encoding.ASCII;
			byte[] prevHash = Convert.FromBase64String(prHash);
			int p, r, s;
			byte[] preHash = new byte[(s = (r = (p = prevHash.Length) + enc.GetByteCount(problem)) + enc.GetByteCount(parms)) + enc.GetByteCount(sol)];
			Array.Copy(prevHash, preHash, prevHash.Length);
			enc.GetBytes(problem, 0, problem.Length, preHash, p);
			enc.GetBytes(parms, 0, parms.Length, preHash, r);
			enc.GetBytes(sol, 0, sol.Length, preHash, s);
			return Convert.ToBase64String(hasher.ComputeHash(preHash));
		}
		protected bool validateSolution(string problem, string parms, string solution){
			var t = problemsRegistry[problem].check(parms, solution, CancellationToken.None);
			t.Wait();//Checking is fast, by definition
			return t.IsCompletedSuccessfully && t.Result;
		}
		protected bool validateBlock((string problem, string parameters, string solution, string hash, string prevHash) b) => validateSolution(b.problem, b.parameters, b.solution) && computeHash(b.prevHash, b.problem, b.parameters, b.solution) == b.hash;


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