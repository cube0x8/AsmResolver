using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace AsmResolver.DotNet.Blob
{
    /// <summary>
    /// Represents a type signature that describes a type that is passed on by reference.
    /// </summary>
    public class ByReferenceTypeSignature : TypeSpecificationSignature
    {
        /// <summary>
        /// Reads a by-reference type signature from an input stream.
        /// </summary>
        /// <param name="parentModule">The module containing the signature.</param>
        /// <param name="reader">The input stream.</param>
        /// <returns>The signature.</returns>
        public new static ByReferenceTypeSignature FromReader(ModuleDefinition parentModule, IBinaryStreamReader reader)
        {
            return FromReader(parentModule, reader, RecursionProtection.CreateNew());
        }
        
        /// <summary>
        /// Reads a by-reference type signature from an input stream.
        /// </summary>
        /// <param name="parentModule">The module containing the signature.</param>
        /// <param name="reader">The input stream.</param>
        /// <param name="protection">The object instance responsible for detecting infinite recursion.</param>
        /// <returns>The signature.</returns>
        public new static ByReferenceTypeSignature FromReader(ModuleDefinition parentModule, IBinaryStreamReader reader,
            RecursionProtection protection)
        {
            return new ByReferenceTypeSignature(TypeSignature.FromReader(parentModule, reader, protection));
        }
        
        /// <summary>
        /// Creates a new by reference type signature.
        /// </summary>
        /// <param name="baseType">The type that is passed on by reference.</param>
        public ByReferenceTypeSignature(TypeSignature baseType) 
            : base(baseType)
        {
        }

        /// <inheritdoc />
        public override ElementType ElementType => ElementType.ByRef;

        /// <inheritdoc />
        public override string Name => BaseType.Name + "&";

        /// <inheritdoc />
        public override bool IsValueType => false;
    }
}