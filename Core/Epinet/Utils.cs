using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Epicoin {

	/// <summary>
	/// Automatically advancing bit buffer, to simplify bit-level IO.
	/// </summary>
	public class BitBuffer {

		protected readonly BitArray ba;

		/// <summary>
		/// Initializes the empty bit buffer - all bits set to 0.
		/// </summary>
		/// <param name="ic">Initial capacity (in bits) of the buffer.</param>
		public BitBuffer(int ic = 4096) => ba = new BitArray(ic);

		/// <summary>
		/// Initializes the bit buffer from the provided binary data.
		/// </summary>
		/// <param name="data">Raw binary data for bits for the buffer.</param>
		/// <param name="lbsbc">Whether last byte is used to store number of overflown bits spaces (can be used to re-init a bit buffer with exactly the same number of bits).</param>
		public BitBuffer(byte[] data, bool lbsbc = true){
			ba = new BitArray(data);
			if(lbsbc) ba.Length -= 8 + data[data.Length-1];
		}

		int wbp = 0, rbp = 0;

		/// <summary>
		/// Writes a single bit to the buffer, and advances write position by 1.
		/// </summary>
		/// <param name="bit">Next bit to write.</param>
		public BitBuffer write(bool bit){
			ba[wbp++] = bit;
			if(wbp == ba.Length) extend();
			return this;
		}

		/// <summary>
		/// Writes a n bits to the buffer, and advances write position by n.
		/// </summary>
		/// <param name="l">Primitive ending with n LSB bits to copy-write.</param>
		/// <param name="bits">n - number of LSB bits to write/copy from l.</param>
		public BitBuffer writeBits(long l, int bits){
			for(int b = bits - 1; b >= 0; b--) write(((l >> b) & 1) != 0);
			return this;
		}

		/// <summary>
		/// Writes a n bits to the buffer, and advances write position by n.
		/// </summary>
		/// <param name="i">Primitive ending with n LSB bits to copy-write.</param>
		/// <param name="bits">n - number of LSB bits to write/copy from i.</param>
		public BitBuffer writeBits(int i, int bits) => writeBits((long) i, bits);

		public BitBuffer writeByte(byte b) => writeBits(b, sizeof(byte)*8);
		public BitBuffer writeChar(char ch) => writeBits(ch, sizeof(char)*8);
		public BitBuffer writeInt(int i) => writeBits(i, sizeof(int)*8);
		public BitBuffer writeUInt(uint i) => writeBits(i, sizeof(uint)*8);

		public BitBuffer writeLong(long i) => writeBits(i, sizeof(long)*8);
		public BitBuffer writeULong(ulong i) => writeBits((long) i, sizeof(ulong)*8);
		public BitBuffer writeFloat(float f) => writeInt(BitConverter.SingleToInt32Bits(f));
		public BitBuffer writeDouble(double d) => writeLong(BitConverter.DoubleToInt64Bits(d));

		/// <summary>
		/// Sets write position.
		/// </summary>
		/// <param name="p">New write position.</param>
		public BitBuffer setWritePos(int p){
			wbp = p;
			return this;
		}

		/// <summary>
		/// Explicitly extends buffer's capacity.
		/// </summary>
		/// <param name="bits">Additional number to add to current capacity (default - 4096).</param>
		public BitBuffer extend(int bits = 4096){
			ba.Length += bits;
			return this;
		}

		/// <summary>
		/// Flips the buffer, from write to read mode: Sets the explicit capacity to current write position, Resets current write position to 0.
		/// </summary>
		public BitBuffer flip(){
			ba.Length = wbp;
			wbp = 0;
			return this;
		}

		/// <summary>
		/// Reads the next bit from the buffer, advancing read position by 1.
		/// </summary>
		public bool read() => ba[rbp++];
		
		/// <summary>
		/// Reads the next n bits from the buffer, copying them into n LSB of a primitive, advancing read position by n.
		/// </summary>
		/// <param name="bits">n - number of bits to read from the buffer / copy into n LSB of the primitive</param>
		public long readBitsL(int bits){
			long l = 0;
			for(int b = bits - 1; b >= 0; b--) l |= (read() ? 1L : 0L) << b;
			return l;
		}

		/// <summary>
		/// Reads the next n bits from the buffer, copying them into n LSB of a primitive, advancing read position by n.
		/// </summary>
		/// <param name="bits">n - number of bits to read from the buffer / copy into n LSB of the primitive</param>
		public int readBits(int bits) => (int) readBitsL(bits);

		public byte readByte() => (byte) readBits(sizeof(byte)*8);
		public char readChar() => (char) readBits(sizeof(char)*8);
		public int readInt() => readBits(sizeof(int)*8);
		public uint readUInt() => (uint) readBits(sizeof(uint)*8);
		public long readLong() => readBitsL(sizeof(long)*8);
		public ulong readULong() => (ulong) readBitsL(sizeof(ulong)*8);
		public float readFloat() => BitConverter.Int32BitsToSingle(readInt());
		public double readDouble() => BitConverter.Int64BitsToDouble(readLong());

		/// <summary>
		/// Sets read position.
		/// </summary>
		/// <param name="p">New write position.</param>
		public BitBuffer setReadPos(int p){
			rbp = p;
			return this;
		}



		public BitArray getUnderlying() => ba;
		/// <summary>[Explicit] number of bits this buffer contains.</summary>
		public int BitCount { get => ba.Length; }
		/// <summary>[Explicit] number of bytes this buffer takes up (<c>ceil(bits/8)</c>).</summary>
		public int ByteCount { get => (int) Math.Ceiling(BitCount/8d); }


		/// <summary>
		/// Copies the bits of the buffer to the destination starting at provided position. Increases the start index ref to the next available byte (next byte after the last copied byte).
		/// </summary>
		/// <param name="arr">Destination array to copy to [zero-based].</param>
		/// <param name="index">Starting index of copy in the destination array [zero-based].</param>
		/// <param name="lbsbc">Whether last byte is used to store number of overflown bits spaces (can be used to re-init a bit buffer with exactly the same number of bits).</param>
		public void CopyTo(byte[] arr, ref int index, bool lbsbc = true){
			ba.CopyTo(arr, index);
			index += ByteCount;
			if(lbsbc){
				arr[index] = (byte) (ByteCount*8 - BitCount);
				index++;
			}
		}
		/// <summary>
		/// Copies the bits of the buffer to the destination starting at provided position.
		/// </summary>
		/// <param name="arr">Destination array to copy to [zero-based].</param>
		/// <param name="index">Starting index of copy in the destination array [zero-based].</param>
		/// <param name="lbsbc">Whether last byte is used to store number of overflown bits spaces (can be used to re-init a bit buffer with exactly the same number of bits).</param>
		public void CopyTo(byte[] arr, int index = 0, bool lbsbc = true) => CopyTo(arr, ref index, lbsbc);
		/// <summary>
		/// Creates and copies the bits of the buffer into a byte array.
		/// </summary>
		/// <param name="lbsbc">Whether last byte is used to store number of overflown bits spaces (can be used to re-init a bit buffer with exactly the same number of bits).</param>
		public byte[] CopyTo(bool lbsbc = true){
			byte[] bytes = new byte[ByteCount + (lbsbc ? 1 : 0)];
			CopyTo(bytes, 0, lbsbc);
			return bytes;
		}

	}

	public class Huffman {

		public static BitArray HuffmanCompress<T>(IEnumerable<T> stuff, Action<T, Action<int, int>> TbitWriter){
			HufflepuffmanNode<T> rut = ComputeHuffmanRoot(stuff.Select(t => (t: t, f: 1)).GroupBy(p => p.t, (k, ps) => (k, ps.Sum(p => p.f))));
			Dictionary<T, int> dict = new Dictionary<T, int>();
			void huffdict(HufflepuffmanNode<T> node, int enc){
				if(node is HufflepuffmanNode<T>.Leaf) dict.Add((node as HufflepuffmanNode<T>.Leaf).t, enc);
				if(node is HufflepuffmanNode<T>.InternalNode){
					var n = node as HufflepuffmanNode<T>.InternalNode;
					huffdict(n.left, enc << 1);
					huffdict(n.right, (enc << 1) + 1);
				}
			}
			huffdict(rut, 1);

			BitArray ba = new BitArray(stuff.Count() * 2);
			int abl = 0;
			void write(bool b){
				ba[abl++] = b;
				if(abl == ba.Length) ba.Length = ba.Length + 4096;
			}
			void writeIb(int i, int bits){
				for(int b = bits - 1; b >= 0; b--) write(((i >> b) & 1) != 0);
			}

			writeIb(dict.Count, 16);
			foreach (var te in dict){
				if(te.Value > 0xFFFF){
					write(true);
					writeIb(te.Value, 32);
				}
				else {
					write(false);
					if(te.Value > 0xFF){
						write(true);
						writeIb(te.Value, 16);
					}
					else write(false);
				}
				writeIb(te.Value, 8);
				TbitWriter(te.Key, writeIb);
			}

			foreach (var s in stuff){
				var huff = dict[s];
				writeIb(huff, (int)Math.Ceiling(Math.Log(huff + .5d, 2)));
			}

			ba.Length = abl;
			return ba;
		}

		public static List<T> HuffmanDecompress<T>(BitArray ba, Func<Func<int, int>, T> TbitReader){
			int abl = 0;
			bool read() => ba[abl++];
			int readIb(int bits){
				int i = 0;
				for(int b = bits - 1; b >= 0; b--) i |= (read() ? 1 : 0) << b;
				return i;
			}

			int dictSize = readIb(16);
			Dictionary<int, T> dict = new Dictionary<int, T>(dictSize);
			for(int i = 0; i < dictSize; i++){
				int enc;
				if (read()) enc = 32;
				else if (read()) enc = 16;
				else enc = 8;
				var len = readIb(enc);
				dict.Add(len, TbitReader(readIb));
			}

			List<T> res = new List<T>();
			int ci = 0;
			while(abl < ba.Length){
				ci = (ci << 1) | (read() ? 1 : 0);
				if (dict.ContainsKey(ci)){
					res.Add(dict[ci]);
					ci = 0;
				}
			}
			return res;
		}

		public static HufflepuffmanNode<T> ComputeHuffmanRoot<T>(IEnumerable<(T t, int freq)> objFreq){
			SortedList<HufflepuffmanNode<T>> nodesPro = new SortedList<HufflepuffmanNode<T>>(objFreq.Select(p => new HufflepuffmanNode<T>.Leaf(p.freq, p.t)));

			while(nodesPro.Count > 1){
				var f2 = nodesPro.TakeFirst(2);
				nodesPro.Add(new HufflepuffmanNode<T>.InternalNode(f2[0], f2[1]));
			}

			return nodesPro.First();
		}

		public class HufflepuffmanNode<T> : IComparable<HufflepuffmanNode<T>> {
			public int freq;

			public HufflepuffmanNode(int freq){
				this.freq = freq;
			}

			public int CompareTo(HufflepuffmanNode<T> other) => freq.CompareTo(other.freq);

			public class Leaf : HufflepuffmanNode<T> {

				public T t;

				public Leaf(int freq, T t) : base(freq){
					this.t = t;
				}

				public override bool Equals(object obj){
					var leaf = obj as Leaf;
					return leaf != null &&
						   EqualityComparer<T>.Default.Equals(t, leaf.t);
				}

				public override int GetHashCode(){
					return 831258139 + EqualityComparer<T>.Default.GetHashCode(t);
				}

				public override string ToString() => $"🍀[{t} x{freq}]";
			}

			public class InternalNode : HufflepuffmanNode<T> {

				public HufflepuffmanNode<T> left, right;

				public InternalNode(HufflepuffmanNode<T> l, HufflepuffmanNode<T> r) : base(l.freq + r.freq){
					this.left = l;
					this.right = r;
				}

				public override bool Equals(object obj){
					var node = obj as InternalNode;
					return node != null && EqualityComparer<HufflepuffmanNode<T>>.Default.Equals(left, node.left) && EqualityComparer<HufflepuffmanNode<T>>.Default.Equals(right, node.right);
				}

				public override int GetHashCode(){
					var hashCode = -124503083;
					hashCode = hashCode * -1521134295 + EqualityComparer<HufflepuffmanNode<T>>.Default.GetHashCode(left);
					hashCode = hashCode * -1521134295 + EqualityComparer<HufflepuffmanNode<T>>.Default.GetHashCode(right);
					return hashCode;
				}

				public override string ToString() => $"🌳[{left} , {right}]";

			}

		}

	}

	public class SortedList<T> : IList<T> {
		private List<T> list = new List<T>();

		public SortedList(){}
		public SortedList(IEnumerable<T> copy){
			foreach (var t in copy) Add(t);
		}

		public int IndexOf(T item){
			var index = list.BinarySearch(item);
			return index < 0 ? -1 : index;
		}

		public void Insert(int index, T item) => throw new NotImplementedException("Cannot insert at index; must preserve order.");

		public void RemoveAt(int index) => list.RemoveAt(index);

		public T this[int index]{
			get => list[index];
			set {
				list.RemoveAt(index);
				this.Add(value);
			}
		}

		public void Add(T item){
			var i = list.BinarySearch(item);
			i = i >= 0 ? i : ~i;
			if (i >= list.Count) list.Add(item);
			else list.Insert(i, item);
		}

		public void Clear() => list.Clear();

		public bool Contains(T item) => list.BinarySearch(item) >= 0;

		public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);

		public int Count { get => list.Count; }

		public bool IsReadOnly { get => false; }

		public T TakeFirst(){
			T t = list[0];
			list.RemoveAt(0);
			return t;
		}
		public T[] TakeFirst(int amount){
			T[] ts = new T[amount];
			for (int i = 0; i < amount; i++) ts[i] = TakeFirst();
			return ts;
		}

		public bool Remove(T item){
			throw new InvalidOperationException("Me do not remove :P");
			var index = list.BinarySearch(item);
			if(index < 0) return false;
			list.RemoveAt(index);
			return true;
		}

		public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
	}

}
