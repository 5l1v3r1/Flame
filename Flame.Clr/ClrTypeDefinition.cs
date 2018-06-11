using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.TypeSystem;
using Mono.Cecil;

namespace Flame.Clr
{
    /// <summary>
    /// A Flame type that wraps an IL type definition.
    /// </summary>
    public sealed class ClrTypeDefinition : IType
    {
        /// <summary>
        /// Creates a Flame type that wraps a particular type
        /// definition.
        /// </summary>
        /// <param name="definition">The definition to wrap.</param>
        /// <param name="assembly">
        /// The assembly that directly defines this type.
        /// </param>
        public ClrTypeDefinition(
            TypeDefinition definition,
            ClrAssembly assembly)
            : this(
                definition,
                assembly,
                new TypeParent(assembly),
                NameConversion
                    .ParseSimpleName(definition.Name)
                    .Qualify(
                        NameConversion.ParseNamespace(
                            definition.Namespace)))
        { }

        /// <summary>
        /// Creates a Flame type that wraps a particular nested type
        /// definition.
        /// </summary>
        /// <param name="definition">The definition to wrap.</param>
        /// <param name="parentType">
        /// The type that directly defines this type.
        /// </param>
        public ClrTypeDefinition(
            TypeDefinition definition,
            ClrTypeDefinition parentType)
            : this(
                definition,
                parentType.Assembly,
                new TypeParent(parentType),
                NameConversion
                    .ParseSimpleName(definition.Name)
                    .Qualify(parentType.FullName))
        { }

        private ClrTypeDefinition(
            TypeDefinition definition,
            ClrAssembly assembly,
            TypeParent parent,
            QualifiedName fullName)
        {
            this.Definition = definition;
            this.Assembly = assembly;
            this.Parent = parent;
            this.contentsInitializer = Assembly
                .CreateSynchronizedInitializer(AnalyzeContents);
            this.OverrideInitializer = Assembly
                .CreateSynchronizedInitializer(AnalyzeOverrides);

            this.FullName = fullName;
            this.nestedTypeCache = Assembly
                .CreateSynchronizedLazy<IReadOnlyList<ClrTypeDefinition>>(() =>
            {
                return definition.NestedTypes
                    .Select(t => new ClrTypeDefinition(t, this))
                    .ToArray();
            });
            this.genericParamCache = Assembly
                .CreateSynchronizedLazy<IReadOnlyList<ClrGenericParameter>>(() =>
            {
                return definition.GenericParameters
                    .Skip(
                        parent.IsType
                            ? parent.TypeOrNull.GenericParameters.Count
                            : 0)
                    .Select(param => new ClrGenericParameter(param, this))
                    .ToArray();
            });
        }

        /// <summary>
        /// Gets the assembly that directly or indirectly defines
        /// this type.
        /// </summary>
        /// <returns>The assembly.</returns>
        public ClrAssembly Assembly { get; private set; }

        /// <summary>
        /// Gets the type definition this type is based on.
        /// </summary>
        /// <returns>The type definition.</returns>
        public TypeDefinition Definition { get; private set; }

        /// <inheritdoc/>
        public TypeParent Parent { get; private set; }

        /// <summary>
        /// Gets the deferred initializer for analyzing method
        /// overrides.
        /// </summary>
        /// <returns>The override analysis initializer.</returns>
        internal DeferredInitializer OverrideInitializer { get; private set; }

        private DeferredInitializer contentsInitializer;
        private IReadOnlyList<IType> baseTypeList;
        private IReadOnlyList<ClrFieldDefinition> fieldDefList;
        private IReadOnlyList<ClrMethodDefinition> methodDefList;
        private Lazy<IReadOnlyList<ClrGenericParameter>> genericParamCache;
        private Lazy<IReadOnlyList<ClrTypeDefinition>> nestedTypeCache;
        private HashSet<IMethod> virtualMethodSet;
        private AttributeMap attributeMap;

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;

        /// <inheritdoc/>
        public AttributeMap Attributes
        {
            get
            {
                contentsInitializer.Initialize();
                return attributeMap;
            }
        }


