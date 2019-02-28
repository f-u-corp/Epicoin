using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Epicoin {

	public class Epicore {

		internal Solver solver;
		internal Validator validator;

		internal Action<Solver.ITM> sendITM2Solver;
		internal Action<Validator.ITM> sendITM2Validator;
		internal Action sendITM2Net;

		internal bool stop { get; private set; }

		public Epicore(){
			st = new Thread((solver = new Solver(this)).InitAndRun);
			vt = new Thread((validator = new Validator(this)).InitAndRun);
			nt = new Thread(() => {}); //TODO wire in network component

			sendITM2Solver = solver.sendITM;
			sendITM2Validator = validator.sendITM;
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