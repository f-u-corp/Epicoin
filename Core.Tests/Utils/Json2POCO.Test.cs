using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

using Epicoin.Core;

using NUnit.Framework;

namespace Epicoin.Test {

	[TestFixture]
	public class ClassBuilderTest {

		[Test]
		public void TestClassCreation(){
			var gcb = new GroupedClassesBuilder("TestClasses");
			var t1 = gcb.Class("Class1").Build();
			var t2 = gcb.Class("Class2").Build();
			Assert.AreEqual("Class1", t1.Name, "Class Creation: Name Mismatch");
			Assert.AreEqual("Class2", t2.Name, "Class Creation: Name Mismatch");
			var fc = gcb.Class("ClassWithFields").Field("aint", typeof(int)).Field("afloat", typeof(float)).Field("staticFlag", typeof(bool), FieldAttributes.Static | FieldAttributes.Public).Field("privateEFOBEList", typeof(List<EFOBE>), FieldAttributes.Private).Build();
			var fnames = fc.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Select(f => f.Name);
			Assert.IsTrue(fnames.Contains("aint"), "Class Creation: Field creation failed. Fields - " + String.Join(",", fnames));
			Assert.IsTrue(fnames.Contains("afloat"), "Class Creation: Field creation failed. Fields - " + String.Join(",", fnames));
			Assert.IsTrue(fnames.Contains("staticFlag"), "Class Creation: Field creation failed. Fields - " + String.Join(",", fnames));
			Assert.IsTrue(fnames.Contains("privateEFOBEList"), "Class Creation: Field creation failed. Fields - " + String.Join(",", fnames));
			FieldInfo	aif = fc.GetField("aint", BindingFlags.Instance | BindingFlags.Public),
						aff = fc.GetField("afloat", BindingFlags.Instance | BindingFlags.Public),
						asf = fc.GetField("staticFlag", BindingFlags.Static | BindingFlags.Public),
						aplf = fc.GetField("privateEFOBEList", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.AreNotEqual(null, aif, "Class Creation: Field creation failed");
			Assert.AreNotEqual(null, aff, "Class Creation: Field creation failed");
			Assert.AreNotEqual(null, asf, "Class Creation: Field creation failed");
			Assert.AreNotEqual(null, aplf, "Class Creation: Field creation failed");

			Assert.AreEqual(typeof(int), aif.FieldType, "Class Creation: Field Type Mismatch");
			Assert.AreEqual(typeof(float), aff.FieldType, "Class Creation: Field Type Mismatch");
			Assert.AreEqual(typeof(bool), asf.FieldType, "Class Creation: Field Type Mismatch");
			Assert.AreEqual(typeof(List<EFOBE>), aplf.FieldType, "Class Creation: Field Type Mismatch");

			Assert.IsTrue(asf.IsStatic, "Class Creation: Field Flags Mismatch");
			Assert.IsTrue(aplf.IsPrivate, "Class Creation: Field Flags Mismatch");

			Assert.DoesNotThrow(() => asf.SetValue(null, true), "Created Class Access: static field access failed");
			Assert.DoesNotThrow(() => asf.GetValue(null), "Created Class Access: static field access failed");
			Assert.AreEqual(true, asf.GetValue(null), "Created Class Access: static field set failed");

			dynamic so = null;
			Assert.DoesNotThrow(() => so = fc.GetConstructor(new Type[]{}).Invoke(new object[]{}), "Created Class: instantiation failed");
			Assert.AreEqual(fc, so.GetType(), "Created Class: instantiation produced wrong type - " + so.GetType());
			Assert.DoesNotThrow(() => {
				so.aint = -89;
				so.afloat = 59.87E-31f;
				Assert.AreEqual(-89, so.aint, "Created Class Access: public field set failed");
				Assert.AreEqual(59.87E-31f, so.afloat, "Created Class Access: public field set failed");
			}, "Created Class Access: public field access failed");
			Assert.Throws(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException), () => so.privateEFOBEList = new List<EFOBE>(), "Created Class Access: private protection failed");
		}

		[Test]
		public void TestStruct(){
			var gcb = new GroupedClassesBuilder("TestStructs");
			var t1 = gcb.Struct("Struct1").Build();
			Assert.AreEqual("Struct1", t1.Name, "Struct Creation: Name Mismatch");
			Assert.IsTrue(t1.IsValueType, "Struct Creation: Is Not a Structure");
		}

	}

}