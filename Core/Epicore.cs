using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Epicoin {

	internal abstract class MainComponent {

		protected readonly Epicore core;

		protected MainComponent(Epicore core) => this.core = core;

		internal abstract void InitAndRun();

	}

	internal class InterThreadComms<M> where M : ITCMessage  {

		protected ConcurrentQueue<M> messages = new ConcurrentQueue<M>();

		public void sendMessage(M message) => messages.Enqueue(message);

		public M readMessageOrDefault() => messages.TryDequeue(out M m) ? m : default(M);

	}

	internal interface ITCMessage {

	}

}