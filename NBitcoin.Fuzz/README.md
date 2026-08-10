# NBitcoin.Fuzz

SharpFuzz target for transaction deserialization (libFuzzer mode).

```bash
dotnet tool install --global SharpFuzz.CommandLine
dotnet publish NBitcoin.Fuzz -c Release -o out

# instrument the library under test
sharpfuzz out/NBitcoin.dll

# libfuzzer-dotnet from https://github.com/Metalnem/libfuzzer-dotnet
mkdir -p corpus && printf '\x39\xa7\x5f\x11\x00\x00\x00\x00\x00\x00' > corpus/empty-tx
libfuzzer-dotnet --target_path=out/NBitcoin.Fuzz corpus
```

Seeding `corpus/` with real raw transactions (legacy, segwit, coinbase) makes it
converge much faster.

The target asserts the canonical round-trip property: a buffer that parses as a
transaction and is fully consumed must re-serialize to the exact same bytes.
Against the code before [#1269](https://github.com/MetacoSA/NBitcoin/pull/1269),
`39a75f19 00 00 00000000` fails it — nVersion `0x195fa739` comes back as
`0x115fa739` because bit 27 was used as an internal marker.
