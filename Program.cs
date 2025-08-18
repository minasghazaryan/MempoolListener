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
        private static readonly Dictionary<string, decimal> TokenWhaleThresholds = new()
        {
            { "USDT", 1000000m }, // $1M USDT
            { "USDC", 1000000m }, // $1M USDC
            { "WBTC", 10m },      // 10 WBTC
            { "DAI", 1000000m },  // $1M DAI
            { "UNI", 100000m },   // 100k UNI
            { "LINK", 50000m }    // 50k LINK
        };

        static async Task Main(string[] args)
        {
            Console.WriteLine("🐋 Ethereum Mempool Whale Listener Starting...");
            Console.WriteLine($"Monitoring for transactions > {WhaleThreshold} ETH");
            Console.WriteLine("Press Ctrl+C to exit\n");

            // Note: You'll need to set up your own Infura or Alchemy API key
            Console.WriteLine("⚠️  IMPORTANT: You need to set up your own API key!");
            Console.WriteLine("1. Go to https://infura.io/ or https://www.alchemy.com/");
            Console.WriteLine("2. Create a free account and get your API key");
            Console.WriteLine("3. Replace 'YOUR-PROJECT-ID' in the code with your actual key\n");

            await ConnectToEthereum();
        }

        static async Task ConnectToEthereum()
        {
            using var client = new ClientWebSocket();
            
            try
            {
                // For now, we'll use a public Ethereum WebSocket endpoint
                // In production, you should use your own Infura/Alchemy API key
                var wsUrl = "wss://eth-mainnet.g.alchemy.com/v2/demo"; // Demo endpoint
                
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
                // For demo purposes, we'll simulate transaction analysis
                // In a real implementation, you'd fetch transaction details from the blockchain
                
                // Simulate random transaction values for demonstration
                var random = new Random();
                var ethValue = random.Next(1, 1000) / 10.0m; // Random ETH value 0.1 to 100
                var gasPrice = random.Next(20, 200); // Gwei
                var gasUsed = random.Next(21000, 500000); // Gas used
                var fee = (gasPrice * gasUsed) / 1000000000.0m; // Convert to ETH

                // Check if this is a whale transaction
                if (ethValue >= WhaleThreshold)
                {
                    ReportWhaleTransaction(txHash, ethValue, fee, gasPrice, gasUsed);
                }

                // Check for high fee transactions (potential whale activity)
                if (fee >= 0.1m) // 0.1 ETH fee threshold
                {
                    ReportHighFeeTransaction(txHash, ethValue, fee, gasPrice, gasUsed);
                }

                // Check for token transfers (simplified)
                if (ethValue < 0.01m && gasUsed > 65000) // Likely token transfer
                {
                    ReportTokenTransfer(txHash, ethValue, fee, gasUsed);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error analyzing transaction: {ex.Message}");
            }
        }

        static Task ReportWhaleTransaction(string txHash, decimal ethValue, decimal fee, int gasPrice, int gasUsed)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var ethPrice = 3000m; // Approximate ETH price for demo
            
            Console.WriteLine($"🐋 ETH WHALE DETECTED! 🐋");
            Console.WriteLine($"⏰ Time: {timestamp}");
            Console.WriteLine($"🆔 TX Hash: {txHash}");
            Console.WriteLine($"💰 Value: {ethValue:F4} ETH (${ethValue * ethPrice:F2})");
            Console.WriteLine($"💸 Fee: {fee:F6} ETH");
            Console.WriteLine($"⛽ Gas Price: {gasPrice} Gwei");
            Console.WriteLine($"⛽ Gas Used: {gasUsed:N0}");
            Console.WriteLine($"🔗 Explorer: https://etherscan.io/tx/{txHash}");
            Console.WriteLine(new string('=', 80));
            
            SendNotification($"🐋 ETH Whale Transaction Detected: {ethValue:F4} ETH", txHash);
            
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

        static Task SendNotification(string message, string txHash)
        {
            Console.WriteLine($"📢 NOTIFICATION: {message}");
            
            // Example webhook implementation for ETH:
            /*
            using var httpClient = new HttpClient();
            var webhookData = new
            {
                content = message,
                embeds = new[]
                {
                    new
                    {
                        title = "ETH Whale Transaction Detected",
                        description = $"Transaction: {txHash}",
                        color = 0x00ff00,
                        url = $"https://etherscan.io/tx/{txHash}"
                    }
                }
            };
            
            var json = JsonConvert.SerializeObject(webhookData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await httpClient.PostAsync("YOUR_WEBHOOK_URL", content);
            */
            
            return Task.CompletedTask;
        }
    }
}
