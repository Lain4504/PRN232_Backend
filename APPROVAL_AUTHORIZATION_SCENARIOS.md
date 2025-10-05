# Approval Authorization Scenarios

## Quyền truy cập Approval API

### 🔐 **Authorization Rules**

| Role | Brand Owner | Assigned Approver | Other Users |
|------|-------------|-------------------|-------------|
| **Admin** | ✅ Full Access | ✅ Full Access | ✅ Full Access |
| **User** | ✅ Full Access | ✅ Read/Approve/Reject/Update | ❌ No Access |

---

## 📋 **Scenarios**

### **Scenario 1: Admin User**
```
UserId: 44444444-4444-4444-4444-444444444444
Role: Admin
Result: ✅ AUTHORIZED for ALL operations on ANY approval
```

### **Scenario 2: Brand Owner**
```
UserId: 11111111-1111-1111-1111-111111111111
BrandId: 33333333-3333-3333-3333-333333333333
Brand.UserId: 11111111-1111-1111-1111-111111111111
Result: ✅ AUTHORIZED for ALL operations (Create/Read/Update/Delete/Approve/Reject)
```

### **Scenario 3: Assigned Approver (Not Brand Owner)**
```
UserId: c607de95-d324-4a55-b7cd-ad138607e4c1
BrandId: 33333333-3333-3333-3333-333333333333
Brand.UserId: 44444444-4444-4444-4444-444444444444
Approval.ApproverId: c607de95-d324-4a55-b7cd-ad138607e4c1
Result: ✅ AUTHORIZED for Read/Approve/Reject/Update
        ❌ FORBIDDEN for Create/Delete
```

### **Scenario 4: Other User (No Relationship)**
```
UserId: 99999999-9999-9999-9999-999999999999
BrandId: 33333333-3333-3333-3333-333333333333
Brand.UserId: 44444444-4444-4444-4444-444444444444
Approval.ApproverId: c607de95-d324-4a55-b7cd-ad138607e4c1
Result: ❌ FORBIDDEN for ALL operations
```

---

## 🎯 **Operations Matrix**

| Operation | Admin | Brand Owner | Assigned Approver | Other Users |
|-----------|-------|-------------|-------------------|-------------|
| **Create** | ✅ | ✅ | ❌ | ❌ |
| **Read** | ✅ | ✅ | ✅ | ❌ |
| **Update** | ✅ | ✅ | ✅ | ❌ |
| **Delete** | ✅ | ✅ | ❌ | ❌ |
| **Approve** | ✅ | ✅ | ✅ | ❌ |
| **Reject** | ✅ | ✅ | ✅ | ❌ |

---

## 🔍 **Authorization Logic**

```csharp
// 1. Admin bypass
if (userRole == UserRoleEnum.Admin) 
    return AUTHORIZED;

// 2. Brand ownership check
if (approval.Content.Brand.UserId == userId) 
    return AUTHORIZED;

// 3. Assigned approver check
if (isApprovalOperation && approval.ApproverId == userId) 
    return AUTHORIZED;

// 4. Default deny
return FORBIDDEN;
```

**Where:**
- `isApprovalOperation` = Read | Approve | Reject | Update
- Brand ownership grants **full access**
- Approver assignment grants **limited access**

---

## 📝 **Sample Data Mapping**

| User | Role | Brands Owned | Approvals Assigned |
|------|------|--------------|-------------------|
| `11111111...` | User | `33333333...`, `66666666...` | - |
| `44444444...` | Admin | - | `90909090...`, `a0a0a0a0...`, `b0b0b0b0...` |
| `c607de95...` | User | - | `11bc6c7b...` |

---

## ⚠️ **Important Notes**

1. **Database Role Check**: Role được query từ database, không dựa vào JWT claims
2. **Navigation Properties**: Cần ensure `Content.Brand` được load đầy đủ
3. **Logging**: Tất cả authorization decisions được log chi tiết
4. **Performance**: Mỗi request = 1 database query để lấy user role
5. **Security**: Approver chỉ có quyền hạn chế, không thể tạo/xóa approval