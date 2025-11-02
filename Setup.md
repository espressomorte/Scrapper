  
# **Scrapper Project Dependency Restoration and Setup Guide**

### ** Ensure .NET SDK 9 is installed**

Check your current SDK:

`dotnet --version` 

If it’s not .NET 9.x, install it on macOS:

`# Using Homebrew (Intel or Apple Silicon) brew install --cask dotnet-sdk` 

Verify installation:

`dotnet --version` 

----------

### ** Restore NuGet packages**

Restore all packages:

`dotnet restore` 

----------

### ** Install EF Core CLI (`dotnet-ef`)**

`dotnet tool install --global dotnet-ef export PATH="$PATH:$HOME/.dotnet/tools"` 

Check installation:

`dotnet ef --version` 

----------

### ** Initialize the database (first-time setup)**

# Create initial migration
```console
dotnet ef migrations add InitialCreate
```

# Apply migration to SQLite database
```console
dotnet ef database update
```

> This will create the database and tables if they do not exist.

----------

### ** Install and run Prometheus Node Exporter**

If your Scrapper collects metrics from Node Exporter:

`# Install via Homebrew 
brew install node_exporter 
# Run Node Exporter directly 
```console
/usr/local/opt/node_exporter/bin/node_exporter_brew_services 

```# Or on Apple Silicon 
/opt/homebrew/opt/node_exporter/bin/node_exporter_brew_services 

Verify it's running:

`curl http://localhost:9100/metrics` 

----------

### ** Verify project setup**

Run your project:

`dotnet run` 

-   Make sure `Scrapper` can connect to Node Exporter (port 9100).
    
-   Check that metrics are being saved to SQLite without errors.
    