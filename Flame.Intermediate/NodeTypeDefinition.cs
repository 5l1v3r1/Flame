﻿using Flame.Build;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class NodeTypeDefinition : INodeStructure<IType>, IType, IInvariantType, INamespaceBranch
    {
        public NodeTypeDefinition(INamespace DeclaringNamespace, NodeSignature Signature)
        {
            this.DeclaringNamespace = DeclaringNamespace;
            this.Signature = Signature;
            this.GenericParameterNodes = EmptyNodeList<IGenericParameter>.Instance;
            this.BaseTypeNodes = EmptyNodeList<IType>.Instance;
            this.NestedTypeNodes = EmptyNodeList<IType>.Instance;
            this.MemberNodes = EmptyNodeList<ITypeMember>.Instance;
        }

        // Format:
        //
        // #type_definition(#member(name, attrs...), { generic_parameters... }, { base_types... }, { nested_types... }, { members... })

        public INamespace DeclaringNamespace { get; private set; }
        public NodeSignature Signature { get; set; }
        public INodeStructure<IEnumerable<IGenericParameter>> GenericParameterNodes { get; set; }
        public INodeStructure<IEnumerable<IType>> BaseTypeNodes { get; set; }
        public INodeStructure<IEnumerable<IType>> NestedTypeNodes { get; set; }
        public INodeStructure<IEnumerable<ITypeMember>> MemberNodes { get; set; }

        public IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
        }

        public IEnumerable<IType> BaseTypes
        {
            get { return BaseTypeNodes.Value; }
        }

        public IEnumerable<IField> Fields
        {
            get { return MemberNodes.Value.OfType<IField>(); }
        }

        public IBoundObject GetDefaultValue()
        {
            return null;
        }

        public IEnumerable<IMethod> Methods
        {
            get { return MemberNodes.Value.OfType<IMethod>(); }
        }

        public IEnumerable<IProperty> Properties
        {
            get { return MemberNodes.Value.OfType<IProperty>(); }
        }

        public IEnumerable<IInvariant> GetInvariants()
        {
            return MemberNodes.Value.OfType<IInvariant>();
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return Signature.Attributes; }
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); }
        }

        public string Name
        {
            get { return Signature.Name; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return GenericParameterNodes.Value; }
        }

        public IEnumerable<INamespaceBranch> Namespaces
        {
            get { return Enumerable.Empty<INamespaceBranch>(); }
        }

        public IAssembly DeclaringAssembly
        {
            get { return DeclaringNamespace.DeclaringAssembly; }
        }

        public IEnumerable<IType> Types
        {
            get { return NestedTypeNodes.Value; }
        }

        public const string TypeDefinitionNodeName = "#type_definition";

        public Node Node
        {
            get 
            {
                return NodeFactory.Call(TypeDefinitionNodeName, new Node[]
                {
                    Signature.Node,
                    GenericParameterNodes.Node,
                    BaseTypeNodes.Node,
                    NestedTypeNodes.Node,
                    MemberNodes.Node
                });
            }
        }

        public IType Value
        {
            get { return this; }
        }
    }
}
