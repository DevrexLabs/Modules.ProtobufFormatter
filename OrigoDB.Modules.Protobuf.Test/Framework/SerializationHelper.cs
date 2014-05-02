using System.IO;
using OrigoDB.Modules.ProtoBuf;

namespace Modules.ProtoBuf.Test.Framework
{
    internal static class SerializationHelper
    {
        internal static Stream Serialize<T>(T instance, ProtoBufFormatter formatter = null)
        {
            formatter = formatter ?? new ProtoBufFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, instance);
            stream.Position = 0;
            return stream;
        }

        internal static T Deserialize<T>(Stream stream, ProtoBufFormatter formatter = null)
        {
            formatter = formatter ?? new ProtoBufFormatter();
            return (T)formatter.Deserialize(stream);
        }

        internal static T Clone<T>(T item, ProtoBufFormatter formatter = null)
        {
            return Deserialize<T>(Serialize(item, formatter), formatter);
        }
    }
}
