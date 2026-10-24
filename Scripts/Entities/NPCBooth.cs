namespace Game.Entities;

using Godot;
using System;

public partial class NPCBooth : Sprite2D
{
    [ExportGroup("Cấu hình tọa độ di chuyển (Từ Editor)")]
    [Export] private Vector2 _startPosition = new Vector2(-300.0f, 750.0f);
    [Export] private Vector2 _targetPosition = new Vector2(-300.0f, 469.0f);
    [Export] private float _walkDuration = 2.0f;

    [ExportGroup("Cấu hình hiệu ứng nhấp nhô (Bobbing)")]
    [Export] private float _stepDuration = 0.2f;
    [Export] private float _bobHeight = 6.0f;
    [Export] private float _squishMultiplier = 0.95f;

    [ExportGroup("Tài liệu bàn giao")]
    [Export] private Document2D _passportDocument;
    [Export] private Vector2 _boothTableDropPosition = new Vector2(520.0f, 620.0f);

    private bool _isWalking = false;
    private Vector2 _originalScale;

    public override void _Ready()
    {
        _originalScale = Scale;
        Position = _startPosition;

        if (_passportDocument == null)
        {
            _passportDocument = GetNodeOrNull<Document2D>("../DocumentDeskArea/Document2D");
        }

        StartWalkingWithBobbing();
    }

    public void StartWalkingWithBobbing()
    {
        if (_isWalking) return;

        _isWalking = true;
        Position = _startPosition;
        Scale = _originalScale;

        GD.Print($"[NPCBooth]: Nhân vật '{Name}' bắt đầu bước vào quầy...");

        Tween moveTween = CreateTween();
        moveTween.SetParallel(true);

        moveTween.TweenProperty(this, "position:x", _targetPosition.X, _walkDuration);
        moveTween.TweenProperty(this, "position:y", _targetPosition.Y, _walkDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        Tween bobbingTween = CreateTween();
        int totalSteps = Math.Max(1, (int)(_walkDuration / (_stepDuration * 2)));
        bobbingTween.SetLoops(totalSteps);

        bobbingTween.TweenProperty(this, "position:y", -_bobHeight, _stepDuration)
            .AsRelative()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);

        bobbingTween.Parallel().TweenProperty(this, "scale:y", _originalScale.Y * _squishMultiplier, _stepDuration);

        bobbingTween.TweenProperty(this, "position:y", _bobHeight, _stepDuration)
            .AsRelative()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);

        bobbingTween.Parallel().TweenProperty(this, "scale:y", _originalScale.Y, _stepDuration);

        moveTween.Finished += () =>
        {
            bobbingTween.Kill();

            Position = _targetPosition;
            Scale = _originalScale;
            _isWalking = false;

            GD.Print($"[NPCBooth]: Nhân vật '{Name}' đã đến quầy. Tiến hành thả bìa hộ chiếu!");
            OnNPCArrivedAtBooth();
        };
    }

    private void OnNPCArrivedAtBooth()
    {
        if (_passportDocument == null)
        {
            GD.PrintErr("[NPCBooth]: Chưa gán hoặc không tìm thấy Node Document2D!");
            return;
        }

        Vector2 throwStartPos = new Vector2(_boothTableDropPosition.X - 80.0f, _boothTableDropPosition.Y - 60.0f);
        _passportDocument.SpawnAsCoverOnBooth(throwStartPos, _boothTableDropPosition);

        GD.Print("[NPCBooth]: Đã thả bìa hộ chiếu xuống bàn gỗ bên trái!");
    }

    /// <summary>
    /// BƯỚC 8: Được gọi khi người chơi đã đóng dấu xong và thả trả lại hộ chiếu về bàn gỗ bên trái
    /// </summary>
    public void WalkAwayAndFinishTurn()
    {
        if (_isWalking) return;
        _isWalking = true;

        // Ẩn cuốn hộ chiếu và xóa vết mực cũ (NPC đã cầm lại)
        if (_passportDocument != null)
        {
            _passportDocument.Visible = false;
            _passportDocument.ClearInkMarks();
        }

        GD.Print($"[NPCBooth]: Nhân vật '{Name}' đã nhận lại hộ chiếu và bắt đầu rời khỏi quầy...");

        // Tạo hiệu ứng đi xuống khuất khỏi quầy kèm nhấp nhô bobbing
        Tween exitTween = CreateTween();
        exitTween.SetParallel(true);
        exitTween.TweenProperty(this, "position:x", _startPosition.X, _walkDuration);
        exitTween.TweenProperty(this, "position:y", _startPosition.Y, _walkDuration)
                 .SetTrans(Tween.TransitionType.Quad)
                 .SetEase(Tween.EaseType.In);

        Tween bobbingTween = CreateTween();
        int totalSteps = Math.Max(1, (int)(_walkDuration / (_stepDuration * 2)));
        bobbingTween.SetLoops(totalSteps);

        bobbingTween.TweenProperty(this, "position:y", -_bobHeight, _stepDuration)
            .AsRelative()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        bobbingTween.Parallel().TweenProperty(this, "scale:y", _originalScale.Y * _squishMultiplier, _stepDuration);

        bobbingTween.TweenProperty(this, "position:y", _bobHeight, _stepDuration)
            .AsRelative()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        bobbingTween.Parallel().TweenProperty(this, "scale:y", _originalScale.Y, _stepDuration);

        exitTween.Finished += () =>
        {
            bobbingTween.Kill();
            Position = _startPosition;
            Scale = _originalScale;
            _isWalking = false;

            GD.Print("[NPCBooth]: NPC đã rời khỏi quầy hoàn tất! Kết thúc lượt kiểm tra.");
        };
    }
}