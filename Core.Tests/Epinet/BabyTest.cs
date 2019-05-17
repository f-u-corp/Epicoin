using System;
using System.Collections.Generic;
using System.Linq;

using Epicoin.Core;

using NUnit.Framework;

namespace Epicoin.Test {

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

		[Test]
		public void TestGCD(){
			Assert.AreEqual(1, RSA.GCD(1, 1), "Invalid GCD 1,1");
			Assert.AreEqual(5, RSA.GCD(5, 5), "Invalid GCD 5,5");
			Assert.AreEqual(1, RSA.GCD(5, 7), "Invalid GCD 5,7");
			Assert.AreEqual(2, RSA.GCD(6, 8), "Invalid GCD 6,8");

			Assert.AreEqual(2, RSA.GCD(-6, -8), "Invalid GCD -6,-8");
			Assert.AreEqual(5, RSA.GCD(-35, 60), "Invalid GCD 5,10");
			Assert.AreEqual(3, RSA.GCD(69, -93), "Invalid GCD 5,10");
		}

		[Test]
		public void TestEratosthenesSieve(){
			Assert.Throws(typeof(InvalidOperationException), () => RSA.EratosthenesSieve(0), "Era sieve must not work for 0");
			Assert.Throws(typeof(InvalidOperationException), () => RSA.EratosthenesSieve(-8129), "Era sieve must not work for negative numbers");

			Assert.AreEqual(new List<int>{2}, RSA.EratosthenesSieve(2), "Wrong era sieve for 2");
			Assert.AreEqual(new List<int>{2, 3}, RSA.EratosthenesSieve(3), "Wrong era sieve for 3");
			Assert.AreEqual(new List<int>{2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43}, RSA.EratosthenesSieve(43), "Wrong era sieve for 3");
			Assert.AreEqual(new List<int>{2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293, 307, 311, 313, 317, 331, 337, 347, 349, 353, 359, 367, 373, 379, 383, 389, 397, 401, 409, 419, 421, 431, 433, 439, 443, 449, 457, 461, 463, 467, 479, 487, 491, 499, 503, 509, 521, 523, 541, 547, 557, 563, 569, 571, 577, 587, 593, 599, 601, 607, 613, 617, 619, 631, 641, 643, 647, 653, 659, 661, 673, 677, 683, 691, 701, 709, 719, 727, 733, 739, 743, 751, 757, 761, 769, 773, 787, 797, 809, 811, 821, 823, 827, 829, 839, 853, 857, 859, 863, 877, 881, 883, 887, 907, 911, 919, 929, 937, 941, 947, 953, 967, 971, 977, 983, 991, 997, 1009, 1013, 1019, 1021, 1031, 1033, 1039, 1049, 1051, 1061, 1063, 1069, 1087, 1091, 1093, 1097, 1103, 1109, 1117, 1123, 1129, 1151, 1153, 1163, 1171, 1181, 1187, 1193, 1201, 1213, 1217}, RSA.EratosthenesSieve(1220), "Wrong era sieve for 3");
		}

	}

}