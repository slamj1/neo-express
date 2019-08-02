﻿using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace Neo.Express.Commands
{
    [Command("export")]
    internal class ExportCommand
    {
        [Option]
        private string Input { get; }

        private int OnExecute(CommandLineApplication app, IConsole console)
        {
            try
            {
                var (devChain, _) = DevChain.Load(Input);
                var password = Prompt.GetPassword("Input password to use for exported wallets");

                for (var i = 0; i < devChain.ConsensusNodes.Count; i++)
                {
                    var consensusNode = devChain.ConsensusNodes[i];
                    console.WriteLine($"Exporting {consensusNode.Wallet.Name} Conensus Node wallet");

                    var walletPath = Path.Combine(Directory.GetCurrentDirectory(), $"{consensusNode.Wallet.Name}.wallet.json");
                    if (File.Exists(walletPath))
                    {
                        File.Delete(walletPath);
                    }

                    consensusNode.Wallet.Export(walletPath, password);

                    WriteNodeConfigJson(password, consensusNode, walletPath);
                }

                WriteProtocolJson(devChain);

                return 0;
            }
            catch (Exception ex)
            {
                console.WriteError(ex.Message);
                app.ShowHelp();
                return 1;
            }
        }

        private static void WriteNodeConfigJson(string password, DevConsensusNode consensusNode, string walletPath)
        {
            using (var stream = File.Open(Path.Combine(Directory.GetCurrentDirectory(), $"{consensusNode.Wallet.Name}.config.json"), FileMode.Create, FileAccess.Write))
            using (var writer = new JsonTextWriter(new StreamWriter(stream)) { Formatting = Formatting.Indented })
            {
                writer.WriteStartObject();
                writer.WritePropertyName("ApplicationConfiguration");
                writer.WriteStartObject();

                writer.WritePropertyName("Paths");
                writer.WriteStartObject();
                writer.WritePropertyName("Chain");
                writer.WriteValue("Chain_{0}");
                writer.WriteEndObject();

                writer.WritePropertyName("P2P");
                writer.WriteStartObject();
                writer.WritePropertyName("Port");
                writer.WriteValue(consensusNode.TcpPort);
                writer.WritePropertyName("WsPort");
                writer.WriteValue(consensusNode.WebSocketPort);
                writer.WriteEndObject();

                writer.WritePropertyName("RPC");
                writer.WriteStartObject();
                writer.WritePropertyName("BindAddress");
                writer.WriteValue("127.0.0.1");
                writer.WritePropertyName("Port");
                writer.WriteValue(consensusNode.RpcPort);
                writer.WritePropertyName("SslCert");
                writer.WriteValue("");
                writer.WritePropertyName("SslCertPassword");
                writer.WriteValue("");
                writer.WriteEndObject();

                writer.WritePropertyName("UnlockWallet");
                writer.WriteStartObject();
                writer.WritePropertyName("Path");
                writer.WriteValue(walletPath);
                writer.WritePropertyName("Password");
                writer.WriteValue(password);
                writer.WritePropertyName("StartConsensus");
                writer.WriteValue(true);
                writer.WritePropertyName("IsActive");
                writer.WriteValue(true);
                writer.WriteEndObject();

                writer.WriteEndObject();
                writer.WriteEndObject();

            }
        }

        private static void WriteProtocolJson(DevChain chain)
        {
            using (var stream = File.Open(Path.Combine(Directory.GetCurrentDirectory(), "protocol.json"), FileMode.Create, FileAccess.Write))
            using (var writer = new JsonTextWriter(new StreamWriter(stream)) { Formatting = Formatting.Indented })
            {
                writer.WriteStartObject();
                writer.WritePropertyName("ProtocolConfiguration");
                writer.WriteStartObject();

                writer.WritePropertyName("Magic");
                writer.WriteValue(chain.Magic);
                writer.WritePropertyName("AddressVersion");
                writer.WriteValue(23);
                writer.WritePropertyName("SecondsPerBlock");
                writer.WriteValue(15);

                writer.WritePropertyName("StandbyValidators");
                writer.WriteStartArray();
                foreach (var conensusNode in chain.ConsensusNodes)
                {
                    writer.WriteValue(conensusNode.Wallet.GetAccounts().Single(a => a.IsDefault).GetKey().PublicKey.EncodePoint(true).ToHexString());
                }
                writer.WriteEndArray();

                writer.WritePropertyName("SeedList");
                writer.WriteStartArray();
                foreach (var node in chain.ConsensusNodes)
                {
                    writer.WriteValue($"{System.Net.IPAddress.Loopback}:{node.TcpPort}");
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
                writer.WriteEndObject();
            }
        }
    }
}