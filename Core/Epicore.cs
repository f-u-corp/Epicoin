using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Epicoin {

	public class Epicore {

		internal bool stop { get; private set; }

		public Epicore(){

		}

		protected Thread vt, st, nt;

		public void Start(){
			vt.Start();
			nt.Start();
			st.Start();
		}

		public void Stop(){
			stop = true;
			while(vt.IsAlive || st.IsAlive || nt.IsAlive) Thread.Yield();
		}

	}

	internal abstract class MainComponent<ITM> where ITM : ITCMessage {

		protected readonly Epicore core;

		protected MainComponent(Epicore core) => this.core = core;

		internal abstract void InitAndRun();

		/*
		 * ITC
		 */
		
		protected InterThreadComms<ITM> itc = new InterThreadComms<ITM>();
		public Action<ITM> sendITM { get => itc.sendMessage; }

	}

	internal class InterThreadComms<M> where M : ITCMessage  {

		protected ConcurrentQueue<M> messages = new ConcurrentQueue<M>();

		public void sendMessage(M message) => messages.Enqueue(message);

		public M readMessageOrDefault() => messages.TryDequeue(out M m) ? m : default(M);

	}

	internal interface ITCMessage {

	}

}