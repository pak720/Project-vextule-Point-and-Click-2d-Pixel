namespace Game.Entities;

using Godot;
using System;

public partial class NPCBooth : Sprite2D
{
    [ExportGroup("Cấu hình tọa độ di chuyển (Từ Editor)")]
    // Vị trí xuất phát ẩn dưới quầy
    [Export] private Vector2 _startPosition = new Vector2(-300.0f, 750.0f);
    // Vị trí đứng chuẩn ở quầy làm việc (khớp với Transform của TEST3)
    [Export] private Vector2 _targetPosition = new Vector2(-300.0f, 469.0f);
    // Thời gian di chuyển đi vào / đi ra (giây)
    [Export] private float _walkDuration = 2.0f;

    [ExportGroup("Cấu hình hiệu ứng nhấp nhô (Bobbing)")]
    // Thời gian của 1 nhịp nhún chân
    [Export] private float _stepDuration = 0.2f;
    // Độ nảy lên cao của đầu nhân vật (pixels)
    [Export] private float _bobHeight = 6.0f;
    // Tỷ lệ co nén thân mình khi dậm chân (Scale.Y)
    [Export] private float _squishMultiplier = 0.95f;

    [ExportGroup("Tài liệu bàn giao")]
    [Export] private Document2D _passportDocument;
    [Export] private TrainTicket2D _trainTicketDocument;
    // Tọa độ mặt bàn gỗ bên trái nơi thả giấy tờ xuống
    [Export] private Vector2 _boothTableDropPosition = new Vector2(520.0f, 620.0f);

    private bool _isWalking = false;
    private Vector2 _originalScale;

    public override void _Ready()
    {
        // Lưu lại Scale ban đầu (ví dụ: 0.285, 0.272) để bảo toàn kích thước pixel art
        _originalScale = Scale;
        Position = _startPosition;

        // Tự động tìm kiếm tham chiếu nếu chưa kéo thả trên Inspector
        if (_passportDocument == null)
        {
            _passportDocument = GetNodeOrNull<Document2D>("../DocumentDeskArea/Document2D");
        }

        if (_trainTicketDocument == null)
        {
            _trainTicketDocument = GetNodeOrNull<TrainTicket2D>("../DocumentDeskArea/TrainTicket2D");
        }

        // Bắt đầu chuỗi bước vào quầy khi màn chơi nạp
        StartWalkingWithBobbing();
    }

    /// <summary>
    /// Kích hoạt hoạt ảnh nhân vật đi vào quầy có hiệu ứng nhấp nhô bobbing
    /// </summary>
    public void StartWalkingWithBobbing()
    {
        if (_isWalking) return;

        _isWalking = true;
        Position = _startPosition;
        Scale = _originalScale;

        GD.Print($"[NPCBooth]: Nhân vật '{Name}' bắt đầu bước vào quầy...");

        // 1. TWEEN TỊNH TIẾN: Di chuyển từ vị trí xuất phát lên quầy
        Tween moveTween = CreateTween();
        moveTween.SetParallel(true);

        moveTween.TweenProperty(this, "position:x", _targetPosition.X, _walkDuration);
        moveTween.TweenProperty(this, "position:y", _targetPosition.Y, _walkDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        // 2. TWEEN NHẤP NHÔ (BOBBING): Tạo nhịp bước chân lặp lại song song
        Tween bobbingTween = CreateTween();
        int totalSteps = Math.Max(1, (int)(_walkDuration / (_stepDuration * 2)));
        bobbingTween.SetLoops(totalSteps);

        // Nhịp 1: Nảy người lên
        bobbingTween.TweenProperty(this, "position:y", -_bobHeight, _stepDuration)
            .AsRelative()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);

        // Nhịp 2: Dậm chân nén nhẹ trục Y theo tỉ lệ Scale gốc
        bobbingTween.Parallel().TweenProperty(this, "scale:y", _originalScale.Y * _squishMultiplier, _stepDuration);

        // Nhịp 3: Trở về chiều cao bình thường
        bobbingTween.TweenProperty(this, "position:y", _bobHeight, _stepDuration)
            .AsRelative()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);

        bobbingTween.Parallel().TweenProperty(this, "scale:y", _originalScale.Y, _stepDuration);

