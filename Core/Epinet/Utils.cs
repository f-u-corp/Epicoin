using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Epicoin {

    public class Huffman
    {

        public static BitArray HuffmanCompress<T>(IEnumerable<T> stuff, Action<T, Action<bool>> TbitWriter)
        {
            HufflepuffmanNode<T> rut = ComputeHuffmanRoot(stuff.Select(t => (t: t, f: 1)).GroupBy(p => p.t, (k, ps) => (k, ps.Sum(p => p.f))));
            Dictionary<T, int> dict = new Dictionary<T, int>();
            void huffdict(HufflepuffmanNode<T> node, int enc)
            {
                if (node is HufflepuffmanNode<T>.Leaf) dict.Add((node as HufflepuffmanNode<T>.Leaf).t, enc);
                if (node is HufflepuffmanNode<T>.InternalNode)
                {
                    var n = node as HufflepuffmanNode<T>.InternalNode;
                    huffdict(n.left, enc << 1);
                    huffdict(n.right, (enc << 1) + 1);
                }
            }
            huffdict(rut, 0);

            BitArray ba = new BitArray(stuff.Count() * 2);
            int abl = 0;
            void write(bool b)
            {
                ba[abl++] = b;
                if (abl == ba.Length) ba.Length = ba.Length + 4096;
            }
            void writeIb(int i, int bits)
            {
                for (int b = bits - 1; b >= 0; b--) write(((i >> b) & 1) != 0);
            }

            writeIb(dict.Count, 16);
            foreach (var te in dict)
            {
                if (te.Value > 0xFFFF)
                {
                    write(true);
                    writeIb(te.Value, 32);
                    continue;
                }
                write(false);
                if (te.Value > 0xFF)
                {
                    write(true);
                    writeIb(te.Value, 16);
                    continue;
                }
                write(false);
                writeIb(te.Value, 8);
                TbitWriter(te.Key, write);
            }

            foreach (var s in stuff)
            {
                var huff = dict[s];
                writeIb(huff, (int)Math.Ceiling(Math.Log(huff, 2)));
            }

            ba.Length = abl;
            return ba;
        }

        public static List<T> HuffmanDecompress<T>(BitArray ba, Func<Func<bool>, T> TbitReader)
        {
            int abl = 0;
            bool read() => ba[abl++];
            int readIb(int bits)
            {
                int i = 0;
                for (int b = bits - 1; bits >= 0; bits++) i |= (read() ? 1 : 0) << b;
                return i;
            }

            int dictSize = readIb(16);
            Dictionary<int, T> dict = new Dictionary<int, T>(dictSize);
            for (int i = 0; i < dictSize; i++)
            {
                int enc;
                if (read()) enc = 32;
                else if (read()) enc = 16;
                else enc = 8;
                dict.Add(enc, TbitReader(read));
            }

            List<T> res = new List<T>();
            int ci = 0;
            while (abl < ba.Length)
            {
                ci = (ci << 1) | (read() ? 1 : 0);
                if (dict.ContainsKey(ci))
                {
                    res.Add(dict[ci]);
                    ci = 0;
                }
            }
            return res;
        }

        public static HufflepuffmanNode<T> ComputeHuffmanRoot<T>(IEnumerable<(T t, int freq)> objFreq) //
        {
            SortedList<HufflepuffmanNode<T>> nodesPro = new SortedList<HufflepuffmanNode<T>>(objFreq.Select(p => new HufflepuffmanNode<T>.Leaf(p.freq, p.t)));

            while (nodesPro.Count > 1)
            {
                var f2 = nodesPro.TakeFirst(2);
                nodesPro.Add(new HufflepuffmanNode<T>.InternalNode(f2[0], f2[1]));
            }

            return nodesPro.First();
        }

        /*public static void Revert(HufflepuffmanNode<T> parent, HufflepuffmanNode<T> presentnode, T laser, T inp)
        {
            if (inp.Length == laser)
            {
                if (presentnode.left == null && presentnode.right == null)
                {
                    return;
                }
            }

            else
            {
                if (presentnode.left == null && presentnode.right == null)
                {
                    Revert(parent, parent, laser, inp);
                }

                else
                {
                    if (inp.Split(laser) == "0")
                    {
                        Revert(parent, presentnode.left, laser, inp);
                        laser = laser + 1;

                    }

                    else
                    {
                        Revert(parent, presentnode.right, laser, inp);
                        laser = laser + 1;
                    }
                }
            }


        }*/

        public class HufflepuffmanNode<T> : IComparable<HufflepuffmanNode<T>>
        {
            public int freq;

            public HufflepuffmanNode(int freq)
            {
                this.freq = freq;
            }

            public int CompareTo(HufflepuffmanNode<T> other) => freq.CompareTo(other.freq);

            public class Leaf : HufflepuffmanNode<T>
            {

                public T t;

                public Leaf(int freq, T t) : base(freq)
                {
                    this.t = t;
                }

                public override bool Equals(object obj)
                {
                    var leaf = obj as Leaf;
                    return leaf != null &&
                           EqualityComparer<T>.Default.Equals(t, leaf.t);
                }

                public override int GetHashCode()
                {
                    return 831258139 + EqualityComparer<T>.Default.GetHashCode(t);
                }

                public override string ToString() => $"🍀[{t} x{freq}]";
            }

            public class InternalNode : HufflepuffmanNode<T>
            {

                public HufflepuffmanNode<T> left, right;

                public InternalNode(HufflepuffmanNode<T> l, HufflepuffmanNode<T> r) : base(l.freq + r.freq)
                {
                    this.left = l;
                    this.right = r;
                }

                public override bool Equals(object obj)
                {
                    var node = obj as InternalNode;
                    return node != null &&
                           EqualityComparer<HufflepuffmanNode<T>>.Default.Equals(left, node.left) &&
                           EqualityComparer<HufflepuffmanNode<T>>.Default.Equals(right, node.right);
                }

                public override int GetHashCode()
                {
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
            foreach(var t in copy) Add(t);
        }

        public int IndexOf(T item){
            var index = list.BinarySearch(item);
            return index < 0 ? -1 : index;
        }

        public void Insert(int index, T item) => throw new NotImplementedException("Cannot insert at index; must preserve order.");

        public void RemoveAt(int index) => list.RemoveAt(index);

        public T this[int index] {
            get => list[index];
            set {
                list.RemoveAt(index);
                this.Add(value);
            }
        }

        public void Add(T item){
            var i = list.BinarySearch(item);
            i = i >= 0 ? i : ~i;
            if(i >= list.Count) list.Add(item);
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
            var index = list.BinarySearch(item);
            if (index < 0) return false;
            list.RemoveAt(index);
            return true;
        }

        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
    }

}
