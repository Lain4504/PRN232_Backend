## Hướng Dẫn Hoàn Chỉnh Về Flow Approval Trong Hệ Thống AISAM

Là một BA/PM với kinh nghiệm dày dặn, tôi sẽ mô tả chi tiết các case có thể hình thành được Approval trong AISAM, dựa trên yêu cầu dự án (tập trung vào content management: tạo, duyệt, lên lịch, đăng bài). Approval là quá trình kiểm tra và xác nhận content (từ bảng `contents`) trước khi nó được approved để lên lịch (`content_calendar`) hoặc đăng bài (`posts`).

Tôi sẽ phân tích:
- **Các case hình thành Approval**: Khi nào và như thế nào Approval được trigger.
- **Điều kiện ràng buộc cho từng actor**: Quyền hạn, điều kiện cần thỏa mãn cho User (Marketers/Content Creators), Vendor/Agency, và Admin.
- **Hướng dẫn flow hoàn chỉnh**: Bước-by-bước để team dev triển khai, bao gồm các branch (success/error/alternative), không code, chỉ mô tả logic.

### 1. Tổng Quan Về Approval
- **Mục đích**: Đảm bảo content phù hợp (không vi phạm policy, đúng brand identity, không lỗi) trước khi đăng lên social media (Facebook, TikTok, Instagram). Approval liên kết với bảng `approvals` (content_id, approver_id, status, notes, approved_at).
- **Trạng thái liên quan** (từ enum `content_status` trong `contents` và `approvals`):
    - `draft`: Content mới tạo, chưa submit.
    - `pending_approval`: Đã submit, chờ duyệt.
    - `approved`: Đã duyệt thành công.
    - `rejected`: Bị từ chối.
    - `published`: Đã đăng (sau approved).
- **Trigger Approval**: Xảy ra khi user submit content từ `draft` sang `pending_approval`. Một bản ghi mới trong `approvals` được tạo.
- **Actor chính**:
    - **User**: Tự duyệt content của mình (nếu không có team).
    - **Vendor/Agency**: Duyệt content của team hoặc brand khách hàng; có thể multi-level approval (e.g., designer submit → marketer approve).
    - **Admin**: Duyệt hoặc reject content flagged (vi phạm policy).
- **Ràng buộc chung**:
    - Chỉ user có quyền (dựa trên role và team_members.permissions) mới duyệt.
    - Content phải ở `pending_approval` để approve/reject.
    - Sau approve, content có thể lên lịch hoặc đăng; sau reject, quay về `draft` để edit.

### 2. Các Case Hình Thành Được Approval
Dựa trên luồng sử dụng, đây là các case chính (success, error, alternative):

#### Case 1: **Tạo và Submit Content Để Approval (Trigger Approval)**
- **Mô tả**: User/vendor tạo content mới (`draft`), sau đó submit để duyệt, tạo bản ghi Approval với `status = 'pending_approval'`.
- **Điều kiện hình thành**:
    - Content phải ở `draft`.
    - User phải là owner của content (dựa trên `contents.brand_id` liên kết với `brands.user_id` hoặc qua team_members).
- **Sub-cases**:
    - **Success**: Submit thành công, tạo Approval, update `contents.status = 'pending_approval'`.
    - **Error**: Content không tồn tại hoặc user không có quyền → Lỗi "Unauthorized".
    - **Alternative**: Nếu content đã ở `pending_approval`, không submit lại (ràng buộc để tránh duplicate Approval).

#### Case 2: **Duyệt Approval (Approve)**
- **Mô tả**: Actor duyệt content, update `approvals.status = 'approved'`, `approved_at`, và update `contents.status = 'approved'`.
- **Điều kiện hình thành**:
    - Approval phải ở `pending_approval`.
    - Actor phải có quyền (approver_id hợp lệ).
    - Có thể thêm notes (tùy chọn).
- **Sub-cases**:
    - **Success**: Duyệt OK, content sẵn sàng lên lịch/đăng.
    - **Error**: Approval không tồn tại hoặc actor không có quyền → Lỗi "Forbidden".
    - **Alternative**: Nếu multi-level (vendor team), cần tất cả approver approve trước final.

#### Case 3: **Từ Chối Approval (Reject)**
- **Mô tả**: Actor reject, update `approvals.status = 'rejected'`, thêm notes (bắt buộc để giải thích lý do), và update `contents.status = 'draft'` để edit lại.
- **Điều kiện hình thành**:
    - Approval ở `pending_approval`.
    - Actor có quyền, và phải cung cấp notes.
