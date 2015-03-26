﻿using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class TryBlockGenerator : ITryBlockGenerator, ICppLocalDeclaringBlock
    {
        public TryBlockGenerator(CppCodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.TryBody = CodeGenerator.CreateBlock();
            this.FinallyBody = CodeGenerator.CreateBlock();
            this.catchClauses = new List<CatchBlockGenerator>();
            this.finallyBlock = new FinallyBlock((ICppBlock)FinallyBody);
        }

        public CppCodeGenerator CodeGenerator { get; private set; }


        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        public ICatchBlockGenerator EmitCatchClause(IVariableMember ExceptionVariable)
        {
            var clause = new CatchBlockGenerator(CodeGenerator, ExceptionVariable);
            this.catchClauses.Add(clause);
            return clause;
        }

        public IBlockGenerator TryBody { get; private set; }
        public IBlockGenerator FinallyBody { get; private set; }
        private ICppLocalDeclaringBlock finallyBlock;
        private List<CatchBlockGenerator> catchClauses;

        protected ICppLocalDeclaringBlock CppTryBody
        {
            get { return (ICppLocalDeclaringBlock)TryBody; }
        }
        protected ICppLocalDeclaringBlock CppFinallyBlock
        {
            get { return finallyBlock; }
        }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return CppTryBody.LocalDeclarations.Concat(CppFinallyBlock.LocalDeclarations).Concat(catchClauses.SelectMany((item) => item.LocalDeclarations)); }
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return CppTryBody.Dependencies.MergeDependencies(CppFinallyBlock.Dependencies).MergeDependencies(catchClauses.Aggregate(Enumerable.Empty<IHeaderDependency>(), (a, b) => a.MergeDependencies(b.Dependencies))); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return CppTryBody.LocalsUsed.Union(CppFinallyBlock.LocalsUsed).Union(catchClauses.Aggregate(Enumerable.Empty<CppLocal>(), (a, b) => a.Union(b.LocalsUsed))); }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.AddLine("try");
            var tryBodyCode = CppTryBody.GetCode();
            var finallyCode = CppFinallyBlock.GetCode();
            if (finallyCode.IsWhitespace)
            {
                cb.AddEmbracedBodyCodeBuilder(tryBodyCode);
            }
            else
            {
                cb.AddEmbracedBodyCodeBuilder(tryBodyCode.PrependStatement(finallyCode));
            }
            foreach (var item in catchClauses)
            {
                cb.AddCodeBuilder(item.GetCode());
            }
            return cb;
        }
    }

    public class CatchBlockGenerator : CppBlockGeneratorBase, ICatchBlockGenerator
    {
        public CatchBlockGenerator(CppCodeGenerator CodeGenerator, IVariableMember ExceptionVariableMember)
            : base(CodeGenerator)
        {
            this.ExceptionVariableDeclaration = new LocalDeclarationReference((CppLocal)CodeGenerator.DeclareVariable(ExceptionVariableMember));
        }

        public LocalDeclarationReference ExceptionVariableDeclaration { get; private set; }

        public IVariable ExceptionVariable { get { return ExceptionVariableDeclaration.Declaration.Local; } }

        public override ICppBlock Simplify()
        {
            return new CatchBlock(ExceptionVariableDeclaration, base.Simplify());
        }
    }
}
