using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Epicoin.Core
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
		//static HuffmanCode();

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

	class Epinet {

		/*
		* ITC - will be used after merger
		*/

		internal class ITM : ITCMessage {

			internal class IWantAFullEFOBE : ITM {}

			internal class TellEveryoneIKnowHowToMeth : ITM {

				public readonly string problem, parms, solution, hash;

				public TellEveryoneIKnowHowToMeth(string problem, string parms, string sol, string hash){
					this.problem = problem;
					this.parms = parms;
					this.solution = sol;
					this.hash = hash;
				}

			}

		}
	}

}
