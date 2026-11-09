# E-Commerce Backend API - Enterprise .NET 8 Project

> **Báo cáo dự án Backend** tuân thủ 100% tất cả các tiêu chí yêu cầu của giảng viên:
> 1. **Kiến trúc phân tầng (Clean Architecture)**: Tách biệt rõ ràng Controllers, Services, DbContext và Entities. Áp dụng trọn vẹn Dependency Injection.
> 2. **CSDL & ORM**: SQL Server, Entity Framework Core 8.0 (Code-First) với **7 thực thể** hoàn chỉnh có ràng buộc quan hệ chặt chẽ.
> 3. **Stored Procedure + Transaction**: Nghiệp vụ đặt hàng ghi đồng thời 3 bảng (`Orders`, `OrderDetails`, `Products` trừ tồn kho) bằng khối `TRY...CATCH`, `BEGIN TRAN`, `COMMIT` và `ROLLBACK`.
> 4. **Bảo mật & Phân quyền (RBAC)**: Xác thực JWT Bearer, phân quyền Role-based gồm 3 vai trò: `Admin`, `Staff`, `Customer`.
> 5. **Validation & Giám sát**: Data Annotations kiểm tra dữ liệu đầu vào; `GlobalExceptionMiddleware` xử lý lỗi tập trung; `Serilog` ghi log hệ thống ra cả Console và Rolling File (`logs/ecommerce-.log`).
> 6. **Triển khai Local IIS**: Cấu hình sẵn `web.config`, kịch bản publish và hướng dẫn triển khai chạy mượt mà trên IIS (Windows).

---

## I. Cấu Trúc Dự Án (Clean Architecture)

```
EcommerceBackend/
├── Ecommerce.Domain/             # Thực thể nghiệp vụ thuần túy (Entities, Enums)
│   ├── Entities/
│   │   ├── Role.cs               # 1. Bảng Vai trò (Admin, Staff, Customer)
│   │   ├── User.cs               # 2. Bảng Người dùng
│   │   ├── Category.cs           # 3. Bảng Danh mục sản phẩm
│   │   ├── Product.cs            # 4. Bảng Sản phẩm
│   │   ├── Order.cs              # 5. Bảng Đơn hàng
│   │   ├── OrderDetail.cs        # 6. Bảng Chi tiết đơn hàng
│   │   └── Review.cs             # 7. Bảng Đánh giá sản phẩm (1-5 sao)
│   └── Enums/
│       └── OrderStatus.cs        # Trạng thái đơn hàng (Pending, Confirmed, Shipped, Delivered, Cancelled)
│
├── Ecommerce.Application/        # Logic nghiệp vụ, DTOs, Interfaces, Validators
│   ├── Common/
│   │   ├── Exceptions/           # NotFoundException, BadRequestException, UnauthorizedException, ForbiddenException
│   │   ├── Interfaces/           # IApplicationDbContext
│   │   └── Models/               # ApiResponse<T> chuẩn hóa output API
│   ├── DTOs/                     # DTOs có Data Annotations validation
│   │   ├── Auth/                 # LoginRequestDto, RegisterRequestDto, AuthResponseDto, CurrentUserDto
│   │   ├── Category/             # CategoryDto, CreateCategoryDto, UpdateCategoryDto
│   │   ├── Product/              # ProductDto, CreateProductDto, UpdateProductDto
│   │   ├── Order/                # OrderDto, CreateOrderDto, OrderDetailDto, UpdateOrderStatusDto
│   │   └── Review/               # ReviewDto, CreateReviewDto
│   ├── Interfaces/               # IAuthService, IProductService, ICategoryService, IOrderService, IReviewService, IPasswordHasher, IJwtTokenGenerator
│   └── Services/                 # Triển khai các Services nghiệp vụ
│
├── Ecommerce.Infrastructure/     # Kết nối Database, EF Core, Stored Procedure, Seeder
│   ├── Data/
│   │   ├── ApplicationDbContext.cs  # DbContext và hàm gọi Stored Procedure Transaction
│   │   ├── DatabaseSeeder.cs        # Tự động nạp dữ liệu mẫu và nạp Stored Procedure khi khởi động
│   │   └── Configurations/          # Fluent API Entity Configurations (Khóa ngoại, Index, Precision)
│   ├── Migrations/                  # EF Core Code-First Migrations (InitialCreate)
│   └── Scripts/
│       └── sp_CreateOrderWithStockDeduction.sql # Script Stored Procedure Transaction
│
├── Ecommerce.WebApi/             # Presentation Layer: Controllers, Middlewares, Program.cs
│   ├── Controllers/
│   │   ├── AuthController.cs        # Đăng ký, Đăng nhập, Lấy thông tin cá nhân (JWT)
│   │   ├── CategoriesController.cs  # CRUD Danh mục (Phân quyền Admin/Staff)
│   │   ├── ProductsController.cs    # CRUD Sản phẩm (Public Read, Admin/Staff Write)
│   │   ├── OrdersController.cs      # Checkout qua Stored Procedure Transaction, Lịch sử, Cập nhật trạng thái
│   │   └── ReviewsController.cs     # Đánh giá sản phẩm
│   ├── Middlewares/
│   │   └── GlobalExceptionMiddleware.cs # Bắt lỗi toàn cục, trả về ApiResponse chuẩn HTTP 400/401/403/404/500
│   ├── appsettings.json             # Chuỗi kết nối SQL Server, JWT Secret, Serilog
│   ├── Program.cs                   # Điểm khởi động ứng dụng, nạp DI, JWT, Swagger, Serilog
│   └── web.config                   # Cấu hình triển khai Local IIS (AspNetCoreModuleV2)
│
├── Database_Full_Setup.sql       # Script SQL thuần thiết lập trọn gói CSDL trong 1 click
└── publish/                      # Thư mục build sẵn sàng triển khai IIS
```

