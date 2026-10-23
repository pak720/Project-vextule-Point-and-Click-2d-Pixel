namespace Game.Entities;

using Godot;
using System;

public partial class NPCBooth : Sprite2D
{
    [ExportGroup("Cấu hình tọa độ di chuyển (Từ Editor)")]
    // Vị trí xuất phát ẩn dưới quầy (tùy chỉnh trên Inspector theo tọa độ màn hình)
    [Export] private Vector2 _startPosition = new Vector2(-300.0f, 750.0f);
    // Vị trí đứng chuẩn ở quầy làm việc (khớp với Transform Position của TEST3)
    [Export] private Vector2 _targetPosition = new Vector2(-300.0f, 469.0f);
    // Tổng thời gian nhân vật bước vào quầy (giây)
    [Export] private float _walkDuration = 2.0f;

    [ExportGroup("Cấu hình hiệu ứng nhấp nhô (Bobbing)")]
    // Thời gian của 1 nhịp nhún chân
    [Export] private float _stepDuration = 0.2f;
    // Độ nảy lên cao của đầu nhân vật (pixels)
    [Export] private float _bobHeight = 6.0f;
    // Tỷ lệ co nén thân mình khi dậm chân (nhân với Scale.Y ban đầu)
    [Export] private float _squishMultiplier = 0.95f;

    [ExportGroup("Tài liệu bàn giao")]
    // Tham chiếu đến Node hộ chiếu trên bàn làm việc
    [Export] private Node2D _passportDocument;
    [Export] private Vector2 _passportDeskPosition = new Vector2(918.0f, 625.0f);

    private bool _isWalking = false;
    private Vector2 _originalScale;

    public override void _Ready()
    {
        // Ghi nhớ tỉ lệ Scale ban đầu (ví dụ 0.285, 0.272) để không bị nhảy kích thước
        _originalScale = Scale;

        // Đưa nhân vật về vị trí ẩn ban đầu
        Position = _startPosition;

        // Tự động tìm hộ chiếu nếu chưa kéo thả trên Inspector
        if (_passportDocument == null)
        {
            _passportDocument = GetNodeOrNull<Node2D>("../DocumentDeskArea/Document2D");
        }

        // Bắt đầu chuỗi bước vào quầy
        StartWalkingWithBobbing();
    }

    /// <summary>
    /// Kích hoạt hoạt ảnh nhân vật tự di chuyển vào quầy với hiệu ứng nhấp nhô bobbing
    /// </summary>
    public void StartWalkingWithBobbing()
    {
        if (_isWalking) return;

        _isWalking = true;
        Position = _startPosition;
        Scale = _originalScale;

        GD.Print($"[NPCBooth]: Nhân vật '{Name}' bắt đầu bước vào quầy...");

        // 1. TWEEN TỊNH TIẾN: Di chuyển mượt từ vị trí ẩn lên vị trí quầy
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

        // 3. KHI ĐẾN NƠI: Khóa vị trí tĩnh và gọi hàm nộp giấy tờ
        moveTween.Finished += () =>
        {
            bobbingTween.Kill();

            Position = _targetPosition;
            Scale = _originalScale;
            _isWalking = false;

            GD.Print($"[NPCBooth]: Nhân vật '{Name}' đã đến quầy. Bắt đầu nộp hộ chiếu!");
            OnNPCArrivedAtBooth();
        };
    }

    /// <summary>
    /// Kích hoạt sau khi nhân vật đã đứng ngay ngắn ở quầy
    /// </summary>
    private void OnNPCArrivedAtBooth()
    {
        if (_passportDocument == null) return;

        // Đặt hộ chiếu xuất phát gần quầy và trượt ra vị trí bàn gỗ
        _passportDocument.Position = new Vector2(Position.X + 200.0f, Position.Y + 50.0f);
        _passportDocument.Visible = true;

        Tween slideTween = CreateTween();
        slideTween.TweenProperty(_passportDocument, "position", _passportDeskPosition, 0.45f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        slideTween.Finished += () =>
        {
            GD.Print("[NPCBooth]: Hộ chiếu đã trượt ra bàn, sẵn sàng để người chơi kiểm tra!");
        };
    }
}
