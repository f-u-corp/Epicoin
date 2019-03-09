using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using NUnit.Framework;

namespace Epicoin {

	[TestFixture]
	public class BitBufferTest {

		[Test(TestOf = typeof(BitBuffer))]
		public void TestBitBufferRW(){
			Random rnd = new Random();
			bool bo = rnd.Next(100) < 50;
			byte b = (byte) rnd.Next(255);
			char ch = (char) rnd.Next(0xFF);
			int i = rnd.Next();
			uint ui = (uint) rnd.Next();
			long l = (((long) rnd.Next()) << 32) | ((long) rnd.Next());
			ulong ul = (((ulong) rnd.Next()) << 32) | ((ulong) rnd.Next());
			float f = (float) rnd.NextDouble();
			double d = rnd.NextDouble();
			long masked = (((long) rnd.Next()) << 32) | ((long) rnd.Next()); int mask = rnd.Next(12, 53);

			BitBuffer bb = new BitBuffer();
			bb.write(bo);
			bb.writeByte(b);
			bb.writeChar(ch);
			bb.writeInt(i);
			bb.writeUInt(ui);
			bb.writeLong(l);
			bb.writeULong(ul);
			bb.writeFloat(f);
			bb.writeDouble(d);
			bb.writeBits(masked, mask);
			bb.flip();
			Assert.AreEqual(bo, bb.read(), "Bool read/write failed.");
			Assert.AreEqual(b, bb.readByte(), "Byte read/write failed.");
			Assert.AreEqual(ch, bb.readChar(), "Char read/write failed.");
			Assert.AreEqual(i, bb.readInt(), "Int read/write failed.");
			Assert.AreEqual(ui, bb.readUInt(), "UInt read/write failed.");
			Assert.AreEqual(l, bb.readLong(), "Long read/write failed.");
			Assert.AreEqual(ul, bb.readULong(), "ULong read/write failed.");
			Assert.AreEqual(f, bb.readFloat(), "Float read/write failed.");
			Assert.AreEqual(d, bb.readDouble(), "Double read/write failed.");
			Assert.AreEqual(masked & ((1<<mask)-1), bb.readBitsL(mask), "Arbitrary bit count read/write failed.");
		}

		[Test(TestOf = typeof(BitBuffer))]
		public void TestBitBufferIO(){
			Random rnd = new Random();
			byte len = (byte) rnd.Next(55, 175);
			List<int> data = Enumerable.Repeat(0, len).Select(i => rnd.Next()).ToList();

			BitBuffer wb = new BitBuffer();
			wb.writeByte(len);
			foreach(int i in data) wb.writeInt(i);
			wb.flip();

			byte[] raw = wb.CopyTo();

			BitBuffer rb = new BitBuffer(raw);
			byte rlen = rb.readByte();
			List<int> rdata = Enumerable.Repeat(0, rlen).Select(i => rb.readInt()).ToList();

			Assert.AreEqual(data, rdata, "Arbitrary amount of data - IO failed.");
		}

	}

    [TestFixture]
    public class HuffmanTest {

        [Test]
        public void TestHuffman() {
            string stuff = "jdsklnvkln w hjwkh rwqwdhnoi fhqw3iRUY2 BJEQAFK qi sefIWWE";
            Console.WriteLine(stuff);
            var compressed = Huffman.HuffmanCompress(stuff, (ch, wrb) => wrb(ch, sizeof(char) * 8));
            var decompressed = String.Concat(Huffman.HuffmanDecompress(compressed, rb => (char) rb(sizeof(char) * 8)));
            Assert.AreEqual(stuff, decompressed, "Huffman compressor did not work :(");
        }

    }
}
