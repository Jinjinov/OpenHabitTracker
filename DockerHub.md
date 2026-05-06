# OpenHabitTracker

https://openhabittracker.net

https://github.com/Jinjinov/OpenHabitTracker

Take Markdown notes, plan tasks, track habits

- Free and Ad-Free
- Open Source
- Privacy Focused: All user data is stored locally on your device
- Available on Windows, Linux, Android, iOS, macOS, and as a web app
- Localized to English, German, Spanish, Slovenian, French, Portuguese, Italian, Japanese, Chinese, Korean, Dutch, Danish, Norwegian, Swedish, Finnish, Polish, Czech, Slovak, Croatian, Serbian

<br>

Key Features:

- Markdown support for notes
- Use categories and priorities to organize your notes, tasks, and habits
- Advanced Search, Filter, and Sort
- Data Export/Import: JSON, YAML, TSV, Markdown
- Import your notes from Google Keep
- Available in 26 themes with Dark and Light modes

<br>

OpenHabitTracker Blazor WASM:
- https://pwa.openhabittracker.net
- all data is saved on your device

<br>

OpenHabitTracker Blazor Server:
- host your own Docker image
- all data is saved on your server

<br>

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

## Sync desktop or mobile app with Docker

### Run the Docker container

Choose the platform where you want to host the OpenHabitTracker server:

#### Docker Desktop

1. Open Docker Desktop and make sure it shows **Engine running**
2. Open a terminal in the folder containing your `.env` and `docker-compose.yml`
3. Run: `docker-compose up -d`
4. Open `http://localhost:5000/login` in a browser to confirm the server is running
5. Find this machine's IP address:
   - Windows: `ipconfig`
   - Mac / Linux: `ifconfig`

<br>

**Address:** `http://<this-machine-ip>:5000`

#### Linux server

1. SSH into your server
2. Install Docker: `sudo apt update && sudo apt install -y docker.io docker-compose`
3. Open a terminal in the folder containing your `.env` and `docker-compose.yml`
4. Run: `docker-compose up -d`
5. Find the server IP: `ifconfig`

<br>

**Address:** `http://<server-ip>:5000`

#### Synology NAS

1. Open **Package Center** and install **Container Manager**
2. Open **Container Manager** → **Project** → **Create**
3. Set the project name to `openhabittracker`
4. Paste the `docker-compose.yml` content, replacing `${APPSETTINGS_...}` placeholders with your actual values
5. Click **Next** → **Done** — the container starts automatically
6. Find your NAS IP: **Control Panel** → **Network** → **Network Interface**

<br>

**Address:** `http://<nas-ip>:5000`

### Enable sync in OpenHabitTracker

1. Open OpenHabitTracker on your device
2. Open the menu and click **Data**
3. Scroll down to **Online sync**
4. Enter the **Address** from your scenario above, plus your **Username** and **Password** from `.env`
5. Check **Remember me** to stay logged in across app restarts
6. Click **Log in**
