﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JSIL;
using JSIL.Ast;
using Mono.Cecil;

namespace WasmSExprEmitter {
    public abstract class SExpression : JSExpression {
        public readonly string Keyword;
        public bool LineBreakAfter, LineBreakInside;

        public SExpression (string keyword, params JSExpression[] values)
            : base (values) {
            Keyword = keyword;
        }

        internal virtual void BeforeWriteValues (AstEmitter astEmitter) {
        }

        internal virtual void AfterWriteValues (AstEmitter astEmitter) {
        }

        public override string ToString () {
            return string.Format("({0} {1})", Keyword, string.Join<JSExpression>(", ", Values));
        }
    }

    public class AbstractSExpression : SExpression {
        public  readonly TypeReference Type;
        private readonly bool _IsConstant;
        public  readonly bool IsConstantIfArgumentsAre;
        private readonly bool _HasGlobalStateDependency;

        public AbstractSExpression (
            string keyword, TypeReference type, JSExpression[] values,
            bool isConstant = false, bool isConstantIfArgumentsAre = false,
            bool hasGlobalStateDependency = false, 
            bool lineBreakAfter = false, bool lineBreakInside = false
        ) : base (keyword, values) {
            Type = type;
            _IsConstant = isConstant;
            IsConstantIfArgumentsAre = isConstantIfArgumentsAre;
            _HasGlobalStateDependency = hasGlobalStateDependency;
            LineBreakAfter = lineBreakAfter;
            LineBreakInside = lineBreakInside;
        }

        public override bool HasGlobalStateDependency {
            get {
                return _HasGlobalStateDependency;
            }
        }

        public override bool IsConstant {
            get {
                if (_IsConstant)
                    return true;

                if (IsConstantIfArgumentsAre && Values.All(v => v.IsConstant))
                    return true;

                return false;
            }
        }

        public override TypeReference GetActualType (TypeSystem typeSystem) {
            return Type;
        }

        public override bool IsLValue {
            get {
                return false;
            }
        }
    }

    public class GetMemory : SExpression {
        public readonly TypeReference Type;
        public readonly bool IsAligned;

        public GetMemory (
            TypeReference type, bool isAligned,
            JSExpression addressInBytes
        ) : base (
            string.Format(
                "{0}.load/{1}{2}",
                WasmUtil.PickTypeKeyword(type),
                WasmUtil.PickMemoryTypeKeyword(type),
                isAligned
                    ? ""
                    : "/1"
            ),
            addressInBytes
        ) {
            Type = type;
            IsAligned = isAligned;
        }

        public override TypeReference GetActualType (TypeSystem typeSystem) {
            return Type;
        }

        public override bool HasGlobalStateDependency {
            get {
                return true;
            }
        }

        public override bool IsConstant {
            get {
                return false;
            }
        }
    }

    public class SetMemory : SExpression {
        public readonly TypeReference Type;
        public readonly bool IsAligned;

        public SetMemory (
            TypeReference type, bool isAligned,
            JSExpression addressInBytes, JSExpression value
        ) : base (
            string.Format(
                "{0}.store/{1}{2}",
                WasmUtil.PickTypeKeyword(type),
                WasmUtil.PickMemoryTypeKeyword(type),
                isAligned
                    ? ""
                    : "/1"
            ),
            addressInBytes, value
        ) {
            Type = type;
            IsAligned = isAligned;

            LineBreakInside = true;
            LineBreakAfter = true;
        }

        public override TypeReference GetActualType (TypeSystem typeSystem) {
            return Type;
        }

        public override bool HasGlobalStateDependency {
            get {
                return true;
            }
        }

        public override bool IsConstant {
            get {
                return false;
            }
        }
    }

    public class InvokeExport : SExpression {
        public readonly string ExportedFunctionName;

        public InvokeExport (string exportedFunctionName, JSExpression[] arguments)
            : base ("invoke", arguments) {

            ExportedFunctionName = exportedFunctionName;
        }

        internal override void BeforeWriteValues (AstEmitter astEmitter) {
            astEmitter.Formatter.Value(ExportedFunctionName);
            astEmitter.Formatter.Space();
        }

        public override TypeReference GetActualType (TypeSystem typeSystem) {
            return typeSystem.Object;
        }
    }

    public class AssertEq : SExpression {
        public readonly string ExportedFunctionName;

        public AssertEq (JSExpression expected, string exportedFunctionName, JSExpression[] arguments)
            : base (
                "assert_eq", 
                (new[] { expected }).Concat(arguments).ToArray()
            ) {

            ExportedFunctionName = exportedFunctionName;
        }

        public JSExpression Expected {
            get {
                return Values[0];
            }
        }

        public IEnumerable<JSExpression> Arguments {
            get {
                return Values.Skip(1);
            }
        }

        public override TypeReference GetActualType (TypeSystem typeSystem) {
            return typeSystem.Void;
        }
    }

    public class AssertHeapEq : SExpression {
        public readonly int Offset, Count;
        public readonly string Expected;

        public AssertHeapEq (int offset, int count, string expected)
            : base (
                "assert_heap_eq"
            ) {

            Offset = offset;
            Count = count;
            Expected = expected;
        }

        public override TypeReference GetActualType (TypeSystem typeSystem) {
            return typeSystem.Void;
        }
    }

    public class GetStringLength : JSExpression {
        public GetStringLength (JSExpression str)
            : base(str) {
        }

        public JSExpression String {
            get {
                return Values[0];
            }
        }

        public override TypeReference GetActualType (TypeSystem typeSystem) {
            return typeSystem.Int32;
        }
    }
}
