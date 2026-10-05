# Messenger

An educational messenger (diploma project) built on .NET 8: an ASP.NET Core server with real-time messaging via SignalR, JWT authentication and data storage in an encrypted SQLite database (SQLCipher). The client is a WPF desktop application.

## Architecture

The solution consists of three projects:

| Project | Type | Purpose |
|---------|------|---------|
| **Messenger.Server** | ASP.NET Core Web API | REST endpoints (registration, login, users, chats) + SignalR hub `/chathub` for real-time messaging |
| **Messenger.Client** | WPF (Windows) | Desktop client: login/registration window, chat list, conversations |
| **Messenger.Shared** | Class Library | Shared models and DTOs for the server and client |

Technologies: ASP.NET Core Minimal API, SignalR, JWT (Bearer), Entity Framework Core + SQLite/SQLCipher, WPF + MVVM (CommunityToolkit.Mvvm), Swagger.

## Requirements

- **Windows** (the client uses WPF and runs on Windows only)
- **.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0
  Verify the installation: `dotnet --version` (should be version 8.x)

## Running

You need to run **two processes**: first the server, then the client. The easiest way is to open two terminal windows in the solution root folder.

### 1. Server

```bash
dotnet run --project Messenger.Server
```

The server will start at `http://localhost:5087`.
API documentation (Swagger) is available at: http://localhost:5087/swagger

The `messenger.db` database is created automatically on first run (if it doesn't exist yet).

### 2. Client

In a separate terminal window:

```bash
dotnet run --project Messenger.Client
```

> The client is hard-coded to use the server address `http://localhost:5087`, so no changes are needed.

Alternatively, you can open `Messenger.sln` in Visual Studio 2022, set **Messenger.Server** as the startup project, run it, and then run **Messenger.Client** separately.

## How to test (demo scenario)

The repository includes a `messenger.db` database with test data, so you can log in right away with existing accounts. Or test it "from scratch":

1. Start the server and the client.
2. In the client window, register a user (e.g. `alice`), then close/restart the client and register a second one (`bob`) — it's convenient to run **two** client instances at the same time.
3. Logged in as one user, find the other one via search and start a conversation.
4. Messages are delivered between the two clients in real time via SignalR.

## API endpoints

| Method | Path | Description | Authorization |
|--------|------|-------------|---------------|
| POST | `/register` | Register a user | — |
| POST | `/login` | Log in, returns a JWT | — |
| GET | `/users?search=` | Search users | JWT |
| GET | `/chats` | List the user's chats | JWT |
| POST | `/chats/private` | Create/get a private chat | JWT |
| — | `/chathub` | SignalR messaging hub | JWT |

## Configuration note

For simplicity, as this is an educational project, the JWT signing key and the database encryption password are set directly in `Messenger.Server/appsettings.json`. In a real (production) environment, these values should be moved to environment variables or a secure secret store rather than kept in the repository.
