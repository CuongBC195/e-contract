# E-Contract

Hệ thống quản lý hợp đồng và biên lai điện tử với chữ ký số và thông báo email tự động.

## 📋 Mô tả

E-Contract là một ứng dụng web full-stack cho phép người dùng tạo, quản lý và ký kết các hợp đồng và biên lai điện tử. Hệ thống hỗ trợ chữ ký số (vẽ hoặc gõ), gửi email mời ký, tự động tạo PDF, và quản lý người dùng với phân quyền Admin/User.

## ✨ Tính năng chính

### 🔐 Xác thực và Phân quyền
- Đăng ký/Đăng nhập người dùng
- Xác thực email bằng OTP (One-Time Password)
- Quên mật khẩu và đặt lại mật khẩu
- Phân quyền Admin và User
- JWT authentication

### 📄 Quản lý Tài liệu
- Tạo hợp đồng và biên lai từ templates có sẵn
- Editor rich text (TipTap) với định dạng đầy đủ
- Lưu trữ và quản lý tài liệu
- Xem chi tiết tài liệu
- Xóa tài liệu (chưa ký đầy đủ)

### ✍️ Chữ ký số
- Chữ ký bằng cách vẽ (signature drawing)
- Chữ ký bằng cách gõ (typed signature)
- Hiển thị preview chữ ký
- Lưu trữ chữ ký dưới dạng JSON
- Hỗ trợ nhiều bên ký (2 bên cho hợp đồng)

### 🔒 Chế độ ký
- **Public**: Bất kỳ ai có link đều có thể ký
- **RequiredLogin**: Chỉ người dùng đã đăng nhập mới có thể ký

### 📧 Email & Thông báo
- Gửi email mời ký tài liệu
- Thông báo khi tài liệu được ký xong
- Email xác thực tài khoản (OTP)
- Email đặt lại mật khẩu
- Template email chuyên nghiệp

### 📊 Dashboard
- Dashboard cho User: Quản lý tài liệu của mình
- Dashboard cho Admin: Quản lý tất cả tài liệu và người dùng
- Pagination (4 items per page)
- Tìm kiếm và lọc
- Thống kê tài liệu

### 👥 Quản lý User (Admin)
- CRUD đầy đủ cho người dùng
- Xem danh sách users với pagination
- Tạo/Sửa/Xóa user
- Quản lý quyền (Admin/User)
- Xác thực email cho user

## 🛠️ Tech Stack

### Frontend
- **Framework**: Next.js 15 (App Router)
- **Language**: TypeScript
- **Styling**: Tailwind CSS
- **Rich Text Editor**: TipTap
- **Icons**: Lucide React
- **PDF Generation**: jsPDF + html-to-image
- **State Management**: React Hooks

### Backend
- **Framework**: ASP.NET Core 8.0
- **Language**: C#
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core
- **Authentication**: JWT (JSON Web Tokens)
- **Email**: MailKit + SMTP
- **PDF Generation**: PuppeteerSharp
- **API Documentation**: Swagger/OpenAPI

## 📁 Cấu trúc Project

```
E-Contract/
├── frontend/                 # Next.js frontend application
│   ├── src/
│   │   ├── app/             # Next.js App Router pages
│   │   │   ├── api/         # API routes (proxies to backend)
│   │   │   ├── admin/       # Admin pages
│   │   │   ├── user/        # User pages
│   │   │   └── ...
│   │   ├── components/      # React components
│   │   ├── data/           # Templates data
│   │   └── lib/            # Utilities and helpers
│   ├── package.json
│   └── ...
├── backend/                 # ASP.NET Core backend
│   ├── src/
│   │   ├── API/            # API Controllers, Services, DTOs
│   │   ├── Domain/         # Domain entities, enums, value objects
│   │   ├── Infrastructure/ # Data access, repositories
│   │   └── Shared/         # Shared models, helpers
│   ├── backend.csproj
│   └── appsettings.json
└── README.md
```

## 🚀 Cài đặt và Chạy

### Yêu cầu hệ thống

- **Node.js**: >= 18.x
- **.NET SDK**: >= 8.0
- **PostgreSQL**: >= 14.x
- **npm** hoặc **yarn**

### 1. Clone repository

```bash
git clone git@github.com:CuongBC195/e-contract.git
cd e-contract
```

### 2. Cấu hình Backend

#### Database Setup

Tạo database PostgreSQL:

```sql
CREATE DATABASE e_contract;
```

#### Copy và cấu hình `backend/appsettings.json`

**QUAN TRỌNG**: File `appsettings.json` không được commit lên git vì chứa thông tin nhạy cảm.

Tạo file `backend/appsettings.json` từ template:

```bash
cp backend/appsettings.example.json backend/appsettings.json
```

