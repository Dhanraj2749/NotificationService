# Notification Service

A scalable, cloud-native notification service built with **C# and .NET 8** — supports Email, SMS, and Push channels with retry logic, dead-letter queue, and real-time observability.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  REST API (ASP.NET Core)                 │
│   POST /api/notifications/send                          │
│   GET  /api/notifications/{id}                          │
│   GET  /api/notifications/stats                         │
└───────────────────────┬─────────────────────────────────┘
                        │ Enqueue
                        ▼
┌─────────────────────────────────────────────────────────┐
│           In-Memory Message Queue                        │
│     (Production: Azure Service Bus / RabbitMQ)          │
└───────────────────────┬─────────────────────────────────┘
                        │ Dequeue
                        ▼
┌─────────────────────────────────────────────────────────┐
│              Notification Worker (BackgroundService)     │
│                                                         │
│   ┌─────────────────────────────────────────────────┐  │
│   │         Retry Logic (Max 3 attempts)             │  │
│   │         Exponential Backoff: 2s → 4s → 8s       │  │
│   └────────────────────┬────────────────────────────┘  │
│                        │                                │
│          ┌─────────────┼──────────────┐                 │
│          ▼             ▼              ▼                 │
│     [Email]         [SMS]          [Push]               │
│    Handler        Handler         Handler               │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│         Notification Repository (Status Tracking)        │
│   Queued → Processing → Delivered / Failed / DeadLetter  │
│     (Production: Azure SQL / CosmosDB)                  │
└─────────────────────────────────────────────────────────┘
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| API | ASP.NET Core 8, Swagger |
| Messaging | In-memory queue (Azure Service Bus ready) |
| Workers | .NET BackgroundService |
| Logging | Serilog |
| Testing | xUnit, Moq, FluentAssertions |
| Container | Docker |

## Getting Started

### Run locally
```bash
cd src/NotificationService.API
dotnet run
```
Open Swagger UI: http://localhost:5000/swagger

### Run with Docker
```bash
docker build -t notification-service .
docker run -p 8080:8080 notification-service
```

### Run tests
```bash
dotnet test
```

## API Usage

### Send a notification
```json
POST /api/notifications/send
{
  "type": "Email",
  "recipient": "user@example.com",
  "subject": "Welcome!",
  "body": "Thanks for signing up."
}
```

### Check status
```
GET /api/notifications/{id}
```

### Get stats
```
GET /api/notifications/stats
```

## Key Features

- **Multi-channel routing** — Email, SMS, Push via pluggable handlers
- **Retry with exponential backoff** — 3 attempts, 2s/4s/8s delays
- **Dead-letter queue** — failed messages tracked for inspection
- **Real-time status tracking** — Queued → Processing → Delivered/Failed/DeadLettered
- **Azure-ready** — swap in-memory queue for Azure Service Bus with one line
- **Fully tested** — unit tests with Moq and FluentAssertions
- **Dockerized** — ready for Azure Container Apps / Kubernetes deployment

## Production Swap Guide

| Current (Local) | Production |
|----------------|-----------|
| InMemoryMessageQueue | Azure Service Bus |
| InMemoryNotificationRepository | Azure SQL / CosmosDB |
| Email handler mock | SendGrid / Azure Communication Services |
| SMS handler mock | Twilio / Azure SMS |
| Push handler mock | Azure Notification Hubs |
