﻿using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class SequenceBlock : ICecilBlock
    {
        public SequenceBlock(ICodeGenerator CodeGenerator, ICecilBlock First, ICecilBlock Second)
        {
            this.CodeGenerator = CodeGenerator;
            this.First = First;
            this.Second = Second;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICecilBlock First { get; private set; }
        public ICecilBlock Second { get; private set; }

        public void Emit(IEmitContext Context)
        {
            First.Emit(Context);
            Second.Emit(Context);
        }

        public IStackBehavior StackBehavior
        {
            get { return new BlockStackBehavior(new IStackBehavior[] { First.StackBehavior, Second.StackBehavior }); }
        }
    }
}
