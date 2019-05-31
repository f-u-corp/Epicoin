using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Epicoin.Core
{
	class NetworkMaestro : MainComponent<NetworkMaestro.ITM>, INet
	{
		internal readonly static log4net.ILog LOG = log4net.LogManager.GetLogger("Epicoin", "Epicore-Network");

		public static CancellationTokenSource cts = new CancellationTokenSource();
		protected string OwnIp;

		//usage of fixed port for now
		public static int Port = 27945; //not in use according to https://en.wikipedia.org/wiki/List_of_TCP_and_UDP_port_numbers

		private /*readonly*/ Baby baby;
		private /*readonly*/ Parent parent;

		public NetworkMaestro(Epicore core) : base(core) {}

		public INetBaby GetBaby() => baby;
		public INetParent GetParent() => parent;

		internal override void InitAndRun()
		{
			LOG.Info("Pre-Loading networking");
			string localIP;
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
			{
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
				localIP = endPoint.Address.ToString();
			}
			LOG.Info("Local IP established");
			LOG.Info("Loading networking components");
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

		/*
		 * ITC
		 */

		internal class ITM : ITCMessage
		{

		}
	}

}
