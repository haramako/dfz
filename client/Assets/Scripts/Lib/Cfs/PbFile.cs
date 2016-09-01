using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Master;
using Google.ProtocolBuffers;

public class PbFile {

	public static IEnumerable<TMessage> ReadPbList<TMessage, TBuilder>(TMessage prototype, Stream s)
		where TMessage : IMessage<TMessage, TBuilder>
		where TBuilder : IBuilder<TMessage, TBuilder>
	{
		var r = new BinaryReader (s);
		var magic = r.ReadByte ();
		if( magic != 'C' ) throw new ArgumentException("Invalid stream header");
		var headerSize = r.ReadByte ();
		r.ReadBytes (headerSize); // 読み飛ばすだけ

		var sizeBuf = new byte[4];
		var builder = prototype.ToBuilder ();
		TMessage message;
		while( true ){
			var len = r.Read (sizeBuf,0,4);
			if (len < 4 ) break; // EOF
			var size = sizeBuf[0] | (sizeBuf[1] << 8) | (sizeBuf[2] << 16) | (sizeBuf[3] << 24);
			// TODO: サイズ０（すべてデフォルト値）の時に問題がおこるので対策を案が得ること
			if (size <= 0) break; // 0 パディングされている場合
			message = builder.Clear().MergeFrom(new LimitedInputStream(s, size)).Build();
			yield return message;
		}
	}

	public static IEnumerable<TMessage> ReadPbListFiles<TMessage,TBuilder>(Cfs.Cfs cfs, TMessage prototype, string filename )
		where TMessage : IMessage<TMessage, TBuilder>
		where TBuilder : IBuilder<TMessage, TBuilder>
	{
		IEnumerable<TMessage> result = new TMessage[0];
		int filecount = 0;
		foreach (var file in cfs.bucket.Files.Values) {
			if (file.Filename.EndsWith ("-" + filename + ".pb")) {
				Configure.Log("MasterLoadingLog", "loading " + file.Filename + "...");
				filecount++;
				using (var stream = new BufferedStream(cfs.GetStream (file.Filename), 8192)) {
					result = result.Concat(ReadPbList<TMessage, TBuilder>(prototype, stream).ToArray());
				}
			}
		}
		if (filecount <= 0) {
			Debug.LogError ("マスターファイルが一つも見つかりません。 filename = " + filename);
		}
		return result;
	}


	// Copy From Google.ProtocolBuffers.AbstractBuilderList.cs

	/// <summary>
	/// Stream implementation which proxies another stream, only allowing a certain amount
	/// of data to be read. Note that this is only used to read delimited streams, so it
	/// doesn't attempt to implement everything.
	/// </summary>
	public sealed class LimitedInputStream : Stream
	{
		private readonly Stream proxied;
		private int bytesLeft;

		internal LimitedInputStream(Stream proxied, int size)
		{
			this.proxied = proxied;
			bytesLeft = size;
		}

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override void Flush()
		{
		}

		public override long Length
		{
			get { throw new NotSupportedException(); }
		}

		public override long Position
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (bytesLeft > 0)
			{
				int bytesRead = proxied.Read(buffer, offset, Math.Min(bytesLeft, count));
				bytesLeft -= bytesRead;
				return bytesRead;
			}
			return 0;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}
	}




}
