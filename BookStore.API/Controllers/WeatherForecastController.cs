using Microsoft.AspNetCore.Mvc;
using BookStore.Common;
using System;
using System.Net;

namespace BookStore.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public ActionResult<GenericResponse<IEnumerable<WeatherForecast>>> Get()
        {
            try
            {
                // Log the action
                _logger.LogInformation("Retrieving weather forecast data");
                
                // Generate forecast data
                var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                })
                .ToArray();
                
                // Return success response with data
                return Ok(GenericResponse<IEnumerable<WeatherForecast>>.CreateSuccess(
                    forecast, 
                    "Weather forecast data retrieved successfully"
                ));
            }
            catch (Exception ex)
            {
                // Log error
                _logger.LogError(ex, "Error retrieving weather forecast data");
                
                // Return error response
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    GenericResponse<IEnumerable<WeatherForecast>>.CreateError(
                        "Failed to retrieve weather forecast data",
                        HttpStatusCode.InternalServerError,
                        "WEATHER_FORECAST_ERROR"
                    )
                );
            }
        }
    }
}
