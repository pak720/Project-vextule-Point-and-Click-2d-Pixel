# Project Vexatule - Point & Click 2D Pixel

Chào mừng bạn đến với dự án **Vexatule**. Đây là một tựa game 2D Pixel thuộc thể loại Point-and-Click, được phát triển trên nền tảng Godot Engine sử dụng C#. 

## 📂 Cấu trúc thư mục dự án

Dưới đây là tổng quan về hệ thống file và thư mục hiện tại của dự án để giúp đội ngũ phát triển dễ dàng nắm bắt:

```text
vexatule/
│
├── .godot/                  # Thư mục hệ thống của Godot (Tự động sinh ra, KHÔNG đụng vào)
├── project.godot            # File cấu hình dự án chính (DO ENGINE TỰ GEN RA)
├── Vexatule.sln             # File Solution của C# (Mở bằng VS Code / Rider)
├── Vexatule.csproj          # File cấu hình dự án C#
│
├── Assets/                  # Chứa toàn bộ tài nguyên gốc (Art, Sound, Fonts)
│   ├── Sprites/
│   │   │── Backgrounds       # Hình ảnh, Texture 2D
│   │   ├── UI/              # Giao diện bàn làm việc, các nút bấm, con dấu
│   │   ├── Characters/      # Ảnh chân dung, quần áo của NPC
│   │   └── Documents/       # Hình ảnh hộ chiếu, giấy phép, thẻ làm việc
│   ├── Audio/               # Âm thanh
│   │   ├── BGM/             # Nhạc nền (Nhạc rùng rợn, u ám phong cách độc tài)
│   │   └── SFX/             # Tiếng đóng dấu, còi phạt, hội thoại "bập bập"
│   └── Fonts/               # Font chữ (Font pixel cổ điển, font chữ viết tay)
│
├── Data/                    # Nơi lưu trữ dữ liệu trò chơi
│   ├── Characters/          # Các file `.tres` (Resource) chứa hồ sơ NPC
│   └── Rules/               # Các file quy định từng ngày (VD: Cấm quốc gia A, B)
│
├── Scripts/                 # Nơi tập trung toàn bộ mã nguồn C# (.cs)
│   ├── Core/                # Logic cốt lõi điều khiển game
│   │   ├── GameManager.cs   # Quản lý vòng lặp game, chuyển ngày, kết quả
│   │   └── DayManager.cs    # Quản lý logic riêng cho từng ngày chơi
│   ├── Resources/           # Định nghĩa các Custom Resource để lưu dữ liệu
│   │   ├── CharacterResource.cs
│   │   └── DailyRuleResource.cs
│   ├── Entities/            # Logic của các đối tượng trong game
│   │   ├── Document2D.cs    # Logic kéo thả, giữ dữ liệu tờ giấy
│   │   ├── StampStation.cs  # Logic lật khuôn dấu, chọn mực, dập dấu
│   │   ├── NPCBooth.cs      # Logic hoạt ảnh NPC đi vào/ra, nộp giấy tờ
│   │   └── Stamper.cs       # Logic con dấu
│   └── UI/                  # Điều khiển giao diện màn hình
│       ├── MainMenuController.cs
│       └── HUDController.cs # Hiển thị số tiền, số lỗi (Citations), đồng hồ
│
└── Scenes/                  # Nơi chứa các phân cảnh của Godot (.tscn)
    ├── UI/                  # Các màn hình menu
    │   ├── MainMenu.tscn
    │   └── GameOver.tscn
    ├── Components/          # Các mảnh phân cảnh nhỏ để ghép vào màn chơi
    │   ├── Document.tscn    # Bản thiết kế tờ giấy (Passport/Ticket)
    │   ├── StampStation.tscn# Bản thiết kế khay đóng dấu
    │   └── CharacterVisual.tscn # Khung hiển thị NPC đối diện
    └── Gameplay.tscn        # Màn hình chơi chính (Nơi ghép các component)
```

> **⚠️ LƯU Ý:** 
> Cấu trúc file này chỉ là thiết kế hiện tại và **CÓ THỂ THAY ĐỔI TRONG TƯƠNG LAI** khi dự án phát triển và mở rộng thêm các tính năng mới.

# Hướng Dẫn Import và Chạy Dự Án Godot 4 (C# / .NET)

Dự án này sử dụng ngôn ngữ lập trình **C#** (nhận biết qua các tệp `Vexatule.csproj` và `Vexatule.sln`), do đó quy trình thiết lập cần có môi trường .NET SDK và phiên bản Godot hỗ trợ C#.

