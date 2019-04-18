using System;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

[assembly: InternalsVisibleTo("Core.Tests")]
namespace Epicoin.Core {

	internal class GroupedClassesBuilder {

		readonly AssemblyName assembly;
		readonly AssemblyBuilder assemblyBuilder;
		readonly ModuleBuilder moduleBuilder;

		public GroupedClassesBuilder(string assembly){
			this.assembly = new AssemblyName(assembly);
			this.assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(this.assembly, AssemblyBuilderAccess.Run);
			this.moduleBuilder = assemblyBuilder.DefineDynamicModule(this.assembly.Name);
		}

		public CBuilder Class(string name, TypeAttributes attr = TypeAttributes.Class | TypeAttributes.Public, Type parent = null) => new CBuilder(this, name, attr | TypeAttributes.AnsiClass, parent);

		public CBuilder Struct(string name, TypeAttributes attr = TypeAttributes.Public) => Class(name, attr | TypeAttributes.Sealed | TypeAttributes.Serializable, typeof(ValueType));

		internal class CBuilder {

			readonly GroupedClassesBuilder group;
			readonly string name;

			readonly TypeBuilder builder;

			internal CBuilder(GroupedClassesBuilder group, string name, TypeAttributes attr, Type parent){
				this.group = group;
				this.name = name;
				this.builder = group.moduleBuilder.DefineType(name, attr, parent);
			}

			public TypeBuilder EmitBuilder => builder;

			public CBuilder Field(string name, Type type, FieldAttributes attr = FieldAttributes.Public, CustomAttributeBuilder cattr = null){
				var f = builder.DefineField(name, type, attr);
				if(cattr != null) f.SetCustomAttribute(cattr);
				return this;
			}

			public Type Build() => builder.CreateType();

		}

	}

	internal class JsonStructCreator {

		public static Dictionary<string, Type> CreateStructs(GroupedClassesBuilder gcb, string json){
			var jtypes = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
			var builders = jtypes.Keys.Select(t => (name: t, t: gcb.Struct(t))).ToDictionary(p => p.name, p => p.t);
			Type GetDType(string name){
				if(name.EndsWith("[]")) return GetDType(name.Substring(0, name.Length-2)).MakeArrayType();
				switch(name){
					case "bool": case "boolean": return typeof(bool);
					case "byte": return typeof(byte);
					case "char": return typeof(char);
					case "int": return typeof(int);
					case "uint": return typeof(uint);
					case "long": return typeof(long);
					case "ulong": return typeof(ulong);
					case "float": return typeof(float);
					case "double": return typeof(double);
					default: return builders[name].EmitBuilder;
				}
			}
			foreach(var tn in builders.Keys){
				var builder = builders[tn];
				foreach(var nt in jtypes[tn].Select(kv => (name: kv.Key, type: GetDType(kv.Value)))) builder.Field(nt.name, nt.type);
			}
			return builders.Values.Select(b => b.Build()).ToDictionary(t => t.Name, t => t);
		}

		public static Dictionary<string, Type> CreateStructs(string assembly, string json) => CreateStructs(new GroupedClassesBuilder(assembly), json);

	}

}