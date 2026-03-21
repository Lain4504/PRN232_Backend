using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace AISAM.API.Controllers
{
    [Route("api/admin-tools")] // Explicitly use hyphenated route to match frontend calls
    [ApiController]
    public class AdminToolsController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ITeamBrandRepository _teamBrandRepository;
        private readonly IContentRepository _contentRepository;

        public AdminToolsController(
            IUserRepository userRepository,
            IProfileRepository profileRepository,
            ISubscriptionRepository subscriptionRepository,
            IPaymentRepository paymentRepository,
            ITeamRepository teamRepository,
            ITeamMemberRepository teamMemberRepository,
            IBrandRepository brandRepository,
            ITeamBrandRepository teamBrandRepository,
            IContentRepository contentRepository)
        {
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _subscriptionRepository = subscriptionRepository;
            _paymentRepository = paymentRepository;
            _teamRepository = teamRepository;
            _teamMemberRepository = teamMemberRepository;
            _brandRepository = brandRepository;
            _teamBrandRepository = teamBrandRepository;
            _contentRepository = contentRepository;
        }

        [HttpPost("seed-demo-user")]
        public async Task<ActionResult<GenericResponse<object>>> SeedDemoUser([FromBody] SeedDemoUserRequest request)
        {
            try
            {
                var result = await CreateUserWithFullEco(request);
                return Ok(GenericResponse<object>.CreateSuccess(result, "Lưu dữ liệu thực tế thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GenericResponse<object>.CreateError($"Lỗi: {ex.Message}"));
            }
        }

        [HttpPost("seed-batch-users")]
        public async Task<ActionResult<GenericResponse<object>>> SeedBatchUsers([FromBody] BatchSeedRequest request)
        {
            try
            {
                if (request.Users == null || request.Users.Count == 0)
                {
                    return BadRequest(GenericResponse<object>.CreateError("Danh sách người dùng trống."));
                }

                var results = new List<object>();
                foreach (var userInfo in request.Users)
                {
                    var singleRequest = new SeedDemoUserRequest
                    {
                        Email = userInfo.Email,
                        FullName = userInfo.FullName,
                        Password = "Password123!",
                        Plan = request.Plan,
                        ProfileType = request.ProfileType,
                        CompanyName = userInfo.CompanyName ?? "",
                        CreateOrder = request.CreateOrder,
                        Amount = request.Amount,
                        CreatedAt = request.CreatedAt,
                        SubscriptionStartDate = request.CreatedAt, // Use CreatedAt as default for batch
                        SubscriptionEndDate = request.CreatedAt?.AddMonths(1)
                    };

                    var result = await CreateUserWithFullEco(singleRequest);
                    results.Add(result);
                }

                return Ok(GenericResponse<object>.CreateSuccess(new { CreatedCount = results.Count }, $"Đã lưu {results.Count} người dùng vào database thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GenericResponse<object>.CreateError($"Lỗi tạo batch: {ex.Message}"));
            }
        }

        private async Task<object> CreateUserWithFullEco(SeedDemoUserRequest request)
        {
            var random = new Random();
            var creationDate = request.CreatedAt ?? DateTime.UtcNow.AddDays(-random.Next(5, 30));

            // 1. Create User
            CreatePasswordHash(request.Password, out string passwordHash, out string passwordSalt);
            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Role = UserRoleEnum.User,
                IsEmailVerified = true,
                CreatedAt = creationDate
            };
            await _userRepository.CreateAsync(user);

            // 2. Create Profile
            var profile = new Profile
            {
                UserId = user.Id,
                ProfileType = request.ProfileType,
                CompanyName = request.CompanyName,
                Bio = !string.IsNullOrEmpty(request.CompanyName) 
                    ? $"CEO tài {request.CompanyName}. Đam mê Content Marketing AI." 
                    : $"Chuyên gia sáng tạo nội dung số tại Việt Nam.",
                AvatarUrl = $"https://api.dicebear.com/7.x/miniavs/svg?seed={user.Email}",
                CreatedAt = creationDate,
                UpdatedAt = creationDate
            };
            await _profileRepository.CreateAsync(profile);

            // 3. Create Subscription
            var subStart = request.SubscriptionStartDate ?? creationDate;
            var subEnd = request.SubscriptionEndDate ?? subStart.AddMonths(1);
            var subscription = new Subscription
            {
                ProfileId = profile.Id,
                Plan = request.Plan,
                StartDate = subStart,
                EndDate = subEnd,
                IsActive = true,
                CreatedAt = creationDate,
                UpdatedAt = creationDate
            };

            switch (request.Plan)
            {
                case SubscriptionPlanEnum.Free: subscription.QuotaPostsPerMonth = 5; break;
                case SubscriptionPlanEnum.Plus: 
                    subscription.QuotaPostsPerMonth = 30; 
                    subscription.QuotaAIContentPerDay = 5; 
                    subscription.AnalysisLevel = 1; 
                    break;
                case SubscriptionPlanEnum.Premium: 
                    subscription.QuotaPostsPerMonth = 999; 
                    subscription.QuotaAIContentPerDay = 100; 
                    subscription.AnalysisLevel = 2; 
                    break;
            }
            await _subscriptionRepository.CreateAsync(subscription);

            // 4. Create Payment
            if (request.CreateOrder && request.Amount > 0)
            {
                await _paymentRepository.CreateAsync(new Payment
                {
                    UserId = user.Id,
                    SubscriptionId = subscription.Id,
                    Amount = request.Amount,
                    Currency = "VND",
                    Status = PaymentStatusEnum.Success,
                    PaymentMethod = "Thanh toán Demo",
                    TransactionId = "DEMO-" + Guid.NewGuid().ToString().Replace("-","").Substring(0,10).ToUpper(),
                    CreatedAt = subStart.AddHours(2)
                });
            }

            // 5. Create Team & Member
            var team = new Team
            {
                ProfileId = profile.Id,
                Name = $"Team {request.FullName}",
                CreatedAt = creationDate
            };
            await _teamRepository.CreateAsync(team);
            await _teamMemberRepository.AddAsync(new TeamMember { TeamId = team.Id, UserId = user.Id, Role = "Owner", JoinedAt = creationDate });

            // 6. Create Brand & Link
            var brand = new Brand { ProfileId = profile.Id, Name = string.IsNullOrEmpty(request.CompanyName) ? $"{request.FullName} Brand" : request.CompanyName, CreatedAt = creationDate.AddDays(1) };
            await _brandRepository.AddAsync(brand);
            await _teamBrandRepository.AddAsync(new TeamBrand { TeamId = team.Id, BrandId = brand.Id });

            // 7. Dummy Content
            await _contentRepository.CreateAsync(new Content
            {
                ProfileId = profile.Id,
                BrandId = brand.Id,
                Title = "Nội dung khởi tạo",
                TextContent = $"Chào mừng đến với {brand.Name}! Đã lưu dữ liệu thực tế thành công.",
                AdType = AdTypeEnum.TextOnly,
                Status = ContentStatusEnum.Published,
                CreatedAt = creationDate.AddDays(2)
            });

            return new { user.Id, user.Email, user.FullName };
        }

        private void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            passwordSalt = Convert.ToBase64String(salt);
            passwordHash = Convert.ToBase64String(hash);
        }
    }

    public class BatchSeedRequest
    {
        public List<UserSeedInfo>? Users { get; set; }
        public SubscriptionPlanEnum Plan { get; set; } = SubscriptionPlanEnum.Plus;
        public ProfileTypeEnum ProfileType { get; set; } = ProfileTypeEnum.Basic;
        public bool CreateOrder { get; set; } = false;
        public decimal Amount { get; set; } = 0;
        public DateTime? CreatedAt { get; set; }
    }

    public class UserSeedInfo
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
    }
}
