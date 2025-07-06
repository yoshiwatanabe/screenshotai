using Microsoft.AspNetCore.Mvc;
using ViewerComponent.Services;
using System.Reflection;

namespace ViewerComponent.Controllers;

[ApiController]
[Route("api")]
public class ViewerController : ControllerBase
{
    private readonly IViewerService _viewerService;

    public ViewerController(IViewerService viewerService)
    {
        _viewerService = viewerService;
    }

    [HttpGet("files")]
    public async Task<IActionResult> GetFiles(CancellationToken cancellationToken)
    {
        var files = await _viewerService.GetImageFilesAsync(cancellationToken);
        return Ok(files);
    }

    [HttpGet("image/{filename}")]
    public async Task<IActionResult> GetImage(string filename, CancellationToken cancellationToken)
    {
        var imageData = await _viewerService.GetImageAsync(filename, cancellationToken);

        if (imageData == null)
        {
            return NotFound();
        }

        return File(imageData, "image/png");
    }

    [HttpGet("analysis/{filename}")]
    public async Task<IActionResult> GetAnalysis(string filename, CancellationToken cancellationToken)
    {
        var analysisData = await _viewerService.GetAnalysisAsync(filename, cancellationToken);

        if (analysisData == null)
        {
            return NotFound();
        }

        return Content(analysisData, "application/json");
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _viewerService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        var version = assembly?.GetName().Version?.ToString() ?? "Unknown";
        var fileVersion = assembly?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? version;
        
        return Ok(new 
        { 
            version = version,
            fileVersion = fileVersion,
            assemblyVersion = assembly?.GetName().Version?.ToString(),
            buildDate = System.IO.File.GetCreationTime(assembly?.Location ?? "").ToString("yyyy-MM-dd HH:mm:ss")
        });
    }
}