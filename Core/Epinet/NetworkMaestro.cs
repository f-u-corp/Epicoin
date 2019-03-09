using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Epicoin
{
	class NetworkMaestro
	{
		public static CancellationTokenSource cts = new CancellationTokenSource();
		protected string OwnIp;

		//usage of fixed port for now
		public static int Port = 27945; //not in use according to https://en.wikipedia.org/wiki/List_of_TCP_and_UDP_port_numbers

		private readonly Baby baby;
		private readonly Parent parent;

		public NetworkMaestro()
		{
			string localIP;
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
			{
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
				localIP = endPoint.Address.ToString();
			}
			this.parent = new Parent();
			this.baby = new Baby(parent);
		}
		public void NetworkLogic()
		{
			//If has no parents and no friends, cry
			if (this.baby.friends.Count == 0)
			{
				baby.Cry(); 
			}

		}


        class HufflepuffmanNode<T> : IComparable<HufflepuffmanNode<T>>
        {
            public int freq;

            public HufflepuffmanNode(int freq){
                this.freq = freq;
            }

            public int CompareTo(HufflepuffmanNode<T> other) => freq.CompareTo(other.freq);

            public class Leaf : HufflepuffmanNode<T> {

                public T t;

                public Leaf(int freq, T t) : base(freq)
                {
                    this.t = t;
                }

            }

            public class InternalNode : HufflepuffmanNode<T>
            {

                public HufflepuffmanNode<T> left, right;

                public InternalNode(HufflepuffmanNode<T> l, HufflepuffmanNode<T> r) : base(l.freq + r.freq)
                {
                    this.left = l;
                    this.right = r;
                }

            }

            public static HufflepuffmanNode<T> ComputeHuffmanRoot(List<(T t, int freq)> objFreq) //
            {
                SortedSet<HufflepuffmanNode<T>> nodesPro = new SortedSet<HufflepuffmanNode<T>>(objFreq.Select(p => new Leaf(p.freq, p.t)));

                while(nodesPro.Count > 1)
                {
                    var f2 = nodesPro.Take(2);
                    foreach (var n in f2) nodesPro.Remove(n);
                    nodesPro.Add(new InternalNode(f2.First(), f2.Last()));
                }

                return nodesPro.First();
            }

            public static Stack<HufflepuffmanNode<T>> Сука(SortedList<HufflepuffmanNode<T> generic )
            {
                int i = 0;
                int j = 0;

                for ()
            }
        }

		class BinTree<T>
		{
			public T InnerValue { get; set; }
			public BinTree<T> Right { get; set; }
			public BinTree<T> Left { get; set; }

			public BinTree(T val)
			{
				this.InnerValue = val;
				this.Right = null;
				this.Left = null;
			}
		}
	}
}
