  
# **Scrapper Project Dependency Restoration and Setup Guide**

### **1️⃣ Ensure .NET SDK 9 is installed**

Check your current SDK:

`dotnet --version` 

If it’s not .NET 9.x, install it on macOS:

`# Using Homebrew (Intel or Apple Silicon) brew install --cask dotnet-sdk` 

Verify installation:

`dotnet --version` 

----------

### **2️⃣ Restore NuGet packages**

Make sure your `.csproj` includes all necessary dependencies. Example:

    ```markdown
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.10">
    <PackageReference Include="PacketDotNet" Version="1.4.8" />
    <PackageReference Include="Serilog" Version="4.3.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
    ```

Restore all packages:

`dotnet restore` 

----------

### **3️⃣ Install EF Core CLI (`dotnet-ef`)**

`dotnet tool install --global dotnet-ef export PATH="$PATH:$HOME/.dotnet/tools"` 

Check installation:

`dotnet ef --version` 

----------

### **4️⃣ Initialize the database (first-time setup)**

#### Option 1: Using migrations

# Create initial migration
```console
dotnet ef migrations add InitialCreate
```

# Apply migration to SQLite database
```console
dotnet ef database update
```

#### Option 2: Auto-create tables (simpler for dev/testing)

In your `DbContext` initialization code:

`using (var context = new MetricsDbContext())
{
    context.Database.EnsureCreated();
}` 

> This will create the database and tables if they do not exist.

----------

### **5️⃣ Install and run Prometheus Node Exporter**

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

### **6️⃣ Verify project setup**

Run your project:

`dotnet run` 

-   Make sure `Scrapper` can connect to Node Exporter (port 9100).
    
-   Check that metrics are being saved to SQLite without errors.
    