### Ví Dụ Minh Họa: Vendor Có Team A Với 4 Thành Viên, Liên Kết 3 Brands (X, Y, Z) Và Check `team_brands` Ở Đâu

Dựa trên câu hỏi của bạn, tôi sẽ đưa ra một ví dụ cụ thể về cách **vendor** trong hệ thống AISAM quản lý **Team A** với **4 thành viên** (có các role thường thấy: Vendor, TeamLeader, Copywriter, Marketer) và liên kết với **3 brands (X, Y, Z)** qua bảng trung gian `team_brands`. Tôi sẽ giải thích rõ ràng **nơi nào cần check bảng `team_brands`** trong các luồng liên quan (đặc biệt là luồng **approval**, nhưng cũng bao quát các tác vụ khác như tạo content, publish post) để đảm bảo team chỉ làm việc với brands được assign. Tôi sẽ sử dụng schema hiện tại (`teams`, `team_members`, `brands`, `contents`, `approvals`, `team_brands`) và giữ logic đơn giản, không code, chỉ mô tả ví dụ, flow, và các điểm check `team_brands`.

---

### 1. Thiết Lập Ví Dụ
#### Dữ Liệu Giả Lập
- **Vendor**: User V (users.id = V1, role = vendor).
- **Team A**:
    - `teams`: { id = T1, vendor_id = V1, name = "Team A", status = "active" }.
    - **Thành viên** (`team_members`):
        1. Vendor (user_id = V1, role = "Vendor", permissions = ["CREATE_TEAM", "ADD_MEMBER", "APPROVE_CONTENT", "PUBLISH_POST", ...]).
        2. TeamLeader (user_id = U1, role = "TeamLeader", permissions = ["APPROVE_CONTENT", "ADD_MEMBER", ...]).
        3. Copywriter (user_id = U2, role = "Copywriter", permissions = ["CREATE_CONTENT", "SUBMIT_FOR_APPROVAL"]).
        4. Marketer (user_id = U3, role = "Marketer", permissions = ["APPROVE_CONTENT", "PUBLISH_POST"]).
- **Brands**:
    - Brand X: { id = B1, user_id = V1, name = "Brand X" }.
    - Brand Y: { id = B2, user_id = V1, name = "Brand Y" }.
    - Brand Z: { id = B3, user_id = V1, name = "Brand Z" }.
- **team_brands**:
    - { TeamId = T1, BrandId = B1 } (Team A - Brand X).
    - { TeamId = T1, BrandId = B2 } (Team A - Brand Y).
    - { TeamId = T1, BrandId = B3 } (Team A - Brand Z).
- **Mô tả**: Team A quản lý 3 brands (X, Y, Z). Các thành viên (Vendor, TeamLeader, Copywriter, Marketer) có thể làm việc với cả 3 brands, miễn là có permissions phù hợp.

#### Tình Huống
- Copywriter tạo content cho Brand X.
- Content được submit để duyệt.
- TeamLeader hoặc Marketer duyệt content.
- Marketer publish content thành post.
- Câu hỏi: Check `team_brands` ở đâu để đảm bảo các tác vụ chỉ được thực hiện cho brands X, Y, Z (thuộc Team A)?

---

### 2. Các Điểm Check `team_brands` Trong Luồng
Dưới đây là các luồng chính (tạo content, submit approval, approve/reject, publish post) và nơi cần check `team_brands` để đảm bảo team members chỉ làm việc với brands được assign (X, Y, Z).

#### a. **Tạo Content (POST /api/contents)**
- **Tình huống**: Copywriter (U2) tạo content cho Brand X.
- **Flow**:
    1. Request: POST /api/contents, body: `{ brand_id: B1, title: "Ad for Brand X", text_content: "Buy now!" }`.
    2. **Validate**:
        - Check quyền: Copywriter có "CREATE_CONTENT" trong `team_members.permissions` (user_id = U2, team_id = T1).
        - Check team membership: Copywriter thuộc Team A (query `team_members`: user_id = U2, team_id = T1).
        - **Check team_brands**: Query `team_brands` để đảm bảo Brand X (brand_id = B1) thuộc Team A (TeamId = T1, BrandId = B1).
            - SQL: `SELECT 1 FROM team_brands WHERE TeamId = 'T1' AND BrandId = 'B1'`.
        - Check brand tồn tại: `brands.id = B1` và `brands.user_id = V1` (vendor sở hữu brand).
    3. Insert: `contents` (brand_id = B1, status = 0 [draft], created_at = now).
    4. Response: Success { content_id: C1, brand_id: B1, ... }.
