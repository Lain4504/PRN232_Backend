using AISAM.Data.Model;

namespace AISAM.Common.Dtos.Response
{
    public class ContentCalendarResponseDto
    {
        public Guid Id { get; set; }
        public Guid ContentId { get; set; }
        public string ContentTitle { get; set; } = string.Empty;
        public Guid BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public TimeSpan? ScheduledTime { get; set; }
        public string Timezone { get; set; } = "UTC";
        public string Status { get; set; } = "scheduled";
        public List<Guid> IntegrationIds { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public static ContentCalendarResponseDto FromModel(ContentCalendar model, string contentTitle = "", string brandName = "")
        {
            var dto = new ContentCalendarResponseDto
            {
                Id = model.Id,
                ContentId = model.ContentId,
                ContentTitle = contentTitle,
                BrandId = model.Content?.BrandId ?? Guid.Empty,
                BrandName = brandName,
                ScheduledDate = model.ScheduledDate,
                ScheduledTime = model.ScheduledTime,
                Timezone = model.Timezone,
                IntegrationIds = ParseIntegrationIds(model.IntegrationIds),
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };

            // Compute status based on model fields
            dto.Status = ComputeStatus(model);

            return dto;
        }

        private static string ComputeStatus(ContentCalendar model)
        {
            var now = DateTime.UtcNow;
            var scheduledDateTime = model.ScheduledDate;
            
            if (model.ScheduledTime.HasValue)
            {
                scheduledDateTime = model.ScheduledDate.Date.Add(model.ScheduledTime.Value);
            }

            if (model.IsDeleted)
            {
                return "cancelled";
            }

            if (!model.IsActive)
            {
                return "cancelled";
            }

            if (scheduledDateTime <= now)
            {
                return "published";
            }

            return "scheduled";
        }

        private static List<Guid> ParseIntegrationIds(string? integrationIdsJson)
        {
            if (string.IsNullOrEmpty(integrationIdsJson))
                return new List<Guid>();

            try
            {
                var ids = System.Text.Json.JsonSerializer.Deserialize<List<string>>(integrationIdsJson);
                return ids?.Select(Guid.Parse).ToList() ?? new List<Guid>();
            }
            catch
            {
                return new List<Guid>();
            }
        }
    }
}
