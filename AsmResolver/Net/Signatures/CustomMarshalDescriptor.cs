﻿using System;
using AsmResolver.Net.Emit;

namespace AsmResolver.Net.Signatures
{
    public class CustomMarshalDescriptor : MarshalDescriptor
    {
        public static CustomMarshalDescriptor FromReader(IBinaryStreamReader reader)
        {
            var descriptor = new CustomMarshalDescriptor();

            Guid guid;
            Guid.TryParse(reader.ReadSerString(), out guid);
            descriptor.Guid = guid;

            descriptor.UnmanagedType = reader.ReadSerString();
            descriptor.ManagedType = reader.ReadSerString();
            descriptor.Cookie = reader.ReadSerString();

            return descriptor;
        }

        public override NativeType NativeType => NativeType.CustomMarshaler;

        public Guid Guid
        {
            get;
            set;
        }

        public string UnmanagedType
        {
            get;
            set;
        }

        public string ManagedType
        {
            get;
            set;
        }

        public string Cookie
        {
            get;
            set;
        }

        public override uint GetPhysicalLength(MetadataBuffer buffer)
        {
            return sizeof(byte) +
                   sizeof(byte) + 38 +
                   UnmanagedType.GetSerStringSize() +
                   ManagedType.GetSerStringSize() +
                   Cookie.GetSerStringSize() +
                   base.GetPhysicalLength(buffer);

        }

        public override void Prepare(MetadataBuffer buffer)
        {
        }

        public override void Write(MetadataBuffer buffer, IBinaryStreamWriter writer)
        {
            writer.WriteByte((byte)NativeType);
            writer.WriteSerString(Guid.ToString("B"));
            writer.WriteSerString(UnmanagedType);
            writer.WriteSerString(ManagedType);
            writer.WriteSerString(Cookie);

            base.Write(buffer, writer);
        }
    }
}