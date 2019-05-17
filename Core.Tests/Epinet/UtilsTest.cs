using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using Epicoin.Core;

using NUnit.Framework;

namespace Epicoin.Test {

	[TestFixture]
	public class BitBufferTest {

		[Test(TestOf = typeof(BitBuffer))]
		[Repeat(10)]
		public void TestBitBufferRW(){
			Random rnd = new Random();
			bool bo = rnd.Next(100) < 50;
			byte b = (byte) rnd.Next(255);
			char ch = (char) rnd.Next(0xFF);
			int i = rnd.Next();
			int si = rnd.Next(-0xFFFF, 0xFFFF);
			uint ui = (uint) rnd.Next();
			long l = (((long) rnd.Next()) << 32) | ((long) rnd.Next());
			ulong ul = (((ulong) rnd.Next()) << 32) | ((ulong) rnd.Next());
			float f = (float) rnd.NextDouble();
			double d = rnd.NextDouble();
			double sd = -16e7 + rnd.NextDouble()*(32e7);
			int maskedI = rnd.Next(); int maskI = rnd.Next(12, 27);
			long maskedL = (((long) rnd.Next()) << 32) | ((long) rnd.Next()); int maskL = rnd.Next(12, 53);
			byte len = (byte) rnd.Next(17, 59);
			List<int> ints = Enumerable.Repeat(0, len).Select(whatava => rnd.Next()).ToList();

			BitBuffer bb = new BitBuffer();
			bb.write(bo);
			bb.writeByte(b);
			bb.writeChar(ch);
			bb.writeInt(i);
			bb.writeInt(si);
			bb.writeUInt(ui);
			bb.writeLong(l);
			bb.writeULong(ul);
			bb.writeFloat(f);
			bb.writeDouble(d);
			bb.writeDouble(sd);
			bb.writeBits(maskedI, maskI);
			bb.writeBits(maskedL, maskL);
			bb.writeByte(len);
			bb.writeInts(ints);
			bb.flip();
			Assert.AreEqual(bo, bb.read(), "Bool read/write failed.");
			Assert.AreEqual(b, bb.readByte(), "Byte read/write failed.");
			Assert.AreEqual(ch, bb.readChar(), "Char read/write failed.");
			Assert.AreEqual(i, bb.readInt(), "Int read/write failed.");
			Assert.AreEqual(si, bb.readInt(), "Signed Int read/write failed.");
			Assert.AreEqual(ui, bb.readUInt(), "UInt read/write failed.");
			Assert.AreEqual(l, bb.readLong(), "Long read/write failed.");
			Assert.AreEqual(ul, bb.readULong(), "ULong read/write failed.");
			Assert.AreEqual(f, bb.readFloat(), "Float read/write failed.");
			Assert.AreEqual(d, bb.readDouble(), "Double read/write failed.");
			Assert.AreEqual(sd, bb.readDouble(), "Scaled signed double read/write failed.");
			Assert.AreEqual(maskedI & ((1<<maskI)-1), bb.readBits(maskI), "Arbitrary bit count [int] read/write failed.");
			Assert.AreEqual(maskedL & ((1L<<maskL)-1L), bb.readBitsL(maskL), "Arbitrary bit count [long] read/write failed.");
			Assert.AreEqual(ints, bb.readInts(bb.readByte()), "Int collection read/write failed.");
		}

		[Test(TestOf = typeof(BitBuffer))]
		public void TestBitBufferIO(){
			Random rnd = new Random();
			byte len = (byte) rnd.Next(55, 175);
			List<int> data = Enumerable.Repeat(0, len).Select(i => rnd.Next()).ToList();

			BitBuffer wb = new BitBuffer();
			wb.writeByte(len);
			wb.writeInts(data);
			wb.flip();

			byte[] raw = wb.CopyTo();

			BitBuffer rb = new BitBuffer(raw);
			byte rlen = rb.readByte();
			List<int> rdata = rb.readInts(rlen);

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
