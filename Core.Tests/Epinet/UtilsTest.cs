using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace Epicoin {

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