- **Error Case**:
    - Nếu Copywriter cố tạo content cho Brand W (brand_id = B4, không thuộc Team A):
        - Check `team_brands`: Không tìm thấy record (TeamId = T1, BrandId = B4).
        - Throw BadRequest ("Brand not assigned to your team").
- **Tại sao check team_brands**:
    - Đảm bảo Copywriter chỉ tạo content cho brands X, Y, Z (thuộc Team A).
    - Ngăn tạo content cho brand ngoài scope (e.g., Brand W của vendor nhưng không assign cho Team A).

#### b. **Submit For Approval (POST /api/contents/{content_id}/submit)**
- **Tình huống**: Copywriter (U2) submit content C1 (brand_id = B1) để duyệt.
- **Flow**:
    1. Request: POST /api/contents/C1/submit.
    2. **Validate**:
        - Check quyền: Copywriter có "SUBMIT_FOR_APPROVAL" (team_members.user_id = U2, team_id = T1).
        - Check content: `contents.id = C1`, brand_id = B1.
        - **Check team_brands**: Query `team_brands` để đảm bảo Brand X (B1) thuộc Team A (TeamId = T1, BrandId = B1).
        - Check team membership: Copywriter thuộc Team A (team_members.user_id = U2, team_id = T1).
    3. Update: `contents.status = 1` (pending).
    4. Insert: `approvals` (content_id = C1, status = 0 [pending], approver_id = null).
    5. Gửi notifications:
        - Query `team_brands`: Lấy BrandId = B1, TeamId = T1.
        - Query `team_members`: Lấy users trong Team A có "APPROVE_CONTENT" (Vendor: V1, TeamLeader: U1, Marketer: U3).
        - Insert `notifications`: { user_id: [V1, U1, U3], type: "content_submitted", message: "Content C1 for Brand X needs approval" }.
    6. Response: Success { content_id: C1, status: "pending" }.
- **Error Case**:
    - Nếu content thuộc Brand W (B4, không trong `team_brands` của Team A):
        - Throw BadRequest ("Brand not assigned to your team").
- **Tại sao check team_brands**:
    - Đảm bảo content submit thuộc brand của Team A (X, Y, Z).
    - Gửi notifications chỉ tới approvers trong Team A, không phải tất cả approvers của vendor.

#### c. **Approve/Reject Content (PUT /api/approvals/{approval_id})**
- **Tình huống**: TeamLeader (U1) duyệt content C1 (brand_id = B1).
- **Flow**:
    1. Request: PUT /api/approvals/A1, body: `{ status: "approved", notes: "Looks good" }`.
    2. **Validate**:
        - Check quyền: TeamLeader có "APPROVE_CONTENT" (team_members.user_id = U1, team_id = T1).
        - Check approval: `approvals.id = A1`, content_id = C1.
        - Check content: `contents.id = C1`, brand_id = B1.
        - **Check team_brands**: Query `team_brands` để đảm bảo Brand X (B1) thuộc Team A (TeamId = T1, BrandId = B1).
        - Check team membership: TeamLeader thuộc Team A (team_members.user_id = U1, team_id = T1).
    3. Update: `approvals` (status = 1 [approved], approved_at = now, notes = "Looks good").
    4. Update: `contents.status = 2` (approved).
    5. Insert `notifications`: { user_id: U2 (Copywriter), type: "content_approved", message: "Content C1 approved" }.
    6. Response: Success { approval_id: A1, status: "approved" }.
- **Error Case**:
    - Nếu TeamLeader cố duyệt content của Brand W (B4):
        - Check `team_brands`: Không tìm thấy (TeamId = T1, BrandId = B4).
        - Throw Forbidden ("Not authorized to approve content for this brand").
- **Tại sao check team_brands**:
    - Đảm bảo chỉ approvers trong Team A (quản lý Brand X, Y, Z) được duyệt content của brands này.
    - Ngăn approver duyệt content ngoài scope team.

#### d. **Publish Post (POST /api/contents/{content_id}/publish)**
- **Tình huống**: Marketer (U3) publish content C1 thành post.
- **Flow**:
    1. Request: POST /api/contents/C1/publish, body: `{ integration_id: S1 }`.
    2. **Validate**:
        - Check quyền: Marketer có "PUBLISH_POST" (team_members.user_id = U3, team_id = T1).
        - Check content: `contents.id = C1`, brand_id = B1, status = 2 (approved).
        - **Check team_brands**: Query `team_brands` để đảm bảo Brand X (B1) thuộc Team A (TeamId = T1, BrandId = B1).
        - Check integration: `social_integrations.id = S1`, brand_id = B1.
    3. Insert: `posts` (content_id = C1, integration_id = S1, status = 4 [published], published_at = now).
    4. Update: `contents.status = 4` (published).
    5. Insert `notifications`: { user_id: V1 (Vendor), type: "post_published", message: "Content C1 published for Brand X" }.
    6. Response: Success { post_id: P1, content_id: C1 }.
