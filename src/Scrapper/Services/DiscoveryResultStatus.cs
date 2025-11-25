namespace Scrapper.Services
{
    /// <summary>
    /// Represents the result of attempting to add/discover a Node Exporter.
    /// </summary>
    public enum DiscoveryResultStatus
    {
        Added,          // Successfully discovered and added to DB
        AlreadyExists,  // Already exists in DB
        Unreachable     // Could not reach the exporter at the specified IP/port
    }
}
