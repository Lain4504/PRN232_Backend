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
        /// Create a draft content and generate AI content for it
        /// </summary>
        [HttpPost("generate-draft")]
        public async Task<ActionResult<GenericResponse<AiGenerationResponse>>> GenerateContentForDraft([FromBody] CreateDraftRequest request)
        {
            try
            {
                var result = await _aiService.GenerateContentForDraftAsync(request);
                return Ok(GenericResponse<AiGenerationResponse>.CreateSuccess(result, "AI content generated for draft"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for AI draft generation");
                return BadRequest(GenericResponse<AiGenerationResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI content for draft");
                return StatusCode(500, GenericResponse<AiGenerationResponse>.CreateError("Failed to generate AI content"));
            }
        }

        /// <summary>
        /// Improve existing content and save as new AI generation
        /// </summary>
        [HttpPost("improve/{contentId}")]
        public async Task<ActionResult<GenericResponse<AiGenerationResponse>>> ImproveContent(Guid contentId, [FromBody] ImproveContentRequest request)
        {
            try
            {
                var result = await _aiService.ImproveContentAsync(contentId, request.Content);
                return Ok(GenericResponse<AiGenerationResponse>.CreateSuccess(result, "Content improved successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for content improvement");
                return BadRequest(GenericResponse<AiGenerationResponse>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error improving content");
                return StatusCode(500, GenericResponse<AiGenerationResponse>.CreateError("Failed to improve content"));
            }
        }

        /// <summary>
        /// Approve AI generation and copy it to the content
        /// </summary>
        [HttpPost("approve/{aiGenerationId}")]
        public async Task<ActionResult<GenericResponse<ContentResponseDto>>> ApproveAIGeneration(Guid aiGenerationId)
        {
            try
            {
                var result = await _aiService.ApproveAIGenerationAsync(aiGenerationId);
                return Ok(GenericResponse<ContentResponseDto>.CreateSuccess(result, "AI generation approved and content updated"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid AI generation ID");
                return BadRequest(GenericResponse<ContentResponseDto>.CreateError(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation on AI generation");
                return BadRequest(GenericResponse<ContentResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving AI generation");
                return StatusCode(500, GenericResponse<ContentResponseDto>.CreateError("Failed to approve AI generation"));
            }
        }

        /// <summary>
        /// Get all AI generations for a content
        /// </summary>
        [HttpGet("generations/{contentId}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<AiGenerationDto>>>> GetContentAIGenerations(Guid contentId)
        {
            try
            {
                var result = await _aiService.GetContentAIGenerationsAsync(contentId);
                return Ok(GenericResponse<IEnumerable<AiGenerationDto>>.CreateSuccess(result, "AI generations retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving AI generations");
                return StatusCode(500, GenericResponse<IEnumerable<AiGenerationDto>>.CreateError("Failed to retrieve AI generations"));
            }
        }
    }
}