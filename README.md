# BetterX API Documentation

## Base URL
```
http://localhost:5000
```

## Endpoints

### 1. Check if user exists
```bash
curl -X GET "http://localhost:5000/users/johndoe"
```
Response:
```json
{
    "exists": true
}
```

### 2. Create new user
```bash
curl -X POST "http://localhost:5000/users" -H "Content-Type: application/json" -d "{\"username\": \"johndoe\", \"token\": \"abc123\"}"
```
Response (201 Created):
```json
{
    "id": 1,
    "username": "johndoe",
    "token": "abc123",
    "status": 1,
    "appVersion": null,
    "usedMoreThanHour": false,
    "country": null
}
```

### 3. Get user status
```bash
curl -X GET "http://localhost:5000/users/johndoe/status"
```
Response:
```json
{
    "status": 1
}
```

### 4. Update status (Authentication required)
```bash
curl -X PUT "http://localhost:5000/users/johndoe/status?token=abc123" -H "Content-Type: application/json" -d "{\"status\": 0}"
```

### 5. Update app version and usage (Authentication required)
```bash
curl -X PUT "http://localhost:5000/users/johndoe/app-update?token=abc123" -H "Content-Type: application/json" -d "{\"version\": \"1.0.0\", \"usedMoreThanHour\": true}"
```

### 6. Save installation country (Authentication required)
```bash
curl -X PUT "http://localhost:5000/users/johndoe/location?token=abc123" -H "Content-Type: application/json" -d "{\"country\": \"France\"}"
```

### 7. Heartbeat (Authentication required)
```bash
curl -X POST "http://localhost:5000/users/johndoe/heartbeat?token=abc123"
```
The heartbeat must be sent every minute. If no heartbeat is received for more than one minute, the user's status will automatically change to "Inactive" (1).

### 8. Twitter Authentication

#### Get Twitter Authentication URL
```bash
curl -X GET "http://localhost:5000/connect-request"
```
Response:
```json
{
    "auth_url": "https://api.twitter.com/oauth/authorize?oauth_token=...",
    "token": "<oauth_token>"
}
```

#### Get Access Token
After user authorization, use the oauth_token and oauth_verifier to get the access token:
```bash
curl -X GET "http://localhost:5000/get-token?oauth_token=YOUR_TOKEN&oauth_verifier=YOUR_VERIFIER"
```
Response:
```json
{
    "oauth_token": "...",
    "oauth_token_secret": "...",
    "user_id": "...",
    "screen_name": "..."
}
```

## Status Codes

- 200 OK: Request successful
- 201 Created: User created successfully
- 400 Bad Request: Error in request (e.g., user already exists)
- 401 Unauthorized: Invalid or missing token
- 404 Not Found: User not found

## UserStatus Model

- 0: Active
- 1: Inactive
- 2: DoNotDisturb
