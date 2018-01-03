using System.Collections.Generic;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A type of object that recursively applies a mapping to
    /// types.
    /// </summary>
    public abstract class TypeVisitor
    {
        /// <summary>
        /// Tells if a type is of interest to this visitor.
        /// Visitors always specify custom behavior for interesting
        /// types, whereas uninteresting composite types are usually
        /// treated the same: the visitor simply visits the types
        /// they are composed of.
        /// </summary>
        /// <param name="type">A type.</param>
        /// <returns>
        /// <c>true</c> if the type is interesting; otherwise, <c>false</c>.
        /// </returns>
        protected abstract bool IsOfInterest(IType type);

        /// <summary>
        /// Visits a type that has been marked as interesting.
        /// </summary>
        /// <param name="type">A type to visit.</param>
        /// <returns>A visited type.</returns>
        protected abstract IType VisitInteresting(IType type);

        /// <summary>
        /// Visits a type that has not been marked as interesting.
        /// </summary>
        /// <param name="type">A type to visit.</param>
        /// <returns>A visited type.</returns>
        protected virtual IType VisitUninteresting(IType type)
        {
            if (type is ContainerType)
            {
                var container = (ContainerType)type;
                return container.WithElementType(Visit(container.ElementType));
            }
            else if (type.IsRecursiveGenericInstance())
            {
                return Visit(type.GetRecursiveGenericDeclaration())
                    .MakeRecursiveGenericType(
                        VisitAll(type.GetRecursiveGenericArguments()));
            }
            else
            {
                return type;
            }
        }

        /// <summary>
        /// Visits a type.
        /// </summary>
        /// <param name="type">A type to visit.</param>
        /// <returns>A visited type.</returns>
        public IType Visit(IType type)
        {
            if (IsOfInterest(type))
            {
                return VisitInteresting(type);
            }
            else
            {
                return VisitUninteresting(type);
            }
        }

        /// <summary>
        /// Visits all types in a list of types.
        /// </summary>
        /// <param name="types">A list of types to visit.</param>
        /// <returns>A list of visited types.</returns>
        public IReadOnlyList<IType> VisitAll(IReadOnlyList<IType> types)
        {
            var results = new IType[types.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = Visit(types[i]);
            }
            return results;
        }
    }
}