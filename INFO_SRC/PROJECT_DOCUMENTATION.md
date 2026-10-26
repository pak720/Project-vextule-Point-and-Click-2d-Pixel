# Tài liệu dự án Vexatule

> Tài liệu tổng hợp theo trạng thái repository tại ngày 25-10-2026. Đường dẫn trong tài liệu dùng dạng tương đối với thư mục gốc dự án. Nội dung mô tả các tệp đang có; hình ảnh, font và tệp nhị phân được kiểm kê theo tên/chức năng, không nhúng dữ liệu nhị phân vào tài liệu.

## 1. Tổng quan

**Vexatule** là game 2D pixel point-and-click xây dựng bằng Godot Engine với C#. Người chơi vào vai nhân viên kiểm soát biên giới, xem hồ sơ nhập cảnh, so sánh thông tin và đóng dấu chấp thuận hoặc từ chối.

| Thuộc tính | Giá trị hiện tại |
|---|---|
| Engine / định dạng scene | Godot 4, scene format 3 |
| Phiên bản SDK Godot trong project | Godot.NET.Sdk 4.7.2 |
| Ngôn ngữ | C# |
| Target framework | .NET 8 (`net8.0`); Android dùng `net9.0` |
| Assembly / solution | `Vexatule` / `Vexatule.sln` |
| Main scene | `Scenes/UI/MainMenu.tscn` |
| Kích thước viewport | 1408 × 950 |
| Tài nguyên chính | PNG pixel art, TTF, dữ liệu JSON |
| Âm thanh | Chưa có tệp âm thanh được theo dõi trong repository |
| Trạng thái thư mục | 128 tệp được Git theo dõi trước khi thêm tài liệu này |

### Vòng đời scene dự kiến

```text
MainMenu
  └─ PLAY → IntroStory
                └─ hết lời dẫn → DayTransition
                                      └─ hết hiệu ứng → Gameplay
```

Scene và bộ điều khiển tương ứng:

| Scene | Script/điểm chính |
|---|---|
| `Scenes/UI/MainMenu.tscn` | `Scripts/UI/MainMenuController.cs`; nút PLAY chuyển sang intro |
| `Scenes/UI/IntroStory.tscn` | `Scripts/UI/IntroStoryController.cs`; 8 câu chuyện, 3 ảnh nền |
| `Scenes/UI/DayTransition.tscn` | `Scripts/UI/DayTransitionController.cs`; hiện “DAY 1”, sau đó vào Gameplay |
| `Scenes/Gameplay.tscn` | `Scripts/Core/GameManager.cs`; bàn làm việc, NPC, hộ chiếu, vé tàu và khay dấu |

## 2. Cấu trúc repository

```text
.
├── Assets/
│   ├── Fonts/
│   └── Sprites/
│       ├── Backgrounds/
│       ├── Characters/
│       ├── Documents/
│       └── UI/
├── Data/
│   ├── Characters/
│   │   ├── Aleksei Volkov 3/
│   │   ├── Arthur Pendelton 1/
│   │   ├── Elena Vasquez 2 (Valid)/
│   │   ├── Lila Moonwhisper 6 (Valid)/
│   │   └── Seraphina Swiftpaw 10 (Missing Documents - Invalid)/
│   └── document_asset_generation.json
├── INFO_SRC/
│   └── PROJECT_DOCUMENTATION.md
├── Scenes/
│   ├── Components/
│   ├── UI/
│   └── Gameplay.tscn
├── Scripts/
│   ├── Core/
│   ├── Entities/
│   └── UI/
├── .editorconfig
├── .gitattributes
├── .gitignore
├── README.md
├── Vexatule.csproj
├── Vexatule.sln
├── icon.svg
└── project.godot
```

`.godot/` là thư mục cache/import sinh bởi Godot, bị loại khỏi Git. Các tệp `*.import` lưu metadata import tài nguyên; `*.cs.uid` lưu định danh script mà Godot dùng trong scene. Không chỉnh sửa các tệp cache sinh tự động bằng tay.

