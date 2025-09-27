# Aetherion Contact API

## C# Web API for handling contact form submissions with Gmail SMTP

### Environment Variables Required

Set these environment variables on Render:

```bash
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-gmail-app-password
TO_EMAIL=contact@aetherion.com
```

### Gmail Setup

1. **Enable 2-Factor Authentication** on your Gmail account
2. **Generate App Password**:
   - Go to Google Account Settings → Security → 2-Step Verification → App passwords
   - Select "Mail" and your device
   - Copy the 16-character password (this is your `SMTP_PASSWORD`)

### API Endpoints

- `POST /api/contact/send` - Send contact form email
- `GET /api/contact/health` - Health check endpoint

### Request Format

```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "company": "Acme Corp",
  "service": "Web Development",
  "message": "I need help with my website"
}
```

### Response Format

```json
{
  "success": true,
  "message": "Thank you for your message! We'll get back to you within 24 hours to schedule your discovery call."
}
```

### Local Development

```bash
cd backend
dotnet run
```

API will be available at: `http://localhost:5000`

### Render Deployment

1. **Create new Web Service** on Render
2. **Connect your repository**
3. **Build Command**: `cd backend && dotnet publish -c Release -o out`
4. **Start Command**: `cd backend/out && dotnet AetherionContactAPI.dll`
5. **Environment Variables**: Set the SMTP variables listed above

### Features

- ✅ CORS enabled for all origins
- ✅ Sends business notification email
- ✅ Sends auto-response to customer
- ✅ Professional HTML email templates
- ✅ Error handling and logging
- ✅ Health check endpoint
- ✅ Environment-based configuration