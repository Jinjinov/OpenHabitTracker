# OpenHabitTracker

https://openhabittracker.net

https://github.com/Jinjinov/OpenHabitTracker

Take Markdown notes, plan tasks, track habits

- Free and Ad-Free
- Open Source
- Privacy Focused: All user data is stored locally on your device
- Available on Windows, Linux, Android, iOS, macOS, and as a web app
- Localized to English, German, Spanish, Slovenian

Key Features:

- Markdown support for notes
- Use categories and priorities to organize your notes, tasks, and habits
- Advanced Search, Filter, and Sort
- Data Export/Import: JSON, YAML, TSV, Markdown
- Import your notes from Google Keep
- Available in 26 themes with Dark and Light modes

OpenHabitTracker Blazor WASM:
- https://pwa.openhabittracker.net
- all data is saved on your device

OpenHabitTracker Blazor Server:
- host your own Docker image
- all data is saved on your server

## This Docker image contains OpenHabitTracker Blazor Server app for one user.

Set your username and password with environment variables:

.env
```
APPSETTINGS_USERNAME=admin
APPSETTINGS_EMAIL=admin@admin.com
APPSETTINGS_PASSWORD=admin
APPSETTINGS_JWT_SECRET=your-extremely-strong-secret-key
```

Replace `your-extremely-strong-secret-key` in Windows terminal:

```
[System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Replace `your-extremely-strong-secret-key` in Linux / macOS terminal:

```
openssl rand -base64 32
```

docker-compose.yml
```
services:
  openhabittracker:
    image: jinjinov/openhabittracker:latest
    ports:
      - "5000:8080"
    environment:
      - AppSettings__UserName=${APPSETTINGS_USERNAME}
      - AppSettings__Email=${APPSETTINGS_EMAIL}
      - AppSettings__Password=${APPSETTINGS_PASSWORD}
      - AppSettings__JwtSecret=${APPSETTINGS_JWT_SECRET}
    volumes:
      - ./.OpenHabitTracker:/app/.OpenHabitTracker
```

After you login at http://localhost:5000/login you can use the same browser tab to access:
- logs: http://localhost:5000/watchdog
- OpenAPI json: http://localhost:5000/openapi/v1.json
- OpenAPI UI: http://localhost:5000/scalar/v1