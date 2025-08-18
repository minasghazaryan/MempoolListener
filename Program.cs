using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace MempoolListener
{
    class Program
    {
        private static readonly string EthereumWebSocketUrl = "wss://mainnet.infura.io/ws/v3/YOUR-PROJECT-ID"; // You'll need to replace with your Infura project ID
        private static readonly string AlchemyWebSocketUrl = "wss://eth-mainnet.g.alchemy.com/v2/YOUR-API-KEY"; // Alternative: Alchemy
        private static readonly decimal WhaleThreshold = 100.0m; // ETH threshold for whale detection
        private static readonly decimal DollarWhaleThreshold = 10000m; // $10K USD threshold for Telegram alerts
        private static readonly Dictionary<string, decimal> TokenWhaleThresholds = new()
        {
            { "USDT", 1000000m }, // $1M USDT
            { "USDC", 1000000m }, // $1M USDC
            { "WBTC", 10m },      // 10 WBTC
            { "DAI", 1000000m },  // $1M DAI
            { "UNI", 100000m },   // 100k UNI
            { "LINK", 50000m }    // 50k LINK
        };

        // Telegram Configuration
        private static readonly string TelegramBotToken = "8352491052:AAHE-cUih9KCPoe3aeBIs7vXWS6aZ-IXLRg";
        private static readonly string TelegramChatId = "770515104";
        private static readonly decimal EthPriceUsd = 3000m; // Current ETH price (you can update this or fetch from API)

        static async Task Main(string[] args)
        {
            Console.WriteLine("🐋 Ethereum Mempool Whale Listener Starting...");
            Console.WriteLine($"Monitoring for transactions > {WhaleThreshold} ETH");
            Console.WriteLine($"📱 Telegram alerts for transactions > ${DollarWhaleThreshold:N0}");
            Console.WriteLine("Press Ctrl+C to exit\n");

            Console.WriteLine("🔑 Using Alchemy API key for real Ethereum data");
            Console.WriteLine("📡 Connecting to Ethereum mainnet...\n");

            // Check Telegram configuration
            if (TelegramBotToken == "YOUR_BOT_TOKEN" || TelegramChatId == "YOUR_CHAT_ID")
            {
                Console.WriteLine("⚠️  TELEGRAM NOT CONFIGURED!");
                Console.WriteLine("To enable Telegram alerts:");
                Console.WriteLine("1. Create a bot with @BotFather on Telegram");
                Console.WriteLine("2. Get your bot token");
                Console.WriteLine("3. Get your chat ID");
                Console.WriteLine("4. Update the TelegramBotToken and TelegramChatId variables\n");
            }
            else
            {
                Console.WriteLine("✅ Telegram integration configured");
                await SendTelegramMessage("🐋 Ethereum Whale Listener Started!\nMonitoring for transactions > $10,000 USD");
            }

            await ConnectToEthereum();
        }

        static async Task ConnectToEthereum()
        {
            using var client = new ClientWebSocket();
            
            try
            {
                // Using your Alchemy API key for real Ethereum data
                var wsUrl = "wss://eth-mainnet.g.alchemy.com/v2/IFLF7XZPqeolSBaCnaHqh";
                
                await client.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
                Console.WriteLine("✅ Connected to Ethereum WebSocket");

                // Subscribe to new pending transactions
                var subscribeMessage = JsonConvert.SerializeObject(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_subscribe",
                    @params = new[] { "newPendingTransactions" }
                });

                var subscribeBytes = Encoding.UTF8.GetBytes(subscribeMessage);
                await client.SendAsync(new ArraySegment<byte>(subscribeBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                Console.WriteLine("📡 Subscribed to Ethereum pending transaction feed\n");

                // Start listening for messages
                await ListenForTransactions(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Connection failed: {ex.Message}");
                Console.WriteLine("💡 Try using your own Infura or Alchemy API key for better reliability");
                await Task.Delay(5000); // Wait before retrying
                await ConnectToEthereum();
            }
        }

        static async Task ListenForTransactions(ClientWebSocket client)
        {
            var buffer = new byte[8192]; // Larger buffer for Ethereum data
            
            while (client.State == WebSocketState.Open)
            {
                try
                {
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _ = ProcessTransactionMessage(message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error receiving message: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine("🔌 WebSocket connection closed. Reconnecting...");
            await Task.Delay(3000);
            await ConnectToEthereum();
        }

        static Task ProcessTransactionMessage(string message)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<dynamic>(message);
                
                // Handle Ethereum subscription confirmation
                if (data?.result != null)
                {
                    Console.WriteLine($"📡 Subscription confirmed: {data.result}");
                    return Task.CompletedTask;
                }

                // Handle new pending transaction
                if (data?.@params?.result != null)
                {
                    var txHash = data.@params.result.ToString();
                    _ = AnalyzeEthereumTransaction(txHash);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing message: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }

        static async Task AnalyzeEthereumTransaction(string txHash)
        {
            try
            {
                // Fetch real transaction data from Alchemy API
                using var httpClient = new HttpClient();
                var requestBody = JsonConvert.SerializeObject(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_getTransactionByHash",
                    @params = new[] { txHash }
                });

                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("https://eth-mainnet.g.alchemy.com/v2/IFLF7XZPqeolSBaCnaHqh", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                var txData = JsonConvert.DeserializeObject<dynamic>(responseContent);

                if (txData?.result != null)
                {
                    var result = txData.result;
                    var value = result.value?.ToString() ?? "0";
                    var gasPrice = result.gasPrice?.ToString() ?? "0";
                    var gas = result.gas?.ToString() ?? "21000";
                    var to = result.to?.ToString() ?? "";
                    var from = result.from?.ToString() ?? "";

                    // Convert hex values to decimal
                    var ethValue = Convert.ToInt64(value, 16) / 1000000000000000000.0m; // Convert from Wei to ETH
                    var gasPriceGwei = Convert.ToInt64(gasPrice, 16) / 1000000000.0m; // Convert from Wei to Gwei
                    var gasUsed = Convert.ToInt32(gas, 16);
                    var fee = (gasPriceGwei * gasUsed) / 1000000000.0m; // Convert to ETH

                    // Calculate USD value
                    var usdValue = ethValue * EthPriceUsd;

                    // Check if this is a whale transaction
                    if (ethValue >= WhaleThreshold)
                    {
                        ReportWhaleTransaction(txHash, ethValue, fee, (int)gasPriceGwei, gasUsed, usdValue);
                    }

                    // Check for high fee transactions (potential whale activity)
                    if (fee >= 0.1m) // 0.1 ETH fee threshold
                    {
                        ReportHighFeeTransaction(txHash, ethValue, fee, (int)gasPriceGwei, gasUsed);
                    }

                    // Check for token transfers (simplified)
                    if (ethValue < 0.01m && gasUsed > 65000) // Likely token transfer
                    {
                        ReportTokenTransfer(txHash, ethValue, fee, gasUsed);
                    }

                    // Check for Telegram whale alerts (>$15K)
                    if (usdValue >= DollarWhaleThreshold)
                    {
                        await SendTelegramWhaleAlert(txHash, ethValue, fee, (int)gasPriceGwei, gasUsed, usdValue, from, to);
                    }

                    // Log all transactions for monitoring
                    Console.WriteLine($"📊 TX: {txHash.Substring(0, 10)}... | Value: {ethValue:F6} ETH (${usdValue:F2}) | Fee: {fee:F6} ETH | Gas: {gasUsed:N0}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error analyzing transaction {txHash}: {ex.Message}");
            }
        }

        static Task ReportWhaleTransaction(string txHash, decimal ethValue, decimal fee, int gasPrice, int gasUsed, decimal usdValue)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            Console.WriteLine($"🐋 ETH WHALE DETECTED! 🐋");
            Console.WriteLine($"⏰ Time: {timestamp}");
            Console.WriteLine($"🆔 TX Hash: {txHash}");
            Console.WriteLine($"💰 Value: {ethValue:F4} ETH (${usdValue:F2})");
            Console.WriteLine($"💸 Fee: {fee:F6} ETH");
            Console.WriteLine($"⛽ Gas Price: {gasPrice} Gwei");
            Console.WriteLine($"⛽ Gas Used: {gasUsed:N0}");
            Console.WriteLine($"🔗 Explorer: https://etherscan.io/tx/{txHash}");
            Console.WriteLine(new string('=', 80));
            
            return Task.CompletedTask;
        }

        static Task ReportHighFeeTransaction(string txHash, decimal ethValue, decimal fee, int gasPrice, int gasUsed)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            Console.WriteLine($"💎 HIGH FEE ETH TRANSACTION 💎");
            Console.WriteLine($"⏰ Time: {timestamp}");
            Console.WriteLine($"🆔 TX Hash: {txHash}");
            Console.WriteLine($"💰 Value: {ethValue:F4} ETH");
            Console.WriteLine($"💸 Fee: {fee:F6} ETH");
            Console.WriteLine($"⛽ Gas Price: {gasPrice} Gwei");
            Console.WriteLine($"⛽ Gas Used: {gasUsed:N0}");
            Console.WriteLine($"🔗 Explorer: https://etherscan.io/tx/{txHash}");
            Console.WriteLine(new string('-', 60));
            
            return Task.CompletedTask;
        }

        static Task ReportTokenTransfer(string txHash, decimal ethValue, decimal fee, int gasUsed)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            Console.WriteLine($"🪙 POTENTIAL TOKEN TRANSFER 🪙");
            Console.WriteLine($"⏰ Time: {timestamp}");
            Console.WriteLine($"🆔 TX Hash: {txHash}");
            Console.WriteLine($"💰 ETH Value: {ethValue:F6} ETH");
            Console.WriteLine($"💸 Fee: {fee:F6} ETH");
            Console.WriteLine($"⛽ Gas Used: {gasUsed:N0}");
            Console.WriteLine($"🔗 Explorer: https://etherscan.io/tx/{txHash}");
            Console.WriteLine(new string('-', 40));
            
            return Task.CompletedTask;
        }

        static async Task SendTelegramMessage(string message)
        {
            if (TelegramBotToken == "YOUR_BOT_TOKEN" || TelegramChatId == "YOUR_CHAT_ID")
            {
                return; // Telegram not configured
            }

            try
            {
                using var httpClient = new HttpClient();
                var telegramUrl = $"https://api.telegram.org/bot{TelegramBotToken}/sendMessage";
                
                var telegramData = new
                {
                    chat_id = TelegramChatId,
                    text = message,
                    parse_mode = "HTML"
                };

                var json = JsonConvert.SerializeObject(telegramData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(telegramUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"📱 Telegram message sent successfully");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to send Telegram message: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending Telegram message: {ex.Message}");
            }
        }

        static async Task SendTelegramWhaleAlert(string txHash, decimal ethValue, decimal fee, int gasPrice, int gasUsed, decimal usdValue, string from, string to)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var shortHash = txHash.Substring(0, 10) + "...";
            var shortFrom = from.Length > 10 ? from.Substring(0, 10) + "..." : from;
            var shortTo = to.Length > 10 ? to.Substring(0, 10) + "..." : to;

            var message = $"🐋 <b>WHALE ALERT!</b> 🐋\n\n" +
                         $"💰 <b>Value:</b> {ethValue:F4} ETH (${usdValue:N0})\n" +
                         $"⏰ <b>Time:</b> {timestamp}\n" +
                         $"🆔 <b>TX:</b> {shortHash}\n" +
                         $"💸 <b>Fee:</b> {fee:F6} ETH\n" +
                         $"⛽ <b>Gas:</b> {gasPrice} Gwei ({gasUsed:N0})\n" +
                         $"📤 <b>From:</b> {shortFrom}\n" +
                         $"📥 <b>To:</b> {shortTo}\n\n" +
                         $"🔗 <a href=\"https://etherscan.io/tx/{txHash}\">View on Etherscan</a>";

            await SendTelegramMessage(message);
        }
    }
}