        /// <inheritdoc/>
        public IReadOnlyList<IType> BaseTypes
        {
            get
            {
                contentsInitializer.Initialize();
                return baseTypeList;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IField> Fields
        {
            get
            {
                contentsInitializer.Initialize();
                return fieldDefList;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> Methods
        {
            get
            {
                contentsInitializer.Initialize();
                return methodDefList;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IProperty> Properties => throw new System.NotImplementedException();

        /// <inheritdoc/>
        public IReadOnlyList<IType> NestedTypes => nestedTypeCache.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters => genericParamCache.Value;

        private void AnalyzeContents()
        {
            // Analyze attributes.
            var attrBuilder = new AttributeMapBuilder();
            if (!Definition.IsValueType)
            {
                attrBuilder.Add(FlagAttribute.ReferenceType);
            }
            if (Definition.IsAbstract)
            {
                attrBuilder.Add(FlagAttribute.Abstract);
            }
            if (!Definition.IsSealed)
            {
                attrBuilder.Add(FlagAttribute.Virtual);
            }
            // TODO: support more attributes.
            attributeMap = new AttributeMap(attrBuilder);

            // Analyze base types and interface implementations.
            baseTypeList = (Definition.BaseType == null
                ? new TypeReference[] { }
                : new[] { Definition.BaseType })
                .Concat(Definition.Interfaces.Select(impl => impl.InterfaceType))
                .Select(Assembly.Resolve)
                .ToArray();

            // Analyze fields.
            fieldDefList = Definition.Fields
                .Select(field => new ClrFieldDefinition(field, this))
                .ToArray();

            // Analyze methods. Exclude methods that have semantics (property
            // and event accessors). Those will be analyzed elsewhere.
            methodDefList = Definition.Methods
                .Where(method => method.SemanticsAttributes == MethodSemanticsAttributes.None)
                .Select(method => new ClrMethodDefinition(method, this))
                .ToArray();
        }

        private void AnalyzeOverrides()
        {
            // A method's base methods consist of its implicit
            // and explicit overrides. (Flame doesn't distinguish
            // between these two.)
            //
            //   * Explicit overrides are extracted directly from
            //     the method definition.
            //
            //   * Implicit overrides are derived by inspecting
            //     the base types of the type declaring the method:
            //     a method declared/defined in one of the base types
            //     is an implicit override candidate if it is not
            //     already overridden either by a method in another
            //     base type or (explicitly) in the declaring type.
            //
            // The ugly bit is that *all* virtual methods in
            // a type participate in override resolution: explicit
            // overrides can be resolved individually, but implicit
            // overrides always depend on other methods.
            //
            // We know that the (type) inheritance graph is a DAG, so
            // we can safely derive overrides by walking the inheritance
            // graph. Specifically, we'll construct a set of virtual
            // methods for each type:
            //
            //   * Initialize the virtual method set as the union of
            //     the virtual method sets of the base types.
            //
            //   * Remove all explicit overrides from the set and add
            //     them to the overriding methods.
            //
            //   * Remove all implicit overrides from the set and add
            //     them to the overriding methods.
            //

            virtualMethodSet = new HashSet<IMethod>();

            contentsInitializer.Initialize();
            foreach (var baseType in baseTypeList)
            {
                if (baseType is ClrTypeDefinition)
                {
                    // Optimization for ClrTypeDefinition instances.
                    var clrBaseType = (ClrTypeDefinition)baseType;
                    clrBaseType.OverrideInitializer.Initialize();
                    virtualMethodSet.UnionWith(clrBaseType.virtualMethodSet);
                }
                else
                {
                    // General case.
                    virtualMethodSet.UnionWith(baseType.GetVirtualMethodSet());
                }
            }

            // Special case: interfaces. These guys never override anything,
            // so we can skip the regular override resolution process and skip
            // right to the part where we add all methods to the virtual
            // method set.
            if (Definition.IsInterface)
            {
                virtualMethodSet.UnionWith(methodDefList);
                return;
            }

            // Handle explicit overrides.
            foreach (var method in methodDefList)
            {
                method.BaseMethodStore = new List<IMethod>();
                foreach (var overrideRef in method.Definition.Overrides)
                {
                    var overrideMethod = Assembly.Resolve(overrideRef);
                    method.BaseMethodStore.Add(overrideMethod);
                    virtualMethodSet.Remove(overrideMethod);
                }
            }

            // Populate a mapping of method signatures to lists of methods
            // so we can efficiently match methods to overrides.
            var virtualMethodSignatures = new Dictionary<ClrMethodSignature, List<IMethod>>();
            foreach (var virtualMethod in virtualMethodSet)
            {
                var signature = ClrMethodSignature.Create(virtualMethod);
                List<IMethod> virtualMethodList;
                if (!virtualMethodSignatures.TryGetValue(
                    signature,
                    out virtualMethodList))
                {
                    virtualMethodList = new List<IMethod>();
                    virtualMethodSignatures[signature] = virtualMethodList;
                }
                virtualMethodList.Add(virtualMethod);
            }

            // Handle implicit overrides.
            foreach (var method in methodDefList)
            {
                if (method.Definition.IsVirtual)
                {
                    var signature = ClrMethodSignature.Create(method);
                    List<IMethod> virtualMethodList;
                    if (virtualMethodSignatures.TryGetValue(
                        signature, out virtualMethodList))
                    {
                        foreach (var overrideMethod in virtualMethodList)
                        {
                            if (virtualMethodSet.Contains(overrideMethod))
                            {
                                method.BaseMethodStore.Add(overrideMethod);
                                virtualMethodSet.Remove(overrideMethod);
                            }
                        }
                    }
                }
            }

            // Add virtual methods to virtual method set.
            foreach (var method in methodDefList)
            {
                if (method.Definition.IsVirtual && !method.Definition.IsFinal)
                {
                    virtualMethodSet.Add(method);
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return FullName.ToString();
        }
    }
}
