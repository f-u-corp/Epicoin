using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Epicoin {

	[TestFixture]
	public class RSATest {

		[Test]
		public void TestIsPrime(){
			Assert.IsTrue(RSA.IsPrime(2), "2 should be prime");
			Assert.IsTrue(RSA.IsPrime(3), "3 should be prime");
			Assert.IsFalse(RSA.IsPrime(4), "4 should not be prime");
			Assert.IsTrue(RSA.IsPrime(5), "5 should be prime");
			Assert.IsFalse(RSA.IsPrime(6), "6 should not be prime");
			Assert.IsTrue(RSA.IsPrime(7), "7 should be prime");
			Assert.IsFalse(RSA.IsPrime(8), "8 should not be prime");
			Assert.IsFalse(RSA.IsPrime(9), "9 should not be prime");

			Assert.IsFalse(RSA.IsPrime(11515), "11515 should not be prime");
			Assert.IsFalse(RSA.IsPrime(4564651), "4564651 should not be prime");
			Assert.IsFalse(RSA.IsPrime(684491), "684491 should not be prime");
			Assert.IsFalse(RSA.IsPrime(157803971), "157803971 should not be prime");
			Assert.IsFalse(RSA.IsPrime(9874631), "9874631 should not be prime");

			Assert.IsTrue(RSA.IsPrime(987631), "987631 should be prime");
			Assert.IsTrue(RSA.IsPrime(43), "43 should be prime");
			Assert.IsTrue(RSA.IsPrime(32969), "7 should be prime");
			Assert.IsTrue(RSA.IsPrime(15485863), "15485863 should be prime");
			Assert.IsTrue(RSA.IsPrime(553105253), "553105253 should be prime");
		}

	}

}