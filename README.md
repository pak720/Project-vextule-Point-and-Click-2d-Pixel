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
│   ├── Sprites/             # Hình ảnh, Texture 2D
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