        // 3. KHI ĐẾN NƠI: Dừng bobbing, cố định vị trí và tiến hành thả giấy tờ
        moveTween.Finished += () =>
        {
            bobbingTween.Kill();

            Position = _targetPosition;
            Scale = _originalScale;
            _isWalking = false;

            GD.Print($"[NPCBooth]: Nhân vật '{Name}' đã đến quầy. Tiến hành thả giấy tờ!");
            OnNPCArrivedAtBooth();
        };
    }

    /// <summary>
    /// Kích hoạt sau khi nhân vật đã đứng ngay ngắn ở quầy làm việc
    /// </summary>
    private void OnNPCArrivedAtBooth()
    {
        // 1. Thả hộ chiếu ở dạng bìa gập xuống bàn gỗ quầy
        if (_passportDocument != null)
        {
            Vector2 passStartPos = new Vector2(_boothTableDropPosition.X - 80.0f, _boothTableDropPosition.Y - 60.0f);
            _passportDocument.SpawnAsCoverOnBooth(passStartPos, _boothTableDropPosition);
            GD.Print("[NPCBooth]: Đã thả bìa hộ chiếu xuống bàn quầy bên trái!");
        }
        else
        {
            GD.PrintErr("[NPCBooth]: Chưa gán Node Document2D!");
        }

        // 2. Thả vé tàu ở bản thu nhỏ (small thumbnail) xuống cạnh hộ chiếu
        if (_trainTicketDocument != null)
        {
            Vector2 ticketDropPos = _boothTableDropPosition + new Vector2(50.0f, 15.0f); // Lệch nhẹ để không đè khít lên hộ chiếu
            Vector2 ticketStartPos = new Vector2(ticketDropPos.X - 80.0f, ticketDropPos.Y - 60.0f);
            _trainTicketDocument.SpawnAsSmallOnBooth(ticketStartPos, ticketDropPos);
            GD.Print("[NPCBooth]: Đã thả vé tàu thu nhỏ xuống bàn quầy bên trái!");
        }
    }

    /// <summary>
    /// Kích hoạt khi người chơi đã đóng dấu xong và thả trả lại giấy tờ về bàn quầy bên trái
    /// </summary>
    public void WalkAwayAndFinishTurn()
    {
        if (_isWalking) return;
        _isWalking = true;

        // 1. Thu hồi và ẩn các giấy tờ (NPC nhận lại đồ)
        if (_passportDocument != null)
        {
            _passportDocument.Visible = false;
            _passportDocument.ClearInkMarks(); // Xóa sạch vết mực cũ để chuẩn bị cho lượt tiếp theo
        }

        if (_trainTicketDocument != null)
        {
            _trainTicketDocument.Visible = false;
        }

        GD.Print($"[NPCBooth]: Nhân vật '{Name}' đã nhận lại giấy tờ và bắt đầu rời khỏi quầy...");

        // 2. Hoạt ảnh đi ngược về vị trí ẩn ban đầu kèm nhấp nhô
        Tween exitTween = CreateTween();
        exitTween.SetParallel(true);

        exitTween.TweenProperty(this, "position:x", _startPosition.X, _walkDuration);
        exitTween.TweenProperty(this, "position:y", _startPosition.Y, _walkDuration)
                 .SetTrans(Tween.TransitionType.Quad)
                 .SetEase(Tween.EaseType.In);

        // Hiệu ứng nhấp nhô bước chân khi đi ra
        Tween exitBobbingTween = CreateTween();
        int totalSteps = Math.Max(1, (int)(_walkDuration / (_stepDuration * 2)));
        exitBobbingTween.SetLoops(totalSteps);

        exitBobbingTween.TweenProperty(this, "position:y", -_bobHeight, _stepDuration)
                        .AsRelative()
                        .SetTrans(Tween.TransitionType.Sine)
                        .SetEase(Tween.EaseType.Out);

        exitBobbingTween.Parallel().TweenProperty(this, "scale:y", _originalScale.Y * _squishMultiplier, _stepDuration);

        exitBobbingTween.TweenProperty(this, "position:y", _bobHeight, _stepDuration)
                        .AsRelative()
                        .SetTrans(Tween.TransitionType.Sine)
                        .SetEase(Tween.EaseType.In);

        exitBobbingTween.Parallel().TweenProperty(this, "scale:y", _originalScale.Y, _stepDuration);

        exitTween.Finished += () =>
        {
            exitBobbingTween.Kill();
            Position = _startPosition;
            Scale = _originalScale;
            _isWalking = false;

            GD.Print($"[NPCBooth]: Nhân vật '{Name}' đã rời khỏi quầy hoàn tất! Lượt chơi kết thúc.");
        };
    }
}