- **Sub-cases**:
    - **Success**: Reject OK, gửi notification cho creator với lý do.
    - **Error**: Không có notes → Lỗi "Notes required for rejection".
    - **Alternative**: Sau reject, creator có thể resubmit → Tạo Approval mới.

#### Case 4: **Admin Flag và Duyệt (System-Level Approval)**
- **Mô tả**: Admin flag content vi phạm (e.g., qua admin_logs), tạo Approval đặc biệt để review.
- **Điều kiện hình thành**:
    - Content ở bất kỳ status nào (thường `approved` hoặc `published`).
    - Chỉ admin trigger.
- **Sub-cases**:
    - **Success**: Admin approve/reject, update content status.
    - **Error**: Không phải admin → Lỗi "Admin only".
    - **Alternative**: Nếu reject, xóa post nếu đã published.

#### Case 5: **Multi-Approver (Cho Vendor Team)**
- **Mô tả**: Vendor có team, cần nhiều approver (e.g., copywriter submit → designer approve → marketer final approve).
- **Điều kiện hình thành**:
    - Vendor config workflow (dựa trên team_members.permissions, e.g., JSONB {"approval_level": 1}).
    - Tạo nhiều bản ghi Approval cho một content (mỗi approver một).
- **Sub-cases**:
    - **Success**: Tất cả approve → Content `approved`.
    - **Error**: Một reject → Toàn bộ reject.
    - **Alternative**: Parallel approval (tất cả approver duyệt song song).

#### Case 6: **Auto-Approval (Optional, Nếu Không Cần Duyệt)**
- **Mô tả**: Với user đơn giản, content auto-approve nếu không có team.
- **Điều kiện hình thành**:
    - Role `user` và không submit manual.
- **Sub-cases**:
    - **Success**: Tạo Approval với `status = 'approved'` ngay khi tạo content.
    - **Alternative**: Config per brand (e.g., trong brands thêm field `auto_approve: boolean`).

### 3. Điều Kiện Ràng Buộc Cho Từng Actor
Dựa trên role và entity (brands, teams, team_members.permissions).

#### Actor: **User (Marketers/Content Creators)**
- **Quyền**: Tự tạo, submit, và approve content của chính mình (nếu không có team).
- **Ràng buộc**:
    - Chỉ submit/approve content thuộc brand của họ (`brands.user_id = user_id`).
    - Không truy cập Approval của người khác.
    - Nếu là team member (của vendor), chỉ approve nếu permissions cho phép (e.g., {"can_approve": true}).
    - Bắt buộc notes nếu reject (dù tự reject để ghi chú).
    - Giới hạn quota từ `subscriptions` (e.g., số content/month).

#### Actor: **Vendor/Agency**
- **Quyền**: Tạo/submit/approve content cho nhiều brand (của họ hoặc khách hàng); quản lý multi-level approval qua team.
- **Ràng buộc**:
    - Chỉ approve nếu là vendor_id của team hoặc approver_id trong approvals.
    - Với team: Kiểm tra team_members.permissions (e.g., marketer mới approve final).
    - Có thể reject với notes bắt buộc, gửi notification cho team member.
    - Giới hạn quota cao hơn (pro plan), nhưng vẫn kiểm tra subscriptions.
    - Nếu multi-approver, cần tất cả complete trước khi content `approved`.

#### Actor: **Admin**
- **Quyền**: Duyệt/reject bất kỳ content nào (system-level), đặc biệt flagged content.
- **Ràng buộc**:
    - Chỉ admin (role = 'admin') trigger.
    - Không cần notes cho approve, nhưng bắt buộc cho reject (log vào admin_logs).
    - Có thể override status mà không qua pending_approval (e.g., direct reject published content).
    - Không bị giới hạn quota.

### 4. Hướng Dẫn Flow Hoàn Chỉnh Để Triển Khai
Dưới đây là flow chi tiết (bước-by-bước) cho team dev triển khai Approval. Sử dụng Agile (sprints), với API endpoints (e.g., POST /contents/submit, PUT /approvals/{id}/approve). Bao gồm success/error/alternative paths.

#### Flow Tổng Quát (Cho Tất Cả Actor)
1. **Tạo Content**: User/vendor tạo content (`draft`) qua API POST /contents. Lưu vào `contents` với brand_id, product_id.
    - Success: Return content_id.
    - Error: Invalid data (e.g., missing text_content) → 400 Bad Request.

