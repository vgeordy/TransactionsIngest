# TransactionsIngest

## Overview

The app fetches the last‑24‑hour snapshot, upserts records by transaction ID, detects and records updates, marks revoked transactions (those missing from the current snapshot while still within 24 hours), and finalize records older than 24 hours.

## Approach

### Data Model

Transaction

```
TransactionId : string
CardLast4 : string
LocationCode : string
ProductName : string
Amount : decimal
TransactionTime : DateTime
Status : TransactionStatus
```

TransactionStatus

```
pending : enum
Revoked : enum
Finalized : enum
```
AuditLog

```
AuditLogId : int
TransactionId : string
EventName : AuditLogStatus
FieldName : string
OldValue : string
NewValue : string
Timestamp: DateTime
Transaction : Transaction
```
AuditLogStatus

```
Created : enum
Updated : enum
Revoked : enum
Finalized : enum
```

### Processing Logic
- TransactionId in snapshot, not in DB -> Add 
- TransactionId in snapshot, in DB, nothing changed -> Ignore
- TransactionId in snapshot, in DB, something changed -> Update
- TransactionId in DB but not in snapshot -> Revoke

When Creating, Updating, Revoking, and finalizing, add that to AuditLog


### Assumptions
- Revocation only applies within 24 hours
- Created and revoked audit logs have null FieldName, OldValue, NewValue
- Only Pending transactions are finalized
- CardLast4 and TransactionId are immutable


## Prerequisites
- .NET 10 SDK


## Build & Run

```
1. dotnet tool install --global dotnet-ef
2. dotnet ef database update
3. dotnet run
```

## Run Tests

```
dotnet test
```

## Configuration

```
{
  "Api": {
    "UseMock": true,
    "Url": "https://gateway.api/transactions"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=transactions.db"
  }
}
```