namespace Game.Entities;

using Godot;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class TrainTicketJsonData
{
    [JsonPropertyName("document_type")] public string DocumentType { get; set; }
    [JsonPropertyName("document_id")] public string DocumentId { get; set; }
    [JsonPropertyName("passenger_name")] public string PassengerName { get; set; }
    [JsonPropertyName("train_id")] public string TrainId { get; set; }
    [JsonPropertyName("travel_date")] public string TravelDate { get; set; }
    [JsonPropertyName("class")] public string TicketClass { get; set; }
    [JsonPropertyName("seat")] public string SeatNumber { get; set; }
    [JsonPropertyName("is_valid")] public object IsValidRaw { get; set; }
    [JsonPropertyName("error_note")] public string ErrorNote { get; set; }
}

public partial class TrainTicket2D : Area2D
{
    [ExportGroup("Cấu hình Thu phóng (Zoom / Normal)")]
    [Export] private Sprite2D _ticketSprite;
    [Export] private Node2D _ticketFields;
    [Export] private Vector2 _smallScale = new Vector2(0.45f, 0.45f); // Thu nhỏ khi ở bàn quầy
    [Export] private Vector2 _normalScale = new Vector2(1.0f, 1.0f);   // Kích thước thật trên bàn làm việc

    [ExportGroup("Ranh giới & Vùng bàn quầy (Area2D)")]
    [Export] private float _deskBoundaryX = 640.0f;
    [Export] private Area2D _boothTableArea;
    [Export] private float _dropOffsetBelowMouse = 20.0f;

    [ExportGroup("Các Label hiển thị thông tin")]
    [Export] private Label _documentIdLabel;
    [Export] private Label _passengerNameLabel;
    [Export] private Label _travelDateLabel;
    [Export] private Label _trainIdLabel;
    [Export] private Label _classLabel;
    [Export] private Label _seatLabel;

    public TrainTicketJsonData CurrentData { get; private set; }

    private bool _isDragging = false;
    private bool _isExpanded = false;
    private Vector2 _dragOffset = Vector2.Zero;
    private Tween _dropTween;
    private Tween _scaleTween;

    public override void _Ready()
    {
        if (_ticketSprite == null) _ticketSprite = GetNodeOrNull<Sprite2D>("TicketSprite");
        if (_ticketFields == null) _ticketFields = GetNodeOrNull<Node2D>("TicketFields");

        // Tự động tìm kiếm các Node Label theo tên nếu chưa kéo thả trên Inspector
        if (_documentIdLabel == null) _documentIdLabel = GetNodeOrNull<Label>("TicketFields/DocumentIdLabel") ?? GetNodeOrNull<Label>("DocumentIdLabel");
        if (_passengerNameLabel == null) _passengerNameLabel = GetNodeOrNull<Label>("TicketFields/PassengerNameLabel") ?? GetNodeOrNull<Label>("PassengerNameLabel");
        if (_travelDateLabel == null) _travelDateLabel = GetNodeOrNull<Label>("TicketFields/TravelDateLabel") ?? GetNodeOrNull<Label>("TravelDateLabel");
        if (_trainIdLabel == null) _trainIdLabel = GetNodeOrNull<Label>("TicketFields/TrainIdLabel") ?? GetNodeOrNull<Label>("TrainIdLabel");
        if (_classLabel == null) _classLabel = GetNodeOrNull<Label>("TicketFields/ClassLabel") ?? GetNodeOrNull<Label>("ClassLabel");
        if (_seatLabel == null) _seatLabel = GetNodeOrNull<Label>("TicketFields/SeatLabel") ?? GetNodeOrNull<Label>("SeatLabel");

        if (_boothTableArea == null)
        {
            _boothTableArea = GetNodeOrNull<Area2D>("../../BoothTableArea");
        }

        InputEvent += OnTicketInput;
        Visible = false;
    }