### Số lượng tệp hiện có theo loại

| Loại | Số lượng | Ghi chú |
|---|---:|---|
| `.cs` | 11 | Mã nguồn ứng dụng |
| `.cs.uid` | 11 | UID đi kèm script |
| `.tscn` | 7 | Scene Godot |
| `.png` | 37 | 26 ảnh trong `Assets`, 11 ảnh hồ sơ trong `Data` |
| `.import` | 40 | Metadata import tài nguyên |
| `.json` | 12 | 11 JSON hồ sơ + 1 manifest tạo asset |
| `.ttf` | 2 | Font |
| `.svg` | 1 | Icon dự án |
| Tệp cấu hình/tài liệu khác | 6 | `.editorconfig`, `.gitattributes`, `.gitignore`, `.md`, `.csproj`, `.sln` |

## 3. Mã nguồn C#

### Core

| Tệp | Vai trò và hành vi hiện tại |
|---|---|
| `Scripts/Core/GameManager.cs` | Singleton `GameManager`; import JSON từ thư mục nhân vật được cấu hình, nạp hộ chiếu/vé lên UI, kiểm tra hồ sơ, lưu quyết định con dấu và thông báo kết quả khi trả giấy tờ. Mặc định hồ sơ là `res://Data/Characters/Arthur Pendelton 1`, ngày game là `2130-01-01`. |
| `Scripts/Core/DayManager.cs` | Node khung; `_Ready()` và `_Process()` hiện chưa có logic. |
| `Scripts/Core/VerificationEngine.cs` | Validator tĩnh `CrossCheckNPC`: kiểm tra hộ chiếu, ngày hết hạn, tên/ngày vé, tên/mã đơn tị nạn, tên/cơ quan cấp giấy ngoại giao. Danh sách mã tị nạn hợp lệ nằm trong class này. Scene/GameManager hiện tại không gọi class này; GameManager có validator riêng. |

### Entities

| Tệp | Vai trò và hành vi hiện tại |
|---|---|
| `Scripts/Entities/Document2D.cs` | DTO `PassportJsonData` và hộ chiếu tương tác: tải dữ liệu JSON, điền nhãn/ảnh, kéo thả, mở/đóng khi qua ngưỡng X, giới hạn vị trí thả vào khu vực bàn và xóa dấu mực cũ. Dùng group `passport` cho hộ chiếu mở; giữ trạng thái giấy đang kéo dùng chung với vé tàu. |
| `Scripts/Entities/TrainTicket2D.cs` | DTO `TrainTicketJsonData` và vé tàu tương tác: tải dữ liệu/điền nhãn, kéo thả, đổi giữa kích thước thu nhỏ và kích thước đọc được theo ngưỡng X, giới hạn điểm thả trên bàn. |
| `Scripts/Entities/NPCBooth.cs` | Điều khiển NPC đi vào/ra quầy bằng tween, hiệu ứng bước chân, bàn giao hồ sơ, thu giấy và rời quầy theo hướng quyết định dấu (Approve sang phải, Denied sang trái). |
| `Scripts/Entities/StampStation.cs` | Mở/thu khay dấu theo click vào `HandleArea`, dùng animation `Extend`, `Retract`, `RESET`. |
| `Scripts/Entities/Stamper.cs` | Kéo con dấu, chạy tween dập, tìm vùng đang chồng lấp thuộc group `passport` và thêm texture dấu lên hộ chiếu. Loại `Record` không báo phán quyết; `Approve`/`Denied` cập nhật `GameManager.PlayerStampDecision`. |

### UI

