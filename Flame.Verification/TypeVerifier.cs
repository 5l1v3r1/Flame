﻿using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public class TypeVerifier : MemberVerifierBase<IType>
    {
        public TypeVerifier()
            : base()
        {
            this.FieldVerifier = new PassVerifier();
            this.PropertyVerifier = new PropertyVerifier();
            this.MethodVerifier = new MethodVerifier();
        }
        public TypeVerifier(IVerifier<IField> FieldVerifier, IVerifier<IProperty> PropertyVerifier,
                            IVerifier<IMethod> MethodVerifier)
            : base()
        {
            this.FieldVerifier = FieldVerifier;
            this.PropertyVerifier = PropertyVerifier;
            this.MethodVerifier = MethodVerifier;
        }
        public TypeVerifier(IVerifier<IField> FieldVerifier, IVerifier<IProperty> PropertyVerifier, 
                            IVerifier<IMethod> MethodVerifier, IEnumerable<IAttributeVerifier<IType>> AttributeVerifiers)
            : base(AttributeVerifiers)
        {
            this.FieldVerifier = FieldVerifier;
            this.PropertyVerifier = PropertyVerifier;
            this.MethodVerifier = MethodVerifier;
        }

        public IVerifier<IField> FieldVerifier { get; private set; }
        public IVerifier<IProperty> PropertyVerifier { get; private set; }
        public IVerifier<IMethod> MethodVerifier { get; private set; }

        protected override bool VerifyMemberCore(IType Member, ICompilerLog Log)
        {
            bool success = true;
            foreach (var item in Member.BaseTypes)
            {
                if (Member.GetIsEnum())
                {
                    if (!item.GetIsValueType() && !item.GetIsPrimitive())
                    {
                        Log.LogError(new LogEntry("Invalid enum backing type", 
                            "enum type '" + Member.FullName + "' must be backed by a primitive or value type. '" + item.FullName + "' is neither.",
                            Member.GetSourceLocation()));
                    }
                }
                else if (!item.GetIsVirtual() && !item.GetIsAbstract() && !item.GetIsInterface())
                {
                    Log.LogError(new LogEntry("Invalid inheritance tree", 
                        "Type '" + Member.FullName + "' cannot derive from non-virtual type '" + item.FullName + "'.",
                        Member.GetSourceLocation()));
                }
            }
            if (!Member.GetIsAbstract() && !Member.GetIsInterface())
            {
                foreach (var item in Member.BaseTypes.Where(item => item.GetIsAbstract() || item.GetIsInterface()))
                {
                    if (!item.VerifyImplementation(Member, Log)) success = false;
                }
            }
            foreach (var item in Member.Fields)
            {
                if (!FieldVerifier.Verify(item, Log)) success = false;
            }
            foreach (var item in Member.Properties)
            {
                if (!PropertyVerifier.Verify(item, Log)) success = false;
            }
            foreach (var item in Member.Methods)
            {
                if (!MethodVerifier.Verify(item, Log)) success = false;
            }
            return success;
        }
    }
}
