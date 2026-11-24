namespace Game.Entities;

using Godot;
using System;

public partial class CitationPrinter : Node2D
{
    [ExportGroup("Tham chiếu Node con")]
    [Export] private CitationSlip2D _citationSlip;
    [Export] private Label _reasonLabel;
    [Export] private Label _penaltyLabel;
    [Export] private Sprite2D _ledActive;

    [ExportGroup("Vùng chứa Giấy tờ trên bàn")]
    [Export] private Node2D _documentDeskArea;
    [Export] private Area2D _boothTableArea;

    [ExportGroup("Cấu hình Animation In giấy")]
    [Export] private float _hiddenSlipY = 70.0f;
    [Export] private float _midBurstSlipY = 2.0f;
    [Export] private float _restingSlipY = -75.0f;
    [Export] private float _firstBurstDuration = 0.24f;
    [Export] private float _burstPauseDuration = 0.11f;
    [Export] private float _secondBurstDuration = 0.30f;

    public int CitationCount { get; private set; } = 0;

    private CitationSlip2D _slipInSlot;
    private Tween _printTween;
    private Tween _ledTween;

    public override void _Ready()
    {
        _citationSlip ??= GetNodeOrNull<CitationSlip2D>("CitationSlip");
        _reasonLabel ??= GetNodeOrNull<Label>("CitationSlip/ReasonLabel");
        _penaltyLabel ??= GetNodeOrNull<Label>("CitationSlip/PenaltyLabel");
        _ledActive ??= GetNodeOrNull<Sprite2D>("LedActive");
        _documentDeskArea ??= GetNodeOrNull<Node2D>("../DocumentDeskArea");
        _boothTableArea ??= GetNodeOrNull<Area2D>("../BoothTableArea");

        EnsureNearestFilter(GetNodeOrNull<Sprite2D>("PrinterBack"));
        EnsureNearestFilter(GetNodeOrNull<Sprite2D>("PrinterFrontLip"));
        EnsureNearestFilter(GetNodeOrNull<Sprite2D>("CitationSlip/SlipSprite"));
        EnsureNearestFilter(_ledActive);

        // Giữ node CitationSlip trong scene làm mẫu ẩn (template) để nhân bản mỗi lần in
        if (_citationSlip != null)
        {
            _citationSlip.IsTemplate = true;
            _citationSlip.Position = new Vector2(0.0f, _hiddenSlipY);
            _citationSlip.Visible = false;
        }

        if (_ledActive != null)
        {
            _ledActive.Visible = false;
        }
    }

    private static void EnsureNearestFilter(Sprite2D sprite)
    {
        if (sprite != null)
        {
            sprite.TextureFilter = TextureFilterEnum.Nearest;
        }
    }

    /// <summary>
    /// In một tờ biên bản vi phạm (Citation) mới trượt lên từ khe máy in.
    /// Người chơi có thể giữ lại và kéo thả tự do trên bàn làm việc (nhưng không thể đưa sang NPC).
    /// </summary>
    public void PrintCitation(string reason)
    {
        CitationCount++;

        string penaltyText = CitationCount <= 2
            ? "WARNING - NO PENALTY"
            : "-5 CREDITS";

        if (_citationSlip == null)
        {
            GD.PrintErr("[CitationPrinter]: Thiếu tham chiếu CitationSlip mẫu!");
            return;
        }

        // Nếu tờ Citation trước vẫn còn cắm ở khe máy in, đẩy nhẹ nó ra bàn để không bị đè mất
        if (_slipInSlot != null && GodotObject.IsInstanceValid(_slipInSlot) && _slipInSlot.IsInSlot)
        {
            _printTween?.Kill();
            Vector2 ejectPos = GlobalPosition + new Vector2(-180.0f + ((CitationCount % 3) * 30.0f), -210.0f);
            _slipInSlot.EjectFromSlotToDesk(ejectPos);
            _slipInSlot = null;
        }

        // Nhân bản tờ Citation mới để người chơi có thể giữ nhiều biên bản trên bàn
        var newSlip = _citationSlip.Duplicate() as CitationSlip2D;
        if (newSlip == null)
        {
            GD.PrintErr("[CitationPrinter]: Không thể nhân bản CitationSlip2D!");
            return;
        }

        newSlip.Name = $"CitationSlip_{CitationCount}";
        newSlip.ZIndex = 11; // Nằm giữa PrinterBack (10) và PrinterFrontLip (12)
        newSlip.Position = new Vector2(0.0f, _hiddenSlipY);
        AddChild(newSlip);

        newSlip.InitializePrintedSlip(
            reason,
            penaltyText,
            _documentDeskArea,
            _boothTableArea,
            () =>
            {
                if (_slipInSlot == newSlip)
                {
                    _slipInSlot = null;
                }
            }
        );

        _slipInSlot = newSlip;

        // 2 nhịp đẩy giấy cơ học giả lập máy in hóa đơn nhiệt (tổng ~0.65s)
        _printTween?.Kill();
        _printTween = CreateTween();
        _printTween.SetParallel(false);
        _printTween.TweenProperty(newSlip, "position:y", _midBurstSlipY, _firstBurstDuration)
                   .SetTrans(Tween.TransitionType.Quad)
                   .SetEase(Tween.EaseType.Out);
        _printTween.TweenInterval(_burstPauseDuration);
        _printTween.TweenProperty(newSlip, "position:y", _restingSlipY, _secondBurstDuration)
                   .SetTrans(Tween.TransitionType.Quad)
                   .SetEase(Tween.EaseType.Out);
        _printTween.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(newSlip))
            {
                newSlip.MarkPrintFinished();
            }
            StopLedBlink();
        };

        StartLedBlink();
        GD.Print($"[CitationPrinter]: Đã in Citation #{CitationCount} | Penalty: {penaltyText}");
    }

    private void StartLedBlink()
    {
        if (_ledActive == null)
            return;

        _ledTween?.Kill();
        _ledActive.Visible = true;

        _ledTween = CreateTween();
        _ledTween.SetParallel(false);

        // Nhấp nháy 4 lần trong lúc giấy đang chạy (~0.64s)
        const int blinkCycles = 4;
        const float halfPeriod = 0.08f;
        for (int i = 0; i < blinkCycles; i++)
        {
            _ledTween.TweenCallback(Callable.From(() => { if (_ledActive != null) _ledActive.Visible = true; }));
            _ledTween.TweenInterval(halfPeriod);
            _ledTween.TweenCallback(Callable.From(() => { if (_ledActive != null) _ledActive.Visible = false; }));
            _ledTween.TweenInterval(halfPeriod);
        }
    }

    private void StopLedBlink()
    {
        _ledTween?.Kill();
        _ledTween = null;

        if (_ledActive != null)
        {
            _ledActive.Visible = false;
        }
    }
}
