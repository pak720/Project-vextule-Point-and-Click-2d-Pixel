namespace Game.Entities;

using Godot;
using System;

public partial class Document2D : Area2D
{
    private bool _isDragging = false;
    private Vector2 _dragOffset = Vector2.Zero;

    public override void _Ready()
    {
        // Đồng bộ kết nối nhận diện chuột
        InputEvent += OnDocumentInput;
    }

    private void OnDocumentInput(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
            {
                _isDragging = true;
                _dragOffset = GlobalPosition - GetGlobalMousePosition();
                // Đưa tờ giấy lên trên cùng tránh bị che khuất
                GetParent().MoveChild(this, GetParent().GetChildCount() - 1);
            }
            else
            {
                _isDragging = false;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_isDragging)
        {
            GlobalPosition = GetGlobalMousePosition() + _dragOffset;
        }
    }
}
