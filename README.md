# Project-vextule-Point-and-Click-2d-Pixel

📂 vexatule/
│
├── 📂 .godot/                  # Thư mục hệ thống của Godot (Tự động sinh ra, không đụng vào)
├── 📄 project.godot            # File cấu hình dự án chính (FILE DO ENGINE TỰ GEN RA)
├── 📄 Vexatule.sln        # File Solution của C# (Mở bằng VS Code / Rider) (FILE DO ENGINE TỰ GEN RA)
├── 📄 Vexatule.csproj   # File cấu hình dự án C# (FILE DO ENGINE TỰ GEN RA)
│
├── 📂 Assets/                  # Chứa toàn bộ file gốc (Art, Sound, Fonts)
│   ├── 📂 Sprites/             # Hình ảnh, Texture 2D
│   │   ├── 📂 UI/              # Giao diện bàn làm việc, các nút bấm, con dấu
│   │   ├── 📂 Characters/      # Ảnh chân dung, quần áo của NPC
│   │   └── 📂 Documents/       # Hình ảnh hộ chiếu, giấy phép, thẻ làm việc
│   ├── 📂 Audio/               # Âm thanh
│   │   ├── 📂 BGM/             # Nhạc nền (Nhạc rùng rợn, u ám phong cách độc tài)
│   │   └── 📂 SFX/             # Tiếng đóng dấu, tiếng còi phạt, tiếng hội thoại "bập bập"
│   └── 📂 Fonts/               # Font chữ (Font pixel cổ điển, font chữ viết tay)
│
├── 📂 Data/                    # Nơi lưu trữ dữ liệu trò chơi
│   ├── 📂 Characters/          # Các file `.tres` (Resource) chứa hồ sơ từng NPC tạo sẵn
│   └── 📂 Rules/               # Các file quy định của từng ngày (Ví dụ: Cấm quốc gia A, B)
│
├── 📂 Scripts/                 # Nơi tập trung toàn bộ mã nguồn C# (.cs)
│   ├── 📂 Core/                # Logic cốt lõi điều khiển game
│   │   ├── 📄 GameManager.cs   # Quản lý vòng lặp game, chuyển ngày, kiểm tra kết quả
│   │   └── 📄 DayManager.cs    # Quản lý logic riêng cho từng ngày chơi
│   ├── 📂 Resources/           # Định nghĩa các Custom Resource để lưu dữ liệu
│   │   ├── 📄 CharacterResource.cs
│   │   └── 📄 DailyRuleResource.cs
│   ├── 📂 Entities/            # Logic của các đối tượng xuất hiện trong game
│   │   ├── 📄 Document2D.cs    # Logic kéo thả, giữ dữ liệu của tờ giấy
│   │   ├── 📄 StampStation.cs # Logic lật khuôn dấu, chọn mực đỏ/xanh, dập dấu
│   │   └── 📄 NPCBooth.cs      # Logic hoạt ảnh NPC đi vào/đi ra, nộp giấy tờ
│   │   └── 📄 Stamper.cs 
│   └── 📂 UI/                  # Điều khiển giao diện màn hình
│       ├── 📄 MainMenuController.cs
│       └── 📄 HUDController.cs # Hiển thị số tiền, số lỗi (Citations), đồng hồ
│
└── 📂 Scenes/                  # Nơi chứa các phân cảnh của Godot (.tscn)
    ├── 📂 UI/                  # Các màn hình menu
    │   ├── 📄 MainMenu.tscn
    │   └── 📄 GameOver.tscn
    ├── 📂 Components/          # Các mảnh phân cảnh nhỏ để ghép vào màn chơi chính
    │   ├── 📄 Document.tscn    # Bản thiết kế của một tờ giấy (Passport/Ticket)
    │   ├── 📄 StampStation.tscn# Bản thiết kế của khay đóng dấu
    │   └── 📄 CharacterVisual.tscn # Khung hiển thị NPC đối diện
    └── 📄 Gameplay.tscn        # Màn hình chơi chính (Nơi ghép tất cả component lại)
NOTE: CẤU TRÚC FILE CÓ THỂ THAY ĐỔI TRONG TƯƠNG LAI

