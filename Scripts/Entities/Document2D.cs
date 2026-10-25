namespace Game.Entities;

using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public class PassportJsonData
{
    [JsonPropertyName("document_type")] public string DocumentType { get; set; }
    [JsonPropertyName("document_id")] public string DocumentId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("gender")] public string Gender { get; set; }
    [JsonPropertyName("birth_date")] public string BirthDate { get; set; }
    [JsonPropertyName("species")] public string Species { get; set; }
    [JsonPropertyName("issuing_country")] public string IssuingCountry { get; set; }
    [JsonPropertyName("expiry_date")] public string ExpiryDate { get; set; }
    [JsonPropertyName("avatar_file")] public string AvatarFile { get; set; }
    [JsonPropertyName("is_valid")] public object IsValidRaw { get; set; }
    [JsonPropertyName("error_note")] public string ErrorNote { get; set; }
}

public partial class Document2D : Area2D
{
    // Biến tĩnh dùng chung trên toàn game: Chỉ cho phép kéo duy nhất 1 giấy tờ tại một thời điểm
    public static Area2D CurrentDraggedDocument { get; set; } = null;
    
    [ExportGroup("Cấu hình Hình ảnh (Gập / Mở)")]
    [Export] private Sprite2D _openSprite;   // Node PassportSprite (Trang mở chi tiết)
    [Export] private Sprite2D _coverSprite;  // Node CoverSprite (Bìa gập nhỏ trên quầy)
    [Export] private CollisionShape2D _collisionShape;
    
    [ExportGroup("Các Node hiển thị Thông tin Hộ chiếu")]
    [Export] private TextureRect _avatarRect;
    [Export] private Label _idLabel;
    [Export] private Label _nameLabel;
    [Export] private Label _genderLabel;
    [Export] private Label _dobLabel;
    [Export] private Label _speciesLabel;
    [Export] private Label _expiryLabel;
    [Export] private Label _countryLabel;
    
    [ExportGroup("Ranh giới & Vùng bàn gỗ (Area2D)")]
    [Export] private float _deskBoundaryX = 640.0f;
    [Export] private Area2D _boothTableArea;
    [Export] private float _dropOffsetBelowMouse = 20.0f;
    
    public PassportJsonData CurrentData { get; private set; }
    
    private bool _isDragging = false;
    private bool _isOpen = false;
    private Vector2 _dragOffset = Vector2.Zero;
    private Tween _dropTween;

    public override void _Ready()
    {
        if (_openSprite == null) _openSprite = GetNodeOrNull<Sprite2D>("PassportSprite");
        if (_coverSprite == null) _coverSprite = GetNodeOrNull<Sprite2D>("CoverSprite");
        if (_collisionShape == null) _collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        
        if (_avatarRect == null) _avatarRect = GetNodeOrNull<TextureRect>("AvatarRect") ?? GetNodeOrNull<TextureRect>("FieldsContainer/AvatarRect");
        if (_idLabel == null) _idLabel = GetNodeOrNull<Label>("FieldsContainer/IdLabel") ?? GetNodeOrNull<Label>("IdLabel");
        if (_nameLabel == null) _nameLabel = GetNodeOrNull<Label>("FieldsContainer/NameLabel") ?? GetNodeOrNull<Label>("NameLabel");
        if (_genderLabel == null) _genderLabel = GetNodeOrNull<Label>("FieldsContainer/GenderLabel") ?? GetNodeOrNull<Label>("GenderLabel");
        if (_dobLabel == null) _dobLabel = GetNodeOrNull<Label>("FieldsContainer/DobLabel") ?? GetNodeOrNull<Label>("DobLabel");
        if (_speciesLabel == null) _speciesLabel = GetNodeOrNull<Label>("FieldsContainer/SpeciesLabel") ?? GetNodeOrNull<Label>("SpeciesLabel");
        if (_expiryLabel == null) _expiryLabel = GetNodeOrNull<Label>("FieldsContainer/ExpiryLabel") ?? GetNodeOrNull<Label>("ExpiryLabel");
        if (_countryLabel == null) _countryLabel = GetNodeOrNull<Label>("FieldsContainer/CountryLabel") ?? GetNodeOrNull<Label>("CountryLabel");
        
        if (_boothTableArea == null)
        {
            _boothTableArea = GetNodeOrNull<Area2D>("../../BoothTableArea");
        }
        
        InputEvent += OnDocumentInput;
        Visible = false;
    }

