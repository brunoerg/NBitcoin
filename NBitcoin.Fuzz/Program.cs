using System;
using System.IO;
using System.Linq;
using NBitcoin;
using NBitcoin.DataEncoders;
using SharpFuzz;

namespace NBitcoin.Fuzz
{
	/// <summary>
	/// Fuzzes transaction deserialization against the canonical round-trip property:
	/// if a buffer parses as a transaction and is fully consumed, re-serializing that
	/// transaction must produce the very same bytes.
	///
	/// This catches the bug fixed by https://github.com/MetacoSA/NBitcoin/pull/1269:
	/// bit 27 of nVersion was used as an internal "no dummy input" marker, so a real
	/// transaction whose version happened to have that bit set (0x195fa739 for the
	/// coinbase of block 896727) had the bit silently stripped while parsing, and the
	/// BIP144 marker/flag was misread. Minimal reproducer: 39a75f19 00 00 00000000
	/// (version 0x195fa739, no input, no output, locktime 0) parses to version
	/// 0x115fa739 and re-serializes as 39a75f11 00 00 00000000.
	/// </summary>
	public class Program
	{
		static readonly ConsensusFactory Factory = Network.Main.Consensus.ConsensusFactory;

		public static void Main() => Fuzzer.LibFuzzer.Run(data => RoundTrip(data.ToArray()));

		static void RoundTrip(byte[] data)
		{
			var tx = Factory.CreateTransaction();
			var input = new MemoryStream(data);
			var reader = new BitcoinStream(input, false) { ConsensusFactory = Factory, AllowNoInputs = true };
			try
			{
				tx.ReadWrite(reader);
			}
			// Expected ways of rejecting a malformed buffer.
			catch (FormatException) { return; }
			catch (EndOfStreamException) { return; }
			catch (InvalidDataException) { return; }
			catch (ArgumentOutOfRangeException) { return; }

			// Not a transaction encoding, just a transaction followed by garbage.
			if (input.Position != input.Length)
				return;

			var output = new MemoryStream();
			tx.ReadWrite(new BitcoinStream(output, true) { ConsensusFactory = Factory, AllowNoInputs = true });

			if (!output.ToArray().SequenceEqual(data))
				throw new Exception($"round-trip mismatch: {Encoders.Hex.EncodeData(data)} != {Encoders.Hex.EncodeData(output.ToArray())}");
		}
	}
}