- **Error Case**:
    - Nếu content thuộc Brand W (B4):
        - Check `team_brands`: Không tìm thấy (TeamId = T1, BrandId = B4).
        - Throw BadRequest ("Brand not assigned to your team").
- **Tại sao check team_brands**:
    - Đảm bảo post chỉ được publish cho brands của Team A (X, Y, Z).
    - Ngăn publish content cho brand ngoài scope.

---

### 3. Lý Do Check `team_brands` Trong Ví Dụ
- **Giới hạn scope team**:
    - Team A chỉ được làm việc với Brands X, Y, Z (qua `team_brands`). Check `team_brands` đảm bảo các tác vụ (tạo content, submit, approve, publish) chỉ áp dụng cho brands được assign, tránh nhầm lẫn (e.g., Copywriter tạo content cho Brand W không thuộc Team A).
- **Phân tách trách nhiệm**:
    - Vendor có thể có nhiều teams (Team A cho Client X/Y/Z, Team B cho Client W). Check `team_brands` đảm bảo Team A không can thiệp vào brands của Team B.
- **Notifications chính xác**:
    - Trong submit/approve, notifications chỉ gửi tới members của Team A (Vendor, TeamLeader, Marketer) khi content thuộc Brand X, Y, Z, không gửi tới members của team khác.
- **Analytics đúng scope**:
    - Analytics của Team A (GET /api/teams/T1/analytics) chỉ bao gồm contents/posts của Brands X, Y, Z (query `team_brands` để filter).

---

### 4. Có Thể Bỏ Check `team_brands` Không?
- **Có thể bỏ nếu không cần giới hạn**:
    - Nếu bạn muốn Team A làm việc với **tất cả brands của vendor** (X, Y, Z, và cả Brand W), có thể bỏ check `team_brands`, chỉ check `brands.user_id = teams.vendor_id`.
    - **Hậu quả**: Team members có thể tạo/approve/publish content cho bất kỳ brand nào của vendor, dẫn đến:
        - Nhầm lẫn: Team A tạo content cho Brand W (thuộc client khác).
        - Notifications không đúng scope (gửi tới tất cả approvers của vendor).
        - Analytics không chính xác (Team A thấy reports của Brand W).
- **Khuyến nghị**: Giữ check `team_brands` để:
    - Phân tách rõ ràng brands theo team (hỗ trợ multi-client agency).
    - Đảm bảo security và consistency (members chỉ làm việc với brands được assign).
    - Nếu vendor nhỏ (ít brands), có thể bỏ check để đơn giản hóa, nhưng cần xác nhận requirement.

---

### 5. Kết Luận
- **Ví dụ tóm tắt**:
    - Team A (Vendor V1, TeamLeader U1, Copywriter U2, Marketer U3) quản lý Brands X, Y, Z (qua `team_brands`).
    - **Check team_brands ở**:
        - **Tạo content**: Đảm bảo `brand_id` (B1, B2, B3) thuộc Team A.
        - **Submit approval**: Gửi notifications chỉ tới approvers của Team A (V1, U1, U3) cho brands X, Y, Z.
        - **Approve/reject**: Chỉ cho phép approvers trong Team A duyệt content của X, Y, Z.
        - **Publish post**: Đảm bảo post thuộc brands của Team A.
- **Lý do check**:
    - Giới hạn scope team, ngăn làm việc với brands ngoài assign (e.g., Brand W).
    - Đảm bảo notifications và analytics đúng context team-brand.
    - Hỗ trợ multi-client workflow (Team A cho Client X/Y/Z, không lẫn với Team B).
- **Đề xuất**: Giữ check `team_brands` trong các API (POST /api/contents, POST /api/contents/{id}/submit, PUT /api/approvals/{id}) để đảm bảo team chỉ làm việc với brands được assign. Thêm index cho `team_brands(TeamId, BrandId)` để tối ưu query.

Nếu bạn cần giả lập flow chi tiết hơn (e.g., với Brand W ngoài scope), hoặc muốn bỏ check `team_brands`, hãy cho biết!