| Tệp | Vai trò và hành vi hiện tại |
|---|---|
| `Scripts/UI/MainMenuController.cs` | Nối nút PLAY và SETTINGS; PLAY mở IntroStory. SETTINGS hiện chỉ ghi log/TODO, chưa mở panel. |
| `Scripts/UI/IntroStoryController.cs` | Hiện lần lượt 8 dòng nội dung, đổi ảnh nền theo nhóm dòng, hỗ trợ click chuột/Space/Enter, fade rồi chuyển sang DayTransition. |
| `Scripts/UI/DayTransitionController.cs` | Fade-in nhãn ngày, giữ màn hình, fade-out rồi chuyển sang Gameplay. |

## 4. Luồng gameplay và tương tác

1. `Gameplay.tscn` gắn `GameManager` lên root và tham chiếu node NPC/hộ chiếu.
2. `GameManager._Ready()` tìm node theo NodePath nếu Inspector chưa gán, tải mọi tệp `.json` trong thư mục nhân vật và chạy `RunCrossCheckValidation()`.
3. Hộ chiếu và vé ban đầu ẩn. NPC đi tới quầy; sau khi tới nơi, `NPCBooth` hiện hồ sơ ở khu vực bàn.
4. Người chơi kéo hộ chiếu/vé. Hộ chiếu mở khi vượt qua ngưỡng X; vé phóng to khi đi sang phía bàn làm việc. Chỉ cho phép kéo một tài liệu tại một thời điểm.
5. Người chơi mở khay dấu, kéo một dấu lên hộ chiếu đang mở. Texture dấu được gắn làm con của hộ chiếu.
6. Khi hộ chiếu được thả sau khi đã có quyết định dấu, `Document2D` yêu cầu NPC thu hồ sơ và rời quầy.
7. `GameManager` ghi log đúng/sai theo so sánh `PlayerStampDecision` với `IsCurrentNPCValid`; lỗi hiện được báo bằng log/citation, chưa có hệ thống điểm hoặc HUD hoàn chỉnh.

## 5. Cấu trúc scene

| Tệp scene | Nội dung |
|---|---|
| `Scenes/Gameplay.tscn` | Bàn làm việc, guard rail, NPC `TEST3`, `DocumentDeskArea`, vùng thả hồ sơ, khay dấu, hình nền bàn. Cấu hình nhân vật ban đầu đang dùng texture Arthur Pendelton. |
| `Scenes/Components/Document.tscn` | Hộ chiếu mở/đóng, collision shape, avatar và nhãn ID/tên/giới tính/ngày sinh/chủng loài/ngày hết hạn/quốc gia. |
| `Scenes/Components/TrainTicket.tscn` | Ticket sprite, collision shape và nhãn ID/tên hành khách/ngày đi/tàu/hạng/ghế. |
| `Scenes/Components/StampStation.tscn` | Khay animation, vùng click tay cầm, ba dấu Approve/Denied/Record và texture mực tương ứng. |
| `Scenes/UI/MainMenu.tscn` | Nút PLAY và SETTINGS. Background hiện chưa được gán texture. |
| `Scenes/UI/IntroStory.tscn` | Background, hộp hội thoại, gợi ý tiếp tục và 3 ảnh intro. |
| `Scenes/UI/DayTransition.tscn` | Nền đen và nhãn “DAY 1”. |

## 6. Dữ liệu hồ sơ nhân vật

Các JSON dùng tên thuộc tính `snake_case`. `passport.json` và `train_ticket.json` được deserialize vào DTO C#; những loại JSON khác được nạp dạng `JsonElement`. `is_valid` và `error_note` hiện là metadata trong tệp, không phải tiêu chí quyết định chính trong `GameManager`.

### 6.1 Arthur Pendelton — `Data/Characters/Arthur Pendelton 1/`

