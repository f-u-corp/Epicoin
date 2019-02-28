using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Epicoin {

	public struct EFOBE {

		private List<Block> blocks;

		public struct Block {

			//TODO - Block contents

		}
	}

	internal class Validator : MainComponent<Validator.ITM> {

		public Validator(Epicore core) : base(core) {}

		internal override void InitAndRun(){

		}


		/*
		 * ITC
		 */

		internal class ITM : ITCMessage {

		}

	}

}