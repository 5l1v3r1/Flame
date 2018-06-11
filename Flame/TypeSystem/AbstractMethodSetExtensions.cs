using System.Collections.Generic;
using System.Linq;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// Defines an extension for finding the abstract method set of
    /// a type: the set of all abstract methods defined in the type
    /// itself or any of its (recursive) base types that are not
    /// (yet) implemented in the type.
    /// </summary>
    public static class AbstractMethodSetExtensions
    {
        private static WeakCache<IType, HashSet<IMethod>> abstractMethodSets =
            new WeakCache<IType, HashSet<IMethod>>();

        /// <summary>
        /// Gets the abstract method set of a particular type: the set of
        /// all abstract methods defined in the type itself or any of
        /// its (recursive) base types that are not (yet) implemented
        /// in the type.
        /// </summary>
        /// <param name="type">The type to query.</param>
        /// <returns>A set of abstract methods.</returns>
        public static IEnumerable<IMethod> GetAbstractMethodSet(this IType type)
        {
            return abstractMethodSets.Get(type, BuildAbstractMethodSet);
        }

        /// <summary>
        /// Gets all methods and accessors defined by a particular type.
        /// </summary>
        /// <param name="type">The type to query for methods and accessors.</param>
        /// <returns>A list of methods and accessors.</returns>
        public static IEnumerable<IMethod> GetMethodsAndAccessors(this IType type)
        {
            return type.Methods
                .Concat(type.Properties.SelectMany(prop => prop.Accessors));
        }

        private static HashSet<IMethod> BuildAbstractMethodSet(IType type)
        {
            var results = new HashSet<IMethod>();
            foreach (var baseType in type.BaseTypes)
            {
                results.UnionWith(baseType.GetAbstractMethodSet());
            }
            foreach (var method in type.GetMethodsAndAccessors())
            {
                if (!method.IsStatic)
                {
                    results.ExceptWith(method.BaseMethods);
                    if (method.IsAbstract())
                    {
                        results.Add(method);
                    }
                }
            }
            return results;
        }
    }
}