- `passport.json`: `document_type=passport`; `document_id=SYL-7421-P`; `name=Arthur Pendelton`; `gender=M`; `birth_date=2101-05-12`; `species=human`; `issuing_country=Kuseropia Repubationtopia`; `expiry_date=2139-08-20`; avatar `res://Assets/Sprites/Characters/arthur_pendelton_avatar.png`; `is_valid=true`; `error_note` rỗng.
- `train_ticket.json`: `document_type=train_ticket`; `document_id=SYL-7421-P`; `passenger_name=Arthur Pendelton`; `train_id=EXPRESS-01`; `travel_date=2134-10-19`; `class=First`; `seat=A-12`; `is_valid=true`; `error_note` rỗng.
- Ảnh hồ sơ: `passport.png`, `Train_ticket_Arthur_Pendelton.png`.

### 6.2 Aleksei Volkov — `Data/Characters/Aleksei Volkov 3/`

- `passport.json`: ID `KUS-8812-V`; tên `Aleksei Volkov`; giới tính `M`; sinh `2104-09-28`; chủng loài `human`; quốc gia `Kuseropia Repubationtopia`; hết hạn `2137-04-10`; avatar `res://Assets/Sprites/Characters/aleksei_volkov_avatar.png`; hợp lệ theo metadata.
- `train_ticket.json`: ID `KUS-8812-V`; hành khách `Aleksei Volkov`; tàu `PLATFORM13-TRANS-04`; ngày `2134-10-19`; hạng `ECO`; ghế `B-13`; hợp lệ theo metadata.
- `asylum_paper.json`: ID `KUS-8812-V`; người nộp `Aleksei Volkov`; lý do `Political Persecution`; mã `ALPHA-992-SEC`; cấp `2134-10-01`; hết hạn `2135-04-01`; fingerprint `FP_HUMAN_01`; hợp lệ theo metadata.
- Ảnh hồ sơ: `passport.png`, `Train_ticket_Aleksei_Volkov.png`, `asylum_paper.png`.

### 6.3 Elena Vasquez — `Data/Characters/Elena Vasquez 2 (Valid)/`

- `passport.json`: ID `LDX-0268-J`; tên `Elena Vasquez`; giới tính `F`; sinh `1990-07-08`; chủng loài `elf`; quốc gia `Kusestan`; hết hạn `2032-08-15`; avatar `res://Assets/Sprites/Characters/elena_vasquez_avatar.png`; hợp lệ theo metadata.
- `train_ticket.json`: ID `LDX-0268-J`; hành khách `Elena Vasquez`; tàu `EXPRESS-25`; ngày `2026-10-21`; hạng `ECO`; ghế `B-15`; hợp lệ theo metadata.
- `asylum_paper.json`: ID `LDX-0268-J`; người nộp `Elena Vasquez`; lý do `Religious Cleansing`; mã `SACRED-771-VEX`; cấp `2026-10-10`; hết hạn `2027-03-15`; fingerprint `FP_ELF_06`; hợp lệ theo metadata.
- Ảnh hồ sơ: `passport.png`, `Train_ticket_Elena_Vasquez.png`, `asylum_paper.png`.

### 6.4 Lila Moonwhisper — `Data/Characters/Lila Moonwhisper 6 (Valid)/`

- `passport.json`: ID `BQF-9802-R`; tên `Lila Moonwhisper`; giới tính `F`; sinh `1997-04-27`; chủng loài `Human`; quốc gia `Kuseropia Repubationtopia`; hết hạn `2032-08-15`; avatar `res://Assets/Sprites/Characters/lila_moonwhisper_avatar.png`; hợp lệ theo metadata.
- `train_ticket.json`: ID `BQF-9802-R`; hành khách `Lila Moonwhisper`; tàu `EXPRESS-28`; ngày `2026-10-21`; hạng `ECO`; ghế `B-32`; hợp lệ theo metadata.
- Ảnh hồ sơ: `passport.png`, `Train_ticket_Lila_Moonwhisper.png`.

### 6.5 Seraphina Swiftpaw — `Data/Characters/Seraphina Swiftpaw 10 (Missing Documents - Invalid)/`

