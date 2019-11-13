using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using AsmResolver.DotNet.Collections;
using AsmResolver.Lazy;
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Metadata;
using AsmResolver.PE.DotNet.Metadata.Tables;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AsmResolver.PE.File;

namespace AsmResolver.DotNet
{
    /// <summary>
    /// Represents an assembly of self-describing modules of an executable file hosted by a common language runtime (CLR).
    /// </summary>
    public class AssemblyDefinition : IMetadataMember, IAssemblyName
    { 
        /// <summary>
        /// Reads a .NET assembly from the provided input buffer.
        /// </summary>
        /// <param name="buffer">The raw contents of the executable file to load.</param>
        /// <returns>The module.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the image does not contain a valid .NET metadata directory.</exception>
        public static AssemblyDefinition FromBytes(byte[] buffer) => FromImage(PEImage.FromBytes(buffer));
        
        /// <summary>
        /// Reads a .NET assembly from the provided input file.
        /// </summary>
        /// <param name="filePath">The file path to the input executable to load.</param>
        /// <returns>The module.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the image does not contain a valid .NET metadata directory.</exception>
        public static AssemblyDefinition FromFile(string filePath) => FromImage(PEImage.FromFile(filePath));

        /// <summary>
        /// Reads a .NET assembly from the provided input file.
        /// </summary>
        /// <param name="file">The portable executable file to load.</param>
        /// <returns>The module.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the image does not contain a valid .NET metadata directory.</exception>
        public static AssemblyDefinition FromFile(PEFile file) => FromImage(PEImage.FromFile(file));

        /// <summary>
        /// Reads a .NET assembly from an input stream.
        /// </summary>
        /// <param name="reader">The input stream pointing at the beginning of the executable to load.</param>
        /// <returns>The module.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the image does not contain a valid .NET metadata directory.</exception>
        public static AssemblyDefinition FromReader(IBinaryStreamReader reader) => FromImage(PEImage.FromReader(reader));
        
        /// <summary>
        /// Initializes a .NET assembly from a PE image.
        /// </summary>
        /// <param name="peImage">The image containing the .NET metadata.</param>
        /// <returns>The module.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the image does not contain a valid .NET metadata directory.</exception>
        public static AssemblyDefinition FromImage(IPEImage peImage)
        {
            if (peImage.DotNetDirectory == null)
                throw new BadImageFormatException("Input PE image does not contain a .NET directory.");
            if (peImage.DotNetDirectory.Metadata == null)
                throw new BadImageFormatException("Input PE image does not contain a .NET metadata directory.");
            return FromMetadata(peImage.DotNetDirectory.Metadata);
        }

        /// <summary>
        /// Initializes a .NET module from a .NET metadata directory.
        /// </summary>
        /// <param name="metadata">The object providing access to the underlying metadata streams.</param>
        /// <returns>The module.</returns>
        public static AssemblyDefinition FromMetadata(IMetadata metadata) =>
            ModuleDefinition.FromMetadata(metadata).Assembly;
        
        private readonly LazyVariable<string> _name;
        private readonly LazyVariable<string> _culture;
        private readonly LazyVariable<byte[]> _publicKey;
        private IList<ModuleDefinition> _modules;

        /// <summary>
        /// Initializes a new assembly definition.
        /// </summary>
        /// <param name="token">The token of the assembly definition.</param>
        protected AssemblyDefinition(MetadataToken token)
        {
            MetadataToken = token;
            _name = new LazyVariable<string>(GetName);
            _culture = new LazyVariable<string>(GetCulture);
            _publicKey = new LazyVariable<byte[]>(GetPublicKey);
        }

        /// <summary>
        /// Creates a new assembly definition.
        /// </summary>
        /// <param name="name">The name of the assembly.</param>
        /// <param name="version">The version of the assembly.</param>
        public AssemblyDefinition(string name, Version version)
            : this(new MetadataToken(TableIndex.Assembly, 1))
        {
            Name = name;
            Version = version;
        }

        /// <inheritdoc />
        public MetadataToken MetadataToken
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the name of the assembly.
        /// </summary>
        /// <remarks>
        /// This property corresponds to the Name column in the assembly definition table. 
        /// </remarks>
        public string Name
        {
            get => _name.Value;
            set => _name.Value = value;
        }

