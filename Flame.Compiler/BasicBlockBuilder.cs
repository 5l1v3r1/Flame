using System;
using System.Collections.Immutable;

namespace Flame.Compiler
{
    /// <summary>
    /// A basic block in a mutable control-flow graph builder.
    /// </summary>
    public sealed class BasicBlockBuilder : IEquatable<BasicBlockBuilder>
    {
        /// <summary>
        /// Creates a basic block builder from a graph and a tag.
        /// </summary>
        /// <param name="graph">The basic block builder's defining graph.</param>
        /// <param name="tag">The basic block's tag.</param>
        internal BasicBlockBuilder(FlowGraphBuilder graph, BasicBlockTag tag)
        {
            this.Graph = graph;
            this.Tag = tag;
        }

        /// <summary>
        /// Gets this basic block's tag.
        /// </summary>
        /// <returns>The basic block's tag.</returns>
        public BasicBlockTag Tag { get; private set; }

        /// <summary>
        /// Gets the control-flow graph builder that defines this
        /// basic block.
        /// </summary>
        /// <returns>The control-flow graph builder.</returns>
        public FlowGraphBuilder Graph { get; private set; }

        /// <summary>
        /// Tells if this basic block builder is still valid, that is,
        /// it has not been removed from its control-flow graph builder's
        /// set of basic blocks.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this basic block builder is still valid; otherwise, <c>false</c>.
        /// </returns>
        public bool IsValid => Graph.ContainsBasicBlock(Tag);

        private BasicBlock ImmutableBlock => Graph.ImmutableGraph.GetBasicBlock(Tag);

        /// <summary>
        /// Gets or sets this basic block's list of parameters.
        /// </summary>
        /// <returns>The basic block's parameters.</returns>
        public ImmutableList<BlockParameter> Parameters
        {
            get
            {
                return ImmutableBlock.Parameters;
            }
            set
            {
                Graph.ImmutableGraph = ImmutableBlock.WithParameters(value).Graph;
            }
        }

        /// <summary>
        /// Gets the list of all instruction tags in this basic block.
        /// </summary>
        /// <returns>The list of all instruction tags.</returns>
        public ImmutableList<ValueTag> InstructionTags => ImmutableBlock.InstructionTags;

        /// <summary>
        /// Gets or sets the control flow at the end of this basic block.
        /// </summary>
        /// <returns>The end-of-block control flow.</returns>
        public BlockFlow Flow
        {
            get
            {
                return ImmutableBlock.Flow;
            }
            set
            {
                Graph.ImmutableGraph = ImmutableBlock.WithFlow(value).Graph;
            }
        }

        /// <summary>
        /// Removes the instruction with a particular tag from
        /// this basic block.
        /// </summary>
        /// <param name="tag">The tag of the instruction to remove.</param>s
        public void RemoveInstruction(ValueTag tag)
        {
            Graph.ImmutableGraph = ImmutableBlock.RemoveInstruction(tag).Graph;
        }

        /// <summary>
        /// Appends a new instruction to the end of this basic block.
        /// Returns the instruction builder for the inserted instruction.
        /// </summary>
        /// <param name="instruction">The instruction to append.</param>
        /// <param name="name">The preferred name of the instruction's tag.</param>
        /// <returns>The appended instruction.</returns>
        public InstructionBuilder AppendInstruction(Instruction instruction, string name)
        {
            var selInsn = ImmutableBlock.AppendInstruction(instruction, name);
            Graph.ImmutableGraph = selInsn.Block.Graph;
            return Graph.GetInstruction(selInsn.Tag);
        }

        /// <summary>
        /// Appends a new instruction to the end of this basic block.
        /// Returns the instruction builder for the inserted instruction.
        /// </summary>
        /// <param name="instruction">The instruction to append.</param>
        /// <returns>The appended instruction.</returns>
        public InstructionBuilder AppendInstruction(Instruction instruction)
        {
            return AppendInstruction(instruction, "");
        }

        /// <summary>
        /// Tests if this basic block equals another basic block.
        /// </summary>
        /// <param name="other">The other basic block.</param>
        /// <returns>
        /// <c>true</c> if this basic block equals the other
        /// basic block; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(BasicBlockBuilder other)
        {
            return Graph == other.Graph && Tag == other.Tag;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is BasicBlockBuilder && Equals((BasicBlockBuilder)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Graph.GetHashCode() << 16) ^ Tag.GetHashCode();
        }
    }
}