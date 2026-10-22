namespace Game.UI;

using Godot;
using System;

public partial class MainMenuController : Control
{
    private Button _playButton;
    private Button _settingButton;

    // Đường dẫn chuyển tiếp sang màn hình Intro Visual Novel sau khi bấm Play
    [Export] private string _introScenePath = "res://Scenes/UI/IntroStory.tscn";

    public override void _Ready()
    {
        // 1. Kết nối các Node nút bấm từ cây phân cảnh của MainMenu.tscn
        _playButton = GetNode<Button>("VBoxContainer/PlayButton");
        _settingButton = GetNode<Button>("VBoxContainer/SettingButton");

        // 2. Đăng ký sự kiện lắng nghe click chuột
        _playButton.Pressed += OnPlayButtonPressed;
        _settingButton.Pressed += OnSettingButtonPressed;
        
        GD.Print("[Hệ thống UI]: Màn hình Main Menu đã khởi tạo thành công!");
    }

    /// <summary>
    /// Xử lý sự kiện khi người chơi bấm nút PLAY: Chuyển sang màn Intro dẫn dắt cốt truyện
    /// </summary>
    private void OnPlayButtonPressed()
    {
        GD.Print("[Hệ thống UI]: Người chơi bấm PLAY -> Chuyển tiếp sang IntroStory...");
        
        Error error = GetTree().ChangeSceneToFile(_introScenePath);
        
        if (error != Error.Ok)
        {
            GD.PrintErr($"[Hệ thống UI]: Không thể tải màn hình Intro! Mã lỗi: {error}");
        }
    }

    /// <summary>
    /// Xử lý sự kiện khi người chơi bấm nút SETTING
    /// </summary>
    private void OnSettingButtonPressed()
    {
        GD.Print("[Hệ thống UI]: Người chơi bấm SETTING -> (Đang chờ hoàn thiện Panel cài đặt)");
        // TODO: Mở Panel Popup cài đặt âm lượng / đồ họa tại đây
    }

    /// <summary>
    /// Hàm dọn dẹp bộ nhớ an toàn: Kiểm tra hợp lệ trước khi hủy đăng ký sự kiện
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Chỉ gỡ liên kết nếu các Node Button chưa bị engine giải phóng trước đó
            if (GodotObject.IsInstanceValid(_playButton))
            {
                _playButton.Pressed -= OnPlayButtonPressed;
            }
            
            if (GodotObject.IsInstanceValid(_settingButton))
            {
                _settingButton.Pressed -= OnSettingButtonPressed;
            }
            
            GD.Print("[Hệ thống UI]: Đã giải phóng an toàn các liên kết sự kiện của Main Menu.");
        }
        
        base.Dispose(disposing);
    }
}
