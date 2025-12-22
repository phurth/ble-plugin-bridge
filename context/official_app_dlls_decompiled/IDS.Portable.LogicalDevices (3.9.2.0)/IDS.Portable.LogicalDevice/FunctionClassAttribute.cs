using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IDS.Portable.LogicalDevice
{
	[AttributeUsage(AttributeTargets.Field)]
	internal class FunctionClassAttribute : Attribute
	{
		private readonly FUNCTION_CLASS[]? _additionalFunctionClasses;

		public FUNCTION_CLASS PrimaryFunctionClass { get; }

		public IEnumerable<FUNCTION_CLASS> FunctionClasses
		{
			[IteratorStateMachine(typeof(_003Cget_FunctionClasses_003Ed__7))]
			get
			{
				//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
				return new _003Cget_FunctionClasses_003Ed__7(-2)
				{
					_003C_003E4__this = this
				};
			}
		}

		public FunctionClassAttribute(FUNCTION_CLASS primaryFunctionClass)
		{
			PrimaryFunctionClass = primaryFunctionClass;
			_additionalFunctionClasses = null;
		}

		public FunctionClassAttribute(FUNCTION_CLASS primaryFunctionClass, params FUNCTION_CLASS[] additionalFunctionClasses)
		{
			PrimaryFunctionClass = primaryFunctionClass;
			_additionalFunctionClasses = additionalFunctionClasses;
		}
	}
}
