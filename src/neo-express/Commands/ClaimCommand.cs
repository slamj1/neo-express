﻿using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace NeoExpress.Commands
{
    [Command("claim")]

    class ClaimCommand
    {
        [Argument(0)]
        private string Asset { get; }

        [Argument(1)]
        private string Account { get; }

        [Option]
        private string Input { get; }

        private async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            try
            {
                var (chain, _) = Program.LoadExpressChain(Input);
                var account = chain.GetAccount(Account);
                if (account == default)
                {
                    throw new Exception($"{Account} account not found.");
                }

                var uri = chain.GetUri();
                var result = await NeoRpcClient.ExpressClaim(uri, Asset, account.ScriptHash)
                    .ConfigureAwait(false);
                console.WriteLine(result.ToString(Formatting.Indented));

                var txid = result["txid"];
                if (txid != null)
                {
                    console.WriteLine("transfer complete");
                }
                else
                {
                    // TODO: DRY + standardize genesis vs. standard signing
                    var data = result.Value<string>("hash-data").ToByteArray();
                    var signatures = new JArray(account.Sign(data));
                    var result2 = await NeoRpcClient.ExpressSubmitSignatures(uri, result["contract-context"], signatures);
                    console.WriteLine(result2.ToString(Formatting.Indented));
                }

                return 0;
            }
            catch (Exception ex)
            {
                console.WriteError(ex.Message);
                app.ShowHelp();
                return 1;
            }
        }
    }
}
