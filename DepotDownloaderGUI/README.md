# DepotDownloader Pro - GUI

A TeraCopy-style graphical user interface for DepotDownloader, making it easy to download Steam content.

## Features

- **TeraCopy-inspired dark UI** - Clean, modern interface with dark theme
- **Steam Authentication** - Secure login with password storage option
- **Download Queue** - Manage multiple downloads with progress tracking
- **File Verification** - Built-in file verification with hash checking
- **Real-time Progress** - Live download speed, progress bars, and file status
- **Detailed Logging** - Track all download operations in the log tab

## How to Use

### 1. Launch the Application

```bash
dotnet run --project DepotDownloaderGUI/DepotDownloaderGUI.csproj
```

Or build and run the executable:

```bash
dotnet build DepotDownloaderGUI/DepotDownloaderGUI.csproj
cd DepotDownloaderGUI/bin/Debug/net9.0-windows
DepotDownloaderGUI.exe
```

### 2. Login to Steam

When the application starts, you'll be prompted to log in:
- Enter your Steam username and password
- Check "Remember password" to save credentials for future sessions
- Click "Login"

### 3. Create a Download

1. Click the **+** button in the toolbar
2. Enter the **App ID** (required) - Find this on SteamDB
3. Optionally enter a **Depot ID** (leave empty to download all depots)
4. Select the **Branch** (default: public)
5. Choose a **Target Directory** or leave empty for default
6. Configure options:
   - **Verify files after download** - Recommended
   - **Download all platforms** - Include Windows, Mac, Linux versions
   - **Download all languages** - Include all language packs
7. Click **Start Download**

### 4. Monitor Progress

The main window shows:
- **Left Panel**: All download jobs with progress bars
- **File List Tab**: Individual files with verification status
  - ✓ Green = Verified
  - ⚠ Yellow = Hash mismatch
  - ▶ Blue = Downloading
- **Status Tab**: Overall download statistics
- **Log Tab**: Detailed operation log

### 5. Control Downloads

Use the bottom control bar:
- **Pause** - Pause the current download
- **Skip** - Skip the current file
- **Stop** - Stop and cancel the download

## Finding App IDs

1. Go to [SteamDB](https://steamdb.info/)
2. Search for your game
3. The App ID is shown in the URL: `https://steamdb.info/app/APPID/`

Examples:
- Counter-Strike 2: `730`
- Dota 2: `570`
- Team Fortress 2: `440`

## Configuration Options

### Branch Names

Common branches:
- `public` - Latest stable release
- `beta` - Beta testing branch
- `experimental` - Experimental features

### Depot IDs

Depots are specific content packages. Leave empty to download all depots for the app, or specify a depot ID to download only that depot.

## Troubleshooting

### Login Failed
- Verify your credentials
- If you have Steam Guard, approve the login on your mobile device
- Try with `-no-mobile` option in advanced settings

### Download Errors
- Check the Log tab for detailed error messages
- Verify you have permission to access the content
- Ensure sufficient disk space

### Hash Mismatches
- Files with yellow warning icons have failed verification
- This may indicate corruption or incomplete downloads
- Try re-downloading with verification enabled

## Architecture

The GUI is built with:
- **WPF** (.NET 9) - Modern Windows UI framework
- **MVVM Pattern** - Clean separation of concerns
- **CommunityToolkit.Mvvm** - Modern MVVM helpers
- **DepotDownloader** - Steam content download engine

## License

Same license as the original DepotDownloader project.
