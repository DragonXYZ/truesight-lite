//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Truesight.Parser.Api.Ops
{
    // newobj, newarr
    [global::Truesight.Parser.Impl.Ops.OpCodesAttribute(0x73, 0x8d)]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public sealed class New : global::Truesight.Parser.Impl.ILOp
    {
        public override global::Truesight.Parser.Api.IILOpType OpType { get { return global::Truesight.Parser.Api.IILOpType.New; } }

        private readonly int? _ctorToken;
        private readonly int? _typeToken;

        internal New(global::Truesight.Parser.Impl.MethodBody source, global::System.IO.BinaryReader reader)
            : this(source, reader, global::XenoGears.Functional.EnumerableExtensions.ToReadOnly(global::System.Linq.Enumerable.Empty<global::Truesight.Parser.Impl.ILOp>()))
        {
        }

        internal New(global::Truesight.Parser.Impl.MethodBody source, global::System.IO.BinaryReader reader, global::System.Collections.ObjectModel.ReadOnlyCollection<global::Truesight.Parser.Impl.ILOp> prefixes)
            : base(source, AssertSupportedOpCode(reader), (int)reader.BaseStream.Position - global::System.Linq.Enumerable.Sum(global::System.Linq.Enumerable.Select(prefixes ?? global::XenoGears.Functional.EnumerableExtensions.ToReadOnly(global::System.Linq.Enumerable.Empty<global::Truesight.Parser.Impl.ILOp>()), prefix => prefix.Size)), prefixes ?? global::XenoGears.Functional.EnumerableExtensions.ToReadOnly(global::System.Linq.Enumerable.Empty<global::Truesight.Parser.Impl.ILOp>()))
        {
            // this is necessary for further verification
            var origPos = reader.BaseStream.Position;

            // initializing _ctorToken
            switch((ushort)OpSpec.OpCode.Value)
            {
                case 0x73: //newobj
                    _ctorToken = ReadMetadataToken(reader);
                    break;
                case 0x8d: //newarr
                    _ctorToken = default(int?);
                    break;
                default:
                    throw global::XenoGears.Assertions.AssertionHelper.Fail();
            }

            // initializing _typeToken
            switch((ushort)OpSpec.OpCode.Value)
            {
                case 0x73: //newobj
                    _typeToken = default(int?);
                    break;
                case 0x8d: //newarr
                    _typeToken = ReadMetadataToken(reader);
                    break;
                default:
                    throw global::XenoGears.Assertions.AssertionHelper.Fail();
            }

            // verify that we've read exactly the amount of bytes we should
            var bytesRead = reader.BaseStream.Position - origPos;
            global::XenoGears.Assertions.AssertionHelper.AssertTrue(bytesRead == SizeOfOperand);

            // now when the initialization is completed verify that we've got only prefixes we support
            global::XenoGears.Assertions.AssertionHelper.AssertAll(Prefixes, prefix => 
            {
                return false;
            });
        }

        private static global::System.Reflection.Emit.OpCode AssertSupportedOpCode(global::System.IO.BinaryReader reader)
        {
            var opcode = global::Truesight.Parser.Impl.Reader.OpCodeReader.ReadOpCode(reader);
            global::XenoGears.Assertions.AssertionHelper.AssertNotNull(opcode);
            // newobj, newarr
            global::XenoGears.Assertions.AssertionHelper.AssertTrue(global::System.Linq.Enumerable.Contains(new ushort[]{0x73, 0x8d}, (ushort)opcode.Value.Value));

            return opcode.Value;
        }


        public global::System.Reflection.ConstructorInfo Ctor
        {
            get
            {
                switch((ushort)OpSpec.OpCode.Value)
                {
                    case 0x73: //newobj
                        return CtorFromToken(global::XenoGears.Assertions.AssertionHelper.AssertValue(_ctorToken));
                    case 0x8d: //newarr
                        return Type != null ? global::XenoGears.Assertions.AssertionHelper.AssertSingle(Type.GetConstructors()) : null;
                    default:
                        throw global::XenoGears.Assertions.AssertionHelper.Fail();
                }
            }
        }

        public int? CtorToken
        {
            get
            {
                return _ctorToken;
            }
        }

        public global::System.Type Type
        {
            get
            {
                switch((ushort)OpSpec.OpCode.Value)
                {
                    case 0x73: //newobj
                        return Ctor != null ? Ctor.DeclaringType : null;
                    case 0x8d: //newarr
                        var elementType = TypeFromToken(global::XenoGears.Assertions.AssertionHelper.AssertValue(_typeToken));
                        return elementType != null ? elementType.MakeArrayType() : null;
                    default:
                        throw global::XenoGears.Assertions.AssertionHelper.Fail();
                }
            }
        }

        public int? TypeToken
        {
            get
            {
                return _typeToken;
            }
        }

        public override global::System.String ToString()
        {
            var offset = OffsetToString(Offset) + ":";
            var prefixSpec = Prefixes.Count == 0 ? "" : ("[" + global::XenoGears.Functional.EnumerableExtensions.StringJoin(Prefixes) + "]");
            var name =  "new";
            var mods = new global::System.Collections.Generic.List<global::System.String>();
            var modSpec = global::XenoGears.Functional.EnumerableExtensions.StringJoin(global::System.Linq.Enumerable.Where(mods, mod => global::XenoGears.Functional.EnumerableExtensions.IsNeitherNullNorEmpty(mod)), ", ");
            var operand = ((Ctor != null ? ConstructorInfoToString(Ctor) : (Type != null ? TypeToString(Type) : null)) ?? (((OpSpec.OpCode.Value == 0x8d /*newarr*/ ? "arr of " : "") + ("0x" + (_ctorToken ?? _typeToken).Value.ToString("x8")))));

            var parts = new []{offset, prefixSpec, name, modSpec, operand};
            var result = global::XenoGears.Functional.EnumerableExtensions.StringJoin(global::System.Linq.Enumerable.Where(parts, p => global::XenoGears.Functional.EnumerableExtensions.IsNeitherNullNorEmpty(p)), " ");

            return result;
        }
    }
}