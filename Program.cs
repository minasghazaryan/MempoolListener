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
        private static readonly string MempoolWebSocketUrl = "wss://mempool.space/api/v1/ws";
        private static readonly decimal WhaleThreshold = 10.0m; // BTC threshold for whale detection
        private static readonly Dictionary<string, decimal> TokenWhaleThresholds = new()
        {
            { "USDT", 1000000m }, // $1M USDT
            { "USDC", 1000000m }, // $1M USDC
            { "ETH", 100m },      // 100 ETH
            { "WBTC", 10m }       // 10 WBTC
        };

        static async Task Main(string[] args)
        {
            Console.WriteLine("🐋 Mempool Whale Listener Starting...");
            Console.WriteLine($"Monitoring for transactions > {WhaleThreshold} BTC");
            Console.WriteLine("Press Ctrl+C to exit\n");

            await ConnectToMempool();
        }

        static async Task ConnectToMempool()
        {
            using var client = new ClientWebSocket();
            
            try
            {
                await client.ConnectAsync(new Uri(MempoolWebSocketUrl), CancellationToken.None);
                Console.WriteLine("✅ Connected to mempool.space WebSocket");

                // Subscribe to new transactions
                var subscribeMessage = JsonConvert.SerializeObject(new
                {
                    action = "want",
                    data = new[] { "transactions" }
                });

                var subscribeBytes = Encoding.UTF8.GetBytes(subscribeMessage);
                await client.SendAsync(new ArraySegment<byte>(subscribeBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                Console.WriteLine("📡 Subscribed to transaction feed\n");

                // Start listening for messages
                await ListenForTransactions(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Connection failed: {ex.Message}");
                await Task.Delay(5000); // Wait before retrying
                await ConnectToMempool();
            }
        }

        static async Task ListenForTransactions(ClientWebSocket client)
        {
            var buffer = new byte[4096];
            
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
            await ConnectToMempool();
        }

        static Task ProcessTransactionMessage(string message)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<dynamic>(message);
                
                if (data?.action?.ToString() == "tx")
                {
                    var txData = data.data;
                    AnalyzeTransaction(txData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing message: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }

        static Task AnalyzeTransaction(dynamic txData)
        {
            try
            {
                var txid = txData.txid?.ToString();
                var fee = txData.fee?.Value ?? 0;
                var weight = txData.weight?.Value ?? 0;
                var value = txData.value?.Value ?? 0;
                var status = txData.status?.ToString() ?? "unknown";

                // Convert satoshis to BTC
                var btcValue = value / 100000000m;
                var btcFee = fee / 100000000m;

                // Check if this is a whale transaction
                if (btcValue >= WhaleThreshold)
                {
                    ReportWhaleTransaction(txid, btcValue, btcFee, weight, status);
                }

                // Check for large fee transactions (potential whale activity)
                if (btcFee >= 0.01m) // 0.01 BTC fee threshold
                {
                    ReportHighFeeTransaction(txid, btcValue, btcFee, weight, status);
                }

                // Check for token transfers (if we can identify them)
                CheckForTokenTransfers(txData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error analyzing transaction: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }

        static Task ReportWhaleTransaction(string txid, decimal btcValue, decimal btcFee, int weight, string status)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            Console.WriteLine($"🐋 WHALE DETECTED! 🐋");
            Console.WriteLine($"⏰ Time: {timestamp}");
            Console.WriteLine($"🆔 TXID: {txid}");
            Console.WriteLine($"💰 Value: {btcValue:F8} BTC (${btcValue * 50000:F2})");
            Console.WriteLine($"💸 Fee: {btcFee:F8} BTC");
            Console.WriteLine($"⚖️  Weight: {weight:N0} vB");
            Console.WriteLine($"📊 Status: {status}");
            Console.WriteLine($"🔗 Explorer: https://mempool.space/tx/{txid}");
            Console.WriteLine(new string('=', 80));
            
            // You could add notification logic here (email, webhook, etc.)
            SendNotification($"🐋 Whale Transaction Detected: {btcValue:F8} BTC", txid);
            
            return Task.CompletedTask;
        }

        static Task ReportHighFeeTransaction(string txid, decimal btcValue, decimal btcFee, int weight, string status)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            Console.WriteLine($"💎 HIGH FEE TRANSACTION 💎");
            Console.WriteLine($"⏰ Time: {timestamp}");
            Console.WriteLine($"🆔 TXID: {txid}");
            Console.WriteLine($"💰 Value: {btcValue:F8} BTC");
            Console.WriteLine($"💸 Fee: {btcFee:F8} BTC");
            Console.WriteLine($"⚖️  Weight: {weight:N0} vB");
            Console.WriteLine($"📊 Status: {status}");
            Console.WriteLine($"🔗 Explorer: https://mempool.space/tx/{txid}");
            Console.WriteLine(new string('-', 60));
            
            return Task.CompletedTask;
        }

        static Task CheckForTokenTransfers(dynamic txData)
        {
            // This is a simplified check - in a real implementation you'd need to
            // decode OP_RETURN data and check for token transfer patterns
            try
            {
                // Look for large outputs that might be token transfers
                if (txData.vout != null)
                {
                    foreach (var output in txData.vout)
                    {
                        var value = output.value?.Value ?? 0;
                        var btcValue = value / 100000000m;
                        
                        // Check if this looks like a token transfer (small BTC amount with large data)
                        if (btcValue < 0.001m && output.scriptpubkey?.ToString().Length > 100)
                        {
                            Console.WriteLine($"🪙 Potential Token Transfer Detected");
                            Console.WriteLine($"🆔 TXID: {txData.txid}");
                            Console.WriteLine($"💰 BTC Value: {btcValue:F8}");
                            Console.WriteLine($"📜 Script Length: {output.scriptpubkey?.ToString().Length}");
                            Console.WriteLine(new string('-', 40));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silently handle token analysis errors
            }
            
            return Task.CompletedTask;
        }

        static Task SendNotification(string message, string txid)
        {
            // Placeholder for notification logic
            // You could implement:
            // - Email notifications
            // - Discord webhook
            // - Telegram bot
            // - WebSocket push to frontend
            // - Database logging
            
            Console.WriteLine($"📢 NOTIFICATION: {message}");
            
            // Example webhook implementation:
            /*
            using var httpClient = new HttpClient();
            var webhookData = new
            {
                content = message,
                embeds = new[]
                {
                    new
                    {
                        title = "Whale Transaction Detected",
                        description = $"Transaction: {txid}",
                        color = 0x00ff00,
                        url = $"https://mempool.space/tx/{txid}"
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