    public void LoadPassportFromJson(string jsonFilePath)
    {
        if (!Godot.FileAccess.FileExists(jsonFilePath))
        {
            GD.PrintErr($"[Document2D]: Không tìm thấy file JSON tại '{jsonFilePath}'");
            return;
        }
        
        try
        {
            using var file = Godot.FileAccess.Open(jsonFilePath, Godot.FileAccess.ModeFlags.Read);
            string jsonText = file.GetAsText();
            CurrentData = JsonSerializer.Deserialize<PassportJsonData>(jsonText);
            
            if (CurrentData != null)
            {
                string folderDir = jsonFilePath.GetBaseDir();
                PopulatePassportUI(CurrentData, folderDir);
                GD.Print($"[Document2D]: Đã điền thông tin hộ chiếu của '{CurrentData.Name}' lên phôi!");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Document2D]: Lỗi khi đọc passport.json: {ex.Message}");
        }
    }

    private void PopulatePassportUI(PassportJsonData data, string folderDir)
    {
        if (_idLabel != null) _idLabel.Text = data.DocumentId ?? "";
        if (_nameLabel != null) _nameLabel.Text = (data.Name ?? "").ToUpper();
        if (_genderLabel != null) _genderLabel.Text = (data.Gender ?? "").ToUpper();
        if (_dobLabel != null) _dobLabel.Text = data.BirthDate ?? "";
        if (_speciesLabel != null) _speciesLabel.Text = (data.Species ?? "").ToUpper();
        if (_expiryLabel != null) _expiryLabel.Text = data.ExpiryDate ?? "";
        if (_countryLabel != null) _countryLabel.Text = (data.IssuingCountry ?? "").ToUpper();
        
        string localPassportImg = folderDir.PathJoin("passport.png");
        
        if (_avatarRect != null)
        {
            if (!string.IsNullOrEmpty(data.AvatarFile) && ResourceLoader.Exists(data.AvatarFile))
            {
                _avatarRect.Texture = GD.Load<Texture2D>(data.AvatarFile);
            }
            else if (ResourceLoader.Exists(localPassportImg))
            {
                _avatarRect.Texture = GD.Load<Texture2D>(localPassportImg);
            }
        }
        else if (_openSprite != null && ResourceLoader.Exists(localPassportImg))
        {
            if (_idLabel == null && _nameLabel == null)
            {
                _openSprite.Texture = GD.Load<Texture2D>(localPassportImg);
                GD.Print($"[Document2D]: Đã nạp trực tiếp ảnh '{localPassportImg}' làm mặt trong hộ chiếu!");
            }
        }
    }

    public void SpawnAsCoverOnBooth(Vector2 startPos, Vector2 dropTargetPos)
    {
        Visible = true;
        GlobalPosition = startPos;
        SetOpenState(false, animate: false);
        
        _dropTween?.Kill();
        _dropTween = CreateTween();
        _dropTween.TweenProperty(this, "global_position", dropTargetPos, 0.35f)
                  .SetTrans(Tween.TransitionType.Quad)
                  .SetEase(Tween.EaseType.Out);
    }

