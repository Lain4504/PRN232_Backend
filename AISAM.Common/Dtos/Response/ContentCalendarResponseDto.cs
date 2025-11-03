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
            if (model.IsDeleted || !model.IsActive)
            {
                return "cancelled";
            }

            // If ScheduledTime is not provided, ScheduledDate is already a full UTC instant
            DateTime scheduledUtc;
            if (!model.ScheduledTime.HasValue)
            {
                scheduledUtc = model.ScheduledDate;
            }
            else
            {
                var tz = TryGetTimeZone(model.Timezone) ?? TimeZoneInfo.Utc;

                var dateOnly = model.ScheduledDate.Date;
                var timeOfDay = model.ScheduledTime ?? TimeSpan.Zero;

                var local = new DateTime(
                    dateOnly.Year,
                    dateOnly.Month,
                    dateOnly.Day,
                    timeOfDay.Hours,
                    timeOfDay.Minutes,
                    timeOfDay.Seconds,
                    DateTimeKind.Unspecified);

                try
                {
                    scheduledUtc = TimeZoneInfo.ConvertTimeToUtc(local, tz);
                }
                catch
                {
                    scheduledUtc = model.ScheduledDate;
                }
            }

            return scheduledUtc <= DateTime.UtcNow ? "published" : "scheduled";
        }

        private static TimeZoneInfo? TryGetTimeZone(string? timezoneId)
        {
            if (string.IsNullOrWhiteSpace(timezoneId)) return null;
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            }
            catch
            {
                return null;
            }
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