- `passport.json`: ID `KUS-1084-S`; tên `Seraphina Swiftpaw`; giới tính `F`; sinh `2108-12-22`; chủng loài `beastman`; quốc gia `Kusestan`; hết hạn `2139-09-14`; avatar `res://Assets/Sprites/Characters/seraphina_swiftpaw_avatar.png`; `is_valid=false`; ghi chú dữ liệu nói thiếu `asylum_paper.json`.
- Chỉ có `passport.json` và `passport.png`; chưa có vé tàu hoặc đơn tị nạn.

### 6.6 Schema JSON

| Loại | Thuộc tính |
|---|---|
| Passport | `document_type`, `document_id`, `name`, `gender`, `birth_date`, `species`, `issuing_country`, `expiry_date`, `avatar_file`, `is_valid`, `error_note` |
| Vé tàu | `document_type`, `document_id`, `passenger_name`, `train_id`, `travel_date`, `class`, `seat`, `is_valid`, `error_note` |
| Đơn tị nạn | `document_type`, `document_id`, `applicant_name`, `reason_for_asylum`, `approval_code`, `issue_date`, `expiry_date`, `fingerprint_id`, `is_valid`, `error_note` |
| Manifest tạo asset | Mảng `assets`, mỗi phần tử có `path`, `mode`, `prompt` |

## 7. Tài nguyên hình ảnh và font

Các tên dưới đây là tệp nguồn; mỗi tệp được Godot theo dõi kèm metadata `.import`.

### Fonts (2)

- `Assets/Fonts/Font2.ttf`
- `Assets/Fonts/fonts.ttf`

### Backgrounds (3)

- `Assets/Sprites/Backgrounds/intro_scene_01.png`
- `Assets/Sprites/Backgrounds/intro_scene_02.png`
- `Assets/Sprites/Backgrounds/intro_scene_03.png`

### Characters (7)

- `Assets/Sprites/Characters/aleksei_volkov.png`
- `Assets/Sprites/Characters/aleksei_volkov_avatar.png`
- `Assets/Sprites/Characters/arthur_pendelton.png`
- `Assets/Sprites/Characters/arthur_pendelton_avatar.png`
- `Assets/Sprites/Characters/elena_vasquez_avatar.png`
- `Assets/Sprites/Characters/lila_moonwhisper_avatar.png`
- `Assets/Sprites/Characters/seraphina_swiftpaw_avatar.png`

### Documents (9)

- `Assets/Sprites/Documents/AsylumDoc.png`
- `Assets/Sprites/Documents/Pass_cover.png`
- `Assets/Sprites/Documents/Passport_sample.png`
- `Assets/Sprites/Documents/StampApprove.png`
- `Assets/Sprites/Documents/StampDenied.png`
- `Assets/Sprites/Documents/StampRecord.png`
- `Assets/Sprites/Documents/Train_ticket.png`
- `Assets/Sprites/Documents/passcover_on_table.png`
- `Assets/Sprites/Documents/passport_new.png`

### UI (7)

- `Assets/Sprites/UI/GuardRail.png`
- `Assets/Sprites/UI/StamperApprove.png`
- `Assets/Sprites/UI/StamperDenied.png`
- `Assets/Sprites/UI/StamperRecord.png`
- `Assets/Sprites/UI/Tray.png`
- `Assets/Sprites/UI/Tray_stuff.png`
- `Assets/Sprites/UI/table1.png`

`Data/document_asset_generation.json` là manifest 13 yêu cầu/đường dẫn asset, không phải asset runtime. Một số path trong manifest trỏ tới thư mục `Aleksei Volkov 3 (Valid)` trong khi thư mục dữ liệu thực tế là `Aleksei Volkov 3`; manifest vì vậy không hoàn toàn đồng bộ với cấu trúc dữ liệu hiện tại.

## 8. Cấu hình và công cụ

