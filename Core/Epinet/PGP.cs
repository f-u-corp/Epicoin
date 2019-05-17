using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.IO;

using Epicoin.Core;

namespace Epicoin.Core.Epinet
{

	public static class PGP
	{
		private static RNGCryptoServiceProvider rngCSP = new RNGCryptoServiceProvider();
		public static byte[] SenderAuth(int privateKey, int publicNKey, byte[] message)
		{
			byte[] hashValue;
			byte[] encryptedData;
			//sender hash the message
			using (SHA256 mySHA256 = SHA256.Create()) //replace with our own SHA256 when it is debugged
			{
				hashValue = mySHA256.ComputeHash(message);
			}

			//sender encrypt the hash with a private key
			encryptedData = RSA.EncryptRSA(hashValue, new int[] { publicNKey, privateKey });

			//sender add the hash in the beginning of the payload
			List<byte> merger = new List<byte>();
			merger.Add((byte)message.Length); //just so we can retrieve the hash from the beginning easily. We probably should get rid of this though
			foreach (byte b in encryptedData)
			{
				merger.Add(b);
			}
			foreach (byte b in message)
			{
				merger.Add(b);
			}
			return merger.ToArray();
		}

		public static bool ReceiverAuth(byte[] receivedMsg, int encryptedDataLength, int n, int privateKey)
		{
			//receiver uses public key of the sender on the beginning of the message and gets the condensate
			byte[] encryptedData = new byte[encryptedDataLength]; //since we use SHA256; the length should be 32
			for(int i = 0; i < encryptedDataLength; i++)
			{
				encryptedData[i] = receivedMsg[i];
			}

			byte[] payload = new byte[receivedMsg.Length - encryptedDataLength];
			for (int i = encryptedDataLength; i < receivedMsg.Length; i++)
			{
				payload[i] = receivedMsg[i];
			}
			byte[] condensate = RSA.DecryptRSA(encryptedData, n, privateKey);
			//receiver then hashes the message and checks if the condensate is the same
			byte[] hashValue;
			using (SHA256 mySHA256 = SHA256.Create()) //replace with our own SHA256 when it is debugged
			{
				hashValue = mySHA256.ComputeHash(payload);
			}
			return hashValue == encryptedData;
		}

		public static byte[][] SenderConfidentiality(string message, int[] publicRSAKey)
		{
			//sender generate a secret key (128bits)
			byte[] bufferKey = new byte[16];
			PGP.rngCSP.GetBytes(bufferKey);

			byte[] encryptedMsg;
			byte[] encryptedKey;
			using (Aes aes = Aes.Create())
			{
				//sender symetric encryption using that key
				encryptedMsg = AES.EncryptStringToBytes_Aes(message, bufferKey, aes.IV);
				//sender encrypt that key using RSA public key
				encryptedKey = RSA.EncryptRSA(bufferKey, publicRSAKey);
			}
			return new byte[][] { encryptedMsg, encryptedKey };
		}

		public static string ReceiverConfident(byte[] encryptedMsg, byte[] encryptedKey, int n, int privateKey) {
			//receiver decrypt the key using RSA private key
			byte[] decryptedKey = RSA.DecryptRSA(encryptedMsg, n, privateKey);
			//receiver uses the key to symetric decrypt
			using (Aes aes = Aes.Create()) {
				return AES.DecryptStringFromBytes_Aes(encryptedMsg, decryptedKey, aes.IV);
			}
		}
	}

	public static class AES //pretty much just a copy paste from the MSDN
	{
		public static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
		{
			// Check arguments.
			if (plainText == null || plainText.Length <= 0)
				throw new ArgumentNullException("plainText");
			if (Key == null || Key.Length <= 0)
				throw new ArgumentNullException("Key");
			if (IV == null || IV.Length <= 0)
				throw new ArgumentNullException("IV");
			byte[] encrypted;

			// Create an Aes object
			// with the specified key and IV.
			using (Aes aesAlg = Aes.Create())
			{
				aesAlg.Key = Key;
				aesAlg.IV = IV;

				// Create an encryptor to perform the stream transform.
				ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

				// Create the streams used for encryption.
				using (MemoryStream msEncrypt = new MemoryStream())
				{
					using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					{
						using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
						{
							//Write all data to the stream.
							swEncrypt.Write(plainText);
						}
						encrypted = msEncrypt.ToArray();
					}
				}
			}


			// Return the encrypted bytes from the memory stream.
			return encrypted;

		}

		public static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
		{
			// Check arguments.
			if (cipherText == null || cipherText.Length <= 0)
				throw new ArgumentNullException("cipherText");
			if (Key == null || Key.Length <= 0)
				throw new ArgumentNullException("Key");
			if (IV == null || IV.Length <= 0)
				throw new ArgumentNullException("IV");

			// Declare the string used to hold
			// the decrypted text.
			string plaintext = null;

			// Create an Aes object
			// with the specified key and IV.
			using (Aes aesAlg = Aes.Create())
			{
				aesAlg.Key = Key;
				aesAlg.IV = IV;

				// Create a decryptor to perform the stream transform.
				ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

				// Create the streams used for decryption.
				using (MemoryStream msDecrypt = new MemoryStream(cipherText))
				{
					using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
					{
						using (StreamReader srDecrypt = new StreamReader(csDecrypt))
						{

							// Read the decrypted bytes from the decrypting stream
							// and place them in a string.
							plaintext = srDecrypt.ReadToEnd();
						}
					}
				}
			}
			return plaintext;
		}
	}
}