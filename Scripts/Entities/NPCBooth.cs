namespace Game.Entities;

using Godot;
using System;
using Game.Core;

public partial class NPCBooth : Sprite2D
{
    [ExportGroup("Cấu hình tọa độ di chuyển")]
    [Export] private Vector2 _startPosition = new Vector2(-300.0f, 750.0f);
    [Export] private Vector2 _targetPosition = new Vector2(-300.0f, 469.0f);
    [Export] private float _walkDuration = 1.8f;

    [ExportGroup("Khoảng cách rời quầy")]
    [Export] private float _exitDistance = 1200.0f;
    [Export] private float _exitDuration = 1.8f;

    [ExportGroup("Cấu hình hiệu ứng nhấp nhô (Bobbing)")]
    [Export] private float _stepDuration = 0.2f;
    [Export] private float _bobHeight = 6.0f;
    [Export] private float _squishMultiplier = 0.95f;

    [ExportGroup("Tài liệu bàn giao")]
    [Export] private Document2D _passportDocument;
    [Export] private TrainTicket2D _trainTicketDocument;
    [Export] private Vector2 _boothTableDropPosition = new Vector2(520.0f, 620.0f);

    private bool _isWalking = false;
    private Vector2 _originalScale;
    private Tween _activeMoveTween;
    private Tween _activeBobbingTween;

    public override void _Ready()
    {
        _originalScale = Scale;
        Position = _startPosition;
        FlipH = false;

        if (_passportDocument == null)
            _passportDocument = GetNodeOrNull<Document2D>("../DocumentDeskArea/Document2D");

        if (_trainTicketDocument == null)
            _trainTicketDocument = GetNodeOrNull<TrainTicket2D>("../DocumentDeskArea/TrainTicket2D");

        StartWalkingWithBobbing();
    }

