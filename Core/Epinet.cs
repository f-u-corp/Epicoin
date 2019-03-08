using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace Epicoin {

	/*
	 * Parent
	 */

	class Parent
    {
        public Baby self { get; set; }
        //private List<Baby> babies; Is this really useful? We already have them in the Baby class
        private List<WebSocket> family;
        private Socket childCareGiver;

        public Parent()
        {

            //init this.childCareGiver
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 9191); //Check that 9191 is not commonly used
            this.childCareGiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.childCareGiver.Bind(endPoint);
        }
        
    }
    struct Tear
    {
        public readonly string IPAddress;
        public readonly string publicKey;

        public Tear(string receivedTear)
        {
            //TODO: get IPAddress from the UDP packet received
            //TDOO: get the publicKey from the UDP packet received
			IPAddress = receivedTear.Split('\n')[0];
			publicKey = receivedTear.Split('\n')[1];
        }
    }

	/*
	 * Baby
	 */

	class Baby
    {
        private Parent self;

        private List<Friend> friends;
        private List<PotentialParent> shoulders;

        private int RSAPrivateKey;
        private int RSAPublicKey;

        public Baby(Parent parent)
        {
            this.self = parent;
            parent.self = this;
            //Try to connect to parent
            //Try to connect to friends
            //If has no parents and no friends, cry
        }

        //first-connection related methods
        private void Call(Friend friend) { }
        private void Cry() { }

        //data downloading methods
        private void Leech(/*data to be leeched*/) { }

        //extending current friendlist
        private void Befriend(int KBRaddress) { }

    }
    struct Friend
    {
        public string IPAddress { get; set; }
        public int KBRAddress { get; set; }

        public Friend(string IP, int KBR) {
            this.IPAddress = IP;
            this.KBRAddress = KBR;
        }
    }

    struct PotentialParent
    {
        public IPEndPoint endPoint { get; }
        public DateTime cryingDate { get; }
        public Socket server { get; }

        public PotentialParent(string hostName, int port)
        {
            this.endPoint = new IPEndPoint(IPAddress.Parse(hostName), port); //check if valid code
            this.cryingDate = DateTime.UtcNow;
            this.server =new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public int SendTo(string msg)
        {
            byte[] data = Encoding.ASCII.GetBytes(msg);
            return this.server.SendTo(data, data.Length, SocketFlags.None, this.endPoint);
        }
    }

	class Epinet {

	}

}