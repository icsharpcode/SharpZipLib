#if NET35
namespace System.IO
{
	static class StreamExtensions
	{
		public static void CopyTo(this Stream fromStream, Stream toStream)

		{

			if (fromStream == null)

				throw new ArgumentNullException("fromStream");

			if (toStream == null)

				throw new ArgumentNullException("toStream");



			var bytes = new byte[8092];

			int dataRead;

			while ((dataRead = fromStream.Read(bytes, 0, bytes.Length)) > 0)

				toStream.Write(bytes, 0, dataRead);

		}
	}
}
#endif
