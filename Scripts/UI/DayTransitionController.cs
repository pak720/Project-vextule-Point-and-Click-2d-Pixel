namespace Game.UI;

using Godot;
using System;

public partial class DayTransitionController : Control
{
    [Export] private Label _dayLabel;
    [Export] private string _gameplayScenePath = "res://Scenes/Gameplay.tscn";

    // Thời gian hiển thị (tính bằng giây)
    [Export] private float _fadeInDuration = 1.0f;
    [Export] private float _stayDuration = 1.5f;
    [Export] private float _fadeOutDuration = 0.8f;

    public override void _Ready()
    {
        if (_dayLabel == null)
        {
            _dayLabel = GetNodeOrNull<Label>("DayLabel");
        }

        // Ẩn chữ ban đầu để chuẩn bị làm hiệu ứng Fade In
        if (_dayLabel != null)
        {
            _dayLabel.Modulate = new Color(1, 1, 1, 0);
        }

        StartDayTransition();
    }

    private void StartDayTransition()
    {
        Tween tween = CreateTween();
        tween.SetParallel(false); // Chạy tuần tự

        // 1. Chữ "Day 1" mờ dần hiện rõ lên (Fade In)
        if (_dayLabel != null)
        {
            tween.TweenProperty(_dayLabel, "modulate:a", 1.0f, _fadeInDuration)
                 .SetTrans(Tween.TransitionType.Cubic)
                 .SetEase(Tween.EaseType.Out);
        }

        // 2. Dừng lại trên màn hình đen một khoảng thời gian
        tween.TweenInterval(_stayDuration);

        // 3. Toàn bộ màn hình mờ dần biến mất (Fade Out)
        tween.TweenProperty(this, "modulate:a", 0.0f, _fadeOutDuration)
             .SetTrans(Tween.TransitionType.Cubic)
             .SetEase(Tween.EaseType.In);

        // 4. Khi kết thúc, chuyển thẳng vào bàn làm việc Gameplay
        tween.Finished += () =>
        {
            Error error = GetTree().ChangeSceneToFile(_gameplayScenePath);
            if (error != Error.Ok)
            {
                GD.PrintErr($"[DayTransition]: Không thể chuyển sang Gameplay! Mã lỗi: {error}");
            }
        };
    }
}
