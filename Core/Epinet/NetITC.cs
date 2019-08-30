using System;
using System.IO;

using Epicoin.Core;

namespace Epicoin.Core.Net {
	internal class ITM : ITCMessage {
		internal class ProblemToSolve : ITM {
			public readonly string Problem, Parameters;

			public ProblemToSolve(string problem, string parms){
				this.Problem = problem;
				this.Parameters = parms;
			}
		}
		internal class EFOBELocalBlockAdded : ITM { //From EFOBE/Validator
			public readonly string Problem, Parameters, Solution;
			public readonly string Parent, Hash;

			public EFOBELocalBlockAdded(string problem, string parms, string sol, string parent, string hash){
				this.Problem = problem;
				this.Parameters = parms;
				this.Solution = sol;
				this.Parent = parent;
				this.Hash = hash;
			}
		}
		internal class EFOBELocalBlockRebase : ITM { //From EFOBE/Validator
			public readonly string Hash;
			public readonly string NewParent, NewHash;

			public EFOBELocalBlockRebase(string hash, string newParent, string newHash){
				this.Hash = hash;
				this.NewParent = newParent;
				this.NewHash = newHash;
			}
		}
		internal class EFOBERequest : ITM {

		}
		internal class EFOBESendRequestReply : ITM {
			public readonly FileInfo cacheEFOBEHere;

			public EFOBESendRequestReply(FileInfo cache){
				this.cacheEFOBEHere = cache;
			}
		}
	}
}