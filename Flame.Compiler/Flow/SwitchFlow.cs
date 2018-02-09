using System.Collections.Generic;
using System.Collections.Immutable;
using Flame.Compiler.Constants;

namespace Flame.Compiler.Flow
{
    /// <summary>
    /// Switch flow, which tries to match a value against a list of
    /// constants in cases and takes an appropriate branch based on
    /// which case is selected, if any.
    /// </summary>
    public sealed class SwitchFlow : BlockFlow
    {
        /// <summary>
        /// Creates switch flow.
        /// </summary>
        /// <param name="switchValue">
        /// An instruction that produces the value to switch on.
        /// </param>
        /// <param name="cases">
        /// A list of switch cases.
        /// </param>
        /// <param name="defaultBranch">
        /// A branch to take if none of the switch cases match
        /// the value being switched on.
        /// </param>
        public SwitchFlow(
            Instruction switchValue,
            ImmutableList<SwitchCase> cases,
            Branch defaultBranch)
        {
            this.SwitchValue = switchValue;
            this.Cases = cases;
            this.DefaultBranch = defaultBranch;
            this.cachedBranchList = CreateBranchList();
        }

        /// <summary>
        /// Gets an instruction that produces the value to switch on.
        /// </summary>
        /// <returns>An instruction that produces the value to switch on.</returns>
        public Instruction SwitchValue { get; private set; }

        /// <summary>
        /// Gets the list of switch cases in this switch flow.
        /// </summary>
        /// <returns>A list of switch cases.</returns>
        public ImmutableList<SwitchCase> Cases { get; private set; }

        /// <summary>
        /// Gets the default branch, which is only taken when no case matches.
        /// </summary>
        /// <returns>The default branch.</returns>
        public Branch DefaultBranch { get; private set; }

        /// <inheritdoc/>
        public override IReadOnlyList<Instruction> Instructions
            => new Instruction[] { SwitchValue };

        /// <inheritdoc/>
        public override IReadOnlyList<Branch> Branches => cachedBranchList;

        private IReadOnlyList<Branch> cachedBranchList;

        private IReadOnlyList<Branch> CreateBranchList()
        {
            var results = new List<Branch>();
            foreach (var item in Cases)
            {
                results.Add(item.Branch);
            }
            results.Add(DefaultBranch);
            return results;
        }

        /// <inheritdoc/>
        public override BlockFlow WithBranches(IReadOnlyList<Branch> branches)
        {
            int branchCount = branches.Count;
            int caseCount = Cases.Count;

            ContractHelpers.Assert(
                branchCount == caseCount + 1,
                "Got '" + branchCount +
                "' branches when re-creating a switch statement, but expected '" +
                (caseCount + 1) + "'.");

            var newCases = ImmutableList<SwitchCase>.Empty.ToBuilder();

            for (int i = 0; i < caseCount; i++)
            {
                newCases.Add(new SwitchCase(Cases[i].Values, branches[i]));
            }
            
            return new SwitchFlow(
                SwitchValue,
                newCases.ToImmutable(),
                branches[caseCount]);
        }

        /// <inheritdoc/>
        public override BlockFlow WithInstructions(IReadOnlyList<Instruction> instructions)
        {
            ContractHelpers.Assert(instructions.Count == 1, "Switch flow takes exactly one instruction.");
            var newSwitchValue = instructions[0];
            if (object.ReferenceEquals(newSwitchValue, SwitchValue))
            {
                return this;
            }
            else
            {
                return new SwitchFlow(newSwitchValue, Cases, DefaultBranch);
            }
        }

        /// <summary>
        /// Creates switch flow that corresponds to if-else flow on
        /// a Boolean condition.
        /// </summary>
        /// <param name="condition">
        /// An instruction that produces Boolean condition.
        /// </param>
        /// <param name="ifBranch">
        /// The 'if' branch, which is taken when the value produced by the
        /// Boolean condition is not false.
        /// </param>
        /// <param name="ifBranch">
        /// The 'else' branch, which is taken when the value produced by the
        /// Boolean condition is false.
        /// </param>
        /// <returns>
        /// Switch flow that corresponds to if-else flow.
        /// </returns>
        public static SwitchFlow CreateIfElse(
            Instruction condition,
            Branch ifBranch,
            Branch elseBranch)
        {
            return new SwitchFlow(
                condition,
                ImmutableList<SwitchCase>.Empty.Add(
                    new SwitchCase(
                        ImmutableHashSet<Constant>.Empty.Add(
                            BooleanConstant.False),
                        elseBranch)),
                ifBranch);
        }
    }

    /// <summary>
    /// A case in switch flow.
    /// </summary>
    public struct SwitchCase
    {
        /// <summary>
        /// Creates a switch case from a set of values and a branch.
        /// </summary>
        /// <param name="values">A set of values for the switch case.</param>
        /// <param name="branch">A branch for the switch case.</param>
        public SwitchCase(ImmutableHashSet<Constant> values, Branch branch)
        {
            this = default(SwitchCase);
            this.Values = values;
            this.Branch = branch;
        }

        /// <summary>
        /// Gets a set of all values for this switch case. If control reaches
        /// this switch case and any of these values match the value being
        /// switched on, then control is redirected to this switch case's
        /// branch target.
        /// </summary>
        /// <returns>The switch case's values.</returns>
        public ImmutableHashSet<Constant> Values { get; private set; }

        /// <summary>
        /// Gets the branch that is taken when any of the values in this
        /// switch case match the value being switched on.
        /// </summary>
        /// <returns>The switch case's branch.</returns>
        public Branch Branch { get; private set; }
    }
}