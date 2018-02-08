using System.Collections.Generic;

namespace Flame.Compiler
{
    /// <summary>
    /// A branch to a particular block that passes a list
    /// of values as arguments.
    /// </summary>
    public sealed class Branch
    {
        /// <summary>
        /// Creates a branch that targets a particular block and
        /// passes a list of arguments.
        /// </summary>
        /// <param name="target">The target block.</param>
        /// <param name="arguments">
        /// A list of arguments to pass to the target block.
        /// </param>
        public Branch(BasicBlockTag target, IReadOnlyList<BranchArgument> arguments)
        {
            this.Target = target;
            this.Arguments = arguments;
        }

        /// <summary>
        /// Gets the branch's target block.
        /// </summary>
        /// <returns>The target block.</returns>
        public BasicBlockTag Target { get; private set; }

        /// <summary>
        /// Gets the arguments passed to the target block
        /// when this branch is taken.
        /// </summary>
        /// <returns>A list of arguments.</returns>
        public IReadOnlyList<BranchArgument> Arguments { get; private set; }

        /// <summary>
        /// Replaces this branch's target with another block.
        /// </summary>
        /// <param name="target">The new target block.</param>
        /// <returns>A new branch.</returns>
        public Branch WithTarget(BasicBlockTag target)
        {
            return new Branch(target, Arguments);
        }

        /// <summary>
        /// Replaces this branch's arguments with a particular
        /// list of arguments.
        /// </summary>
        /// <param name="arguments">The new arguments.</param>
        /// <returns>A new branch.</returns>
        public Branch WithArguments(IReadOnlyList<BranchArgument> arguments)
        {
            return new Branch(Target, arguments);
        }
    }

    /// <summary>
    /// An enumeration of things a branch argument can be.
    /// </summary>
    public enum BranchArgumentKind
    {
        /// <summary>
        /// The branch argument simply passes a value to a target
        /// basic block.
        /// </summary>
        Value,

        /// <summary>
        /// The branch argument passes the result of a 'try' flow's
        /// inner instruction to the target block. Only valid on success
        /// branches of 'try' flows.
        /// </summary>
        TryResult,

        /// <summary>
        /// The branch argument passes the exception thrown by a
        /// 'try' flow's inner instruction to the target block. Only
        /// valid on exception branches of 'try' flows.
        /// </summary>
        TryException
    }

    /// <summary>
    /// An argument to a branch.
    /// </summary>
    public struct BranchArgument
    {
        private BranchArgument(BranchArgumentKind kind, ValueTag value)
        {
            this = default(BranchArgument);
            this.Kind = kind;
            this.ValueOrNull = value;
        }

        /// <summary>
        /// Gets a description of this branch argument's kind.
        /// </summary>
        /// <returns>The branch argument's kind.</returns>
        public BranchArgumentKind Kind { get; private set; }

        /// <summary>
        /// Gets the value referred to by this branch argument. This is
        /// non-null if and only if this branch argument is a value.
        /// </summary>
        /// <returns>The value referred to by this branch argument.</returns>
        public ValueTag ValueOrNull { get; private set; }

        /// <summary>
        /// Tests if this branch argument is a value.
        /// </summary>
        public bool IsValue => Kind == BranchArgumentKind.Value;

        /// <summary>
        /// Tests if this branch argument is the result of a 'try' flow's
        /// inner instruction.
        /// </summary>
        public bool IsTryResult => Kind == BranchArgumentKind.TryResult;

        /// <summary>
        /// Tests if this branch argument is the exception thrown by a
        /// 'try' flow's inner instruction.
        /// </summary>
        public bool IsTryException => Kind == BranchArgumentKind.TryException;

        /// <summary>
        /// Creates a branch argument that passes a particular value
        /// to the branch's target block.
        /// </summary>
        /// <param name="value">The value to pass to the target block.</param>
        /// <returns>A branch argument.</returns>
        public BranchArgument FromValue(ValueTag value)
        {
            return new BranchArgument(BranchArgumentKind.Value, value);
        }

        /// <summary>
        /// Gets a branch argument that represents the result of a 'try'
        /// flow's inner instruction.
        /// </summary>
        /// <returns>A branch argument.</returns>
        public static BranchArgument TryResult =>
            new BranchArgument(BranchArgumentKind.TryResult, null);

        /// <summary>
        /// Gets a branch argument that represents the exception thrown by
        /// a 'try' flow's inner instruction.
        /// </summary>
        /// <returns>A branch argument.</returns>
        public static BranchArgument TryException =>
            new BranchArgument(BranchArgumentKind.TryException, null);
    }
}