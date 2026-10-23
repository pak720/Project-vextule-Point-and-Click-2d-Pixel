namespace Game.UI;

using Godot;
using System;

public partial class IntroStoryController : Control
{
    [Export] private RichTextLabel _dialogLabel;
    [Export] private Label _continueHint;
    [Export] private TextureRect _backgroundRect;

    // Danh sách 3 ảnh nền bối cảnh được gán trên Inspector
    [Export] private Texture2D[] _backgroundTextures;

    // Đích đến sau khi kết thúc Intro: Màn hình chuyển tiếp Day 1
    [Export] private string _dayTransitionScenePath = "res://Scenes/UI/DayTransition.tscn";

    // Danh sách các câu dẫn dắt bối cảnh và câu chuyện của nhân vật chính
    private readonly string[] _storyLines = new string[]
    {
        // [ẢNH 1] Bối cảnh thế giới
        "Năm 2167, Khu vực biên giới Kusetan quân sự hóa nghiêm ngặt.",
        "Trật tự cũ sụp đổ, các thành phố giờ đây ngập tràn trong khói đạn và những đống đổ nát lạnh lẽo.",
        
        // [ẢNH 2] Hoàn cảnh nhân vật chính & Lý do nhận việc
        "Bạn — một cựu binh may mắn sống sót, đang phải chật vật từng ngày nuôi sống gia đình trong một căn hầm tối tăm tại thủ đô.",
        "Giữa cái đói và sự kiểm soát gắt gao của chính quyền mới, cơ hội duy nhất đến từ 'Cuộc xổ số việc làm lao động quốc gia'.",
        "Tên của bạn được xướng lên. Bạn được bổ nhiệm làm Thanh tra kiểm soát nhập cảnh tại Trạm biên giới Kusetan.",
        "Một công việc có thể mang lại thức ăn, sưởi ấm cho gia đình, nhưng cái giá phải trả là sự phục tùng tuyệt đối.",

        // [ẢNH 3] Nhiệm vụ bắt đầu
        "Đứng trước ranh giới của sự chia cắt, thế giới đang chờ đợi một quyết định có thể thay đổi toàn bộ lịch sử.",
        "Nhiệm vụ của bạn bắt đầu ngay hôm nay. Hãy cẩn thận với từng quyết định."
    };

    private int _currentLineIndex = 0;
    private bool _isTransitioning = false;

    public override void _Ready()
    {
        if (_dialogLabel == null)
            _dialogLabel = GetNodeOrNull<RichTextLabel>("DialogContainer/DialogLabel");

        if (_continueHint == null)
            _continueHint = GetNodeOrNull<Label>("ContinueHint");

        if (_backgroundRect == null)
            _backgroundRect = GetNodeOrNull<TextureRect>("Background");

        ShowCurrentLine();
    }

    // Bắt sự kiện click chuột trực tiếp trên giao diện màn hình
    public override void _GuiInput(InputEvent @event)
    {
        if (_isTransitioning) return;

        if (@event is InputEventMouseButton mouseBtn && mouseBtn.ButtonIndex == MouseButton.Left && mouseBtn.Pressed)
        {
            NextDialogue();
        }
    }

    // Bắt phím Space hoặc Enter toàn cục
    public override void _Input(InputEvent @event)
    {
        if (_isTransitioning) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && 
            (keyEvent.Keycode == Key.Space || keyEvent.Keycode == Key.Enter))
        {
            NextDialogue();
        }
    }

    private void ShowCurrentLine()
    {
        if (_dialogLabel == null) return;

        if (_currentLineIndex < _storyLines.Length)
        {
            // 1. Hiển thị nội dung câu thoại
            _dialogLabel.Text = _storyLines[_currentLineIndex];

            // 2. Tự động chuyển đổi ảnh nền tương ứng theo mạch truyện:
            // Câu 0, 1 -> Ảnh 0 (Dàn xe tăng bên hàng rào)
            // Câu 2, 3, 4, 5 -> Ảnh 1 (Thành phố đổ nát / Hoàn cảnh nghèo đói)
            // Câu 6, 7 -> Ảnh 2 (Xe tăng trên đồi cỏ nhìn về biên giới)
            if (_backgroundRect != null && _backgroundTextures != null && _backgroundTextures.Length > 0)
            {
                int textureIndex = 0;

                if (_currentLineIndex >= 2 && _currentLineIndex <= 5)
                {
                    textureIndex = 1;
                }
                else if (_currentLineIndex >= 6)
                {
                    textureIndex = 2;
                }

                // Bảo vệ chống tràn mảng nếu gán thiếu ảnh trên Inspector
                textureIndex = Math.Min(textureIndex, _backgroundTextures.Length - 1);

                if (_backgroundTextures[textureIndex] != null)
                {
                    _backgroundRect.Texture = _backgroundTextures[textureIndex];
                }
            }

            // Hiệu ứng mờ dần (Fade In) mượt mà cho chữ mỗi khi chuyển câu
            _dialogLabel.Modulate = new Color(1, 1, 1, 0);
            Tween tween = CreateTween();
            tween.TweenProperty(_dialogLabel, "modulate:a", 1.0f, 0.35f);
        }
    }

    private void NextDialogue()
    {
        _currentLineIndex++;

        if (_currentLineIndex < _storyLines.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            FinishIntroAndStartGame();
        }
    }

    private void FinishIntroAndStartGame()
    {
        _isTransitioning = true;

        // Làm mờ toàn bộ màn hình trước khi chuyển cảnh
        Tween tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.5f);
        tween.Finished += () =>
        {
            Error error = GetTree().ChangeSceneToFile(_dayTransitionScenePath);
            if (error != Error.Ok)
            {
                GD.PrintErr($"[IntroStory]: Không thể nạp DayTransition! Mã lỗi: {error}");
            }
        };
    }
}