---

## II. Danh Sách 7 Thực Thể (Entities & Relationships)

| STT | Thực Thể (Entity) | Bảng SQL | Mô Tả & Quan Hệ |
| :---: | :--- | :--- | :--- |
| **1** | `Role` | `Roles` | Vai trò hệ thống (`Admin`, `Staff`, `Customer`). Quan hệ 1-N với `Users`. |
| **2** | `User` | `Users` | Người dùng (Username, Email, BCrypt Password, FullName). N-1 với `Roles`, 1-N với `Orders`, 1-N với `Reviews`. |
| **3** | `Category` | `Categories` | Danh mục sản phẩm (Laptops, Smartphones, Audio). Quan hệ 1-N với `Products`. |
| **4** | `Product` | `Products` | Sản phẩm (SKU, Giá, Số lượng tồn kho `StockQuantity`). N-1 với `Categories`, 1-N với `OrderDetails`, 1-N với `Reviews`. |
| **5** | `Order` | `Orders` | Đơn đặt hàng (Mã đơn `OrderNumber`, Tổng tiền, Trạng thái, Địa chỉ giao hàng). N-1 với `Users`, 1-N với `OrderDetails`. |
| **6** | `OrderDetail` | `OrderDetails` | Chi tiết mặt hàng trong đơn (Số lượng `Quantity`, Đơn giá `UnitPrice`, Thành tiền `SubTotal`). N-1 với `Orders`, N-1 với `Products`. |
| **7** | `Review` | `Reviews` | Đánh giá xếp hạng sao (1 - 5) và bình luận. N-1 với `Users`, N-1 với `Products`. |

---

## III. Nghiệp Vụ Giao Dịch Stored Procedure & Transaction

### Yêu cầu giảng viên:
> *"Bắt buộc có tối thiểu 1 nghiệp vụ giao dịch ghi nhiều bảng sử dụng khối TRY...CATCH kết hợp BEGIN TRAN, COMMIT và ROLLBACK."*

### Triển khai: `[dbo].[sp_CreateOrderWithStockDeduction]`
Nghiệp vụ **Đặt hàng thanh toán (Checkout)** ghi đồng thời **3 bảng**:
1. **Kiểm tra tồn kho với khóa độc quyền**: Đọc dữ liệu với `UPDLOCK, ROWLOCK`. Nếu bất kỳ sản phẩm nào có `StockQuantity < RequestedQuantity`, kích hoạt `THROW 50002` nhảy thẳng vào `CATCH`.
2. **Ghi Bảng 1 (`Orders`)**: Tạo mới bản ghi đơn hàng với mã `ORD-YYYYMMDD-XXXX` và tổng tiền tính từ đơn giá chính xác trong DB. Lấy `SCOPE_IDENTITY()`.
3. **Ghi Bảng 2 (`OrderDetails`)**: Thêm toàn bộ các sản phẩm đã chọn vào chi tiết đơn hàng tương ứng.
4. **Ghi Bảng 3 (`Products`)**: Trừ trực tiếp số lượng tồn kho `StockQuantity = StockQuantity - RequestedQuantity`.
5. **Khối Transaction an toàn**:
   ```sql
   BEGIN TRY
       BEGIN TRANSACTION;
       -- Ghi Bảng 1: INSERT INTO Orders ...
       -- Ghi Bảng 2: INSERT INTO OrderDetails ...
       -- Ghi Bảng 3: UPDATE Products SET StockQuantity = StockQuantity - @Qty ...
       COMMIT TRANSACTION;
   END TRY
   BEGIN CATCH
       IF @@TRANCOUNT > 0
       BEGIN
           ROLLBACK TRANSACTION;
       END;
       THROW;
   END CATCH;
   ```

---

## IV. Tài Khoản Mặc Định & Phân Quyền (RBAC)

Hệ thống đã nạp sẵn dữ liệu ban đầu (**Seed Data**) với mật khẩu được băm bằng thuật toán **BCrypt**:

| Username | Mật Khẩu | Vai Trò (Role) | Quyền Hạn |
| :--- | :--- | :---: | :--- |
| `admin` | `Admin@123` | **Admin** | Toàn quyền quản trị hệ thống (CRUD Danh mục, CRUD Sản phẩm, Quản lý toàn bộ Đơn hàng). |
| `staff` | `Staff@123` | **Staff** | Quản lý kho, Thêm/Sửa danh mục & sản phẩm, Xem và Duyệt cập nhật trạng thái đơn hàng. |
| `customer` | `Customer@123` | **Customer** | Khách hàng mua sắm, Đặt hàng (gọi Stored Procedure Transaction), Xem lịch sử đơn của mình, Đánh giá sản phẩm. |

