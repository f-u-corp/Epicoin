using System;
using System.Collections.Generic;

namespace Epicoin {
	public class SHA256 {

		public const int ibs = sizeof(int)*8;
		public static uint rol(uint i, int n) => n < 0 ? ror(i, -n) : (i << (n%ibs)) | (i >> (ibs-(n%ibs)));
		public static uint ror(uint i, int n) => n < 0 ? rol(i, -n) : (i >> (n%ibs)) | (i << (ibs-(n%ibs)));

		public static uint roru(uint c, int n){
			int uint_mask = 32 - 1;

			if(n < 0){
				n = -n;
				return (uint) ((c << n) | (c >> (-n & uint_mask)));
			} else {
				return (uint) ((c >> n) | (c << (-n & uint_mask)));
			}
		}

		private static readonly uint[]	HASHESSQR = {	0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a, 0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19 },
										RNDCNSTCBRT = {	0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
														0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
														0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
														0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
														0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
														0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
														0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
														0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2 };

		public static byte[] Hash(byte[] input){
			uint[] hashesSqr = new uint[HASHESSQR.Length];
			Array.Copy(HASHESSQR, hashesSqr, HASHESSQR.Length);

			BitBuffer msg = new BitBuffer(input, false);
			int ibc = input.Length*8, K = ibc%512 < 448 ? 448 - ibc%512 : 512 + 448 - ibc%512;
			msg.setWritePos(ibc).extend(K);

			//Padding 
			msg.write(true);
			for(int i = 0; i < K; i++) msg.write(false);
			msg.writeULong((ulong) ibc);
			msg.flip();

			uint fCH(uint x, uint y, uint z) => (x&y)^((~x)&z);
			uint fMAJ(uint x, uint y, uint z) => (x&y)^(x&z)^(y&z);
			uint fΣ(uint x, int a, int b, int c) => ror(x, a)^ror(x, b)^ror(x, c);
			uint fΣ1(uint x) => fΣ(x, 2, 13, 22);
			uint fΣ2(uint x) => fΣ(x, 6, 11, 25);
			uint fσ(uint x, int a, int b, int c) => ror(x, a)^ror(x, b)^(x>>c);
			uint fσ1(uint x) => fσ(x, 7, 18, 3);
			uint fσ2(uint x) => fσ(x, 17, 19, 10);

			for(int ch = 0; ch < msg.BitCount/512; ch++){
				var sched = new uint[64];
				for(int w = 0; w < 16; w++) sched[w] = msg.readUInt();

				for(int i = 16; i < 64; i++) sched[i] = sched[i-16] + fσ1(sched[i-15]) + sched[i-7] + fσ2(sched[i-2]);

				var hs = new uint[8];
				Array.Copy(hashesSqr, hs, hs.Length);

				for(int i = 0; i < 64; i++){
					var t1 = hs[7] + fΣ2(hs[4]) + fCH(hs[4], hs[5], hs[6]) + RNDCNSTCBRT[i] + sched[i];
					var t2 = fΣ1(hs[0]) + fMAJ(hs[0], hs[1], hs[2]);
					var ihs = new uint[hs.Length];
					/*Array.Copy(hs, 0, ihs, 1, hs.Length-1);
					ihs[0] = t1 + t2;
					ihs[4] += t1;
					hs = ihs;*/
					hs = new uint[]{t1+t2, hs[0], hs[1], hs[2], hs[3]+t1, hs[4], hs[5], hs[6]};
				}

				for(int h = 0; h < 8; h++) hashesSqr[h] += hs[h];
			}

			byte[] res = new byte[32];
			Buffer.BlockCopy(hashesSqr, 0, res, 0, res.Length);
			return res;

			/*
			//512 bits chunk division
			bool[][] chunks = new bool[msg.BitCount/512][];
			for(int i = 0; i < chunks.Length; i++){
				bool[] chunk = chunks[i] = new bool[512];
				for(int j = 0; j < 512; j++) chunk[j] = msg.read();
			}

			foreach(var chunk in chunks){
				
			}


			
			for(int i = 0; i < chunks.Length; i++){

				messageSchedules[i] = new uint[64];

				for(int j = 0; j < 16; j++){
					messageSchedules[i] += (chunks[i][j] ? 1 : 0)*(uint)Math.Pow(2,15-j);
				}

				for(int j = 16; j < 64; j++){
					uint[] temp = new uint[2];
					temp[0] = roru(messageSchedules[i][j-15],7)^roru(messageSchedules[i][j-15],18)^(messageSchedules[i][j-15]>>3);
					temp[1] = roru(messageSchedules[i][j-2],17)^roru(messageSchedules[i][j-2],19)^(messageSchedules[i][j-2]>>10);
				
					messageSchedules[i][j] = messageSchedules[i][j-16]+temp[0]+messageSchedules[i][j-7]+temp[1];
				}

				//init workHashes
				uint[] workHashes = new uint[8];
				for(int j =0; j < 8; j++){
					workHashes[j] = hashesSqr[j];
				}

				//Compression 
				for(int j = 0; j < 64; j++){
					uint[] temp = new uint[4];
					uint[] comput = new uint[2];

					temp[0] = roru(workHashes[4],6)^roru(workHashes[4],11)^roru(workHashes[4],25); //corresponds to S1
					temp[1] = (workHashes[4] & workHashes[5])^((~workHashes[4]) & workHashes[6]); //corresponds to ch
					
					comput[0] = workHashes[7] + temp[0]+temp[1]+roundConstants[j]+messageSchedules[i][j];//corresponds to temp1
					
					temp[2] = roru(workHashes[0], 1)^roru(workHashes[0], 13)^roru(workHashes[0], 22); //corresponds to S0
					temp[3] = (workHashes[0] & workHashes[1])^(workHashes[0] & workHashes[2])^(workHashes[1] & workHashes[2]); //corresponds to maj
					
					comput[1] = temp[2]+temp[3];
					
					workHashes[7] = workHashes[6];
					workHashes[6] = workHashes[5];
					workHashes[5] = workHashes[4];
					workHashes[4] = workHashes[3]+comput[0];
					workHashes[2] = workHashes[1];
					workHashes[1] = workHashes[0];
					workHashes[0] = comput[0] + comput[1];
				}

				for(int j = 0; j < 8; j++){
					hashesSqr[j]+= workHashes[j];
				}

			}

			List<uint> output = new List<uint>();
			for(int i = 0; i < 8; i++){
				output.Add(hashesSqr[i]);
			}
			
			return output;*/
		}

	}
}