    private void OnDocumentInput(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
            {
                // Kiểm tra xung đột: Nếu đang kéo một giấy tờ khác thì chặn lại
                if (CurrentDraggedDocument != null && CurrentDraggedDocument != this)
                    return;
                    
                _dropTween?.Kill();
                _isDragging = true;
                CurrentDraggedDocument = this;
                _dragOffset = GlobalPosition - GetGlobalMousePosition();
                
                // Đưa hộ chiếu lên trên cùng để không bị che khuất
                GetParent()?.MoveChild(this, GetParent().GetChildCount() - 1);
                
                // Chặn sự kiện click chuột không truyền xuyên thấu xuống các đối tượng nằm dưới
                GetViewport().SetInputAsHandled();
            }
            else if (_isDragging)
            {
                _isDragging = false;
                
                if (CurrentDraggedDocument == this)
                {
                    CurrentDraggedDocument = null;
                }
                
                if (!_isOpen)
                {
                    DropToBoothTableAreaUnderMouse();
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_isDragging)
        {
            GlobalPosition = GetGlobalMousePosition() + _dragOffset;
            bool shouldBeOpen = GlobalPosition.X >= _deskBoundaryX;
            if (shouldBeOpen != _isOpen)
            {
                SetOpenState(shouldBeOpen, animate: true);
            }
        }
    }

    private void DropToBoothTableAreaUnderMouse()
    {
        Vector2 mousePos = GetGlobalMousePosition();
        Vector2 finalDropPos = mousePos + new Vector2(0, _dropOffsetBelowMouse);
        
        if (_boothTableArea != null)
        {
            Rect2 tableBounds = GetAreaGlobalRect(_boothTableArea);
            if (tableBounds.Size != Vector2.Zero)
            {
                float clampedX = Mathf.Clamp(mousePos.X, tableBounds.Position.X, tableBounds.End.X);
                float clampedY = Mathf.Clamp(mousePos.Y + _dropOffsetBelowMouse, tableBounds.Position.Y, tableBounds.End.Y);
                finalDropPos = new Vector2(clampedX, clampedY);
            }
        }
        
        float distance = GlobalPosition.DistanceTo(finalDropPos);
        float duration = Mathf.Clamp(distance / 900.0f, 0.12f, 0.4f);
        
        _dropTween?.Kill();
        _dropTween = CreateTween();
        _dropTween.TweenProperty(this, "global_position", finalDropPos, duration)
                  .SetTrans(Tween.TransitionType.Bounce)
                  .SetEase(Tween.EaseType.Out);
                  
        _dropTween.Finished += () =>
        {
            if (Game.Core.GameManager.Instance != null &&
                Game.Core.GameManager.Instance.PlayerStampDecision != null)
            {
                var booth = GetTree().CurrentScene.GetNodeOrNull<NPCBooth>("TEST3");
                booth?.WalkAwayAndFinishTurn();
            }
        };
    }

    private Rect2 GetAreaGlobalRect(Area2D area)
    {
        CollisionShape2D colShape = area.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (colShape != null && colShape.Shape is RectangleShape2D rectShape)
        {
            Vector2 size = rectShape.Size * colShape.GlobalScale;
            Vector2 topLeft = colShape.GlobalPosition - (size * 0.5f);
            return new Rect2(topLeft, size);
        }
        return new Rect2();
    }

    private void SetOpenState(bool open, bool animate)
    {
        _isOpen = open;
        if (_openSprite != null) _openSprite.Visible = open;
        if (_coverSprite != null) _coverSprite.Visible = !open;
        
        foreach (Node child in GetChildren())
        {
            if (child is CanvasItem item && item != _openSprite && item != _coverSprite && item != _collisionShape)
            {
                item.Visible = open;
            }
        }
        
        if (open)
        {
            AddToGroup("passport");
            if (animate && _openSprite != null)
            {
                _openSprite.Scale = new Vector2(0.8f, 0.8f);
                Tween popTween = CreateTween();
                popTween.TweenProperty(_openSprite, "scale", Vector2.One, 0.15f)
                        .SetTrans(Tween.TransitionType.Back)
                        .SetEase(Tween.EaseType.Out);
            }
        }
        else
        {
            if (IsInGroup("passport")) RemoveFromGroup("passport");
        }
    }

    /// <summary>
    /// Xóa toàn bộ các vết mực đã đóng trên hộ chiếu khi kết thúc lượt của NPC
    /// </summary>
    public void ClearInkMarks()
    {
        foreach (Node child in GetChildren())
        {
            if (child is Sprite2D sprite && sprite != _openSprite && sprite != _coverSprite)
            {
                sprite.QueueFree();
            }
        }
        GD.Print("[Document2D]: Đã xóa sạch các vết mực cũ trên hộ chiếu!");
    }
}
