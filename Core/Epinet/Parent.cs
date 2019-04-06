using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace Epicoin.Core
{
	/*
     * TODO: Write the event handler (on reception, add tear)
     * 
     */
	class Parent
	{
		public Baby Self { get; set; }
		//private List<Baby> babies; Is this really useful? We already have them in the Baby class
		public Dictionary<Friend,ClientWebSocket> Family { get; set; }
		private readonly Socket childCareGiver;

		public Parent()
		{
			NetworkMaestro.LOG.Info("Loading Parent");
			//init this.childCareGiver
			IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 9191); //Check that 9191 is not commonly used
			this.childCareGiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			this.childCareGiver.Bind(endPoint);
			NetworkMaestro.LOG.Info("Initialized child caregiver");
		}

	}
	struct Tear
	{
		public readonly string IPAddress;
		public readonly string publicKey;

		public Tear(string receivedTear)
		{
            //TODO: get IPAddress from the UDP packet received
            /*IPEndPoint endPoint = new IPEndPoint(address);

            int bytes = UdpClient.ReceiveFrom(ref UDPclient);

            string UDPPort = ((IPEndPoint)ClientWebSocket).Port.ToString()*/
            //TDOO: get the publicKey from the UDP packet received
            throw new NotImplementedException();
		}
	}
}
