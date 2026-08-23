# OpenHabitTracker

https://openhabittracker.net

https://github.com/Jinjinov/OpenHabitTracker

Take notes, plan tasks, track habits

OpenHabitTracker is free and open source. The app has no ads, no trackers, no account, no subscriptions and no in-app purchases.

- All your data stays on your device, and syncs only to a server you run yourself
- Available on Windows, Linux, Android, iOS, macOS, and as a web app
- Localized to English, German, Spanish, Slovenian, French, Portuguese, Italian, Japanese, Chinese, Korean, Dutch, Danish, Norwegian, Swedish, Finnish, Polish, Czech, Slovak, Croatian, Serbian

<br>

Key features:

- Markdown notes, rendered in the list so reading one costs no clicks
- Tasks with a planned date and time, a duration, a checklist and a timer
- Habits measured by how overdue they are rather than by an unbroken streak: a habit with a 10 day interval that is 2 days overdue reads 120%
- Categories, 6 priority levels and 12 colours across notes, tasks and habits
- Import and export JSON, YAML, TSV and Markdown, plus import from a Google Keep Takeout ZIP
- 26 themes with dark and light mode
- Full keyboard navigation and screen reader support

<br>

Search, filter and sort:

- Six sections - search, date, category, priority, status and sort - each folding independently
- Planned dates and done dates filtered separately, each with four comparisons: before, on, after and not on
- Date filters take a fixed date or a number of days from today, worked out when the query runs, so a saved filter never goes stale
- Filter habits by how overdue they are, with a lower bound, an upper bound, or both
- Turn the done-date filter around to see what you have not done in a period, including what you have never done at all
- Eleven sort keys, chosen separately for notes, tasks and habits
- Search reads note text and checklist items, not only titles

<br>

Customization:

- Twenty-five settings on one screen: which page and which sidebar open at start, what each row shows, which calendars appear, how the filters are drawn, and how tightly the lists are packed
- Six of those add or remove whole blocks from a habit, so the same habit is either a short form or a full statistics page
- Settings are saved, synced, and included in JSON and YAML backups

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
      - "5050:8080"
    environment:
      - AppSettings__UserName=${APPSETTINGS_USERNAME}
      - AppSettings__Email=${APPSETTINGS_EMAIL}
      - AppSettings__Password=${APPSETTINGS_PASSWORD}
      - AppSettings__JwtSecret=${APPSETTINGS_JWT_SECRET}
      - TZ=Europe/Berlin # replace with your timezone
    volumes:
      - ./.OpenHabitTracker:/app/.OpenHabitTracker
```

Timezone strings: [List of tz database time zones](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones) (TZ identifier column).

The default port is `5050`. You can change it to `80` to avoid typing the port in the address, or to any other free port if `5050` is already in use - update the port in `docker-compose.yml` accordingly.

After you login at http://localhost:5050/login you can use the same browser tab to access:
- logs: http://localhost:5050/watchdog
- OpenAPI json: http://localhost:5050/openapi/v1.json
- OpenAPI UI: http://localhost:5050/scalar/v1

## Sync desktop or mobile app with Docker

### Run the Docker container

Choose the platform where you want to host the OpenHabitTracker server:

#### Docker Desktop

1. Open Docker Desktop and make sure it shows **Engine running**
2. Open a terminal in the folder containing your `.env` and `docker-compose.yml`
3. Run: `docker-compose up -d`
4. Open `http://localhost:5050/login` in a browser to confirm the server is running
5. Find this machine's IP address:
   - Windows: `ipconfig`
   - Mac / Linux: `ifconfig`

<br>

#### Linux server

1. SSH into your server
2. Install Docker: `sudo apt update && sudo apt install -y docker.io docker-compose`
3. Open a terminal in the folder containing your `.env` and `docker-compose.yml`
4. Run: `docker-compose up -d`
5. Find the server IP: `ifconfig`

<br>

#### Synology NAS

1. Open **Package Center** and install **Container Manager**
2. Open **Container Manager** → **Project** → **Create**
3. Set the project name to `openhabittracker`
4. Paste the `docker-compose.yml` content, replacing `${APPSETTINGS_...}` placeholders with your actual values
5. Click **Next** → **Done** — the container starts automatically
6. Find your NAS IP: **Control Panel** → **Network** → **Network Interface**

<br>

### Enable sync in OpenHabitTracker

1. Open OpenHabitTracker on your device
2. Open the menu and click **Data**
3. Scroll down to **Online sync**
4. Enter the **Address**: `http://` + the IP from your scenario above + `:5050` or whichever port you chose (omit the port entirely if you chose `80`)
5. Enter your **Username** and **Password** from `.env`
6. Check **Remember me** to stay logged in across app restarts
7. Click **Log in**
