using ChartVisualizationPr.Models;
using ChartVisualizationPr.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChartVisualizationPr.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChartDataController : ControllerBase
    {
        private readonly IChartDataService _chartDataService;
        private readonly ILogger<ChartDataController> _logger;

        public ChartDataController(IChartDataService chartDataService, ILogger<ChartDataController> logger)
        {
            _chartDataService = chartDataService;
            _logger = logger;
        }

        [HttpGet("candles/{symbol}")]
        public async Task<ActionResult<List<CandleData>>> GetCandles(string symbol)
        {
            try
            {
                var data = await _chartDataService.GetCandleDataAsync(symbol);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting candles for {symbol}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("indicators/{symbol}")]
        public async Task<ActionResult<List<IndicatorData>>> GetIndicators(string symbol)
        {
            try
            {
                var data = await _chartDataService.GetIndicatorDataAsync(symbol);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting indicators for {symbol}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("markers/{symbol}")]
        public async Task<ActionResult<List<MarkerData>>> GetMarkers(string symbol)
        {
            try
            {
                var data = await _chartDataService.GetMarkersAsync(symbol);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting markers for {symbol}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("group/{symbol}")]
        public async Task<ActionResult<List<InvestorData>>> GetGroupData(string symbol)
        {
            try
            {
                var data = await _chartDataService.GetGroupDataAsync(symbol);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting group data for {symbol}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("foreign/{symbol}")]
        public async Task<ActionResult<List<InvestorData>>> GetForeignData(string symbol)
        {
            try
            {
                var data = await _chartDataService.GetForeignDataAsync(symbol);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting foreign data for {symbol}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("markers")]
        public async Task<ActionResult<MarkerData>> SaveMarker([FromBody] MarkerData marker)
        {
            try
            {
                var saved = await _chartDataService.SaveMarkerAsync(marker);
                return Ok(saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving marker");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("markers/{id}")]
        public async Task<ActionResult> DeleteMarker(string id)
        {
            try
            {
                var deleted = await _chartDataService.DeleteMarkerAsync(id);
                if (deleted)
                    return Ok(new { message = "Marker deleted successfully" });
                return NotFound(new { error = "Marker not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting marker {id}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
