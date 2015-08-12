﻿using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class BinaryOpBlock : ICecilBlock
    {
        public BinaryOpBlock(ICodeGenerator CodeGenerator, ICecilBlock Left, ICecilBlock Right, Operator Operator)
        {
            this.CodeGenerator = CodeGenerator;
            this.Left = Left;
            this.Right = Right;
            this.Operator = Operator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICecilBlock Left { get; private set; }
        public ICecilBlock Right { get; private set; }
        public Operator Operator { get; private set; }

        public void Emit(IEmitContext Context)
        {
            var aType = Left.BlockType;
            var bType = Right.BlockType;

            if (IsIntrinsicType(aType) && IsIntrinsicType(bType))
            {
                EmitInstrinsicOp(aType, bType, Context);
            }
            else
            {
                EmitCallOp(aType, bType, Context);
            }
        }

        private void EmitCallOp(IType aType, IType bType, IEmitContext Context)
        {
            var overload = GetBinaryOverload(aType, Operator, bType);
            if (overload != null)
            {
                var call = (ICecilBlock)CodeGenerator.EmitOperatorCall(overload, Left, Right);
                call.Emit(Context);
            }
            else
            {
                Operator negOp;
                if (TryGetNegatedOperator(Operator, out negOp))
                {
                    var negOverload = GetBinaryOverload(aType, negOp, bType);
                    if (negOverload != null)
                    {
                        var call = (ICecilBlock)CodeGenerator.EmitOperatorCall(negOverload, Left, Right);
                        call.Emit(Context);
                        UnaryOpBlock.EmitBooleanNot(Context);
                        return;
                    }
                }
                EmitInstrinsicOp(aType, bType, Context);
            }
        }

        private static void EmitInstrinsicCode(IType aType, IType bType, Operator Op, IEmitContext Context)
        {
            OpCode opCode;
            if (TryGetOpCode(Op, aType, bType, out opCode))
            {
                Context.Emit(opCode);
            }
            else // Special cases - no direct mapping to IL
            {
                Operator negOp;
                if (TryGetNegatedOperator(Op, out negOp))
	            {
                    Context.Emit(GetOpCode(negOp, aType, bType));
                    UnaryOpBlock.EmitBooleanNot(Context);
	            }
                else
                {
                    throw new NotImplementedException("THe IL back-end does not support binary operator '" + Op + "' for '" + aType.FullName + "' and '" + bType.FullName + "'.");
                }
            }
        }

        private void EmitInstrinsicOp(IType aType, IType bType, IEmitContext Context)
        {
            Left.Emit(Context);
            Right.Emit(Context);

            Context.Stack.Pop();
            Context.Stack.Pop();
            Context.Stack.Push(BlockType);

            EmitInstrinsicCode(aType, bType, Operator, Context);
        }

        public IType BlockType
        {
            get
            {
                if (IsCheck(Operator))
                {
                    return PrimitiveTypes.Boolean;
                }
                else
                {
                    return Left.BlockType;
                }
            }
        }

        #region GetOpCode

        public static OpCode GetOpCode(Operator Op, IType A, IType B)
        {
            OpCode result;
            if (!TryGetOpCode(Op, A, B, out result))
            {
                throw new NotSupportedException();
            }
            return result;
        }

        public static bool TryGetOpCode(Operator Op, IType A, IType B, out OpCode Result)
        {
            if (Op.Equals(Operator.Add))
            {
                Result = OpCodes.Add;
            }
            else if (Op.Equals(Operator.Subtract))
            {
                Result = OpCodes.Sub;
            }
            else if (Op.Equals(Operator.Multiply))
            {
                Result = OpCodes.Mul;
            }
            else if (Op.Equals(Operator.Divide))
            {
                if (A.get_IsUnsignedInteger() && B.get_IsUnsignedInteger())
                {
                    Result = OpCodes.Div_Un;
                }
                else
                {
                    Result = OpCodes.Div;
                }
            }
            else if (Op.Equals(Operator.And))
            {
                Result = OpCodes.And;
            }
            else if (Op.Equals(Operator.Or))
            {
                Result = OpCodes.Or;
            }
            else if (Op.Equals(Operator.Xor))
            {
                Result = OpCodes.Xor;
            }
            else if (Op.Equals(Operator.RightShift))
            {
                Result = OpCodes.Shr;
            }
            else if (Op.Equals(Operator.LeftShift))
            {
                Result = OpCodes.Shl;
            }
            else if (Op.Equals(Operator.Remainder))
            {
                if (A.get_IsUnsignedInteger() && B.get_IsUnsignedInteger())
                {
                    Result = OpCodes.Rem_Un;
                }
                else
                {
                    Result = OpCodes.Rem;
                }
            }
            else if (Op.Equals(Operator.CheckEquality))
            {
                Result = OpCodes.Ceq; 
            }
            else if (Op.Equals(Operator.CheckGreaterThan))
            {
                if (A.get_IsUnsignedInteger() && B.get_IsUnsignedInteger())
                {
                    Result = OpCodes.Cgt_Un;
                }
                else
                {
                    Result = OpCodes.Cgt;
                }
            }
            else if (Op.Equals(Operator.CheckLessThan))
            {
                if (A.get_IsUnsignedInteger() && B.get_IsUnsignedInteger())
                {
                    Result = OpCodes.Clt_Un;
                }
                else
                {
                    Result = OpCodes.Clt;
                }
            }
            else
            {
                Result = default(OpCode);
                return false;
            }
            return true;
        }

        #endregion

        #region IsSupported

        public static bool IsSupported(Operator Op)
        {
            OpCode opCode;
            if (TryGetOpCode(Op, PrimitiveTypes.Int32, PrimitiveTypes.Int32, out opCode))
            {
                return true;
            }
            else
            {
                return IsCheck(Op);
            }
        }

        #endregion

        #region IsIntrinsicType

        public static bool IsIntrinsicType(IType Type)
        {
            return (Type.get_IsPrimitive() && !Type.Equals(PrimitiveTypes.String)) || Type.get_IsEnum();
        }

        #endregion

        #region GetBinaryOverload

        private static IMethod GetEqualsOverload(IType LeftType, IType RightType)
        {
            var eqMethods = LeftType.GetAllMethods().Where((item) => item.Name == "Equals");
            var staticEq = eqMethods.GetBestMethod(true, null, new IType[] { LeftType, RightType });
            if (staticEq != null)
            {
                return staticEq;
            }
            else
            {
                return eqMethods.GetBestMethod(false, LeftType, new IType[] { RightType });
            }
        }

        private static IMethod GetBinaryOverload(IType LeftType, Operator Op, IType RightType)
        {
            var overload = Op.GetOperatorOverload(new IType[] { LeftType, RightType });
            if (overload != null)
            {
                return overload;
            }
            if (Op.Equals(Operator.CheckEquality))
            {
                return GetEqualsOverload(LeftType, RightType);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Operator negation

        #region IsCheck

        public static bool IsCheck(Operator Op)
        {
            return operatorNegationDict.ContainsKey(Op);
        }

        #endregion

        private static Dictionary<Operator, Operator> operatorNegationDict = new Dictionary<Operator, Operator>()
        {
            { Operator.CheckEquality, Operator.CheckInequality },
            { Operator.CheckInequality, Operator.CheckEquality },
            { Operator.CheckLessThanOrEqual, Operator.CheckGreaterThan },
            { Operator.CheckGreaterThan, Operator.CheckLessThanOrEqual },
            { Operator.CheckGreaterThanOrEqual, Operator.CheckLessThan },
            { Operator.CheckLessThan, Operator.CheckGreaterThanOrEqual }
        };

        public static bool TryGetNegatedOperator(Operator Value, out Operator Result)
        {
            if (operatorNegationDict.ContainsKey(Value))
            {
                Result = operatorNegationDict[Value];
                return true;
            }
            else
            {
                Result = Operator.Undefined;
                return false;
            }
        }

        #endregion
    }
}