        /// <summary>
        /// Gets or sets the version of the assembly.
        /// </summary>
        /// <remarks>
        /// This property corresponds to the MajorVersion, MinorVersion, BuildNumber and RevisionNumber columns in the assembly definition table. 
        /// </remarks>
        public Version Version
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the attributes associated to the assembly.
        /// </summary>
        /// <remarks>
        /// This property corresponds to the Attributes column in the assembly definition table. 
        /// </remarks>
        public AssemblyAttributes Attributes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the assembly holds the full (unhashed) public key.
        /// </summary>
        /// <remarks>
        /// This property does not automatically update after <see cref="PublicKey"/> was updated.
        /// </remarks>
        public bool HasPublicKey
        {
            get => (Attributes & AssemblyAttributes.PublicKey) != 0;
            set => Attributes = (Attributes & ~AssemblyAttributes.PublicKey)
                                | (value ? AssemblyAttributes.PublicKey : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating just-in-time (JIT) compiler tracking is enabled for the assembly.
        /// </summary>
        /// <remarks>
        /// This attribute originates from the <see cref="DebuggableAttribute"/> attribute.
        /// </remarks>
        public bool EnableJitCompileTracking 
        {
            get => (Attributes & AssemblyAttributes.EnableJitCompileTracking) != 0;
            set => Attributes = (Attributes & ~AssemblyAttributes.EnableJitCompileTracking)
                                | (value ? AssemblyAttributes.EnableJitCompileTracking : 0);
        }

        /// <summary>
        /// Gets or sets a value indicating any just-in-time (JIT) compiler optimization is disabled for the assembly.
        /// This is the exact opposite of the meaning that is suggested by the member name.
        /// </summary>
        /// <remarks>
        /// This attribute originates from the <see cref="DebuggableAttribute"/> attribute.
        /// </remarks>
        public bool DisableJitCompileOptimizer
        {
            get => (Attributes & AssemblyAttributes.DisableJitCompileOptimizer) != 0;
            set => Attributes = (Attributes & ~AssemblyAttributes.DisableJitCompileOptimizer)
                                | (value ? AssemblyAttributes.DisableJitCompileOptimizer : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the assembly contains Windows Runtime (WinRT) code or not.
        /// </summary>
        public bool IsWindowsRuntime
        {
            get => (Attributes & AssemblyAttributes.ContentMask) == AssemblyAttributes.ContentWindowsRuntime;
            set => Attributes = (Attributes & ~AssemblyAttributes.ContentMask)
                                | (value ? AssemblyAttributes.ContentWindowsRuntime : 0);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the assembly can be retargeted (at runtime) to an assembly from
        /// a different publisher.
        /// </summary>
        public bool IsRetargetable
        {
            get => (Attributes & AssemblyAttributes.Retargetable) != 0;
            set => Attributes = (Attributes & ~AssemblyAttributes.Retargetable)
                                | (value ? AssemblyAttributes.Retargetable : 0);
        }
        
        /// <summary>
        /// Gets or sets the locale string of the assembly (if available).
        /// </summary>
        /// <remarks>
        /// <para>If this value is set to <c>null</c>, the default locale will be used</para>
        /// <para>This property corresponds to the Culture column in the assembly definition table.</para> 
        /// </remarks>
        public string Culture
        {
            get => _culture.Value;
            set => _culture.Value = value;
        }

        /// <summary>
        /// Gets or sets the public key of the assembly to use for verification of a signature.
        /// </summary>
        /// <remarks>
        /// <para>If this value is set to <c>null</c>, no public key will be assigned.</para>
        /// <para>This property does not automatically update the <see cref="HasPublicKey"/> property.</para>
        /// <para>This property corresponds to the Culture column in the assembly definition table.</para> 
        /// </remarks>
        public byte[] PublicKey
        {
            get => _publicKey.Value;
            set => _publicKey.Value = value;
        }

        /// <summary>
        /// Gets the main module of the .NET assembly containing the assembly's manifest. 
        /// </summary>
        public ModuleDefinition ManifestModule => Modules.Count > 0 ? Modules[0] : null;

        /// <summary>
        /// Gets a collection of modules that this .NET assembly defines.
        /// </summary>
        public IList<ModuleDefinition> Modules
        {
            get
            {
                if (_modules == null)
                    Interlocked.CompareExchange(ref _modules, GetModules(), null);
                return _modules;
            }
        }

        /// <inheritdoc />
        public byte[] GetPublicKeyToken()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obtains the name of the assembly definition.
        /// </summary>
        /// <returns>The name.</returns>
        /// <remarks>
        /// This method is called upon initializing the <see cref="Name"/> property.
        /// </remarks>
        protected virtual string GetName() => null;

        /// <summary>
        /// Obtains the locale string of the assembly definition.
        /// </summary>
        /// <returns>The locale string.</returns>
        /// <remarks>
        /// This method is called upon initializing the <see cref="Culture"/> property.
        /// </remarks>
        protected virtual string GetCulture() => null;

        /// <summary>
        /// Obtains the public key of the assembly definition.
        /// </summary>
        /// <returns>The public key.</returns>
        /// <remarks>
        /// This method is called upon initializing the <see cref="PublicKey"/> property.
        /// </remarks>
        protected virtual byte[] GetPublicKey() => null;

        /// <summary>
        /// Obtains the list of defined modules in the .NET assembly. 
        /// </summary>
        /// <returns>The modules.</returns>
        /// <remarks>
        /// This method is called upon initializing the <see cref="Modules"/> and/or <see cref="ManifestModule"/> property.
        /// </remarks>
        protected virtual IList<ModuleDefinition> GetModules()
            => new OwnedCollection<AssemblyDefinition, ModuleDefinition>(this);
    }
}