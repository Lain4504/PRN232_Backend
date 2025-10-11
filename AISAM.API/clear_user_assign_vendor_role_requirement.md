### Làm Thế Nào Để Cấp Role Vendor Cho User Trong Hệ Thống AISAM

Dựa trên schema database hiện tại của AISAM (bảng `users` với `role` là integer [0=user, 1=vendor, 2=admin], `profiles` với `profile_type` [personal/business], và các bảng liên quan như `teams`, `brands`, `team_members`), cùng yêu cầu dự án (quản lý quảng cáo social media, vendor/agency tạo teams, manage brands), tôi sẽ giải thích rõ ràng cách cấp role `vendor` cho một user, các điều kiện cần thiết, và xác nhận rằng việc cấp role không phụ thuộc vào việc tạo profile business. Tôi sẽ giữ logic đơn giản, không code, chỉ mô tả flow, điều kiện, và tích hợp với hệ thống.

---

### 1. Role Vendor Là Gì Trong AISAM?
- **Định nghĩa**: Role `vendor` (integer 1 trong `users.role`) dành cho user có khả năng quản lý agency, bao gồm:
    - Tạo và quản lý teams (`teams.vendor_id`).
    - Thêm/xóa thành viên (`team_members`).
    - Quản lý brands (`brands.user_id`).
    - Thực hiện actions như tạo content, publish posts, link social integrations (qua permissions trong `team_members`).
- **Quyền mặc định**: Vendor có full permissions trong teams họ tạo (e.g., ["CREATE_TEAM", "ADD_MEMBER", "PUBLISH_POST", ...] trong `team_members.permissions`).

---

### 2. Làm Thế Nào Để Cấp Role Vendor Cho User?
Trong AISAM, role `vendor` được cấp thông qua một quy trình quản lý quyền, không tự động dựa trên việc tạo profile business. Dưới đây là cách cấp role `vendor` và các điều kiện liên quan:

#### a. **Ai Có Quyền Cấp Role Vendor?**
- **Admin**: Chỉ user với role `admin` (users.role = 2) có quyền thay đổi role của user khác (từ `user` sang `vendor`).
    - Schema xác nhận: `admin_logs` lưu actions của admin (e.g., "Change user role"), nên admin thực hiện qua API hoặc admin dashboard.
- **Không tự cấp**: User thường (role `user`) không thể tự cấp role `vendor` (ngăn privilege escalation).
- **Không dựa vào profile**: Việc cấp role `vendor` không yêu cầu user có profile business (profiles.profile_type = 1). Profile chỉ cung cấp metadata (name, company_name), không liên quan đến quyền.

#### b. **Điều Kiện Để Cấp Role Vendor**
Để cấp role `vendor` cho một user, cần thỏa mãn các điều kiện sau:
1. **User tồn tại và hợp lệ**:
    - User phải có record trong `users` (users.id tồn tại, users.is_deleted = false nếu có soft delete).
    - Email đã xác thực (users.email verified, nếu AISAM có email verification).
2. **Yêu cầu từ admin**:
    - Admin (users.role = 2) gửi request qua API hoặc dashboard để đổi role.
    - Ví dụ: Admin xác nhận user đủ điều kiện làm agency (e.g., đăng ký agency plan, KYC).
3. **Quota hoặc Subscription (nếu có)**:
    - Kiểm tra subscriptions.plan (e.g., premium plan cho phép vendor role).
    - Ví dụ: `subscriptions.is_active = true` và `plan` hỗ trợ vendor features (team management, multiple brands).
    - Nếu không có subscription yêu cầu, admin có thể cấp role mà không check quota.
4. **Không yêu cầu profile business**:
    - Schema: `users` không liên kết trực tiếp với `profiles.profile_type` khi set role.
    - Requirement: Không có ràng buộc rằng user phải có profile business để được cấp role `vendor`. User với profile personal (hoặc không có profile) vẫn có thể trở thành vendor.
5. **KYC hoặc điều kiện business (tùy requirement)**:
    - Nếu AISAM yêu cầu KYC (Know Your Customer) cho agency (vendor), admin kiểm tra thông tin (e.g., giấy phép kinh doanh, tax ID) trước khi cấp.
    - Đây là điều kiện ngoài schema, thường xử lý qua admin dashboard hoặc third-party verification.

#### c. **Flow Cấp Role Vendor**
Dưới đây là flow đơn giản để cấp role `vendor` cho user:

1. **Admin gửi request** (POST /api/users/{user_id}/set-role):
    - **Body**: { role: "vendor" } (hoặc integer 1).
    - **Auth**: JWT của admin (users.role = 2).
2. **Validate quyền**:
    - Check requester là admin (JWT role = 2).
    - **Error**: Không phải admin → Unauthorized ("Only admins can change roles").
3. **Validate user**:
    - Check user_id tồn tại (`users.id`), không deleted (nếu có soft delete).
    - **Error**: User không tồn tại → NotFound ("User not found").
4. **Check subscription (nếu áp dụng)**:
    - Query `subscriptions` (user_id, is_active = true).
    - Nếu plan yêu cầu vendor (e.g., premium), check plan hợp lệ.
    - **Error**: No active subscription hoặc plan không hỗ trợ vendor → Forbidden ("Subscription not eligible").
