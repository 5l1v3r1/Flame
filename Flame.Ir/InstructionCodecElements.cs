using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Instructions;
using Flame.TypeSystem;
using Loyc.Syntax;
using Pixie.Markup;

namespace Flame.Ir
{
    /// <summary>
    /// Defines codec elements for instruction prototypes.
    /// </summary>
    public static class InstructionCodecElements
    {
        /// <summary>
        /// A codec element for alloca-array instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<AllocaArrayPrototype, IReadOnlyList<LNode>> AllocaArray =
            new CodecElement<AllocaArrayPrototype, IReadOnlyList<LNode>>(
                "alloca_array", EncodeAllocaArray, DecodeAllocaArray);

        private static AllocaArrayPrototype DecodeAllocaArray(IReadOnlyList<LNode> data, DecoderState state)
        {
            return AllocaArrayPrototype.Create(state.DecodeType(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeAllocaArray(AllocaArrayPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.ResultType) };
        }

        /// <summary>
        /// A codec element for alloca instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<AllocaPrototype, IReadOnlyList<LNode>> Alloca =
            new CodecElement<AllocaPrototype, IReadOnlyList<LNode>>(
                "alloca", EncodeAlloca, DecodeAlloca);

        private static AllocaPrototype DecodeAlloca(IReadOnlyList<LNode> data, DecoderState state)
        {
            return AllocaPrototype.Create(state.DecodeType(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeAlloca(AllocaPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.ElementType) };
        }

        /// <summary>
        /// A codec element for constant instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<ConstantPrototype, IReadOnlyList<LNode>> Constant =
            new CodecElement<ConstantPrototype, IReadOnlyList<LNode>>(
                "const", EncodeConstant, DecodeConstant);

        private static ConstantPrototype DecodeConstant(IReadOnlyList<LNode> data, DecoderState state)
        {
            return ConstantPrototype.Create(state.DecodeConstant(data[0]), state.DecodeType(data[1]));
        }

        private static IReadOnlyList<LNode> EncodeConstant(ConstantPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.Value), state.Encode(value.ResultType) };
        }

        /// <summary>
        /// A codec element for call instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<CallPrototype, IReadOnlyList<LNode>> Call =
            new CodecElement<CallPrototype, IReadOnlyList<LNode>>(
                "call", EncodeCall, DecodeCall);

        private static CallPrototype DecodeCall(IReadOnlyList<LNode> data, DecoderState state)
        {
            return CallPrototype.Create(
                state.DecodeMethod(data[0]),
                state.DecodeMethodLookup(data[1]));
        }

        private static IReadOnlyList<LNode> EncodeCall(CallPrototype value, EncoderState state)
        {
            return new LNode[]
            {
                state.Encode(value.Callee),
                state.Encode(value.Lookup)
            };
        }

        /// <summary>
        /// A codec element for copy instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<CopyPrototype, IReadOnlyList<LNode>> Copy =
            new CodecElement<CopyPrototype, IReadOnlyList<LNode>>(
                "copy", EncodeCopy, DecodeCopy);

