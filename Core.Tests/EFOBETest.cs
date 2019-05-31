using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Epicoin.Core;

using NUnit.Framework;

namespace Epicoin.Test {

	internal class DummySuspendedEpicore : Epicore {

		public DummySuspendedEpicore(Action<Solver.ITM> sendITM2Solver, Action<Validator.ITM> sendITM2Validator, Action<Epicoin.Core.Net.ITM> sendITM2Net) : base(){
			this.sendITM2Solver = sendITM2Solver;
			this.sendITM2Validator = sendITM2Validator;
			this.sendITM2Net = sendITM2Net;
		}

		public DummySuspendedEpicore() : this(m => {}, m => {}, m => {}){}

		//public override void Start(){} TODO override Start with nothing

	}

	[TestFixture]
	public class ValidatorTest {

		private Validator dummyValidator() => new DummySuspendedEpicore().validator;

		[Test]
		public void TestEFOBEValidation(){
			var validator = dummyValidator();
			var ifp = new NPcPProblemWrapperTest.InefficientIntFactProblem();
			validator.sendITM(new Validator.ITM.GetProblemsRegistry(new Dictionary<string, NPcProblemWrapper>{ {ifp.getName(), new NPcProblemWrapper(ifp)} }));
			validator.init();

			var tmpE = new FileInfo("temp-test-efobe.json");
			File.WriteAllText(tmpE.FullName, "{ \"blocks\": [] }");
			validator.sendITM(new Validator.ITM.EFOBEReqReply(tmpE));
			validator.keepChecking(); //Will receive and bind EFOBE
			var efobeView = validator.GetLocalEFOBE().blocksV();

			Assert.IsEmpty(efobeView, "EFOBE did not reset, or reset to a non-empty state.");

			validator.sendITM(new Validator.ITM.ProblemSolved(ifp.getName(), "{ \"o\": 242 }", "{ \"o\": [2,11,11] }"));
			validator.keepChecking();
			Assert.IsTrue(efobeView.Count == 1, "Valid result did not pass validation.");

			validator.sendITM(new Validator.ITM.ProblemSolved(ifp.getName(), "{ \"o\": 242 }", "{ \"o\": [2,12,12] }"));
			validator.keepChecking();
			Assert.IsTrue(efobeView.Count == 1, "Invaluid result passed validation");

			var outdatedTop = validator.GetLocalEFOBE().TopBlock();
			var gp = (pro: ifp.getName(), par: "{ \"o\": 242 }", sol: "{ \"o\": [2,11,11] }");
			//validator.sendITM(new Validator.ITM.EFOBERemoteBlockAdded(gp.pro, gp.par, gp.sol, validator.computeHash(outdatedTop, gp.pro, gp.par, gp.sol)));
			validator.keepChecking();
			//Assert.IsTrue(efobeView.Count == 2, "Valid hash did not pass validation.");
			
			//validator.sendITM(new Validator.ITM.EFOBERemoteBlockAdded(gp.pro, gp.par, gp.sol, validator.computeHash(outdatedTop, gp.pro, gp.par, gp.sol)));
			validator.keepChecking();
			//Assert.IsTrue(efobeView.Count == 2, "Invalid hash passed validation.");

			validator.cleanup();
		}

	}

}