Sau đó chỉnh sửa `backend/appsettings.json` với thông tin thực tế của bạn:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=e_contract;Username=your_username;Password=your_password"
  },
  "Jwt": {
    "SecretKey": "your-super-secret-key-minimum-32-characters",
    "Issuer": "E-Contract",
    "Audience": "E-Contract-Users",
    "ExpirationMinutes": 1440
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "E-Contract System"
  },
  "Frontend": {
    "BaseUrl": "http://localhost:3000"
  }
}
```

#### Chạy Migrations

Backend sẽ tự động chạy migrations khi khởi động (đã cấu hình trong `Program.cs`).

#### Chạy Backend

```bash
cd backend
dotnet restore
dotnet run
# Hoặc với hot reload:
dotnet watch run
```

Backend sẽ chạy tại: `http://localhost:5100` (hoặc port được cấu hình)

API Documentation (Swagger): `http://localhost:5100/swagger`

### 3. Cấu hình Frontend

#### Cài đặt dependencies

```bash
cd frontend
npm install
# Hoặc
yarn install
```

#### Cấu hình Environment Variables

Tạo file `.env.local` (optional):

```env
NEXT_PUBLIC_BACKEND_URL=http://localhost:5100
```

#### Chạy Frontend

```bash
npm run dev
# Hoặc
yarn dev
```

Frontend sẽ chạy tại: `http://localhost:3000`

### 4. Tài khoản mặc định

Sau khi chạy migrations, hệ thống sẽ tự động tạo admin user:

- **Email**: `admin@econtract.com`
- **Password**: `3do_econtract`

## 🔧 Cấu hình

### Backend Configuration

#### Database Connection

Sửa `ConnectionStrings:DefaultConnection` trong `appsettings.json`.

#### JWT Settings

- `SecretKey`: Key để mã hóa JWT (tối thiểu 32 ký tự)
- `ExpirationMinutes`: Thời gian hết hạn của token (mặc định: 1440 phút = 24 giờ)

#### Email Configuration

Cấu hình SMTP để gửi email:
- **Gmail**: Sử dụng App Password (không phải mật khẩu thường)
- **SmtpPort**: 587 (TLS) hoặc 465 (SSL)

### Frontend Configuration

#### Backend URL

Mặc định: `http://localhost:5100`

Có thể thay đổi bằng biến môi trường `NEXT_PUBLIC_BACKEND_URL`.

## 📖 API Documentation

Sau khi chạy backend, truy cập Swagger UI tại:

```
http://localhost:5100/swagger
```

### Các Endpoints chính:

#### Authentication
- `POST /api/auth/register` - Đăng ký user mới
- `POST /api/auth/login` - Đăng nhập
- `POST /api/auth/verify-email` - Xác thực email bằng OTP
- `POST /api/auth/forgot-password` - Yêu cầu đặt lại mật khẩu
- `POST /api/auth/reset-password` - Đặt lại mật khẩu
- `GET /api/auth/me` - Lấy thông tin user hiện tại

#### Documents
- `GET /api/documents` - Lấy danh sách tài liệu (có pagination)
- `GET /api/documents/{id}` - Lấy chi tiết tài liệu
- `POST /api/documents` - Tạo tài liệu mới
- `PUT /api/documents/{id}` - Cập nhật tài liệu
- `DELETE /api/documents/{id}` - Xóa tài liệu
- `POST /api/documents/{id}/sign` - Ký tài liệu
- `POST /api/documents/{id}/track-view` - Theo dõi lượt xem

#### Email
- `POST /api/email/send-invitation` - Gửi email mời ký

#### Admin
- `GET /api/admin/users` - Lấy danh sách users (có pagination)
- `GET /api/admin/users/{id}` - Lấy chi tiết user
- `POST /api/admin/users` - Tạo user mới
- `PUT /api/admin/users/{id}` - Cập nhật user
- `DELETE /api/admin/users/{id}` - Xóa user

## 🗄️ Database Schema

### Tables chính:

- **Users**: Thông tin người dùng
- **Documents**: Tài liệu (hợp đồng/biên lai)
- **Signatures**: Chữ ký của tài liệu
- **Templates**: Templates (nếu cần)

## 🎨 Templates

Hệ thống có sẵn các templates hợp đồng:
- Hợp đồng trống
- Biên lai tiền
- Hợp đồng lao động
- Hợp đồng mua bán
- Và nhiều template khác...

Templates được định nghĩa trong `frontend/src/data/templates.ts`.

## 📝 Chú ý

### Security
- JWT tokens được lưu trong HTTP-only cookies
- Passwords được hash bằng BCrypt
- Email verification bắt buộc cho user mới
- Rate limiting cho các endpoints quan trọng

### File Storage
- Chữ ký được lưu dưới dạng JSON string trong database
- PDF được generate động, không lưu trữ trên disk
- Frontend có thể export PDF từ web view để đảm bảo tính nhất quán

### Pagination
- Mặc định: 4 items per page
- Có thể điều chỉnh qua query parameters `page` và `pageSize`

## 🤝 Contributing

1. Fork the project
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is private and proprietary.

## 👤 Author

**CuongBC195**
- GitHub: [@CuongBC195](https://github.com/CuongBC195)

## 🙏 Acknowledgments

- Next.js team
- ASP.NET Core team
- TipTap editor
- Tailwind CSS

