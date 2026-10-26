namespace Game.Entities;

using Godot;
using System;
using System.Text.Json;

public class AsylumPaperJsonData
{
    public string document_type { get; set; }
    public string document_id { get; set; }
    public string applicant_name { get; set; }
    public string reason_for_asylum { get; set; }
    public string approval_code { get; set; }
    public string issue_date { get; set; }
    public string expiry_date { get; set; }
    public string fingerprint_id { get; set; }
    public string is_valid { get; set; }
    public string error_note { get; set; }
}

public partial class AsylumPaper2D : Area2D
{
    [ExportGroup("Cấu hình Thu phóng (Zoom / Normal)")]
    [Export] private Vector2 _smallScale = new Vector2(0.45f, 0.45f);
    [Export] private Vector2 _normalScale = new Vector2(1.0f, 1.0f);

    [ExportGroup("Ranh giới & Vùng bàn quầy (Area2D)")]
    [Export] private float _deskBoundaryX = 640.0f;
    [Export] private Area2D _boothTableArea;
    [Export] private float _dropOffsetBelowMouse = 20.0f;

    [ExportGroup("Liên kết UI Labels")]
    [Export] private Label _lblDocumentId;
    [Export] private Label _lblApplicantName;
    [Export] private Label _lblReason;
    [Export] private Label _lblApprovalCode;
    [Export] private Label _lblIssueDate;
    [Export] private Label _lblExpiryDate;
    [Export] private Label _lblFingerprintId;

    private bool _isDragging = false;
    private bool _isExpanded = false;
    private Vector2 _dragOffset = Vector2.Zero;
    private Tween _dropTween;
    private Tween _scaleTween;

    // Dùng chung biến tĩnh của Document2D để đảm bảo chỉ kéo 1 tờ giấy tại 1 thời điểm
    public static Area2D CurrentDraggedDocument 
    {
        get => Document2D.CurrentDraggedDocument;
        set => Document2D.CurrentDraggedDocument = value;
    }

    public override void _Ready()
    {
        if (_boothTableArea == null)
        {
            _boothTableArea = GetNodeOrNull<Area2D>("../../BoothTableArea");
        }

        InputEvent += OnInputEvent;
        Visible = false;
    }

    public void LoadAsylumFromJson(string jsonPath)
    {
        try
        {
            using var file = Godot.FileAccess.Open(jsonPath, Godot.FileAccess.ModeFlags.Read);
            string jsonText = file.GetAsText();
            var data = JsonSerializer.Deserialize<AsylumPaperJsonData>(jsonText);

            if (_lblDocumentId != null) _lblDocumentId.Text = data.document_id;
            if (_lblApplicantName != null) _lblApplicantName.Text = data.applicant_name;
            if (_lblReason != null) _lblReason.Text = data.reason_for_asylum;
            if (_lblApprovalCode != null) _lblApprovalCode.Text = data.approval_code;
            if (_lblIssueDate != null) _lblIssueDate.Text = data.issue_date;
            if (_lblExpiryDate != null) _lblExpiryDate.Text = data.expiry_date;
            if (_lblFingerprintId != null) _lblFingerprintId.Text = data.fingerprint_id;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[AsylumPaper2D] Lỗi: {ex.Message}");
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

    private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            if (CurrentDraggedDocument != null && CurrentDraggedDocument != this)
                return;

            _dropTween?.Kill();
            _isDragging = true;
            CurrentDraggedDocument = this;
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
        if (CurrentDraggedDocument == this)
        {
            CurrentDraggedDocument = null;
        }

        // Nếu đang ở khu vực quầy NPC (bên trái) thì mới rớt xuống bàn
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