---

## 1. Yêu Cầu Tiền Đề (Prerequisites)

Trước khi mở dự án, hãy đảm bảo máy tính đã cài đặt đầy đủ:

1. **.NET SDK:**
   - Tải phiên bản .NET SDK mới nhất (khuyến nghị .NET 8.0 LTS) từ [trang chủ Microsoft .NET](https://dotnet.microsoft.com/download).
   - Kiểm tra cài đặt bằng Terminal/PowerShell:
     ```bash
     dotnet --version
     ```
2. **Godot Engine (.NET Edition):**
   - Tải bản Godot có nhãn **.NET** (hoặc **Mono**) từ trang chủ Godot.
   - *Lưu ý:* Không sử dụng bản Standard thông thường vì sẽ không thể biên dịch mã nguồn C#.

---

## 2. Tải Mã Nguồn Về Máy

Bạn có thể tải dự án bằng một trong hai cách sau:

### Cách 1: Tải file ZIP từ GitHub
1. Trên trang kho lưu trữ GitHub, nhấn nút xanh **`<> Code`**.
2. Chọn **Download ZIP**.
3. Giải nén tệp `.zip` vào một thư mục trên máy (ví dụ: `D:/GodotProjects/Vexatule`).
4. **Kiểm tra:** Đảm bảo tệp `project.godot` nằm trực tiếp trong thư mục gốc vừa giải nén.

### Cách 2: Sử dụng Git Clone (Khuyên dùng)
Mở Terminal/PowerShell tại thư mục bạn muốn lưu và chạy lệnh:
```bash
git clone <URL_CUA_REPO_GITHUB>
```

---

## 3. Quy Trình Import Vào Godot Engine

1. Khởi động ứng dụng **Godot Engine (.NET)**.
2. Tại màn hình **Project Manager**, nhấn nút **Import** ở thanh công cụ phía trên bên phải.
3. Trong cửa sổ bật lên:
   - Nhấn **Browse** tại mục **Project Path**.
   - Điều hướng tới thư mục dự án vừa giải nén/clone.
   - Chọn tệp **`project.godot`** và nhấn **Open**.
4. Nhấn **Import & Edit** để Godot nạp tài nguyên và mở giao diện chỉnh sửa.

---

## 4. Biên Dịch (Build) và Khởi Chạy

1. **Build mã nguồn C#:**
   - Sau khi Godot mở xong, nhìn lên góc trên cùng bên phải thanh công cụ.
   - Nhấn vào biểu tượng **Build** (hình chiếc búa).
   - Chờ MSBuild tải các gói dependencies và biên dịch mã nguồn. Kiểm tra tab **Build** ở panel phía dưới để đảm bảo không có lỗi (`0 Errors`).
2. **Chạy thử dự án:**
   - Nhấn phím **`F5`** (hoặc nút **Play** ở góc trên bên phải) để chạy scene chính của dự án.
   - Nếu dự án chưa chọn Main Scene, Godot sẽ hiển thị hộp thoại yêu cầu chọn: nhấn **Select Current** hoặc điều hướng vào thư mục `Scenes/` để chọn scene mặc định.

---

## 5. Thiết Lập Trình Soạn Thảo Code (Tùy chọn)

Để code C# thuận tiện hơn với gợi ý code (IntelliSense):
- **Visual Studio Code:**
  - Cài extension: `C# Dev Kit` và `Godot C# Tools`.
  - Trong Godot: Vào `Editor` -> `Editor Settings` -> `Dotnet` -> `Editor` -> `External Editor`, chọn **Visual Studio Code**.
- **Visual Studio Community:**
  - Cài workload **.NET desktop development**.
  - Godot sẽ tự động nhận diện nếu Visual Studio đã được cài đặt.

---

## 6. Xử Lý Các Lỗi Thường Gặp (Troubleshooting)

| Lỗi | Nguyên nhân | Cách khắc phục |
| :--- | :--- | :--- |
| `Failed to build project solution` | Chưa cài đặt .NET SDK hoặc sai đường dẫn. | Kiểm tra lệnh `dotnet --version` trong terminal và khởi động lại Godot. |
| Không thấy nút Build (hình búa) | Đang dùng bản Godot Standard. | Tải lại bản **Godot Engine - .NET**. |
| `Missing C# project file` | Tệp `.csproj` bị mất hoặc sai tên. | Vào menu `Project` -> `Tools` -> `C#` -> `Create C# solution`. |
