# MyBankAI

Banking AI microservices platform built with .NET 8, Clean Architecture, Kafka, PostgreSQL, and Groq AI for real-time fraud detection.

## Architecture

```
┌──────────────────────────────────────────────────────────┐
│                      API Gateway :5000                    │
│              (YARP + JWT Auth + Rate Limiting)            │
└────────┬──────────────┬───────────────────────────────────┘
         │              │
         ▼              ▼
┌─────────────┐  ┌──────────────────┐
│  Account    │  │  Transaction     │
│  Service   │  │  Service         │
│  :5001     │  │  :5002           │
└─────────────┘  └────────┬─────────┘
                          │ Kafka: transaction.created
                          ▼
                 ┌──────────────────┐
                 │ FraudDetection   │
                 │ Service :5003    │
                 │ (Groq AI)        │
                 └────────┬─────────┘
                          │ Kafka: fraud.alert / notification.send
                          ▼
                 ┌──────────────────┐
                 │ Notification     │
                 │ Service :5004    │
                 └──────────────────┘
```

## Services

| Service | Port | Responsibility |
|---------|------|---------------|
| ApiGateway | 5000 | YARP reverse proxy, JWT validation, rate limiting |
| AccountService | 5001 | Users, accounts/wallets, JWT issuance |
| TransactionService | 5002 | Transfers, balance, paginated statement |
| FraudDetectionService | 5003 | Kafka consumer + Groq AI risk analysis |
| NotificationService | 5004 | Kafka consumer + alert dispatch |

## Kafka Topics

| Topic | Producer | Consumer |
|-------|----------|----------|
| `transaction.created` | TransactionService | FraudDetectionService |
| `transaction.approved` | FraudDetectionService | TransactionService |
| `transaction.rejected` | FraudDetectionService | TransactionService |
| `fraud.alert` | FraudDetectionService | NotificationService |
| `notification.send` | FraudDetectionService | NotificationService |

## Quick Start

### Prerequisites
- Docker & Docker Compose
- .NET 8 SDK (for local development)

### 1. Configure environment

```bash
cp .env.example .env
# Edit .env and set GROQ_API_KEY and JWT_SECRET
```

### 2. Run all services

```bash
docker-compose up -d --build
```

### 3. Check health

```bash
curl http://localhost:5000/health
curl http://localhost:5001/health
curl http://localhost:5002/health
```

## API Endpoints

All requests through the Gateway (port 5000).

### Auth & Accounts

```http
POST /api/accounts/users/register
Content-Type: application/json
{
  "name": "João Silva",
  "email": "joao@example.com",
  "password": "StrongPass123!"
}

POST /api/accounts/users/login
{
  "email": "joao@example.com",
  "password": "StrongPass123!"
}

POST /api/accounts/accounts
Authorization: Bearer <token>
{
  "currency": "BRL"
}

GET /api/accounts/accounts/{accountId}
Authorization: Bearer <token>
```

### Transactions

```http
POST /api/transactions/transactions
Authorization: Bearer <token>
{
  "fromAccountId": "guid",
  "toAccountId": "guid",
  "amount": 100.00,
  "currency": "BRL",
  "description": "Payment"
}

GET /api/transactions/transactions/{accountId}/statement?cursor=<cursor>&pageSize=20
Authorization: Bearer <token>

GET /api/transactions/transactions/{transactionId}
Authorization: Bearer <token>
```

### Fraud (internal/admin)

```http
GET /api/fraud/analyses/{transactionId}
Authorization: Bearer <token>
```

## Local Development

Each service can be run independently:

```bash
cd AccountService/src/AccountService.API
dotnet run
```

Run tests:

```bash
cd AccountService
dotnet test
```

## Project Structure

```
MyBankAI/
├── ApiGateway/
├── AccountService/
│   ├── src/
│   │   ├── AccountService.Domain/
│   │   ├── AccountService.Application/
│   │   ├── AccountService.Infrastructure/
│   │   └── AccountService.API/
│   └── tests/
│       ├── AccountService.UnitTests/
│       └── AccountService.IntegrationTests/
├── TransactionService/
├── FraudDetectionService/
├── NotificationService/
├── docker-compose.yml
├── .env.example
└── README.md
```

## Tech Stack

- **Runtime**: .NET 8, C#
- **Database**: PostgreSQL 16 + Entity Framework Core 8
- **Messaging**: Apache Kafka 7.6 (Confluent.Kafka)
- **AI**: Groq API — llama-3.3-70b-versatile (OpenAI-compatible)
- **Auth**: JWT Bearer tokens
- **Gateway**: YARP (Yet Another Reverse Proxy)
- **Logging**: Serilog (structured JSON logs)
- **Containers**: Docker + Docker Compose
