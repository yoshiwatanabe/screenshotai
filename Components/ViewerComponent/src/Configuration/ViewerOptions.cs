namespace ViewerComponent.Configuration;

public class ViewerOptions
{
    public const string SectionName = "Viewer";

    public int Port { get; set; } = 8080;
    public string Host { get; set; } = "localhost";
    public bool EnableCors { get; set; } = true;
    public string StaticFilesPath { get; set; } = "wwwroot";
    public string OutputDirectory { get; set; } = "_output";
}