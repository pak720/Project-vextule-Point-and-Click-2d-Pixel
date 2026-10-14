namespace Game.Entities;

using Godot;
using System;

public partial class StampStation : Node2D
{
    // Sử dụng [Export] để kéo thả trực tiếp từ Godot Editor Inspector
    [Export] private AnimationPlayer _animPlayer;
    [Export] private Area2D _handleArea;
    
    private bool _isOpen = false;

    public override void _Ready()
    {
        // Kiểm tra an toàn để tránh crash game nếu lỡ quên kéo thả trên Editor
        if (_handleArea == null)
        {
            GD.PrintErr("LỖI: Chưa kéo thả Node HandleArea vào mục Inspector của StampStation!");
            return;
        }
        
        if (_animPlayer == null)
        {
            GD.PrintErr("LỖI: Chưa kéo thả Node AnimationPlayer vào mục Inspector của StampStation!");
            return;
        }

        // Gắn sự kiện click chuột vào vùng tay cầm (Handle)
        _handleArea.InputEvent += OnHandleInputEvent;
        
        // Mặc định ban đầu khi bắt đầu màn chơi, khay sẽ ở trạng thái đóng (Giấu đi)
        _animPlayer.Play("RESET"); 
    }

    private void OnHandleInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        // Kiểm tra xem người chơi có click chuột trái vào tay cầm hay không
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            ToggleTray();
        }
    }

    private void ToggleTray()
    {
        // Nếu khay đang trong quá trình trượt (Animation đang chạy), chặn không cho bấm tiếp
        if (_animPlayer.IsPlaying()) return; 

        if (_isOpen)
        {
            _animPlayer.Play("Retract");
            _isOpen = false;
        }
        else
        {
            _animPlayer.Play("Extend");
            _isOpen = true;
        }
    }
}
