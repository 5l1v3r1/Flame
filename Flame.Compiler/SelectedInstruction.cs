using System;
using System.Threading;

namespace Flame.Compiler
{
    /// <summary>
    /// An instruction in the context of a control-flow graph.
    /// </summary>
    public sealed class SelectedInstruction : IEquatable<SelectedInstruction>
    {
        internal SelectedInstruction(
            BasicBlock block, ValueTag tag, Instruction instruction)
        {
            this.Block = block;
            this.Tag = tag;
            this.Instruction = instruction;
            this.instrIndexValue = -1;
        }

        internal SelectedInstruction(
            BasicBlock block,
            ValueTag tag,
            Instruction instruction,
            int instructionIndex)
        {
            this.Block = block;
            this.Tag = tag;
            this.Instruction = instruction;
            this.instrIndexValue = instructionIndex;
        }

        /// <summary>
        /// Gets the tag assigned to this instruction.
        /// </summary>
        /// <returns>The instruction's tag.</returns>
        public ValueTag Tag { get; private set; }

        /// <summary>
        /// Gets the basic block that defines this selected instruction.
        /// </summary>
        /// <returns>The basic block.</returns>
        public BasicBlock Block { get; private set; }

        /// <summary>
        /// Gets the actual instruction behind this instruction selector.
        /// </summary>
        /// <returns>The instruction.</returns>
        public Instruction Instruction { get; private set; }

        private int instrIndexValue;

        /// <summary>
        /// Gets the index of this instruction in the defining block's
        /// list of instructions.
        /// </summary>
        /// <returns>The instruction index.</returns>
        public int InstructionIndex
        {
            get
            {
                if (instrIndexValue < 0)
                {
                    instrIndexValue = Block.InstructionTags.IndexOf(Tag);
                    Interlocked.MemoryBarrier();
                }
                return instrIndexValue;
            }
        }

        /// <summary>
        /// Gets the previous instruction in the basic block that defines
        /// this instruction. Returns null if there is no such instruction.
        /// </summary>
        /// <returns>The previous instruction or null.</returns>
        public SelectedInstruction PreviousInstructionOrNull
        {
            get
            {
                int prevIndex = InstructionIndex - 1;
                if (prevIndex < 0)
                {
                    return null;
                }
                else
                {
                    return Block.Graph.GetInstruction(Block.InstructionTags[prevIndex]);
                }
            }
        }

        /// <summary>
        /// Gets the next instruction in the basic block that defines
        /// this instruction. Returns null if there is no such instruction.
        /// </summary>
        /// <returns>The next instruction or null.</returns>
        public SelectedInstruction NextInstructionOrNull
        {
            get
            {
                int nextIndex = InstructionIndex + 1;
                if (nextIndex == Block.InstructionTags.Count)
                {
                    return null;
                }
                else
                {
                    return Block.Graph.GetInstruction(Block.InstructionTags[nextIndex]);
                }
            }
        }

        /// <summary>
        /// Replaces this instruction with another instruction. Returns
        /// the new instruction in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">
        /// The other instruction to replace this instruction with.
        /// </param>
        /// <returns>
        /// A new instruction in a new control-flow graph.
        /// </returns>
        public SelectedInstruction ReplaceInstruction(Instruction instruction)
        {
            return Block.Graph.ReplaceInstruction(Tag, instruction);
        }

        /// <summary>
        /// Inserts a particular instruction just before this instruction.
        /// Returns the new instruction in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="name">The preferred name for the instruction.</param>
        /// <returns>The inserted instruction in a new control-flow graph.</returns>
        public SelectedInstruction InsertBefore(Instruction instruction, string name)
        {
            return Block.Graph.InsertInstructionInBasicBlock(
                Block.Tag,
                instruction,
                name,
                InstructionIndex);
        }

        /// <summary>
        /// Inserts a particular instruction just before this instruction.
        /// Returns the new instruction in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <returns>The inserted instruction in a new control-flow graph.</returns>
        public SelectedInstruction InsertBefore(Instruction instruction)
        {
            return InsertBefore(instruction, "");
        }

        /// <summary>
        /// Inserts a particular instruction just after this instruction.
        /// Returns the new instruction in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="name">The preferred name for the instruction.</param>
        /// <returns>The inserted instruction in a new control-flow graph.</returns>
        public SelectedInstruction InsertAfter(Instruction instruction, string name)
        {
            return Block.Graph.InsertInstructionInBasicBlock(
                Block.Tag,
                instruction,
                name,
                InstructionIndex + 1);
        }

        /// <summary>
        /// Inserts a particular instruction just after this instruction.
        /// Returns the new instruction in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <returns>The inserted instruction in a new control-flow graph.</returns>
        public SelectedInstruction InsertAfter(Instruction instruction)
        {
            return InsertAfter(instruction, "");
        }

        /// <summary>
        /// Tests if this selected instruction is the same instruction
        /// as another selected instruction.
        /// </summary>
        /// <param name="other">The other selected instruction.</param>
        /// <returns>
        /// <c>true</c> if this selected instruction is the same as
        /// the other selected instruction; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(SelectedInstruction other)
        {
            return Tag == other.Tag && Block.Graph == other.Block.Graph;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is SelectedInstruction
                && Equals((SelectedInstruction)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Block.Graph.GetHashCode() << 16) ^ Tag.GetHashCode();
        }
    }
}