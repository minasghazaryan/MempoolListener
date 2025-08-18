using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MempoolListener
{
    class Program
    {
        private static readonly string EthereumWebSocketUrl = "wss://mainnet.infura.io/ws/v3/YOUR-PROJECT-ID";
        private static readonly string AlchemyWebSocketUrl = "wss://eth-mainnet.g.alchemy.com/v2/YOUR-API-KEY";
        private static readonly decimal WhaleThreshold = 100.0m; // ETH threshold for whale detection
        private static readonly decimal DollarWhaleThreshold = 10000m; // $10K USD threshold for Telegram alerts
        private static readonly decimal CopyTradeThreshold = 50000m; // $50K USD for copy trading
        private static readonly Dictionary<string, decimal> TokenWhaleThresholds = new()
        {
            { "USDT", 1000000m },
            { "USDC", 1000000m },
            { "WBTC", 10m },
            { "DAI", 1000000m },
            { "UNI", 100000m },
            { "LINK", 50000m }
        };

        // Telegram Configuration
        private static readonly string TelegramBotToken = "8352491052:AAHE-cUih9KCPoe3aeBIs7vXWS6aZ-IXLRg";
        private static readonly string TelegramChatId = "770515104";
        private static readonly decimal EthPriceUsd = 3000m;

        // Whale Tracking
        private static readonly Dictionary<string, WhaleTracker> WhaleWallets = new();
        private static readonly List<WhaleTransaction> WhaleHistory = new();
        private static readonly Dictionary<string, TokenAnalysis> TokenAnalytics = new();
        private static readonly List<CopyTradeSignal> CopyTradeSignals = new();

        // Configuration
        private static readonly int MaxWhaleHistory = 1000;
        private static readonly int PatternAnalysisWindow = 24; // hours
        private static readonly decimal MinSuccessRate = 0.7m; // 70% success rate for copy trading

        static async Task Main(string[] args)
        {
            Console.WriteLine("🐋 Ethereum Whale Tracker & Copy Trading Strategy Starting...");
            Console.WriteLine($"Monitoring for transactions > {WhaleThreshold} ETH");
            Console.WriteLine($"📱 Telegram alerts for transactions > ${DollarWhaleThreshold:N0}");
            Console.WriteLine($"🎯 Copy trading signals for transactions > ${CopyTradeThreshold:N0}");
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
                await SendTelegramMessage("🐋 Whale Tracker & Copy Trading Started!\nMonitoring for transactions > $10,000 USD\nCopy trading signals > $50,000 USD");
            }

            // Start background tasks
            _ = Task.Run(WhalePatternAnalysis);
            _ = Task.Run(GenerateCopyTradeSignals);
            _ = Task.Run(SaveWhaleData);

            await ConnectToEthereum();
        }

        static async Task ConnectToEthereum()
        {
            using var client = new ClientWebSocket();
            
            try
            {
                var wsUrl = "wss://eth-mainnet.g.alchemy.com/v2/IFLF7XZPqeolSBaCnaHqh";
                
                await client.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
                Console.WriteLine("✅ Connected to Ethereum WebSocket");

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

                await ListenForTransactions(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Connection failed: {ex.Message}");
                await Task.Delay(5000);
                await ConnectToEthereum();
            }
        }

        static async Task ListenForTransactions(ClientWebSocket client)
        {
            var buffer = new byte[8192];
            
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
                
                if (data?.result != null)
                {
                    Console.WriteLine($"📡 Subscription confirmed: {data.result}");
                    return Task.CompletedTask;
                }

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
                    var input = result.input?.ToString() ?? "";

                    var ethValue = Convert.ToInt64(value, 16) / 1000000000000000000.0m;
                    var gasPriceGwei = Convert.ToInt64(gasPrice, 16) / 1000000000.0m;
                    var gasUsed = Convert.ToInt32(gas, 16);
                    var fee = (gasPriceGwei * gasUsed) / 1000000000.0m;
                    var usdValue = ethValue * EthPriceUsd;

                    // Track whale wallets
                    if (usdValue >= DollarWhaleThreshold)
                    {
                        TrackWhaleWallet(from, to, ethValue, usdValue, fee, gasUsed, input, txHash);
                    }

                    // Check for whale transactions
                    if (ethValue >= WhaleThreshold)
                    {
                        ReportWhaleTransaction(txHash, ethValue, fee, (int)gasPriceGwei, gasUsed, usdValue);
                    }

                    // Check for high fee transactions
                    if (fee >= 0.1m)
                    {
                        ReportHighFeeTransaction(txHash, ethValue, fee, (int)gasPriceGwei, gasUsed);
                    }

                    // Check for token transfers
                    if (ethValue < 0.01m && gasUsed > 65000)
                    {
                        ReportTokenTransfer(txHash, ethValue, fee, gasUsed);
                    }

                    // Check for copy trading signals
                    if (usdValue >= CopyTradeThreshold)
                    {
                        await AnalyzeCopyTradeSignal(txHash, ethValue, fee, (int)gasPriceGwei, gasUsed, usdValue, from, to, input);
                    }

                    // Telegram whale alerts
                    if (usdValue >= DollarWhaleThreshold)
                    {
                        await SendTelegramWhaleAlert(txHash, ethValue, fee, (int)gasPriceGwei, gasUsed, usdValue, from, to);
                    }

                    Console.WriteLine($"📊 TX: {txHash.Substring(0, 10)}... | Value: {ethValue:F6} ETH (${usdValue:F2}) | Fee: {fee:F6} ETH | Gas: {gasUsed:N0}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error analyzing transaction {txHash}: {ex.Message}");
            }
        }

        static void TrackWhaleWallet(string from, string to, decimal ethValue, decimal usdValue, decimal fee, int gasUsed, string input, string txHash)
        {
            var timestamp = DateTime.Now;
            var whaleTx = new WhaleTransaction
            {
                Timestamp = timestamp,
                From = from,
                To = to,
                EthValue = ethValue,
                UsdValue = usdValue,
                Fee = fee,
                GasUsed = gasUsed,
                Input = input,
                TxHash = txHash,
                IsBuy = IsBuyTransaction(input, to),
                IsSell = IsSellTransaction(input, from)
            };

            lock (WhaleHistory)
            {
                WhaleHistory.Add(whaleTx);
                if (WhaleHistory.Count > MaxWhaleHistory)
                {
                    WhaleHistory.RemoveAt(0);
                }
            }

            // Track individual whale wallets
            if (!string.IsNullOrEmpty(from))
            {
                TrackWallet(from, whaleTx, true);
            }
            if (!string.IsNullOrEmpty(to))
            {
                TrackWallet(to, whaleTx, false);
            }
        }

        static void TrackWallet(string address, WhaleTransaction tx, bool isSender)
        {
            if (!WhaleWallets.ContainsKey(address))
            {
                WhaleWallets[address] = new WhaleTracker
                {
                    Address = address,
                    FirstSeen = DateTime.Now,
                    TotalVolume = 0,
                    TransactionCount = 0,
                    BuyCount = 0,
                    SellCount = 0,
                    AverageTransactionSize = 0,
                    SuccessRate = 0,
                    LastActivity = DateTime.Now
                };
            }

            var wallet = WhaleWallets[address];
            wallet.TotalVolume += tx.UsdValue;
            wallet.TransactionCount++;
            wallet.LastActivity = DateTime.Now;

            if (tx.IsBuy) wallet.BuyCount++;
            if (tx.IsSell) wallet.SellCount++;

            wallet.AverageTransactionSize = wallet.TotalVolume / wallet.TransactionCount;
        }

        static bool IsBuyTransaction(string input, string to)
        {
            // Check if this is a buy transaction (e.g., Uniswap buy, DEX interaction)
            return !string.IsNullOrEmpty(input) && input.Length > 10 && 
                   (input.StartsWith("0xa9059cbb") || // ERC20 transfer
                    input.StartsWith("0x23b872dd") || // ERC20 transferFrom
                    input.StartsWith("0x38ed1739"));  // Uniswap swap
        }

        static bool IsSellTransaction(string input, string from)
        {
            // Check if this is a sell transaction
            return !string.IsNullOrEmpty(input) && input.Length > 10 && 
                   (input.StartsWith("0xa9059cbb") || // ERC20 transfer
                    input.StartsWith("0x23b872dd") || // ERC20 transferFrom
                    input.StartsWith("0x38ed1739"));  // Uniswap swap
        }

        static async Task AnalyzeCopyTradeSignal(string txHash, decimal ethValue, decimal fee, int gasPrice, int gasUsed, decimal usdValue, string from, string to, string input)
        {
            // Analyze if this whale has a good track record
            if (WhaleWallets.ContainsKey(from))
            {
                var whale = WhaleWallets[from];
                var successRate = CalculateWhaleSuccessRate(from);
                
                if (successRate >= MinSuccessRate && whale.TransactionCount >= 5)
                {
                    var signal = new CopyTradeSignal
                    {
                        Timestamp = DateTime.Now,
                        WhaleAddress = from,
                        TargetAddress = to,
                        EthValue = ethValue,
                        UsdValue = usdValue,
                        SuccessRate = successRate,
                        TransactionCount = whale.TransactionCount,
                        SignalType = DetermineSignalType(input, to),
                        Confidence = CalculateConfidence(whale, successRate),
                        TxHash = txHash
                    };

                    lock (CopyTradeSignals)
                    {
                        CopyTradeSignals.Add(signal);
                        if (CopyTradeSignals.Count > 100)
                        {
                            CopyTradeSignals.RemoveAt(0);
                        }
                    }

                    await SendCopyTradeAlert(signal);
                }
            }
        }

        static decimal CalculateWhaleSuccessRate(string address)
        {
            // Calculate success rate based on historical performance
            // This is a simplified version - in reality, you'd track actual price movements
            var recentTxs = WhaleHistory.Where(tx => tx.From == address && 
                                                    tx.Timestamp > DateTime.Now.AddHours(-PatternAnalysisWindow))
                                       .ToList();

            if (recentTxs.Count < 3) return 0.5m;

            // Simplified success calculation (in reality, you'd check actual price impact)
            var buyTxs = recentTxs.Count(tx => tx.IsBuy);
            var sellTxs = recentTxs.Count(tx => tx.IsSell);
            
            return buyTxs > sellTxs ? 0.8m : 0.3m;
        }

        static string DetermineSignalType(string input, string to)
        {
            if (string.IsNullOrEmpty(input) || input.Length < 10)
                return "ETH_TRANSFER";

            if (input.StartsWith("0x38ed1739"))
                return "UNISWAP_SWAP";
            if (input.StartsWith("0xa9059cbb"))
                return "TOKEN_TRANSFER";
            if (input.StartsWith("0x23b872dd"))
                return "TOKEN_TRANSFER_FROM";

            return "CONTRACT_INTERACTION";
        }

        static decimal CalculateConfidence(WhaleTracker whale, decimal successRate)
        {
            var volumeScore = Math.Min(whale.TotalVolume / 1000000m, 1.0m); // Normalize to 0-1
            var frequencyScore = Math.Min(whale.TransactionCount / 50m, 1.0m);
            var recencyScore = (DateTime.Now - whale.LastActivity).TotalHours < 24 ? 1.0m : 0.5m;

            return (successRate * 0.4m + volumeScore * 0.3m + frequencyScore * 0.2m + recencyScore * 0.1m);
        }

        static async Task WhalePatternAnalysis()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5)); // Run every 5 minutes

                    var topWhales = WhaleWallets.Values
                        .OrderByDescending(w => w.TotalVolume)
                        .Take(10)
                        .ToList();

                    if (topWhales.Any())
                    {
                        var analysis = "🐋 **WHALE PATTERN ANALYSIS** 🐋\n\n";
                        analysis += $"📊 Top 10 Whales (Last 24h):\n\n";

                        foreach (var whale in topWhales)
                        {
                            var successRate = CalculateWhaleSuccessRate(whale.Address);
                            analysis += $"🐳 **{whale.Address.Substring(0, 8)}...**\n";
                            analysis += $"💰 Volume: ${whale.TotalVolume:N0}\n";
                            analysis += $"📈 Txs: {whale.TransactionCount} | Success: {successRate:P0}\n";
                            analysis += $"🔄 Buy/Sell: {whale.BuyCount}/{whale.SellCount}\n";
                            analysis += $"⏰ Last: {whale.LastActivity:HH:mm}\n\n";
                        }

                        await SendTelegramMessage(analysis);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error in whale pattern analysis: {ex.Message}");
                }
            }
        }

        static async Task GenerateCopyTradeSignals()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(10)); // Run every 10 minutes

                    var highConfidenceSignals = CopyTradeSignals
                        .Where(s => s.Confidence >= 0.8m && s.Timestamp > DateTime.Now.AddHours(-1))
                        .OrderByDescending(s => s.Confidence)
                        .Take(5)
                        .ToList();

                    if (highConfidenceSignals.Any())
                    {
                        var signals = "🎯 **HIGH CONFIDENCE COPY TRADE SIGNALS** 🎯\n\n";

                        foreach (var signal in highConfidenceSignals)
                        {
                            signals += $"🐋 Whale: {signal.WhaleAddress.Substring(0, 8)}...\n";
                            signals += $"💰 Value: ${signal.UsdValue:N0}\n";
                            signals += $"📊 Confidence: {signal.Confidence:P0}\n";
                            signals += $"🎯 Type: {signal.SignalType}\n";
                            signals += $"⏰ Time: {signal.Timestamp:HH:mm}\n\n";
                        }

                        await SendTelegramMessage(signals);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error generating copy trade signals: {ex.Message}");
                }
            }
        }

        static async Task SaveWhaleData()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(30)); // Save every 30 minutes

                    var whaleData = new
                    {
                        Timestamp = DateTime.Now,
                        WhaleWallets = WhaleWallets.Values.ToList(),
                        WhaleHistory = WhaleHistory.TakeLast(100).ToList(),
                        CopyTradeSignals = CopyTradeSignals.TakeLast(50).ToList()
                    };

                    var json = JsonConvert.SerializeObject(whaleData, Formatting.Indented);
                    await File.WriteAllTextAsync($"whale_data_{DateTime.Now:yyyyMMdd_HHmm}.json", json);

                    Console.WriteLine($"💾 Whale data saved: {whaleData.WhaleWallets.Count} wallets, {whaleData.WhaleHistory.Count} transactions");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error saving whale data: {ex.Message}");
                }
            }
        }

        static async Task SendCopyTradeAlert(CopyTradeSignal signal)
        {
            var message = $"🎯 **COPY TRADE SIGNAL** 🎯\n\n" +
                         $"🐋 **Whale:** {signal.WhaleAddress.Substring(0, 10)}...\n" +
                         $"💰 **Value:** {signal.EthValue:F4} ETH (${signal.UsdValue:N0})\n" +
                         $"📊 **Confidence:** {signal.Confidence:P0}\n" +
                         $"🎯 **Type:** {signal.SignalType}\n" +
                         $"📈 **Success Rate:** {signal.SuccessRate:P0}\n" +
                         $"🔄 **Tx Count:** {signal.TransactionCount}\n" +
                         $"⏰ **Time:** {signal.Timestamp:HH:mm:ss}\n\n" +
                         $"🔗 <a href=\"https://etherscan.io/tx/{signal.TxHash}\">View Transaction</a>";

            await SendTelegramMessage(message);
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
                return;
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

    // Data Models
    public class WhaleTracker
    {
        public string Address { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastActivity { get; set; }
        public decimal TotalVolume { get; set; }
        public int TransactionCount { get; set; }
        public int BuyCount { get; set; }
        public int SellCount { get; set; }
        public decimal AverageTransactionSize { get; set; }
        public decimal SuccessRate { get; set; }
    }

    public class WhaleTransaction
    {
        public DateTime Timestamp { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public decimal EthValue { get; set; }
        public decimal UsdValue { get; set; }
        public decimal Fee { get; set; }
        public int GasUsed { get; set; }
        public string Input { get; set; }
        public string TxHash { get; set; }
        public bool IsBuy { get; set; }
        public bool IsSell { get; set; }
    }

    public class CopyTradeSignal
    {
        public DateTime Timestamp { get; set; }
        public string WhaleAddress { get; set; }
        public string TargetAddress { get; set; }
        public decimal EthValue { get; set; }
        public decimal UsdValue { get; set; }
        public decimal SuccessRate { get; set; }
        public int TransactionCount { get; set; }
        public string SignalType { get; set; }
        public decimal Confidence { get; set; }
        public string TxHash { get; set; }
    }

    public class TokenAnalysis
    {
        public string TokenAddress { get; set; }
        public string TokenSymbol { get; set; }
        public decimal TotalVolume { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTransactionSize { get; set; }
        public DateTime LastActivity { get; set; }
    }
}
