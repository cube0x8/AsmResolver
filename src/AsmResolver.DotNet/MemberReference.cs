using AsmResolver.DotNet.Blob;
using AsmResolver.Lazy;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace AsmResolver.DotNet
{
    /// <summary>
    /// Represents a reference to a method or a field in an (external) .NET assembly.
    /// </summary>
    public class MemberReference : IMetadataMember, IMemberDescriptor
    {
        private readonly LazyVariable<IMemberRefParent> _parent;
        private readonly LazyVariable<string> _name;
        private readonly LazyVariable<CallingConventionSignature> _signature;

        /// <summary>
        /// Initializes a new member reference.
        /// </summary>
        /// <param name="token">The metadata token of the reference.</param>
        protected MemberReference(MetadataToken token)
        {
            MetadataToken = token;
            
            _parent = new LazyVariable<IMemberRefParent>(GetParent);
            _name = new LazyVariable<string>(GetName);
            _signature = new LazyVariable<CallingConventionSignature>(GetSignature);
        }

        /// <summary>
        /// Creates a new reference to a member in an (external) .NET assembly. 
        /// </summary>
        /// <param name="parent">The declaring member that defines the referenced member.</param>
        /// <param name="name">The name of the referenced member.</param>
        /// <param name="signature">The signature of the referenced member. This dictates whether the
        /// referenced member is a field or a method.</param>
        public MemberReference(IMemberRefParent parent, string name, MemberSignature signature)
            : this(new MetadataToken(TableIndex.MemberRef, 0))
        {
            Parent = parent;
            Name = name;
            Signature = signature;
        }

        /// <inheritdoc />
        public MetadataToken MetadataToken
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the member that declares the referenced member.
        /// </summary>
        public IMemberRefParent Parent
        {
            get => _parent.Value;
            set => _parent.Value = value;
        }

        /// <summary>
        /// Gets or sets the name of the referenced member.
        /// </summary>
        public string Name
        {
            get => _name.Value;
            set => _name.Value = value;
        }

        /// <summary>
        /// Gets or sets the signature of the referenced member.
        /// </summary>
        /// <remarks>
        /// This property dictates whether the referenced member is a field or a method.
        /// </remarks>
        public CallingConventionSignature Signature
        {
            get => _signature.Value;
            set => _signature.Value = value;
        }

        /// <summary>
        /// Gets a value indicating whether the referenced member is a field.
        /// </summary>
        public bool IsField => Signature is FieldSignature;

        /// <summary>
        /// Gets a value indicating whether the referenced member is a method
        /// </summary>
        public bool IsMethod => Signature is MethodSignature;

        /// <inheritdoc />
        public string FullName
        {
            get
            {
                if (IsField)
                    return FullNameGenerator.GetFieldFullName(Name, DeclaringType, (FieldSignature) Signature);
                if (IsMethod)
                    return FullNameGenerator.GetMethodFullName(Name, DeclaringType, (MethodSignature) Signature);
                return Name;
            }
        }

        /// <inheritdoc />
        public ModuleDefinition Module => DeclaringType.Module; // TODO: handle module ref.

        /// <summary>
        /// Gets the type that declares the referenced member, if available.
        /// </summary>
        public ITypeDefOrRef DeclaringType =>
            Parent switch
            {
                ITypeDefOrRef typeDefOrRef => typeDefOrRef,
                MethodDefinition method => method.DeclaringType,
                _ => null
            };

        ITypeDescriptor IMemberDescriptor.DeclaringType => DeclaringType;
        
        /// <summary>
        /// Obtains the parent of the member reference.
        /// </summary>
        /// <returns>The parent</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Parent"/> property.
        /// </remarks>
        protected virtual IMemberRefParent GetParent() => null;

        /// <summary>
        /// Obtains the name of the member reference.
        /// </summary>
        /// <returns>The name.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Name"/> property.
        /// </remarks>
        protected virtual string GetName() => null;

        /// <summary>
        /// Obtains the signature of the member reference.
        /// </summary>
        /// <returns>The signature</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Signature"/> property.
        /// </remarks>
        protected virtual CallingConventionSignature GetSignature() => null;

        /// <inheritdoc />
        public override string ToString() => FullName;
    }
}