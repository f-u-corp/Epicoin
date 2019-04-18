using System;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;

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

		public CBuilder Struct(string name, TypeAttributes attr = TypeAttributes.Public) => Class(name, attr | TypeAttributes.Sealed | TypeAttributes.ExplicitLayout | TypeAttributes.Serializable, typeof(ValueType));

		internal class CBuilder {

			readonly GroupedClassesBuilder group;
			readonly string name;

			readonly TypeBuilder builder;

			internal CBuilder(GroupedClassesBuilder group, string name, TypeAttributes attr, Type parent){
				this.group = group;
				this.name = name;
				this.builder = group.moduleBuilder.DefineType(name, attr, parent);
			}

			public CBuilder Field(string name, Type type, FieldAttributes attr = FieldAttributes.Public, CustomAttributeBuilder cattr = null){
				var f = builder.DefineField(name, type, attr);
				if(cattr != null) f.SetCustomAttribute(cattr);
				return this;
			}

			public Type Build() => builder.CreateType();

		}

	}

}