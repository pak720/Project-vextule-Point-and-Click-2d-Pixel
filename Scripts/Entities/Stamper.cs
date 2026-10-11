namespace Game.Entities;

using Godot;
using System;

public partial class Stamper : Area2D
{
    private bool _isDragging = false;
    private Vector2 _dragOffset = Vector2.Zero;
    
    // Biến lưu vị trí mặc định ban đầu trên khay dấu
    private Vector2 _originalLocalPosition;

    public override void _Ready()
    {
        // Nhớ vị trí ban đầu (Local Position) ngay khi nạp game
        _originalLocalPosition = Position;
        
        // Kết nối sự kiện tương tác chuột
        InputEvent += OnStamperInput;
    }

    private void OnStamperInput(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
            {
                // Bắt đầu kéo con dấu
                _isDragging = true;
                _dragOffset = GlobalPosition - GetGlobalMousePosition();
            }
            else if (_isDragging)
            {
                // 🔄 THAY ĐỔI Ở ĐÂY: Khi người chơi THẢ CHUỘT TRÁI ra
                ResetToTray();
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_isDragging)
        {
            // Di chuyển tự do theo con trỏ chuột toàn màn hình
            GlobalPosition = GetGlobalMousePosition() + _dragOffset;
        }
    }

    /// <summary>
    /// Đưa con dấu lập tức quay trở lại vị trí mặc định trên khay dấu
    /// </summary>
    public void ResetToTray()
    {
        _isDragging = false;
        
        // Gán lại tọa độ local gốc, con dấu sẽ tự động bắt dính về khay 
        // kể cả khi khay đang đóng hay mở, hoặc đang chạy animation trượt.
        Position = _originalLocalPosition;
    }
}
