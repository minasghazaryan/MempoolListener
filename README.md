# 🐋 Mempool Whale Listener

A real-time Bitcoin mempool monitoring application that detects and alerts on whale transactions and large transfers.

## Features

- **Real-time Monitoring**: Connects to mempool.space WebSocket API for live transaction monitoring
- **Whale Detection**: Identifies transactions above configurable thresholds (default: 10 BTC)
- **High Fee Detection**: Alerts on transactions with unusually high fees
- **Token Transfer Detection**: Basic detection of potential token transfers
- **Configurable Thresholds**: Easy customization via `appsettings.json`
- **Multiple Notification Options**: Support for Discord, Telegram, and email notifications
- **Comprehensive Logging**: Console and file logging with Serilog
- **Auto-reconnection**: Automatic reconnection on connection loss

## Quick Start

### Prerequisites

- .NET 9.0 or later
- Internet connection for mempool.space API access

### Installation

1. Clone the repository:
```bash
git clone https://github.com/minasghazaryan/MempoolListener.git
cd MempoolListener
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Run the application:
```bash
dotnet run
```

## Configuration

Edit `appsettings.json` to customize the application behavior:

### Whale Thresholds
```json
"WhaleThresholds": {
  "BTC": 10.0,        // Minimum BTC amount to trigger whale alert
  "USDT": 1000000,    // Minimum USDT amount
  "USDC": 1000000,    // Minimum USDC amount
  "ETH": 100,         // Minimum ETH amount
  "WBTC": 10          // Minimum WBTC amount
}
```

### High Fee Detection
```json
"HighFeeThreshold": 0.01  // Minimum BTC fee to trigger high fee alert
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
1. Connect to mempool.space WebSocket API
2. Subscribe to transaction feed
3. Monitor all incoming transactions
4. Alert when whale transactions are detected
5. Log all activity to console and file

### Output Example
```
🐋 Mempool Whale Listener Starting...
Monitoring for transactions > 10 BTC
Press Ctrl+C to exit

✅ Connected to mempool.space WebSocket
📡 Subscribed to transaction feed

🐋 WHALE DETECTED! 🐋
⏰ Time: 2024-01-15 14:30:25
🆔 TXID: abc123def456...
💰 Value: 15.50000000 BTC ($775,000.00)
💸 Fee: 0.00010000 BTC
⚖️  Weight: 1,200 vB
📊 Status: mempool
🔗 Explorer: https://mempool.space/tx/abc123def456...
================================================================================
```

## Advanced Features

### Custom Whale Detection
The application can detect different types of whale activity:

1. **Large BTC Transfers**: Transactions above the BTC threshold
2. **High Fee Transactions**: Transactions with unusually high fees
3. **Token Transfers**: Basic detection of potential token transfers
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
1. **New Token Support**: Add token addresses and thresholds to configuration
2. **Additional Notifications**: Implement new notification providers
3. **Enhanced Analysis**: Add more sophisticated transaction analysis
4. **Database Integration**: Store transaction data for historical analysis

## API Reference

### Mempool.space WebSocket API
The application connects to the mempool.space WebSocket API at:
```
wss://mempool.space/api/v1/ws
```

### Transaction Data Structure
```json
{
  "action": "tx",
  "data": {
    "txid": "transaction_hash",
    "fee": 1000,
    "weight": 1200,
    "value": 1550000000,
    "status": "mempool",
    "vout": [...]
  }
}
```

## Troubleshooting

### Common Issues

1. **Connection Failed**
   - Check internet connection
   - Verify mempool.space is accessible
   - Check firewall settings

2. **No Whale Alerts**
   - Verify threshold settings in `appsettings.json`
   - Check if thresholds are too high for current market activity

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

**Happy Whale Watching! 🐋📊**