2. **Submit Để Approval**: Gọi API POST /contents/{id}/submit. Update `contents.status = 'pending_approval'`, tạo Approval với approver_id (tự assign hoặc từ team).
    - Success: Tạo Approval, gửi notification cho approver(s).
    - Error: Content không phải draft hoặc không quyền → 403 Forbidden.
    - Alternative: Nếu auto-approve (config), skip và set `approved`.

3. **Xem Danh Sách Pending Approval**: Gọi GET /approvals/pending. Filter theo user_id hoặc team_id.
    - Success: Return list với content details.
    - Error: Không quyền → 403.
    - Alternative: Vendor thấy pending của team; user chỉ thấy của mình.

4. **Approve**: Gọi PUT /approvals/{id}/approve với optional notes. Update approvals.status = 'approved', approved_at, và `contents.status = 'approved'`.
    - Success: Update, gửi notification cho creator ("Content approved").
    - Error: Không phải pending_approval hoặc không quyền approver → 403.
    - Alternative: Nếu multi-level, check tất cả approvals approved trước update content.

5. **Reject**: Gọi PUT /approvals/{id}/reject với required notes. Update approvals.status = 'rejected', và `contents.status = 'draft'`.
    - Success: Update, gửi notification cho creator với notes.
    - Error: Không notes hoặc không quyền → 400/403.
    - Alternative: Vendor resubmit sau edit.

6. **Admin Flag và Review**: Admin gọi POST /admin/flag-content/{content_id} với notes. Tạo Approval đặc biệt (status pending), log vào admin_logs.
    - Success: Update content.status = 'pending_approval' (nếu đang approved), gửi notification cho owner.
    - Error: Không phải admin → 403.
    - Alternative: Nếu reject, có thể delete content nếu vi phạm nghiêm trọng.

#### Flow Chi Tiết Theo Actor

##### **Flow Cho User (Role: `user`)**
1. Tạo content (`draft`).
2. Submit → Tạo Approval với approver_id = chính user (tự duyệt).
3. Duyệt/reject tự (nếu reject, edit rồi resubmit).
4. Sau approve, lên lịch hoặc đăng.
- Ràng buộc: Chỉ content của chính user. Không multi-level.

##### **Flow Cho Vendor/Agency (Role: `vendor`)**
1. Vendor hoặc team member tạo content (`draft`).
2. Submit → Tạo Approval với approver_id từ team_members (e.g., marketer).
3. Approver(s) duyệt/reject (multi-level nếu config qua permissions).
4. Nếu reject, team member edit và resubmit (tạo Approval mới).
5. Sau approve final, vendor lên lịch/đăng.
- Ràng buộc: Kiểm tra permissions (e.g., chỉ marketer approve). Vendor override nếu cần.

##### **Flow Cho Admin (Role: `admin`)**
1. Xem flagged content qua GET /admin/flagged-contents.
2. Flag content → Tạo Approval system-level.
3. Duyệt/reject (override bất kỳ).
4. Nếu reject, log và notify owner.
- Ràng buộc: Không ảnh hưởng quota. Có quyền delete nếu cần.

#### **Tích Hợp Với Các Flow Khác**
- **Notifications**: Trigger notification khi submit (cho approver), approve/reject (cho creator).
- **AI Generations**: Nếu content từ AI, submit sau khi merge generated data.
- **Posts**: Chỉ post nếu content `approved`.
- **Error Handling**: Luôn check role/permissions đầu flow. Log errors vào audit_logs nếu có.
- **UI/UX**: Pending list với filter (my approvals, team approvals). Button approve/reject với form notes.

#### **Hướng Dẫn Triển Khai Cho Team Dev**
- **Sprint Planning**: Sprint 1: Implement create/submit. Sprint 2: Approve/reject + notifications. Sprint 3: Multi-level + admin flow.
- **API Design**: Sử dụng RESTful (POST/PUT/GET), JWT auth để check role.
- **Validation**: Backend validate status transitions (e.g., finite state machine cho content_status).
- **Testing**: Unit test (status changes), Integration test (full flow), E2E test (UI submit → approve).
- **Edge Cases**: Content deleted mid-approval → Cascade delete approvals. Concurrent approve (use locking).

Flow này đảm bảo Approval an toàn, scalable. Nếu cần diagram flow (e.g., UML), hãy cho tôi biết!