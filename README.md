# 📊 MutualFundConsolidated — Enterprise Mutual Fund Platform & AI MCP Gateway

[![Framework](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Frontend](https://img.shields.io/badge/Angular-18.0-DD0031?style=flat&logo=angular)](https://angular.io/)
[![Protocol](https://img.shields.io/badge/MCP-Model_Context_Protocol-00C7B7?style=flat)](https://modelcontextprotocol.io)
[![Cloud DB](https://img.shields.io/badge/Database-MySQL_Aiven_Cloud-4479A1?style=flat&logo=mysql)](https://aiven.io/)
[![Hosting](https://img.shields.io/badge/Hosted_on-Render_Cloud-46E3B7?style=flat&logo=render)](https://render.com/)
[![Messaging](https://img.shields.io/badge/Messaging-Telegram_%26_WhatsApp-25D366?style=flat&logo=whatsapp)](https://ultramsg.com/)

An enterprise-grade, clean architecture financial portfolio tracking engine and **Model Context Protocol (MCP)** server. The platform ingests daily AMFI market data, calculates scheme-wise portfolio analytics (XIRR, Net P&L, Live NAV, Avg Purchase NAV), and automatically pushes rich morning digests with 24-bit color profit/loss badges to **Telegram** and **WhatsApp** every day at **05:05 AM IST**.

---

## 📐 System Architecture

```mermaid
flowchart TD
    AMFI[AMFI India Daily NAV Feed] -->|05:00 AM Sync| NavWorker[NavDownloadWorker]
    NavWorker --> DB[(Aiven Cloud MySQL DB)]
    
    subgraph Core .NET 8 Engine
        DB --> FamilyQuery[Family Portfolio Query]
        DB --> HoldingsQuery[Holdings Aggregator]
        FamilyQuery --> DigestTools[Messaging MCP Tools]
        HoldingsQuery -->|Consolidate 204 -> 9 Schemes| DigestTools
    end

    subgraph Automated Worker & MCP Server
        DigestWorker[DailyDigestWorker\n05:05 AM IST Cron] --> DigestTools
        LLM[LLM AI Assistants\nClaude / Antigravity] -->|MCP Protocol| DigestTools
    end

    DigestTools -->|HTML Push| Telegram[Telegram Bot API]
    DigestTools -->|WhatsApp Markdown| UltraMsg[UltraMsg Webhook Gateway]

    Telegram -->|Push Notification| TelegramApp[📱 Telegram Client]
    UltraMsg -->|Push Notification| WhatsAppApp[💬 WhatsApp Client]
```

---

## ✨ Key Features

- **🤖 Model Context Protocol (MCP) Integration**:
  Exposes portfolio analytics, scheme breakdowns, and messaging tools directly to LLM AI Assistants via standardized MCP endpoints.

- **📊 Consolidated 9-Scheme Portfolio Aggregator**:
  Reduces 200+ raw holdings into 9 consolidated mutual fund schemes, calculating Current Value, Invested Capital, Net Profit %, Today's P&L, Live NAV, and Average Buy NAV.

- **📱 Multi-Channel Push Gateway**:
  - **Telegram Bot (`@my_mf_digest_bot`)**: Free instant push notifications.
  - **WhatsApp (`UltraMsg Webhook`)**: Mobile-formatted Markdown digests with solid 24-bit color badges (`🟩` Profit / `🟥` Loss) and shaded monospace data cards (`<code>...</code>`).

- **⏰ Resilient 05:05 AM IST Daily Scheduler**:
  Cross-platform `DailyDigestWorker` background service supporting **Asia/Kolkata** (Linux/Render) and **India Standard Time** (Windows) with automatic cold-start catch-up logic.

---

## 🛠️ Project Structure

```text
C:\MutualFundConsolidated\
├── MutualFund.ConsolidatedAPI/     # Main Monolith Web API & Hosted Services
│   ├── Modules/
│   │   ├── Auth/                   # JWT & User Access Management
│   │   ├── Investment/             # Portfolio & Holdings Queries
│   │   ├── Nav/                    # Automated AMFI NAV Ingestion Worker
│   │   ├── Scheme/                 # Scheme Enrollment & Catalog
│   │   └── Messaging/              # Telegram, WhatsApp & DailyDigestWorker
│   └── Program.cs
├── MutualFund.Mcp.API/             # Standalone MCP Messaging Gateway API
│   ├── Controllers/
│   ├── Jobs/
│   ├── Services/
│   ├── Tools/
│   └── Dockerfile
└── MutualFundShell-Web/            # Angular 18 Single Page Application
```

---

## ⚡ API Endpoints Reference

| Endpoint | Method | Description |
|---|---|---|
| `/api/mcp/digest/preview` | `GET` | Generates a live mobile-formatted daily digest HTML preview |
| `/api/mcp/digest/send?channel=telegram` | `POST` | Dispatches the daily digest to Telegram chat |
| `/api/mcp/digest/send?channel=whatsapp` | `POST` | Dispatches the daily digest to WhatsApp via UltraMsg |
| `/api/nav/sync` | `POST` | Triggers immediate AMFI NAV market data synchronization |

---

## ⚙️ Configuration & Environment Variables

Create or set the following settings in `appsettings.json` or Render Environment Variables:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=mysql-host.aivencloud.com;Port=26779;Database=defaultdb;User=avnadmin;Password=YOUR_PASSWORD;SslMode=Required;"
  },
  "Telegram": {
    "BotToken": "YOUR_TELEGRAM_BOT_TOKEN",
    "ChatId": "YOUR_TELEGRAM_CHAT_ID"
  },
  "WhatsApp": {
    "Provider": "UltraMsg",
    "UltraMsgInstanceId": "YOUR_ULTRAMSG_INSTANCE_ID",
    "UltraMsgToken": "YOUR_ULTRAMSG_TOKEN",
    "ToPhone": "YOUR_PHONE_NUMBER"
  }
}
```

---

## 🚀 Running Locally

```bash
# Clone repository
git clone https://github.com/shafiq0225/MutualFundConsolidated.git
cd MutualFundConsolidated

# Build and run main Consolidated API
dotnet build MutualFund.ConsolidatedAPI/MutualFund.ConsolidatedAPI.csproj
dotnet run --project MutualFund.ConsolidatedAPI/MutualFund.ConsolidatedAPI.csproj
```

---

## 📄 License
This project is open-source under the [MIT License](LICENSE).