        private static CopyPrototype DecodeCopy(IReadOnlyList<LNode> data, DecoderState state)
        {
            return CopyPrototype.Create(state.DecodeType(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeCopy(CopyPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.ResultType) };
        }

        /// <summary>
        /// A codec element for indirect call instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<IndirectCallPrototype, IReadOnlyList<LNode>> IndirectCall =
            new CodecElement<IndirectCallPrototype, IReadOnlyList<LNode>>(
                "indirect_call", EncodeIndirectCall, DecodeIndirectCall);

        private static IndirectCallPrototype DecodeIndirectCall(IReadOnlyList<LNode> data, DecoderState state)
        {
            return IndirectCallPrototype.Create(
                state.DecodeType(data[0]),
                data.Slice<LNode>(1)
                    .EagerSelect<LNode, IType>(state.DecodeType));
        }

        private static IReadOnlyList<LNode> EncodeIndirectCall(IndirectCallPrototype value, EncoderState state)
        {
            var results = new List<LNode>();
            results.Add(state.Encode(value.ResultType));
            foreach (var paramType in value.ParameterTypes)
            {
                results.Add(state.Encode(paramType));
            }
            return results;
        }

        /// <summary>
        /// A codec element for load instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<LoadPrototype, IReadOnlyList<LNode>> Load =
            new CodecElement<LoadPrototype, IReadOnlyList<LNode>>(
                "load", EncodeLoad, DecodeLoad);

        private static LoadPrototype DecodeLoad(IReadOnlyList<LNode> data, DecoderState state)
        {
            return LoadPrototype.Create(state.DecodeType(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeLoad(LoadPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.ResultType) };
        }

        /// <summary>
        /// A codec element for new-delegate instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<NewDelegatePrototype, IReadOnlyList<LNode>> NewDelegate =
            new CodecElement<NewDelegatePrototype, IReadOnlyList<LNode>>(
                "new_delegate", EncodeNewDelegate, DecodeNewDelegate);

        private static NewDelegatePrototype DecodeNewDelegate(IReadOnlyList<LNode> data, DecoderState state)
        {
            return NewDelegatePrototype.Create(
                state.DecodeType(data[0]),
                state.DecodeMethod(data[1]),
                state.DecodeBoolean(data[2]),
                state.DecodeMethodLookup(data[3]));
        }

        private static IReadOnlyList<LNode> EncodeNewDelegate(NewDelegatePrototype value, EncoderState state)
        {
            return new LNode[]
            {
                state.Encode(value.ResultType),
                state.Encode(value.Callee),
                state.Encode(value.HasThisArgument),
                state.Encode(value.Lookup)
            };
        }


        /// <summary>
        /// A codec element for new-delegate instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<NewObjectPrototype, IReadOnlyList<LNode>> NewObject =
            new CodecElement<NewObjectPrototype, IReadOnlyList<LNode>>(
                "new_object", EncodeNewObject, DecodeNewObject);

        private static NewObjectPrototype DecodeNewObject(IReadOnlyList<LNode> data, DecoderState state)
        {
            return NewObjectPrototype.Create(state.DecodeMethod(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeNewObject(NewObjectPrototype value, EncoderState state)
        {
            return new LNode[]
            {
                state.Encode(value.Constructor)
            };
        }


        /// <summary>
        /// A codec element for reinterpret cast instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<ReinterpretCastPrototype, IReadOnlyList<LNode>> ReinterpretCast =
            new CodecElement<ReinterpretCastPrototype, IReadOnlyList<LNode>>(
                "reinterpret_cast", EncodeReinterpretCast, DecodeReinterpretCast);

        private static ReinterpretCastPrototype DecodeReinterpretCast(IReadOnlyList<LNode> data, DecoderState state)
        {
            var targetType = state.DecodeType(data[0]);
            if (targetType is PointerType)
            {
                return ReinterpretCastPrototype.Create((PointerType)targetType);
            }
            else
            {
                state.Log.LogSyntaxError(
                    data[0],
                    new Text("expected a pointer type."));
                return null;
            }
        }

        private static IReadOnlyList<LNode> EncodeReinterpretCast(ReinterpretCastPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.TargetType) };
        }

        /// <summary>
        /// A codec element for store instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<StorePrototype, IReadOnlyList<LNode>> Store =
            new CodecElement<StorePrototype, IReadOnlyList<LNode>>(
                "store", EncodeStore, DecodeStore);

        private static StorePrototype DecodeStore(IReadOnlyList<LNode> data, DecoderState state)
        {
            return StorePrototype.Create(state.DecodeType(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeStore(StorePrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.ResultType) };
        }

        /// <summary>
        /// Gets a codec that contains all sub-codecs defined in this class.
        /// </summary>
        /// <returns>A codec.</returns>
        public static Codec<InstructionPrototype, LNode> All
        {
            get
            {
                return new PiecewiseCodec<InstructionPrototype>()
                    .Add(AllocaArray)
                    .Add(Alloca)
                    .Add(Call)
                    .Add(Constant)
                    .Add(Copy)
                    .Add(IndirectCall)
                    .Add(Load)
                    .Add(NewDelegate)
                    .Add(NewObject)
                    .Add(ReinterpretCast)
                    .Add(Store);
            }
        }
    }
}
