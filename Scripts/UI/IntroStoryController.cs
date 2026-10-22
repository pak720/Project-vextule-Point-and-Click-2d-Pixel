namespace Game.UI;

using Godot;
using System;

public partial class IntroStoryController : Control
{
    [Export] private RichTextLabel _dialogLabel;
    [Export] private Label _continueHint;
    
    // Đảm bảo Node Background trên cây phân cảnh là kiểu TextureRect
    [Export] private TextureRect _backgroundRect;

    // Khai báo mảng chứa danh sách các ảnh nền tương ứng với các phân cảnh
    [Export] private Texture2D[] _backgroundTextures;

    [Export] private string _gameplayScenePath = "res://Scenes/Gameplay.tscn";

    // Danh sách các đoạn dẫn dắt bối cảnh (3 câu đầu tương ứng với 3 ảnh của bạn)
    private readonly string[] _storyLines = new string[]
    {
        "Năm 2167, Khu vực biên giới Kusetan quân sự hóa nghiêm ngặt.",
        "Trật tự cũ sụp đổ, các thành phố giờ đây ngập tràn trong khói đạn và những đống đổ nát lạnh lẽo.",
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

    // Nhận click chuột trực tiếp từ khung giao diện
    public override void _GuiInput(InputEvent @event)
    {
        if (_isTransitioning) return;

        if (@event is InputEventMouseButton mouseBtn && mouseBtn.ButtonIndex == MouseButton.Left && mouseBtn.Pressed)
        {
            NextDialogue();
        }
    }

    // Nhận phím Space/Enter toàn cục
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
            // 1. Cập nhật nội dung chữ thoại
            _dialogLabel.Text = _storyLines[_currentLineIndex];

            // 2. Tự động đổi ảnh nền tương ứng (Nếu mảng ảnh được gán đầy đủ trên Inspector)
            if (_backgroundRect != null && _backgroundTextures != null && _currentLineIndex < _backgroundTextures.Length)
            {
                if (_backgroundTextures[_currentLineIndex] != null)
                {
                    _backgroundRect.Texture = _backgroundTextures[_currentLineIndex];
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
            Error error = GetTree().ChangeSceneToFile(_gameplayScenePath);
            if (error != Error.Ok)
            {
                GD.PrintErr($"[IntroStory]: Không thể chuyển tới Gameplay! Lỗi: {error}");
            }
        };
    }
}
