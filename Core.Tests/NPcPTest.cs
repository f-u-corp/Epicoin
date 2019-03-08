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

		[Test]
		public void Delegation(){
			NPcProblemWrapper wrapper = new NPcProblemWrapper(new InefficientIntFactProblem());
			const string p1 = "{\"o\":242}";

			string s1 = wrapper.solve(p1);
			Assert.AreEqual("{\"o\":[2,11,11]}", s1, "Solving failed - unexpected result");
			Assert.True(wrapper.check(p1, s1), "Solution check [242] failed - false negative");

			Random random = new Random();
			for(int i = 0; i < 100; i++){
				int n = random.Next(25, 12500);
				if(n == 242) continue;
				string p2 = "{ o: " + n + " }";
				string s2 = wrapper.solve(p2);
				Assert.True(wrapper.check(p2, s2), "Solution [rand] solve/check failed");
				Assert.False(wrapper.check(p1, s2), "Solution [rand] check failed - false positive (against 242)");
			}
		}

		public class InefficientIntFactProblem : INPcProblem<int, List<int>> {

			public string getName() => "inef-int-fact";

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