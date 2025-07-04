using Microsoft.AspNetCore.Mvc;
using ViewerComponent.Services;

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
}