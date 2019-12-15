using System;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace AsmResolver.DotNet.Blob
{
    /// <summary>
    /// Represents a blob type signature referencing an element type defined in a common object runtime library such as
    /// mscorlib (.NET framework) or System.Private.CorLib (.NET Core).
    /// </summary>
    public class CorLibTypeSignature : TypeSignature
    {
        internal CorLibTypeSignature(IResolutionScope corlibScope, ElementType elementType, string name)
        {
            Scope = corlibScope;
            ElementType = elementType;
            Name = name;
        }

        /// <inheritdoc />
        public override string Name
        {
            get;
        }

        /// <inheritdoc />
        public override string Namespace => "System";

        /// <inheritdoc />
        public override bool IsValueType =>
            ElementType switch
            {
                ElementType.String => false,
                ElementType.Object => false,
                ElementType.Void => true,
                ElementType.Boolean => true,
                ElementType.Char => true,
                ElementType.I1 => true,
                ElementType.U1 => true,
                ElementType.I2 => true,
                ElementType.U2 => true,
                ElementType.I4 => true,
                ElementType.U4 => true,
                ElementType.I8 => true,
                ElementType.U8 => true,
                ElementType.R4 => true,
                ElementType.R8 => true,
                ElementType.TypedByRef => true,
                ElementType.I => true,
                ElementType.U => true,
                _ => throw new ArgumentOutOfRangeException()
            };

        /// <inheritdoc />
        public override ElementType ElementType
        {
            get;
        }

        /// <inheritdoc />
        public override IResolutionScope Scope
        {
            get;
        }

        /// <inheritdoc />
        public override uint GetPhysicalSize() => sizeof(ElementType);

        /// <inheritdoc />
        public override void Write(IBinaryStreamWriter writer) => writer.WriteByte((byte) ElementType);
        
    }
}