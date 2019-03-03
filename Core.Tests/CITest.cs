using NUnit.Framework;

namespace Epicoin {
	[TestFixture]
	public class CITest {

		[Test]
		public void TestShouldPass(){
			Assert.Pass();
		}

		[Test]
		public void TestShouldFail(){
			Assert.Fail();
		}

	}
}