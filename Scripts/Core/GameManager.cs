namespace Game.Core;
using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Game.Entities;

public partial class GameManager : Node2D
{
    public static GameManager Instance { get; private set; }

    [ExportGroup("Cấu hình Dữ liệu Màn 1")]
    [Export(PropertyHint.Dir)]
    private string _characterFolderPath = "res://Data/Characters/Arthur Pendelton 1";

    [ExportGroup("Ngày hiện tại trong Game (Để kiểm tra hạn giấy tờ)")]
    [Export] private string _currentGameDate = "2130-01-01";

    [ExportGroup("Tham chiếu Thực thể trong Scene")]
    [Export] private Sprite2D _npcVisual;             // Node TEST3 (hiển thị NPC đứng quầy)
    [Export] private Document2D _passportDocument;     // Node Document2D (cuốn hộ chiếu trên bàn)
    [Export] private NPCBooth _npcBoothController;     // Node điều khiển hoạt ảnh bước vào của NPC

    // Lưu trữ dữ liệu JSON của NPC hiện tại
    public PassportJsonData CurrentPassport { get; private set; }
    public Dictionary<string, JsonElement> LoadedDocuments { get; private set; } = new();

    // Kết quả sau khi chạy đối chiếu chéo
    public bool IsCurrentNPCValid { get; private set; } = true;
    public List<string> DiscrepancyErrors { get; private set; } = new();

    // Lưu trạng thái con dấu người chơi đã đóng lên hộ chiếu (null = chưa đóng dấu)
    public bool? PlayerStampDecision { get; private set; } = null;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        // 1. Tự động tìm kiếm các Node nếu chưa kéo thả trên Inspector
        if (_npcVisual == null) 
            _npcVisual = GetNodeOrNull<Sprite2D>("TEST3");
        
        if (_npcBoothController == null) 
            _npcBoothController = GetNodeOrNull<NPCBooth>("TEST3");
        
        if (_passportDocument == null) 
            _passportDocument = GetNodeOrNull<Document2D>("DocumentDeskArea/Document2D");

        // 2. Import dữ liệu từ folder Arthur Pendelton 1
        ImportCharacterFolder(_characterFolderPath);

