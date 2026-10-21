namespace Game.UI;

using Godot;
using System;

public partial class MainMenuController : Control
{
    private Button _playButton;
    private Button _settingButton;

    // Đường dẫn trỏ đến phân cảnh màn chơi chính trong dự án của bạn
    [Export] private string _gameplayScenePath = "res://Scenes/Gameplay.tscn";

    public override void _Ready()
    {
        // 1. Kết nối các Node nút bấm dựa trên cấu trúc cây phân cảnh Editor bạn vừa thiết lập
        _playButton = GetNode<Button>("VBoxContainer/PlayButton");
        _settingButton = GetNode<Button>("VBoxContainer/SettingButton");

        // 2. Đăng ký sự kiện (Signal) lắng nghe hành động click chuột
        _playButton.Pressed += OnPlayButtonPressed;
        _settingButton.Pressed += OnSettingButtonPressed;
        
        GD.Print("[Hệ thống UI]: Màn hình Main Menu đã khởi tạo thành công!");
    }

    /// <summary>
    /// Xử lý sự kiện khi người chơi bấm nút PLAY
    /// </summary>
    private void OnPlayButtonPressed()
    {
        GD.Print("[Hệ thống UI]: Người chơi bấm PLAY -> Tiến hành chuyển sang màn chơi chính...");
        
        // Chuyển scene sang màn hình Gameplay bằng API chuẩn của Godot 4.7
        Error error = GetTree().ChangeSceneToFile(_gameplayScenePath);
        
        if (error != Error.Ok)
        {
            GD.PrintErr($"[Hệ thống UI]: Không thể tải màn chơi chính! Mã lỗi: {error}");
        }
    }

    /// <summary>
    /// Xử lý sự kiện khi người chơi bấm nút SETTING
    /// </summary>
    private void OnSettingButtonPressed()
    {
        GD.Print("[Hệ thống UI]: Người chơi bấm SETTING -> (Tính năng đang chờ thiết kế giao diện từ GD)");
        // TODO: Thực hiện bật Panel Popup cài đặt âm lượng hoặc đồ họa tại đây
    }

    /// <summary>
    /// GIẢI PHÁP AN TOÀN: Hàm hủy phân rã bộ nhớ được bọc bộ lọc kiểm tra thực thể vật lý
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Chỉ thực hiện gỡ hàm khỏi bộ nhớ nếu các Node nút bấm này vẫn chưa bị dọn dẹp bởi cơ chế nội bộ của Godot
            if (GodotObject.IsInstanceValid(_playButton))
            {
                _playButton.Pressed -= OnPlayButtonPressed;
            }
            
            if (GodotObject.IsInstanceValid(_settingButton))
            {
                _settingButton.Pressed -= OnSettingButtonPressed;
            }
            
            GD.Print("[Hệ thống UI]: Đã gỡ bỏ an toàn các liên kết sự kiện của Main Menu.");
        }
        
        // Gọi hàm cơ sở để hoàn tất chu trình giải phóng bộ nhớ C++ của Engine
        base.Dispose(disposing);
    }
}
