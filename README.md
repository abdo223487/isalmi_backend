# IslamiApi — .NET 10 / PostgreSQL

## Run (Docker)
```bash
docker compose up --build
```

## Run (local)
```bash
dotnet run
# Swagger: http://localhost:5000/swagger
```

---

## Auth — `/api/auth`

| Method | Endpoint | Auth | Notes |
|--------|----------|------|-------|
| POST | `/register` | — | Password contains `@admin` → admin account |
| POST | `/login` | — | Returns `accessToken` + `refreshToken` + `role` |
| POST | `/refresh` | — | Body: `{ refreshToken }` |
| POST | `/logout` | — | Revokes single refresh token |
| POST | `/logout-all` | — | Revokes all sessions |
| GET  | `/me` | ✅ | Returns user info |
| POST | `/change-password` | ✅ | `{ oldPassword, newPassword }` |

---

## Azkar — `/api/azkar`

| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `/` | — |
| GET | `/categories` | — |
| GET | `/category/{category}` | — |
| GET | `/{id}` | — |
| POST | `/` | Admin |
| PUT | `/{id}` | Admin |
| DELETE | `/{id}` | Admin |

---

## Fatwa — `/api/fatwa`

| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `/?page=1&pageSize=10` | — |
| GET | `/{id}` | — |
| POST | `/` | Admin |
| PUT | `/{id}` | Admin |
| DELETE | `/{id}` | Admin |

---

## Hadith — `/api/hadith`

| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `/?page=1&pageSize=10` | — |
| GET | `/{id}` | — |
| GET | `/random` | — |
| POST | `/` | Admin |
| POST | `/bulk` | Admin |
| PUT | `/{id}` | Admin |
| DELETE | `/{id}` | Admin |

---

## Sira — `/api/sira`

| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `/?page=1&pageSize=10` | — |
| GET | `/{id}` | — |
| POST | `/` | Admin |
| POST | `/bulk` | Admin |
| PUT | `/{id}` | Admin |
| DELETE | `/{id}` | Admin |

---

## Chat — REST `/api/chat` + WebSocket `/hubs/chat`

### REST
| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `/pending` | Admin |
| GET | `/active` | Admin |
| GET | `/my` | Customer |
| GET | `/{conversationId}/messages?page=1&pageSize=50` | Both |

### WebSocket (SignalR)
Connect: `wss://your-host/hubs/chat?access_token=YOUR_JWT`

| Client → Server | Payload | Description |
|-----------------|---------|-------------|
| `RequestChat` | — | Customer starts chat request |
| `AcceptChat` | `conversationId` | Admin accepts pending chat |
| `JoinConversation` | `conversationId` | Join room to receive messages |
| `SendMessage` | `conversationId, message` | Send message |
| `CloseChat` | `conversationId` | Admin closes conversation |
| `JoinAdminGroup` | — | Admin subscribes to new requests |

| Server → Client | Payload | Description |
|-----------------|---------|-------------|
| `chatRequested` | `{ conversationId, status }` | Confirmation to customer |
| `newChatRequest` | `{ conversationId, customerId, createdAt }` | Broadcast to admins |
| `chatAccepted` | `{ conversationId, adminId, status }` | Broadcast to room |
| `newMessage` | `{ id, senderId, senderRole, message, createdAt }` | Broadcast to room |
| `chatClosed` | `{ conversationId, closedAt }` | Broadcast to room |
| `joinedConversation` | `{ conversationId }` | Confirmation |
| `chatAlreadyExists` | `{ conversationId, status }` | If customer already has active chat |
| `error` | `{ error }` | Error message |

---

## Roles
- `admin` — register with password containing `@admin` (stripped before hashing)
- `customer` — default