    public void LoadTicketFromJson(string jsonFilePath)
    {
        if (!Godot.FileAccess.FileExists(jsonFilePath))
        {
            GD.PrintErr($"[TrainTicket2D]: Không tìm thấy file JSON tại '{jsonFilePath}'");
            return;
        }

        try
        {
            using var file = Godot.FileAccess.Open(jsonFilePath, Godot.FileAccess.ModeFlags.Read);
            string jsonText = file.GetAsText();

            CurrentData = JsonSerializer.Deserialize<TrainTicketJsonData>(jsonText);
            if (CurrentData != null)
            {
                // Điền dữ liệu từ JSON vào các nhãn trên phôi vé
                if (_documentIdLabel != null) _documentIdLabel.Text = CurrentData.DocumentId ?? "";
                if (_passengerNameLabel != null) _passengerNameLabel.Text = (CurrentData.PassengerName ?? "").ToUpper();
                if (_travelDateLabel != null) _travelDateLabel.Text = CurrentData.TravelDate ?? "";
                if (_trainIdLabel != null) _trainIdLabel.Text = CurrentData.TrainId ?? "";
                if (_classLabel != null) _classLabel.Text = (CurrentData.TicketClass ?? "").ToUpper();
                if (_seatLabel != null) _seatLabel.Text = CurrentData.SeatNumber ?? "";

                GD.Print($"[TrainTicket2D]: Đã nạp thành công vé {CurrentData.DocumentId} cho '{CurrentData.PassengerName}' (Toa: {CurrentData.TicketClass}, Ghế: {CurrentData.SeatNumber})");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[TrainTicket2D]: Lỗi giải mã vé tàu: {ex.Message}");
        }
    }

    public void SpawnAsSmallOnBooth(Vector2 startPos, Vector2 dropTargetPos)
    {
        Visible = true;
        GlobalPosition = startPos;
        SetExpandedState(false, animate: false);

        _dropTween?.Kill();
        _dropTween = CreateTween();
        _dropTween.TweenProperty(this, "global_position", dropTargetPos, 0.35f)
                  .SetTrans(Tween.TransitionType.Quad)
                  .SetEase(Tween.EaseType.Out);
    }

    private void OnTicketInput(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            if (Document2D.CurrentDraggedDocument != null && Document2D.CurrentDraggedDocument != this)
                return;

            _dropTween?.Kill();
            _isDragging = true;
            Document2D.CurrentDraggedDocument = this;
            _dragOffset = GlobalPosition - GetGlobalMousePosition();

            GetParent()?.MoveChild(this, GetParent().GetChildCount() - 1);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isDragging && @event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && !mouseEvent.Pressed)
        {
            EndDrag();
            GetViewport().SetInputAsHandled();
        }
    }

    private void EndDrag()
    {
        _isDragging = false;
        if (Document2D.CurrentDraggedDocument == this)
        {
            Document2D.CurrentDraggedDocument = null;
        }

        if (!_isExpanded)
        {
            DropToBoothTableAreaUnderMouse();
        }
    }

    public override void _Process(double delta)
    {
        if (_isDragging)
        {
            GlobalPosition = GetGlobalMousePosition() + _dragOffset;

            bool shouldBeExpanded = GlobalPosition.X >= _deskBoundaryX;
            if (shouldBeExpanded != _isExpanded)
            {
                SetExpandedState(shouldBeExpanded, animate: true);
            }
        }
    }

    private void SetExpandedState(bool expanded, bool animate)
    {
        _isExpanded = expanded;
        Vector2 targetScale = expanded ? _normalScale : _smallScale;

        // Bù trừ offset khi co giãn để điểm nắm chuột không bị trôi lệch tâm
        if (_isDragging && Scale.X > 0 && Scale.Y > 0)
        {
            Vector2 scaleRatio = new Vector2(targetScale.X / Scale.X, targetScale.Y / Scale.Y);
            _dragOffset *= scaleRatio;
            GlobalPosition = GetGlobalMousePosition() + _dragOffset;
        }

        _scaleTween?.Kill();
        if (animate)
        {
            _scaleTween = CreateTween();
            _scaleTween.TweenProperty(this, "scale", targetScale, 0.15f)
                       .SetTrans(Tween.TransitionType.Back)
                       .SetEase(Tween.EaseType.Out);
        }
        else
        {
            Scale = targetScale;
        }

        if (_ticketFields != null)
        {
            _ticketFields.Visible = expanded;
        }
    }

    private void DropToBoothTableAreaUnderMouse()
    {
        Vector2 mousePos = GetGlobalMousePosition();
        Vector2 finalDropPos = mousePos + new Vector2(0, _dropOffsetBelowMouse);

        if (_boothTableArea != null)
        {
            CollisionShape2D colShape = _boothTableArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
            if (colShape != null && colShape.Shape is RectangleShape2D rectShape)
            {
                Vector2 size = rectShape.Size * colShape.GlobalScale;
                Vector2 topLeft = colShape.GlobalPosition - (size * 0.5f);
                Rect2 tableBounds = new Rect2(topLeft, size);

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
    }
}