    public void StartWalkingWithBobbing()
    {
        KillAllTweens();
        _isWalking = true;
        Position = _startPosition;
        Scale = _originalScale;
        FlipH = false;

        GD.Print($"[NPCBooth]: '{Name}' bước vào quầy...");

        _activeMoveTween = CreateTween();
        _activeMoveTween.SetParallel(true);
        _activeMoveTween.TweenProperty(this, "position:x", _targetPosition.X, _walkDuration);
        _activeMoveTween.TweenProperty(this, "position:y", _targetPosition.Y, _walkDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        _activeBobbingTween = CreateTween();
        int totalSteps = Math.Max(1, (int)(_walkDuration / (_stepDuration * 2)));
        _activeBobbingTween.SetLoops(totalSteps);
        _activeBobbingTween.TweenProperty(this, "position:y", -_bobHeight, _stepDuration).AsRelative().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _activeBobbingTween.Parallel().TweenProperty(this, "scale:y", _originalScale.Y * _squishMultiplier, _stepDuration);
        _activeBobbingTween.TweenProperty(this, "position:y", _bobHeight, _stepDuration).AsRelative().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        _activeBobbingTween.Parallel().TweenProperty(this, "scale:y", _originalScale.Y, _stepDuration);

        _activeMoveTween.Finished += () =>
        {
            KillAllTweens();
            Position = _targetPosition;
            Scale = _originalScale;
            _isWalking = false;
            OnNPCArrivedAtBooth();
        };
    }

    private void OnNPCArrivedAtBooth()
    {
        if (_passportDocument != null)
        {
            Vector2 passStartPos = new Vector2(_boothTableDropPosition.X - 80.0f, _boothTableDropPosition.Y - 60.0f);
            _passportDocument.SpawnAsCoverOnBooth(passStartPos, _boothTableDropPosition);
        }

        if (_trainTicketDocument != null)
        {
            Vector2 ticketDropPos = _boothTableDropPosition + new Vector2(50.0f, 15.0f);
            Vector2 ticketStartPos = new Vector2(ticketDropPos.X - 80.0f, ticketDropPos.Y - 60.0f);
            _trainTicketDocument.SpawnAsSmallOnBooth(ticketStartPos, ticketDropPos);
        }
    }

    /// <summary>
    /// Thu hồi tài liệu trên bàn rồi kích hoạt rời quầy theo phán quyết
    /// </summary>
    public void TakeDocumentsAndExit(bool isApproved)
    {
        // Tự động kiểm tra chéo với GameManager nếu bị truyền nhầm false
        if (GameManager.Instance?.PlayerStampDecision.HasValue == true)
        {
            isApproved = GameManager.Instance.PlayerStampDecision.Value;
        }

        KillAllTweens();

        Vector2 handoverTarget = new Vector2(Position.X, Position.Y + 80.0f);
        Tween docTween = CreateTween();
        docTween.SetParallel(true);

        if (_passportDocument != null && _passportDocument.Visible)
        {
            docTween.TweenProperty(_passportDocument, "global_position", handoverTarget, 0.35f);
            docTween.TweenProperty(_passportDocument, "scale", Vector2.Zero, 0.35f);
        }

        if (_trainTicketDocument != null && _trainTicketDocument.Visible)
        {
            docTween.TweenProperty(_trainTicketDocument, "global_position", handoverTarget, 0.35f);
            docTween.TweenProperty(_trainTicketDocument, "scale", Vector2.Zero, 0.35f);
        }

        bool finalApproved = isApproved;
        docTween.Finished += () =>
        {
            if (_passportDocument != null)
            {
                _passportDocument.Visible = false;
                _passportDocument.ClearInkMarks();
            }

            if (_trainTicketDocument != null)
            {
                _trainTicketDocument.Visible = false;
            }

            ExitBooth(finalApproved);
        };
    }

    /// <summary>
    /// NPC đi sang phải nếu Approve (+), đi sang trái nếu Denied (-)
    /// </summary>
    public void ExitBooth(bool isApproved)
    {
        // Đảm bảo lấy phán quyết chuẩn xác từ GameManager
        if (GameManager.Instance?.PlayerStampDecision.HasValue == true)
        {
            isApproved = GameManager.Instance.PlayerStampDecision.Value;
        }

        KillAllTweens();
        _isWalking = true;

        // Approve (+1): Đi qua PHẢI | Denied (-1): Đi qua TRÁI
        float direction = isApproved ? 1.0f : -1.0f;
        FlipH = !isApproved; // Lật mặt nhìn sang trái khi Denied

        float targetExitX = Position.X + (direction * _exitDistance);

        GD.Print($"=================================================");
        GD.Print($"[NPC EXIT]: isApproved = {isApproved}");
        GD.Print($"[NPC EXIT]: Hướng đi = {(isApproved ? "SANG PHẢI (APPROVE)" : "SANG TRÁI (DENIED)")}");
        GD.Print($"[NPC EXIT]: Tọa độ từ {Position.X} -> {targetExitX}");
        GD.Print($"=================================================");

        _activeMoveTween = CreateTween();
        _activeMoveTween.SetParallel(true);
        _activeMoveTween.TweenProperty(this, "position:x", targetExitX, _exitDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);

        _activeBobbingTween = CreateTween();
        int totalSteps = Math.Max(1, (int)(_exitDuration / (_stepDuration * 2)));
        _activeBobbingTween.SetLoops(totalSteps);
        _activeBobbingTween.TweenProperty(this, "position:y", -_bobHeight, _stepDuration).AsRelative().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _activeBobbingTween.Parallel().TweenProperty(this, "scale:y", _originalScale.Y * _squishMultiplier, _stepDuration);
        _activeBobbingTween.TweenProperty(this, "position:y", _bobHeight, _stepDuration).AsRelative().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        _activeBobbingTween.Parallel().TweenProperty(this, "scale:y", _originalScale.Y, _stepDuration);

        _activeMoveTween.Finished += () =>
        {
            KillAllTweens();
            Scale = _originalScale;
            _isWalking = false;
            FlipH = false;
            Position = _startPosition;
            GD.Print($"[NPCBooth]: NPC đã rời khỏi màn hình hoàn tất.");
        };
    }

    /// <summary>
    /// Hàm tương thích: Tự động lấy PlayerStampDecision từ GameManager thay vì mặc định false
    /// </summary>
    public void WalkAwayAndFinishTurn()
    {
        bool decision = GameManager.Instance?.PlayerStampDecision ?? false;
        TakeDocumentsAndExit(decision);
    }

    private void KillAllTweens()
    {
        if (_activeMoveTween != null && _activeMoveTween.IsValid())
        {
            _activeMoveTween.Kill();
            _activeMoveTween = null;
        }

        if (_activeBobbingTween != null && _activeBobbingTween.IsValid())
        {
            _activeBobbingTween.Kill();
            _activeBobbingTween = null;
        }
    }
}
