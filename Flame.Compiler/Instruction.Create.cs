using System.Collections.Generic;
using Flame.Compiler.Instructions;

namespace Flame.Compiler
{
    public partial struct Instruction
    {
        /// <summary>
        /// Creates an instruction that allocates storage on the stack
        /// for a variable number of elements of a particular type.
        /// </summary>
        /// <param name="elementType">
        /// The type of value to allocate storage for.
        /// </param>
        /// <returns>
        /// An alloca-array instruction.
        /// </returns>
        public static Instruction CreateAllocaArray(
            IType elementType, ValueTag elementCount)
        {
            return AllocaArrayPrototype.Create(elementType).Instantiate(elementCount);
        }

        /// <summary>
        /// Creates an instruction that allocates storage on the stack
        /// for a single value element of a particular type.
        /// </summary>
        /// <param name="elementType">
        /// The type of value to allocate storage for.
        /// </param>
        /// <returns>
        /// An alloca instruction.
        /// </returns>
        public static Instruction CreateAlloca(IType elementType)
        {
            return AllocaPrototype.Create(elementType).Instantiate();
        }

        /// <summary>
        /// Creates an instruction that calls a particular method.
        /// </summary>
        /// <param name="callee">The method to call.</param>
        /// <param name="lookup">
        /// The method implementation lookup technique to use for calling the method.
        /// </param>
        /// <param name="arguments">
        /// The extended argument list: a list of arguments prefixed with a 'this'
        /// argument, if applicable.
        /// </param>
        /// <returns>
        /// A call instruction.
        /// </returns>
        public static Instruction CreateCall(
            IMethod callee,
            MethodLookup lookup,
            IReadOnlyList<ValueTag> arguments)
        {
            return CallPrototype.Create(callee, lookup).Instantiate(arguments);
        }

        /// <summary>
        /// Creates an instruction that calls a particular method.
        /// </summary>
        /// <param name="callee">The method to call.</param>
        /// <param name="lookup">
        /// The method implementation lookup technique to use for calling the method.
        /// </param>
        /// <param name="thisArgument">
        /// The 'this' argument for the method call.
        /// </param>
        /// <param name="arguments">
        /// The argument list for the method call.
        /// </param>
        /// <returns>
        /// A call instruction.
        /// </returns>
        public static Instruction CreateCall(
            IMethod callee,
            MethodLookup lookup,
            ValueTag thisArgument,
            IReadOnlyList<ValueTag> arguments)
        {
            return CallPrototype.Create(callee, lookup)
                .Instantiate(thisArgument, arguments);
        }

        /// <summary>
        /// Creates an instruction that creates a constant value of a
        /// particular type.
        /// </summary>
        /// <param name="value">
        /// The constant value to produce.
        /// </param>
        /// <param name="type">
        /// The type of value created by the instruction.
        /// </param>
        /// <returns>
        /// A constant instruction.
        /// </returns>
        public static Instruction CreateConstant(Constant value, IType type)
        {
            return ConstantPrototype.Create(value, type).Instantiate();
        }
    }
}