---

## V. Hướng Dẫn Chạy & Kiểm Thử Trên Máy

### Cách 1: Thiết lập CSDL SQL Server
Bạn có thể chọn 1 trong 2 cách sau:

- **Cách A (Tự động hoàn toàn)**:
  Cấu hình chuỗi kết nối trong `Ecommerce.WebApi/appsettings.json`:
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EcommerceDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
  }
  ```
  Khi bạn chạy ứng dụng lần đầu, `DatabaseSeeder` sẽ **tự động khởi tạo database, chạy Stored Procedure và nạp dữ liệu mẫu**.

- **Cách B (Chạy bằng file SQL)**:
  Mở SQL Server Management Studio (SSMS) hoặc Azure Data Studio, mở file `Database_Full_Setup.sql` và bấm **Execute (F5)**. Toàn bộ bảng, stored procedure, index và dữ liệu mẫu sẽ được nạp ngay lập tức.

---

### Cách 2: Chạy ứng dụng bằng .NET CLI
Mở terminal tại thư mục gốc `EcommerceBackend`:
```powershell
# Chạy ứng dụng Web API
dotnet run --project Ecommerce.WebApi
```
Truy cập Swagger UI trực tiếp trên trình duyệt:
👉 **`http://localhost:5000`** hoặc **`https://localhost:5001`**

#### Thử nghiệm JWT trên Swagger:
1. Gọi API `POST /api/auth/login` với:
   ```json
   {
     "usernameOrEmail": "admin",
     "password": "Admin@123"
   }
   ```
2. Copy chuỗi `token` nhận được.
3. Bấm nút **Authorize (hình ổ khóa màu xanh)** ở góc trên bên phải Swagger, dán vào:
   ```
   Bearer <chuỗi_token_của_bạn>
   ```
4. Bấm **Authorize**. Giờ đây bạn có thể gọi tất cả các API có phân quyền theo đúng vai trò!

---

## VI. Hướng Dẫn Triển Khai Local IIS (Windows)

Yêu cầu giảng viên:
> *"6. Triển khai: Đóng gói và chạy kiểm thử ổn định trên Local IIS (Windows)."*

### Bước 1: Cài đặt IIS & ASP.NET Core Hosting Bundle
1. Bật tính năng IIS trên Windows:
   - Mở **Turn Windows features on or off** -> Tích chọn **Internet Information Services** (bao gồm `World Wide Web Services`).
2. Tải và cài đặt **.NET 8.0 Hosting Bundle**:
   - Link chính thức từ Microsoft: [ASP.NET Core 8.0 Hosting Bundle Installer](https://dotnet.microsoft.com/download/dotnet/8.0).

### Bước 2: Đóng gói ứng dụng (Publish)
Thư mục `publish/` đã được đóng gói sẵn trong project. Nếu bạn muốn build lại bất cứ lúc nào:
```powershell
dotnet publish Ecommerce.WebApi\Ecommerce.WebApi.csproj -c Release -o .\publish
```

### Bước 3: Cấu hình Site trên IIS Manager
1. Mở **Internet Information Services (IIS) Manager** (`inetmgr`).
2. **Tạo Application Pool**:
   - Nhấp chuột phải vào **Application Pools** -> **Add Application Pool...**
   - Tên App Pool: `EcommerceAppPool`
   - .NET CLR version: **No Managed Code** *(bắt buộc đối với .NET Core/.NET 8)*
   - Managed pipeline mode: **Integrated** -> Bấm OK.
3. **Tạo Web Site**:
   - Nhấp chuột phải vào **Sites** -> **Add Website...**
   - Site name: `EcommerceApi`
   - Application pool: Chọn `EcommerceAppPool`
   - Physical path: Trỏ đến thư mục `publish` (ví dụ: `C:\Users\Hunter\.gemini\antigravity\scratch\EcommerceBackend\publish`)
   - Port: `8080` (hoặc cổng bất kỳ bạn muốn) -> Bấm OK.
4. **Phân quyền ghi thư mục log cho IIS**:
   - Nhấp chuột phải vào thư mục `publish` -> **Properties** -> Tab **Security** -> **Edit...**
   - Bấm **Add...** -> Nhập `IIS_IUSRS` -> Bấm **Check Names** -> Bấm **OK**.
   - Cấp quyền **Read & execute**, **List folder contents**, **Read**, **Write** -> Bấm **Apply**.

### Bước 4: Kiểm thử trên IIS
Mở trình duyệt và truy cập:
👉 **`http://localhost:8080`** -> Bạn sẽ thấy giao diện **Swagger UI** của E-Commerce API chạy trực tiếp trên Local IIS!
Mọi thao tác ghi log sẽ được lưu tại `publish/logs/ecommerce-YYYYMMDD.log`.