5. **Update role**:
    - Update `users.role = 1` (vendor) cho user_id.
    - Set `users.updated_at = DateTime.UtcNow` (nếu có trường).
6. **Log action**:
    - Insert `admin_logs`:
        - `admin_id`: ID của admin thực hiện.
        - `action_type`: "CHANGE_ROLE".
        - `target_id`: user_id.
        - `target_type`: "user".
        - `notes`: "Changed role to vendor".
7. **Gửi notification**:
    - Insert `notifications`:
        - `user_id`: ID của user được cấp role.
        - `type`: "role_updated".
        - `message`: "Your role has been updated to Vendor".
8. **Response**:
    - **Success**: { success: true, data: { user_id, role: "vendor" } }.
    - **Error**: { success: false, message: "Error message", errorCode: "UNAUTHORIZED|NOT_FOUND|FORBIDDEN" }.

#### d. **Các Case Chính**
1. **Success Case**: Admin cấp role vendor thành công.
    - User có subscription hợp lệ, admin update role, log vào `admin_logs`, gửi notification.
2. **Error Case: Không phải admin**:
    - Requester không có role admin → Unauthorized.
3. **Error Case: User không tồn tại**:
    - user_id không có trong `users` → NotFound.
4. **Error Case: Subscription không hợp lệ**:
    - User không có subscription active hoặc plan không hỗ trợ vendor → Forbidden.
5. **Alternative Case: KYC yêu cầu**:
    - Admin kiểm tra KYC (manual hoặc third-party) trước khi update role.
6. **Edge Case: User đã là vendor**:
    - Nếu users.role đã là 1, skip update, return success hoặc message "User is already vendor".

---

### 3. Profile Business Có Cần Để Cấp Role Vendor?
**Không bắt buộc**. Dựa trên schema và yêu cầu:
- **Schema xác nhận**:
    - `users.role` là integer (0=user, 1=vendor, 2=admin), không liên kết với `profiles.profile_type` (personal=0, business=1).
    - `profiles` chỉ cung cấp metadata (name, company_name, bio), không ảnh hưởng quyền/role.
    - `teams` và `team_members` không yêu cầu `profile_id`, nên tạo team hoặc manage team_members không cần profile business.
- **Requirement xác nhận**:
    - Quyền tạo team (POST /api/teams) chỉ yêu cầu `users.role = 'vendor'` và permission "CREATE_TEAM", không cần profile business.
    - Tính năng team/team_member (add member, publish post) dựa trên `team_members.permissions`, không yêu cầu profile type.
- **Kết luận**: Profile business chỉ là khuyến nghị (cho agency professional look, e.g., display company_name trong UI), nhưng không phải điều kiện để cấp role `vendor` hoặc sử dụng tính năng team/team_member.

**Ví dụ**:
- User A (role `user`) được admin cấp role `vendor` qua API, dù chỉ có profile personal ("John Doe").
- User A tạo team (POST /api/teams), add members, manage brands bình thường (dựa trên `vendor_id` và permissions).
- Nếu User A tạo profile business ("Agency XYZ"), nó chỉ dùng để display (e.g., team.description = "Managed by Agency XYZ"), không ảnh hưởng quyền.

---

### 4. Mối Liên Hệ Với Luồng Team Và Brand
- **Team**:
    - **Cấp role vendor**: Cho phép user tạo team (`teams.vendor_id = user_id`) và manage team_members.
    - **Profile**: Không bắt buộc, chỉ dùng để display (e.g., `profiles.name` hoặc `company_name` trong team UI).
    - **Flow**: Sau khi được cấp role vendor, user gọi POST /api/teams, insert `teams` và `team_members` (vendor làm TeamLeader), không cần profile business.
- **Brand**:
    - **Cấp role vendor**: Cho phép user tạo brands (`brands.user_id = user_id`) và link với teams (`team_brands`).
    - **Profile**: `brands.profile_id` optional, dùng để display metadata (company_name).
    - **Flow**: Vendor tạo brand (POST /api/brands), có thể không gắn profile_id, vẫn dùng tính năng content/posting.
- **Tích hợp**: Role `vendor` mở khóa quyền agency (create team, manage brands), profile chỉ là metadata, không là điều kiện.

---

### 5. Kết Luận
- **Cách cấp role vendor**:
    - Admin (users.role = 2) update `users.role = 1` qua API (POST /api/users/{user_id}/set-role).
    - Điều kiện: User tồn tại, subscription hợp lệ (nếu có), admin có quyền, không cần profile business.
- **Profile business không bắt buộc**:
    - Role `vendor` và tính năng team/team_member (create team, add member, publish post) dựa trên `users.role` và `team_members.permissions`, không yêu cầu profile business.
    - Profile chỉ dùng để display metadata (name, company_name) và phân tách danh tính (personal/business).
- **Tác dụng khác của profile**:
    - Phân tách danh tính cho multi-client (vendor switch profiles để manage brands/teams).
    - Context cho actions (e.g., post từ business profile).
    - Metadata cho analytics/UI (e.g., show company_name trong brand/team).
- **Schema hỗ trợ**: `users.role` độc lập với `profiles.profile_type`, nên không cần profile business để cấp role vendor.

Nếu bạn cần flow chi tiết hơn (e.g., API set role vendor), hoặc muốn mở rộng schema (thêm `profile_id` vào `team_members`), hãy cho biết!