- `project.godot`: tên project, main scene, viewport, stretch mode, icon, renderer `mobile`, driver Windows D3D12 và tính năng Godot `4.7`, `C#`, `Mobile`.
- `Vexatule.csproj`: Godot.NET.Sdk 4.7.2; .NET 8 mặc định, .NET 9 cho Android; bật dynamic loading.
- `Vexatule.sln`: solution C# với cấu hình Debug/ExportDebug/ExportRelease.
- `.editorconfig`: mã hóa UTF-8.
- `.gitattributes`: chuẩn hóa line ending LF cho file text.
- `.gitignore`: loại `.godot/` và `/android/`.
- `icon.svg`: icon có biểu tượng Godot.
- `README.md`: hướng dẫn import/build; một phần mô tả cấu trúc là bản thiết kế cũ, không phản ánh thư mục hiện có.

### Build và chạy

1. Cài Godot bản .NET tương thích với SDK trong project và .NET SDK.
2. Mở `project.godot` bằng Godot .NET.
3. Build C# từ Godot hoặc chạy `dotnet build Vexatule.sln`.
4. Chạy project bằng F5; main scene đã cấu hình là MainMenu.

## 9. Các điểm cần lưu ý khi phát triển

1. **Có hai validator:** `GameManager.RunCrossCheckValidation()` là validator đang chạy. `VerificationEngine.CrossCheckNPC()` chứa kiểm tra bổ sung (bao gồm ngày vé tàu và tài liệu ngoại giao) nhưng chưa được gọi từ luồng hiện tại. Không nên giả định các điều kiện của class này đã được áp dụng trong gameplay.
2. **Khác biệt điều kiện kiểm tra:** GameManager hiện kiểm tra hộ chiếu bắt buộc, ngày hết hạn hộ chiếu, tên vé, tên/mã đơn tị nạn và tên/cơ quan cấp giấy ngoại giao. GameManager hiện không so ngày vé với ngày game; class VerificationEngine có so ngày nếu được gọi.
3. **Ngày và bộ hồ sơ:** ngày game mặc định của GameManager là `2130-01-01`; ngày vé trong Arthur/Aleksei là `2134-10-19`, Elena/Lila là `2026-10-21`. Do validator đang chạy không so ngày vé, giá trị này không tự tạo lỗi vé trong luồng hiện tại.
4. **Seraphina:** hồ sơ được gắn nhãn invalid và ghi thiếu giấy tị nạn; đây hiện là ghi chú trong JSON, không phải field mà GameManager tự kiểm tra. Logic hiện có chỉ ghi lỗi khi phát hiện các điều kiện được lập trình.
5. **Tài liệu tị nạn không hiển thị:** có JSON/ảnh asylum cho Aleksei và Elena, nhưng `Gameplay.tscn` hiện đặt một `AsylumDoc` sprite mẫu trên bàn; GameManager chỉ tải passport và ticket lên các component tương ứng.
6. **Vòng ngày chưa hoàn thiện:** `DayManager` trống; chưa có tiến trình ngày/NPC theo lượt, điểm citation, HUD hoàn chỉnh hoặc lưu game.
7. **UI Settings:** nút có tồn tại nhưng chưa mở panel cài đặt.
8. **Metadata `is_valid`:** giá trị đang lưu dạng chuỗi (`"true"`/`"false"`) trong JSON; đừng nhầm với kết quả kiểm tra runtime.
9. **Dữ liệu mẫu có thời gian/bối cảnh khác nhau:** các hồ sơ chứa ngày năm 2026, 2130, 2134, 2137/2139. Cần thống nhất luật/ngày chơi trước khi mở rộng kiểm tra ngày.

## 10. Tệp được tài liệu hóa

Tài liệu này bao quát mã nguồn, scenes, cấu hình, JSON và tên các tài nguyên được Git theo dõi tại thời điểm khảo sát. Nội dung pixel bên trong PNG, dữ liệu font nhị phân và cache do Godot sinh ra không được sao chép vào tài liệu; xem trực tiếp các tệp gốc trong `Assets/`, `Data/` và `.godot/` khi cần kiểm tra chi tiết.
