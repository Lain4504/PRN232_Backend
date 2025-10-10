using System;
using System.Collections.Generic;

namespace AISAM.Common.Dtos.Request
{
    public class PublishPostRequest
    {
        public IEnumerable<Guid> IntegrationIds { get; set; } = Array.Empty<Guid>();
    }
}