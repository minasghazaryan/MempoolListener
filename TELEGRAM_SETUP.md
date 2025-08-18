# 📱 Telegram Setup Guide

Follow these steps to set up Telegram alerts for whale transactions (>$15K):

## Step 1: Create a Telegram Bot

1. **Open Telegram** and search for `@BotFather`
2. **Start a chat** with BotFather
3. **Send the command**: `/newbot`
4. **Choose a name** for your bot (e.g., "Whale Alert Bot")
5. **Choose a username** for your bot (must end with 'bot', e.g., "whale_alert_bot")
6. **Copy the bot token** that BotFather gives you (looks like: `123456789:ABCdefGHIjklMNOpqrsTUVwxyz`)

## Step 2: Get Your Chat ID

### Method 1: Using @userinfobot
1. Search for `@userinfobot` in Telegram
2. Start a chat with it
3. It will reply with your chat ID (a number like `123456789`)

### Method 2: Using your bot
1. Start a chat with your bot
2. Send any message to your bot
3. Visit this URL in your browser (replace with your bot token):
   ```
   https://api.telegram.org/botYOUR_BOT_TOKEN/getUpdates
   ```
4. Look for the "chat" object and find your "id" number

## Step 3: Update the Code

Replace these values in `Program.cs`:

```csharp
private static readonly string TelegramBotToken = "YOUR_BOT_TOKEN"; // Replace with your actual bot token
private static readonly string TelegramChatId = "YOUR_CHAT_ID"; // Replace with your actual chat ID
```

### Example:
```csharp
private static readonly string TelegramBotToken = "123456789:ABCdefGHIjklMNOpqrsTUVwxyz";
private static readonly string TelegramChatId = "123456789";
```

## Step 4: Test the Integration

1. **Build and run** the application:
   ```bash
   dotnet run
   ```

2. **You should see**:
   ```
   ✅ Telegram integration configured
   📱 Telegram message sent successfully
   ```

3. **Check your Telegram** for the startup message:
   ```
   🐋 Ethereum Whale Listener Started!
   Monitoring for transactions > $15,000 USD
   ```

## Step 5: Whale Alerts

Once configured, you'll receive Telegram messages like this when whale transactions are detected:

```
🐋 WHALE ALERT! 🐋

💰 Value: 5.2500 ETH ($15,750)
⏰ Time: 14:30:25
🆔 TX: 0x12345678...
💸 Fee: 0.001234 ETH
⛽ Gas: 45 Gwei (21,000)
📤 From: 0xabcd1234...
📥 To: 0x5678efgh...

🔗 View on Etherscan
```

## Troubleshooting

### Bot not responding?
- Make sure you started a chat with your bot
- Check that the bot token is correct
- Verify the chat ID is correct

### No messages received?
- Check that the bot token and chat ID are properly set
- Make sure you're monitoring transactions >$15K
- Check the console for error messages

### Want to change the threshold?
- Update the `DollarWhaleThreshold` value in the code:
  ```csharp
  private static readonly decimal DollarWhaleThreshold = 15000m; // Change this value
  ```

## Security Notes

- **Never share your bot token** publicly
- **Keep your chat ID private**
- **Consider using environment variables** for production use

## Advanced Features

You can also:
- **Add multiple chat IDs** for group notifications
- **Customize message format**
- **Add different thresholds** for different alert types
- **Include price feeds** for real-time ETH prices

---

**Happy Whale Watching! 🐋📱**
