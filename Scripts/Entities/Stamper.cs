namespace Game.Entities;

using Godot;
using System;
using Game.Core;

public partial class Stamper : Area2D
{
    public enum StampType
    {
        Approve,
        Denied,
        Record
    }

    [ExportGroup("Cấu hình Con dấu")]
    [Export] private StampType _stampType = StampType.Approve;
    [Export] private Texture2D _inkTexture;
    [Export] private Vector2 _inkOffset = Vector2.Zero;

    private bool _isDragging = false;
    private bool _isStamping = false;
    private Vector2 _dragOffset = Vector2.Zero;
    private Vector2 _originalLocalPosition;
    private Vector2 _exactDropGlobalPos;

    public override void _Ready()
    {
        _originalLocalPosition = Position;
        InputEvent += OnStamperInput;
    }

    private void OnStamperInput(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (_isStamping) return;

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
            {
                _isDragging = true;
                _dragOffset = GlobalPosition - GetGlobalMousePosition();
            }
            else if (_isDragging)
            {
                _isDragging = false;
                _exactDropGlobalPos = GlobalPosition;
                PlayStampAnimation();
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_isDragging && !_isStamping)
        {
            GlobalPosition = GetGlobalMousePosition() + _dragOffset;
        }
    }

    private void PlayStampAnimation()
    {
        _isStamping = true;

        Vector2 downGlobalPos = _exactDropGlobalPos + new Vector2(0, 25);

        Tween tween = CreateTween();
        tween.SetParallel(false);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);

        // 1. Dập xuống (0.15s)
        tween.TweenProperty(this, "global_position", downGlobalPos, 0.15f);

        // 2. Chạm đáy: Quét va chạm và in vết mực
        tween.TweenCallback(Callable.From(TryApplyInkMark));

        // 3. Nhấc lên lại vị trí buông chuột (0.15s)
        tween.TweenProperty(this, "global_position", _exactDropGlobalPos, 0.15f);

        // 4. Bay ngược về khay dấu (0.2s)
        tween.TweenProperty(this, "position", _originalLocalPosition, 0.2f)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.InOut);

        tween.Finished += () =>
        {
            _isStamping = false;
        };
    }

    private void TryApplyInkMark()
    {
        ForceUpdateTransform();

        var overlappingAreas = GetOverlappingAreas();
        Node2D validPassportNode = null;

        foreach (var area in overlappingAreas)
        {
            if (area.IsInGroup("passport"))
            {
                validPassportNode = area;
                break;
            }
        }

        if (validPassportNode != null)
        {
            if (_inkTexture == null)
            {
                GD.PrintErr($"[Stamper]: Chưa gán Ink Texture cho Node '{Name}'!");
                return;
            }

            Sprite2D inkMark = new Sprite2D();
            inkMark.Texture = _inkTexture;
            inkMark.TextureFilter = TextureFilterEnum.Nearest;

            Sprite2D stamperSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
            Vector2 spriteOffset = stamperSprite != null ? stamperSprite.Position : Vector2.Zero;

            GetTree().CurrentScene.AddChild(inkMark);
            inkMark.GlobalPosition = _exactDropGlobalPos + spriteOffset + _inkOffset;
            inkMark.Reparent(validPassportNode, true);

            GD.Print($"[Stamper]: Đã in vết mực '{_stampType}' từ con dấu {Name} lên Hộ chiếu!");

            // Báo kết quả đóng dấu về GameManager nếu là dấu Approve hoặc Denied
            if (_stampType != StampType.Record && GameManager.Instance != null)
            {
                bool isApprove = (_stampType == StampType.Approve);
                GameManager.Instance.OnPassportStamped(isApprove);
            }
        }
        else
        {
            GD.Print("[Stamper]: Dập hụt ra ngoài! Không in mực lên bàn.");
        }
    }

    public void ResetToTray()
    {
        _isDragging = false;
        _isStamping = false;
        Position = _originalLocalPosition;
    }
}