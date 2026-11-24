namespace Game.Entities;

using Godot;
using System;

public partial class CitationSlip2D : Area2D
{
    [ExportGroup("Cấu hình Thu phóng (Zoom / Normal)")]
    [Export] private Vector2 _smallScale = new Vector2(0.45f, 0.45f);
    [Export] private Vector2 _normalScale = new Vector2(1.0f, 1.0f);

    [ExportGroup("Ranh giới & Vùng bàn quầy (Area2D)")]
    [Export] private float _deskBoundaryX = 460.0f;
    [Export] private Area2D _boothTableArea;
    [Export] private float _dropOffsetBelowMouse = 20.0f;

    public bool IsTemplate { get; set; } = true;
    public bool IsPrinting { get; private set; } = false;
    public bool IsInSlot { get; private set; } = false;

    private Sprite2D _slipSprite;
    private Label _reasonLabel;
    private Label _penaltyLabel;
    private Node2D _deskContainer;
    private Action _onDetachedFromSlot;

    private bool _isDragging = false;
    private bool _isExpanded = true;
    private Vector2 _dragOffset = Vector2.Zero;
    private Tween _dropTween;
    private Tween _scaleTween;

    public override void _Ready()
    {
        _slipSprite = GetNodeOrNull<Sprite2D>("SlipSprite");
        _reasonLabel = GetNodeOrNull<Label>("ReasonLabel");
        _penaltyLabel = GetNodeOrNull<Label>("PenaltyLabel");

        if (_slipSprite != null)
        {
            _slipSprite.TextureFilter = TextureFilterEnum.Nearest;
        }
    }

    public void InitializePrintedSlip(
        string reason,
        string penaltyText,
        Node2D deskContainer,
        Area2D boothTableArea,
        Action onDetachedFromSlot)
    {
        IsTemplate = false;
        IsPrinting = true;
        IsInSlot = true;
        _isExpanded = true;
        _deskContainer = deskContainer;
        _boothTableArea = boothTableArea ?? GetTree().CurrentScene?.GetNodeOrNull<Area2D>("BoothTableArea");
        _onDetachedFromSlot = onDetachedFromSlot;

        if (_reasonLabel == null) _reasonLabel = GetNodeOrNull<Label>("ReasonLabel");
        if (_penaltyLabel == null) _penaltyLabel = GetNodeOrNull<Label>("PenaltyLabel");

        if (_reasonLabel != null)
        {
            _reasonLabel.Text = (reason ?? string.Empty).ToUpper();
        }

        if (_penaltyLabel != null)
        {
            _penaltyLabel.Text = penaltyText ?? string.Empty;
        }

        Scale = _normalScale;
        Visible = true;
        Modulate = Colors.White;
        InputEvent += OnSlipInputEvent;
    }

    public void MarkPrintFinished()
    {
        IsPrinting = false;
    }

    /// <summary>
    /// Nếu máy in cần in tờ mới mà tờ cũ vẫn còn cắm trong khe, tự đẩy tờ cũ ra bàn để người chơi vẫn giữ được.
    /// </summary>
    public void EjectFromSlotToDesk(Vector2 targetGlobalPos)
    {
        if (IsTemplate) return;

        IsPrinting = false;
        DetachFromPrinterSlot();

        _dropTween?.Kill();
        _dropTween = CreateTween();
        _dropTween.TweenProperty(this, "global_position", targetGlobalPos, 0.25f)
                  .SetTrans(Tween.TransitionType.Quad)
                  .SetEase(Tween.EaseType.Out);
    }

    private void DetachFromPrinterSlot()
    {
        if (!IsInSlot) return;

        IsInSlot = false;
        _onDetachedFromSlot?.Invoke();
        _onDetachedFromSlot = null;

        if (_deskContainer != null && GodotObject.IsInstanceValid(_deskContainer) && GetParent() != _deskContainer)
        {
            Reparent(_deskContainer, keepGlobalTransform: true);
        }

        // Trả ZIndex về 0 để xếp chồng tự nhiên cùng các giấy tờ khác trong DocumentDeskArea
        ZIndex = 0;
        GetParent()?.MoveChild(this, GetParent().GetChildCount() - 1);
    }

    private void OnSlipInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (IsTemplate || IsPrinting)
            return;

        if (@event is InputEventMouseButton mouseEvent &&
            mouseEvent.ButtonIndex == MouseButton.Left &&
            mouseEvent.Pressed)
        {
            // Dùng chung khóa kéo với Document2D / TrainTicket2D / AsylumPaper2D / RuleBook2D
            if (Document2D.CurrentDraggedDocument != null && Document2D.CurrentDraggedDocument != this)
                return;

            _dropTween?.Kill();
            DetachFromPrinterSlot();

            _isDragging = true;
            Document2D.CurrentDraggedDocument = this;
            _dragOffset = GlobalPosition - GetGlobalMousePosition();

            GetParent()?.MoveChild(this, GetParent().GetChildCount() - 1);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_isDragging && @event is InputEventMouseButton mouseEvent &&
            mouseEvent.ButtonIndex == MouseButton.Left && !mouseEvent.Pressed)
        {
            EndDrag();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (!_isDragging)
            return;

        GlobalPosition = GetGlobalMousePosition() + _dragOffset;

        bool shouldBeExpanded = GlobalPosition.X >= _deskBoundaryX;
        if (shouldBeExpanded != _isExpanded)
        {
            SetExpandedState(shouldBeExpanded, animate: true);
        }
    }

    private void EndDrag()
    {
        _isDragging = false;

        if (Document2D.CurrentDraggedDocument == this)
        {
            Document2D.CurrentDraggedDocument = null;
        }

        // Nếu đang ở khu vực quầy NPC (bên trái) thì thả rơi xuống vùng bàn gỗ quầy NPC như các giấy tờ khác
        // (nhưng NPC sẽ không nhận hoặc thu hồi tờ Citation này)
        if (!_isExpanded)
        {
            DropToBoothTableAreaUnderMouse();
        }
    }

    private void SetExpandedState(bool expanded, bool animate)
    {
        _isExpanded = expanded;
        Vector2 targetScale = expanded ? _normalScale : _smallScale;

        // Bù trừ offset khi co giãn để điểm nắm chuột không bị trôi lệch tâm
        if (_isDragging && Scale.X > 0.001f && Scale.Y > 0.001f)
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

        if (_reasonLabel != null) _reasonLabel.Visible = expanded;
        if (_penaltyLabel != null) _penaltyLabel.Visible = expanded;
    }

    private void DropToBoothTableAreaUnderMouse()
    {
        if (_boothTableArea == null)
        {
            _boothTableArea = GetTree().CurrentScene?.GetNodeOrNull<Area2D>("BoothTableArea");
        }

        Vector2 mousePos = GetGlobalMousePosition();
        Vector2 finalDropPos = mousePos + new Vector2(0.0f, _dropOffsetBelowMouse);

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Document2D.CurrentDraggedDocument == this)
            {
                Document2D.CurrentDraggedDocument = null;
            }
            InputEvent -= OnSlipInputEvent;
        }
        base.Dispose(disposing);
    }
}
