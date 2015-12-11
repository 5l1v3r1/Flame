﻿using Flame.Build;
using Flame.Cpp.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public abstract class CppTypeNamerBase : TypeNamerBase
    {
        public CppTypeNamerBase(INamespace CurrentNamespace)
        {
            this.CurrentNamespace = CurrentNamespace;
        }

        public INamespace CurrentNamespace { get; private set; }

        public abstract string NameInt(int PrimitiveMagnitude);
        public abstract string NameUInt(int PrimitiveMagnitude);
        public abstract string NameFloat(int PrimitiveMagnitude);

        protected override string ConvertPrimitiveType(IType Type)
        {
            if (Type.Equals(PrimitiveTypes.Void))
            {
                return "void";
            }
            else if (Type.GetIsSignedInteger())
            {
                return NameInt(Type.GetPrimitiveMagnitude());
            }
            else if (Type.GetIsUnsignedInteger() || Type.GetIsBit())
            {
                return NameUInt(Type.GetPrimitiveMagnitude());
            }
            else if (Type.GetIsFloatingPoint())
            {
                return NameFloat(Type.GetPrimitiveMagnitude());
            }
            else if (Type.Equals(PrimitiveTypes.String))
            {
                return "std::string";
            }
            else if (Type.Equals(PrimitiveTypes.Char))
            {
                return "char";
            }
            else if (Type.Equals(PrimitiveTypes.Boolean))
            {
                return "bool";
            }
            else if (Type.Equals(PrimitiveTypes.Null))
            {
                return Convert(PrimitiveTypes.Void.MakePointerType(PointerKind.TransientPointer));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected override string MakePointerType(string ElementType, PointerKind Kind)
        {
            if (Kind.Equals(PointerKind.ReferencePointer))
            {
                return MakeGenericType("std::shared_ptr", new string[] { ElementType });
            }
            else
            {
                return ElementType + Kind.Extension;
            }
        }

        protected override string ConvertArrayType(IArrayType Type)
        {
            if (Type.GetIsGenericInstance())
            {
                return ConvertGenericInstance(Type);
            }
            else
            {
                return base.ConvertArrayType(Type);
            }
        }

        protected override string ConvertVectorType(IVectorType Type)
        {
            if (Type.GetIsGenericInstance())
            {
                return ConvertGenericInstance(Type);
            }
            else
            {
                return base.ConvertVectorType(Type);
            }
        }

        protected override string ConvertDelegateType(IType Type)
        {
            return MakeGenericType("std::function", new string[] { base.ConvertDelegateType(Type) });
        }

        protected override string ConvertGenericParameter(IGenericParameter Type)
        {
            return Type.Name;
        }

        protected override string ConvertTypeDeclaration(IType Type)
        {
            if (Type.DeclaringNamespace is IType)
            {
                return Convert((IType)Type.DeclaringNamespace) + "::" + Type.Name;
            }
            return base.ConvertTypeDeclaration(Type);
        }

        protected override string ConvertTypeDefault(IType Type)
        {
            if (Type.IsGlobalType())
            {
                return CppNameExtensions.RemoveRedundantScope(Type.DeclaringNamespace.FullName, CurrentNamespace);
            }
            else
            {
                return CppNameExtensions.RemoveRedundantScope(Type.FullName, CurrentNamespace);
            }
        }
    }
}
