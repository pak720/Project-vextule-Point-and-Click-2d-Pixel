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
    
    [ExportGroup("Cấu hình Dữ liệu Màn 1 (Danh sách NPC)")]
    [Export(PropertyHint.Dir)]
    private string[] _npcFolders = new string[] 
    {
        "res://Data/Characters/Arthur Pendelton 1",
        "res://Data/Characters/Aleksei Volkov 3",
        "res://Data/Characters/Elena Vasquez 2",
        "res://Data/Characters/Lila Moonwhisper 6"
    };
    
    [ExportGroup("Ngày hiện tại trong Game (Để kiểm tra hạn giấy tờ)")]
    [Export] private string _currentGameDate = "2130-01-01";
    
    [ExportGroup("Tham chiếu Thực thể trong Scene")]
    [Export] private Sprite2D _npcVisual;                  // Node TEST3 (hiển thị NPC đứng quầy)
    [Export] private Document2D _passportDocument;         // Node Document2D (Hộ chiếu trên bàn)
    [Export] private TrainTicket2D _trainTicketDocument;   // Node TrainTicket2D (Vé tàu trên bàn)
    [Export] private AsylumPaper2D _asylumPaperDocument;   // Node AsylumPaper2D (Đơn tị nạn trên bàn)
    [Export] private NPCBooth _npcBoothController;         // Node điều khiển NPC đi vào / rời quầy
    
    // Dữ liệu JSON của NPC hiện tại
    public PassportJsonData CurrentPassport { get; private set; }
    public TrainTicketJsonData CurrentTicket { get; private set; }
    public AsylumPaperJsonData CurrentAsylumPaper { get; private set; }
    public Dictionary<string, JsonElement> LoadedDocuments { get; private set; } = new();
    
    // Kết quả sau khi chạy đối chiếu chéo
    public bool IsCurrentNPCValid { get; private set; } = true;
    public List<string> DiscrepancyErrors { get; private set; } = new();
    
    // Lưu trạng thái con dấu người chơi đã đóng lên hộ chiếu (null = chưa đóng dấu, true = Approved, false = Denied)
    public bool? PlayerStampDecision { get; private set; } = null;
    
    // Biến quản lý luồng NPC
    private Queue<string> _characterFoldersQueue = new Queue<string>();
    private Timer _nextNpcTimer;

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
            
        if (_trainTicketDocument == null)
            _trainTicketDocument = GetNodeOrNull<TrainTicket2D>("DocumentDeskArea/TrainTicket2D");
            
        if (_asylumPaperDocument == null)
            _asylumPaperDocument = GetNodeOrNull<AsylumPaper2D>("DocumentDeskArea/AsylumPaper2D");
            
        // 2. Khởi tạo Timer để chờ 3 giây trước khi gọi người tiếp theo
        _nextNpcTimer = new Timer();
        _nextNpcTimer.WaitTime = 3.0f; // Chờ 3 giây
        _nextNpcTimer.OneShot = true;
        _nextNpcTimer.Timeout += OnNextNpcTimerTimeout;
        AddChild(_nextNpcTimer);

        // 3. Nạp danh sách NPC vào hàng đợi
        foreach (var folder in _npcFolders)
        {
            _characterFoldersQueue.Enqueue(folder);
        }

        // 4. Bắt đầu luồng bằng cách gọi NPC đầu tiên
        AdvanceToNextNPC();
    }
    
    /// <summary>
    /// Hàm nạp NPC tiếp theo từ hàng đợi
    /// </summary>
    private void AdvanceToNextNPC()
    {
        if (_characterFoldersQueue.Count > 0)
        {
            string nextFolderPath = _characterFoldersQueue.Dequeue();
            ImportCharacterFolder(nextFolderPath);
            
            // Chạy thuật toán đối chiếu chéo thông tin giữa các giấy tờ
            RunCrossCheckValidation();

            // Gọi NPC bước vào quầy
            if (_npcBoothController != null)
            {
                _npcBoothController.StartWalkingWithBobbing();
            }
        }
        else
        {
            GD.Print("[GameManager]: Đã hết NPC trong danh sách (End of Day).");
            // Tại đây có thể thêm logic kết thúc ngày (Fade-out màn hình, hiện bảng tổng kết điểm...)
        }
    }

    /// <summary>
    /// Sự kiện khi Timer 3 giây kết thúc
    /// </summary>
    private void OnNextNpcTimerTimeout()
    {
        AdvanceToNextNPC();
    }
    
    /// <summary>
    /// Quét thư mục nhân vật, nạp tất cả file .json và hình ảnh liên quan
    /// </summary>
    public void ImportCharacterFolder(string folderPath)
    {
        LoadedDocuments.Clear();
        CurrentPassport = null;
        CurrentTicket = null;
        CurrentAsylumPaper = null;
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
            using var file = Godot.FileAccess.Open(fullPath, Godot.FileAccess.ModeFlags.Read);
            string jsonText = file.GetAsText();
            using JsonDocument doc = JsonDocument.Parse(jsonText);
            LoadedDocuments[fileName] = doc.RootElement.Clone();
            
            if (fileName == "passport.json")
            {
                CurrentPassport = JsonSerializer.Deserialize<PassportJsonData>(jsonText);
                if (_passportDocument != null)
                {
                    _passportDocument.LoadPassportFromJson(fullPath);
                }
                
                // Đồng bộ ảnh NPC đứng quầy dựa trên Avatar của Passport
                if (_npcVisual != null && !string.IsNullOrEmpty(CurrentPassport.AvatarFile))
                {
                    string npcImagePath = CurrentPassport.AvatarFile.Replace("_avatar.png", ".png");
                    _npcVisual.Texture = GD.Load<Texture2D>(npcImagePath);
                }
            }
            else if (fileName == "train_ticket.json")
            {
                CurrentTicket = JsonSerializer.Deserialize<TrainTicketJsonData>(jsonText);
                if (_trainTicketDocument != null)
                {
                    _trainTicketDocument.LoadTicketFromJson(fullPath);
                }
            }
            else if (fileName == "asylum_paper.json")
            {
                CurrentAsylumPaper = JsonSerializer.Deserialize<AsylumPaperJsonData>(jsonText);
                if (_asylumPaperDocument != null)
                {
                    _asylumPaperDocument.LoadAsylumFromJson(fullPath);
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
        
        if (CurrentPassport == null)
        {
            AddDiscrepancy("Thiếu Hộ chiếu (Passport) bắt buộc.");
            PrintValidationSummary();
            return;
        }
        
        if (DateTime.TryParse(CurrentPassport.ExpiryDate, out DateTime expDate) &&
            DateTime.TryParse(_currentGameDate, out DateTime gameDate))
        {
            if (expDate < gameDate)
            {
                AddDiscrepancy($"Hộ chiếu đã hết hạn ({CurrentPassport.ExpiryDate} < {_currentGameDate}).");
            }
        }
        
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
            GD.Print("=> TRẠNG THÁI: HỢP LỆ 100% (Quyết định đúng: APPROVE)");
        }
        else
        {
            GD.Print($"=> TRẠNG THÁI: KHÔNG HỢP LỆ - Phát hiện {DiscrepancyErrors.Count} lỗi (Quyết định đúng: DENIED):");
            foreach (var err in DiscrepancyErrors)
            {
                GD.Print($"   - [LỖI]: {err}");
            }
        }
        GD.Print("==================================================");
    }
    
    /// <summary>
    /// Bước 1: Ghi nhận con dấu khi dập lên Hộ chiếu (Approve = true, Denied = false).
    /// </summary>
    public void OnPassportStamped(bool isApprovedStamp)
    {
        PlayerStampDecision = isApprovedStamp;
        GD.Print($"[GameManager]: Đã đóng dấu {(isApprovedStamp ? "APPROVE" : "DENIED")} lên Hộ chiếu.");
        GD.Print("[GameManager]: Giấy tờ vẫn còn trên bàn. Hãy trả lại giấy tờ cho NPC để kết thúc lượt kiểm tra!");
    }
    
    /// <summary>
    /// Bước 2: Kích hoạt khi trả giấy tờ cho NPC (kéo thả hoặc nhấn nút trả).
    /// </summary>
    public void ReturnDocumentsToNPC()
    {
        // Dự phòng: Nếu chưa ghi nhận PlayerStampDecision, kiểm tra xem Hộ chiếu đã có dấu hay chưa
        bool isApproved = PlayerStampDecision ?? false;
        GD.Print($"[GameManager]: Thực hiện ReturnDocumentsToNPC với PlayerStampDecision = {PlayerStampDecision}");
        
        if (isApproved == IsCurrentNPCValid)
        {
            GD.Print("🎉 [PHÁN QUYẾT CHÍNH XÁC]: Bạn đã xử lý đúng quy định biên giới!");
        }
        else
        {
            string reason = DiscrepancyErrors.Count > 0 ? string.Join(" | ", DiscrepancyErrors) : "Hồ sơ hợp lệ nhưng bị từ chối.";
            GD.PrintErr($"🚨 [VI PHẠM QUY ĐỊNH - CITATION]: Xử lý sai! Lý do: {reason}");
        }
        
        if (_npcBoothController != null)
        {
            _npcBoothController.TakeDocumentsAndExit(isApproved);
        }
        else
        {
            GD.PrintErr("[GameManager]: Không tìm thấy tham chiếu _npcBoothController!");
        }
    }

    /// <summary>
    /// Hàm này được gọi bởi NPCBooth khi NPC hiện tại đã rời khỏi màn hình hoàn toàn
    /// </summary>
    public void OnNPCFinishedExit()
    {
        GD.Print("[GameManager]: Bắt đầu chờ 3 giây trước khi gọi NPC tiếp theo...");
        _nextNpcTimer.Start();
    }
}