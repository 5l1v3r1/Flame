using System;

namespace Flame.Compiler
{
    /// <summary>
    /// An instruction in a mutable control-flow graph builder.
    /// </summary>
    public sealed class InstructionBuilder : IEquatable<InstructionBuilder>
    {
        /// <summary>
        /// Creates an instruction builder from a graph and a tag.
        /// </summary>
        /// <param name="graph">The instruction builder's defining graph.</param>
        /// <param name="tag">The instruction's tag.</param>
        internal InstructionBuilder(FlowGraphBuilder graph, ValueTag tag)
        {
            this.Graph = graph;
            this.Tag = tag;
        }

        /// <summary>
        /// Gets this instruction's tag.
        /// </summary>
        /// <returns>The instruction's tag.</returns>
        public ValueTag Tag { get; private set; }

        /// <summary>
        /// Gets the control-flow graph builder that defines this
        /// instruction.
        /// </summary>
        /// <returns>The control-flow graph builder.</returns>
        public FlowGraphBuilder Graph { get; private set; }

        /// <summary>
        /// Tells if this instruction builder is still valid, that is,
        /// it has not been removed from its control-flow graph builder's
        /// set of instructions.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this instruction builder is still valid; otherwise, <c>false</c>.
        /// </returns>
        public bool IsValid => Graph.ContainsInstruction(Tag);

        private SelectedInstruction ImmutableInstruction =>
            Graph.ImmutableGraph.GetInstruction(Tag);

        /// <summary>
        /// Gets the actual instruction behind this instruction selector.
        /// </summary>
        /// <returns>The instruction.</returns>
        public Instruction Instruction
        {
            get
            {
                return ImmutableInstruction.Instruction;
            }
            set
            {
                Graph.ImmutableGraph =
                    ImmutableInstruction.ReplaceInstruction(value).Block.Graph;
            }
        }

        /// <summary>
        /// Inserts a particular instruction just before this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="name">The preferred name for the instruction.</param>
        /// <returns>The inserted instruction.</returns>
        public InstructionBuilder InsertBefore(Instruction instruction, string name)
        {
            var selInsn = ImmutableInstruction.InsertBefore(instruction, name);
            Graph.ImmutableGraph = selInsn.Block.Graph;
            return Graph.GetInstruction(selInsn.Tag);
        }

        /// <summary>
        /// Inserts a particular instruction just before this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <returns>The inserted instruction.</returns>
        public InstructionBuilder InsertBefore(Instruction instruction)
        {
            return InsertBefore(instruction, "");
        }

        /// <summary>
        /// Inserts a particular instruction just after this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="name">The preferred name for the instruction.</param>
        /// <returns>The inserted instruction.</returns>
        public InstructionBuilder InsertAfter(Instruction instruction, string name)
        {
            var selInsn = ImmutableInstruction.InsertAfter(instruction, name);
            Graph.ImmutableGraph = selInsn.Block.Graph;
            return Graph.GetInstruction(selInsn.Tag);
        }

        /// <summary>
        /// Inserts a particular instruction just after this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <returns>The inserted instruction.</returns>
        public InstructionBuilder InsertAfter(Instruction instruction)
        {
            return InsertAfter(instruction, "");
        }

        /// <summary>
        /// Tests if this instruction builder is the same instruction
        /// as another instruction builder.
        /// </summary>
        /// <param name="other">The other instruction builder.</param>
        /// <returns>
        /// <c>true</c> if this instruction builder is the same as
        /// the other instruction builder; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(InstructionBuilder other)
        {
            return Tag == other.Tag && Graph == other.Graph;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is InstructionBuilder
                && Equals((InstructionBuilder)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Graph.GetHashCode() << 16) ^ Tag.GetHashCode();
        }
    }
}