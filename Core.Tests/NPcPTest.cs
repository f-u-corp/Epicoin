using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Epicoin {

	[TestFixture]
	public class NPcPProblemWrapperTest {

		[Test]
		public void Instantiation(){
			NPcProblemWrapper wrapper = new NPcProblemWrapper(new InefficientIntFactProblem());
			Assert.IsNotNull(wrapper, "Instatiation failed");
		}

		public class InefficientIntFactProblem : INPcProblem<int, List<int>> {

			public List<int> solve(int i){
				int nextFact(int pf){
					for(int nf = pf; nf <= i; nf++) if(nf != 1 && i % nf == 0){ i /= nf; return nf; }
					return 1;
				}
				List<int> facts = new List<int>();
				while(i > 1) facts.Add(nextFact(facts.Count > 0 ? facts.Last() : 1));
				return facts;
			}

			public bool check(int i, List<int> facts) => facts.Aggregate(1, (p, f) => p*f) == i;

		}

	}

}