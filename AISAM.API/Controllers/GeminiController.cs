using Microsoft.AspNetCore.Mvc;
using AISAM.Common;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly AISAM.Services.IServices.IAIService _aiService;
        private readonly ILogger<AIController> _logger;

        public AIController(
            AISAM.Services.IServices.IAIService aiService,
            ILogger<AIController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Generate content using AI based on a prompt
        /// </summary>
        [HttpPost("generate")]
        public async Task<ActionResult<GenericResponse<string>>> GenerateContent([FromBody] GenerateContentRequest request)
        {
            try
            {
                var result = await _aiService.GenerateContentAsync(request.Prompt);
                return Ok(GenericResponse<string>.CreateSuccess(result, "Content generated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating content");
                return StatusCode(500, GenericResponse<string>.CreateError("Failed to generate content"));
            }
        }

        /// <summary>
        /// Improve existing content using AI
        /// </summary>
        [HttpPost("improve")]
        public async Task<ActionResult<GenericResponse<string>>> ImproveContent([FromBody] ImproveContentRequest request)
        {
            try
            {
                var result = await _aiService.ImproveContentAsync(request.Content);
                return Ok(GenericResponse<string>.CreateSuccess(result, "Content improved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error improving content");
                return StatusCode(500, GenericResponse<string>.CreateError("Failed to improve content"));
            }
        }

        /// <summary>
        /// Save approved AI-generated content to database
        /// </summary>
        [HttpPost("save-content")]
        public async Task<ActionResult<GenericResponse<ContentResponseDto>>> SaveAIContent([FromBody] AISaveContentRequest request)
        {
            try
            {
                var result = await _aiService.SaveAIContentAsync(request);
                return Ok(GenericResponse<ContentResponseDto>.CreateSuccess(result, "AI-generated content saved successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for AI content saving");
                return BadRequest(GenericResponse<ContentResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving AI content");
                return StatusCode(500, GenericResponse<ContentResponseDto>.CreateError("Failed to save AI content"));
            }
        }
    }

    public class GenerateContentRequest
    {
        public string Prompt { get; set; } = string.Empty;
    }

    public class ImproveContentRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}