        // 3. Chạy thuật toán đối chiếu chéo thông tin giữa các giấy tờ
        RunCrossCheckValidation();
    }

    /// <summary>
    /// Quét thư mục nhân vật, nạp tất cả file .json liên quan
    /// </summary>
    public void ImportCharacterFolder(string folderPath)
    {
        LoadedDocuments.Clear();
        CurrentPassport = null;
        PlayerStampDecision = null;

        string globalPath = ProjectSettings.GlobalizePath(folderPath);

        if (!DirAccess.DirExistsAbsolute(folderPath) && !Directory.Exists(globalPath))
        {
            GD.PrintErr($"[GameManager]: Không tìm thấy thư mục nhân vật tại '{folderPath}'!");
            return;
        }

        GD.Print($"[GameManager]: Bắt đầu import hồ sơ từ '{folderPath}'...");

        using var dir = DirAccess.Open(folderPath);
        if (dir == null) return;

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        while (!string.IsNullOrEmpty(fileName))
        {
            if (!dir.CurrentIsDir() && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                string fullPath = folderPath.PathJoin(fileName);
                LoadJsonDocument(fileName.ToLower(), fullPath);
            }
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();
    }

    private void LoadJsonDocument(string fileName, string fullPath)
    {
        try
        {
            // Sử dụng rõ tiền tố Godot.FileAccess để tránh xung đột với System.IO.FileAccess
            using var file = Godot.FileAccess.Open(fullPath, Godot.FileAccess.ModeFlags.Read);
            string jsonText = file.GetAsText();
            using JsonDocument doc = JsonDocument.Parse(jsonText);
            
            LoadedDocuments[fileName] = doc.RootElement.Clone();

            // Nếu là passport.json thì chỉ nạp dữ liệu và ảnh thẻ vào bên trong tờ Hộ chiếu (Document2D)
            // KHÔNG gán đè lên _npcVisual (TEST3) để giữ nguyên hình dáng nhân vật đứng ở quầy
            if (fileName == "passport.json")
            {
                CurrentPassport = JsonSerializer.Deserialize<PassportJsonData>(jsonText);
                
                if (_passportDocument != null)
                {
                    _passportDocument.LoadPassportFromJson(fullPath);
                }
            }
            GD.Print($"[GameManager]: Đã nạp thành công file '{fileName}'");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GameManager]: Lỗi khi đọc file '{fullPath}': {ex.Message}");
        }
    }

    /// <summary>
    /// Logic đối chiếu chéo (Cross-Check) toàn bộ giấy tờ của NPC
    /// </summary>
    public void RunCrossCheckValidation()
    {
        DiscrepancyErrors.Clear();
        IsCurrentNPCValid = true;

        // Kiểm tra 1: Có hộ chiếu hay không
        if (CurrentPassport == null)
        {
            AddDiscrepancy("Thiếu Hộ chiếu (Passport) bắt buộc.");
            PrintValidationSummary();
            return;
        }

        // Kiểm tra 2: Hạn sử dụng của Hộ chiếu so với ngày hiện tại của màn chơi
        if (DateTime.TryParse(CurrentPassport.ExpiryDate, out DateTime expDate) &&
            DateTime.TryParse(_currentGameDate, out DateTime gameDate))
        {
            if (expDate < gameDate)
            {
                AddDiscrepancy($"Hộ chiếu đã hết hạn ({CurrentPassport.ExpiryDate} < {_currentGameDate}).");
            }
        }

        // Kiểm tra 3: Đối chiếu chéo với Vé tàu hỏa (train_ticket.json) nếu có
        if (LoadedDocuments.TryGetValue("train_ticket.json", out JsonElement ticketRoot))
        {
            if (ticketRoot.TryGetProperty("passenger_name", out JsonElement ticketNameProp))
            {
                string ticketName = ticketNameProp.GetString();
                if (!string.Equals(CurrentPassport.Name, ticketName, StringComparison.OrdinalIgnoreCase))
                {
                    AddDiscrepancy($"Lệch tên trên Vé tàu: '{ticketName}' khác với Hộ chiếu '{CurrentPassport.Name}'.");
                }
            }
        }

        // Kiểm tra 4: Đối chiếu chéo với Đơn xin tị nạn (asylum_paper.json) nếu có
        if (LoadedDocuments.TryGetValue("asylum_paper.json", out JsonElement asylumRoot))
        {
            if (asylumRoot.TryGetProperty("applicant_name", out JsonElement asylumNameProp))
            {
                string asylumName = asylumNameProp.GetString();
                if (!string.Equals(CurrentPassport.Name, asylumName, StringComparison.OrdinalIgnoreCase))
                {
                    AddDiscrepancy($"Lệch tên trên Đơn tị nạn: '{asylumName}' khác với Hộ chiếu '{CurrentPassport.Name}'.");
                }
            }

            if (asylumRoot.TryGetProperty("approval_code", out JsonElement codeProp))
            {
                string code = codeProp.GetString();
                string[] validCodes = { "ALPHA-992-SEC", "SACRED-771-VEX", "REBEL-041-BOOT", "BIO-442-QUAR", "CYBER-009-RAW", "SHIELD-553-SAFE" };
                
                if (Array.IndexOf(validCodes, code) == -1)
                {
                    AddDiscrepancy($"Mã phê duyệt tị nạn '{code}' không có trong danh mục hợp pháp.");
                }
            }
        }

        // Kiểm tra 5: Đối chiếu chéo với Thẻ ngoại giao (diplomat_auth.json) nếu có
        if (LoadedDocuments.TryGetValue("diplomat_auth.json", out JsonElement dipRoot))
        {
            if (dipRoot.TryGetProperty("diplomat_name", out JsonElement dipNameProp))
            {
                string dipName = dipNameProp.GetString();
                if (!string.Equals(CurrentPassport.Name, dipName, StringComparison.OrdinalIgnoreCase))
                {
                    AddDiscrepancy($"Lệch tên Ngoại giao: '{dipName}' khác với Hộ chiếu '{CurrentPassport.Name}'.");
                }
            }

            if (dipRoot.TryGetProperty("issuing_authority", out JsonElement authProp))
            {
                string authority = authProp.GetString() ?? "";
                if (!authority.Contains(CurrentPassport.IssuingCountry, StringComparison.OrdinalIgnoreCase))
                {
                    AddDiscrepancy($"Cơ quan cấp ngoại giao '{authority}' không khớp với quốc gia '{CurrentPassport.IssuingCountry}'.");
                }
            }
        }

        PrintValidationSummary();
    }

    private void AddDiscrepancy(string errorMsg)
    {
        IsCurrentNPCValid = false;
        DiscrepancyErrors.Add(errorMsg);
    }

    private void PrintValidationSummary()
    {
        GD.Print("==================================================");
        GD.Print($"[KẾT QUẢ ĐỐI CHIẾU CHÉO - NPC: {CurrentPassport?.Name ?? "Unknown"}]");
        
        if (IsCurrentNPCValid)
        {
            GD.Print("=> TRẠNG THÁI: HỢP LỆ 100% (Người chơi phải đóng dấu APPROVE)");
        }
        else
        {
            GD.Print($"=> TRẠNG THÁI: KHÔNG HỢP LỆ - Phát hiện {DiscrepancyErrors.Count} lỗi (Người chơi phải đóng dấu DENIED):");
            foreach (var err in DiscrepancyErrors)
            {
                GD.Print($"   - [LỖI]: {err}");
            }
        }
        GD.Print("==================================================");
    }

    /// <summary>
    /// Ghi nhận con dấu khi người chơi dập lên Hộ chiếu
    /// </summary>
    public void OnPassportStamped(bool isApprovedStamp)
    {
        PlayerStampDecision = isApprovedStamp;
        GD.Print($"[GameManager]: Đã đóng dấu {(isApprovedStamp ? "APPROVE" : "DENIED")} lên Hộ chiếu.");
        
        if (isApprovedStamp == IsCurrentNPCValid)
        {
            GD.Print("🎉 [PHÁN QUYẾT CHÍNH XÁC]: Bạn đã xử lý đúng quy định biên giới!");
        }
        else
        {
            string reason = DiscrepancyErrors.Count > 0 ? string.Join(" | ", DiscrepancyErrors) : "Hồ sơ hoàn toàn hợp lệ nhưng lại từ chối.";
            GD.PrintErr($"🚨 [VI PHẠM QUY ĐỊNH - CITATION]: Xử lý sai! Lý do: {reason}");
        }
    }
}
