# 🐋 Ethereum Mempool Whale Listener

A real-time Ethereum mempool monitoring application that detects and alerts on whale transactions and large transfers.

## Features

- **Real-time Monitoring**: Connects to Ethereum WebSocket API for live transaction monitoring
- **Whale Detection**: Identifies transactions above configurable thresholds (default: 100 ETH)
- **High Fee Detection**: Alerts on transactions with unusually high gas fees
- **Token Transfer Detection**: Basic detection of potential ERC-20 token transfers
- **Configurable Thresholds**: Easy customization via `appsettings.json`
- **Multiple Notification Options**: Support for Discord, Telegram, and email notifications
- **Comprehensive Logging**: Console and file logging with Serilog
- **Auto-reconnection**: Automatic reconnection on connection loss

## Quick Start

### Prerequisites

- .NET 9.0 or later
- Internet connection for Ethereum API access
- **Infura or Alchemy API key** (free tier available)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/minasghazaryan/MempoolListener.git
cd MempoolListener
```

2. Get your API key:
   - **Infura**: Go to [https://infura.io/](https://infura.io/) and create a free account
   - **Alchemy**: Go to [https://www.alchemy.com/](https://www.alchemy.com/) and create a free account

3. Update the WebSocket URL in `Program.cs`:
   ```csharp
   // Replace with your actual API key
   var wsUrl = "wss://eth-mainnet.g.alchemy.com/v2/YOUR-API-KEY";
   ```

4. Restore dependencies:
```bash
dotnet restore
```

5. Run the application:
```bash
dotnet run
```

## Configuration

Edit `appsettings.json` to customize the application behavior:

### Whale Thresholds
```json
"WhaleThresholds": {
  "ETH": 100.0,        // Minimum ETH amount to trigger whale alert
  "USDT": 1000000,     // Minimum USDT amount
  "USDC": 1000000,     // Minimum USDC amount
  "WBTC": 10,          // Minimum WBTC amount
  "DAI": 1000000,      // Minimum DAI amount
  "UNI": 100000,       // Minimum UNI amount
  "LINK": 50000        // Minimum LINK amount
}
```

### High Fee Detection
```json
"HighFeeThreshold": 0.1  // Minimum ETH fee to trigger high fee alert
```

### Notifications

#### Discord Webhook
```json
"DiscordWebhookUrl": "https://discord.com/api/webhooks/YOUR_WEBHOOK_URL"
```

#### Telegram Bot
```json
"TelegramBotToken": "YOUR_BOT_TOKEN",
"TelegramChatId": "YOUR_CHAT_ID"
```

#### Email Notifications
```json
"EmailSmtpServer": "smtp.gmail.com",
"EmailFrom": "your-email@gmail.com",
"EmailTo": "recipient@example.com",
"EmailPassword": "your-app-password"
```

## Usage

### Basic Monitoring
```bash
dotnet run
```

The application will:
1. Connect to Ethereum WebSocket API
2. Subscribe to pending transaction feed
3. Monitor all incoming Ethereum transactions
4. Alert when whale transactions are detected
5. Log all activity to console and file

### Output Example
```
🐋 Ethereum Mempool Whale Listener Starting...
Monitoring for transactions > 100 ETH
Press Ctrl+C to exit

⚠️  IMPORTANT: You need to set up your own API key!
1. Go to https://infura.io/ or https://www.alchemy.com/
2. Create a free account and get your API key
3. Replace 'YOUR-PROJECT-ID' in the code with your actual key

✅ Connected to Ethereum WebSocket
📡 Subscribed to Ethereum pending transaction feed

🐋 ETH WHALE DETECTED! 🐋
⏰ Time: 2024-01-15 14:30:25
🆔 TX Hash: 0xabc123def456...
💰 Value: 150.5000 ETH ($451,500.00)
💸 Fee: 0.001234 ETH
⛽ Gas Price: 45 Gwei
⛽ Gas Used: 21,000
🔗 Explorer: https://etherscan.io/tx/0xabc123def456...
================================================================================
```

## Advanced Features

### Custom Whale Detection
The application can detect different types of whale activity:

1. **Large ETH Transfers**: Transactions above the ETH threshold
2. **High Gas Fee Transactions**: Transactions with unusually high gas fees
3. **Token Transfers**: Basic detection of potential ERC-20 token transfers
4. **Exchange Activity**: Large transactions that might indicate exchange movements

### Logging
- Console output with colored emojis for easy reading
- File logging with daily rotation
- Structured logging with Serilog
- Log files stored in `logs/` directory

### Error Handling
- Automatic reconnection on connection loss
- Graceful error handling for malformed messages
- Timeout handling for network issues

## Development

### Project Structure
```
MempoolListener/
├── Program.cs              # Main application logic
├── appsettings.json        # Configuration file
├── MempoolListener.csproj  # Project file
├── README.md              # This file
└── logs/                  # Log files (created at runtime)
```

### Adding New Features
1. **New Token Support**: Add token contract addresses and thresholds to configuration
2. **Additional Notifications**: Implement new notification providers
3. **Enhanced Analysis**: Add more sophisticated transaction analysis
4. **Database Integration**: Store transaction data for historical analysis

## API Reference

### Ethereum WebSocket API
The application connects to Ethereum WebSocket APIs:
- **Infura**: `wss://mainnet.infura.io/ws/v3/YOUR-PROJECT-ID`
- **Alchemy**: `wss://eth-mainnet.g.alchemy.com/v2/YOUR-API-KEY`

### Transaction Data Structure
```json
{
  "jsonrpc": "2.0",
  "method": "eth_subscription",
  "params": {
    "subscription": "0x1234567890abcdef",
    "result": "0xtransaction_hash"
  }
}
```

## Troubleshooting

### Common Issues

1. **Connection Failed**
   - Check internet connection
   - Verify your API key is correct
   - Check if you've exceeded your API rate limits

2. **No Whale Alerts**
   - Verify threshold settings in `appsettings.json`
   - Check if thresholds are too high for current market activity
   - Ensure your API key has sufficient permissions

3. **High Memory Usage**
   - Monitor log file sizes
   - Adjust logging levels if needed

### Debug Mode
Enable debug logging by changing the log level in `appsettings.json`:
```json
"LogLevel": {
  "Default": "Debug"
}
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Disclaimer

This application is for educational and monitoring purposes only. It does not provide financial advice. Always do your own research before making investment decisions.

## Support

For issues and questions:
- Create an issue on GitHub
- Check the troubleshooting section
- Review the configuration options

---

**Happy Ethereum Whale Watching! 🐋📊**
