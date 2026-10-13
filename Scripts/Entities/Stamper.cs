namespace Game.Entities;

using Godot;
using System;

public partial class Stamper : Area2D
{
    // [Export] giúp gán ảnh vết mực riêng cho từng con dấu từ Editor Inspector
    [Export] private Texture2D _inkTexture;

    // Biến offset phụ trợ nếu bạn vẫn muốn nhích vết mực thủ công bằng mắt trên Inspector
    [Export] private Vector2 _inkOffset = Vector2.Zero;

    private bool _isDragging = false;
    private bool _isStamping = false; // Biến khóa chặn tương tác khi đang dập hoạt ảnh
    private Vector2 _dragOffset = Vector2.Zero;
    private Vector2 _originalLocalPosition;

    // Biến toàn cục ghi nhớ chính xác vị trí tâm Area2D khi buông tay chuột
    private Vector2 _exactDropGlobalPos;

    public override void _Ready()
    {
        // Ghi nhớ vị trí ô mặc định trên khay dấu làm gốc tương đối
        _originalLocalPosition = Position;
        
        // Đăng ký sự kiện lắng nghe chuột trái
        InputEvent += OnStamperInput;
    }

    private void OnStamperInput(Node viewport, InputEvent @event, long shapeIdx)
    {
        // Nếu đang trong quá trình dập hoặc chạy Tween, cấm tuyệt đối không cho click phá hoạt ảnh
        if (_isStamping) return;

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
            {
                _isDragging = true;
                _dragOffset = GlobalPosition - GetGlobalMousePosition();
            }
            else if (_isDragging)
            {
                _isDragging = false;
                
                // 🎯 ĐỒNG BỘ: Lưu lại đúng vị trí Global của tâm con dấu tại thời điểm thả chuột
                _exactDropGlobalPos = GlobalPosition;

                // Người chơi buông chuột trái -> Thực hiện chuỗi hoạt ảnh dập dấu ngay tại chỗ
                PlayStampAnimation();
            }
        }
    }

    public override void _Process(double delta)
    {
        // Chỉ cập nhật vị trí theo chuột nếu đang kéo và KHÔNG phải đang dập dấu
        if (_isDragging && !_isStamping)
        {
            GlobalPosition = GetGlobalMousePosition() + _dragOffset;
        }
    }

    /// <summary>
    /// Tạo hoạt ảnh dập dấu (Xuống -> Lên trong đúng 0.5s) bằng Tween rồi mới bay về khay
    /// </summary>
    private void PlayStampAnimation()
    {
        _isStamping = true;

        // Tính toán tọa độ nhún xuống (Dịch xuống 25 pixel theo trục Y)
        Vector2 downGlobalPos = _exactDropGlobalPos + new Vector2(0, 25);

        // Khởi tạo hệ thống Tween xử lý hoạt ảnh chuyển động mềm mại của Godot 4.7
        Tween tween = CreateTween();
        tween.SetParallel(false); // Ép chạy tuần tự từng bước 1
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);

        // ⏱️ CHẶNG 1 (Giây 0.00 -> 0.15): Con dấu di chuyển nhún xuống điểm thấp nhất
        tween.TweenProperty(this, "global_position", downGlobalPos, 0.15f);

        // ⏱️ CHẶNG 2 (CHẠM ĐÁY HÀNH TRÌNH): Gọi hàm quét va chạm vật lý và in vết mực lên giấy
        tween.TweenCallback(Callable.From(TryApplyInkMark));

        // ⏱️ CHẶNG 3 (Giây 0.15 -> 0.30): Con dấu nhấc ngược lên lại vị trí thả chuột ban đầu
        tween.TweenProperty(this, "global_position", _exactDropGlobalPos, 0.15f);

        // ⏱️ CHẶNG 4 (Giây 0.30 -> 0.50): Con dấu tự động bay ngược về đúng vị trí ô mặc định trên khay dấu
        tween.TweenProperty(this, "position", _originalLocalPosition, 0.2f)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.InOut);

        // Hoàn thành toàn bộ chuỗi 0.5 giây hoạt ảnh -> Mở khóa cho lượt tương tác tiếp theo
        tween.Finished += () => 
        {
            _isStamping = false;
        };
    }

    /// <summary>
    /// Kiểm tra va chạm vùng Area2D tại chỗ, nếu trúng Hộ chiếu sẽ tạo vết mực dính chặt vào nó
    /// </summary>
    private void TryApplyInkMark()
    {
        // Ép Godot cập nhật và đồng bộ vị trí vật lý của con dấu ngay lập tức tại Frame này
        ForceUpdateTransform();

        // Tiến hành lấy danh sách các Area2D đang đè/giao nhau với con dấu
        var overlappingAreas = GetOverlappingAreas();
        Node2D validPassportNode = null;

        foreach (var area in overlappingAreas)
        {
            // Kiểm tra xem đối tượng va chạm trúng có nằm trong Group "passport" đã cài trên Editor không
            if (area.IsInGroup("passport"))
            {
                validPassportNode = area;
                break; // Tìm thấy vùng hộ chiếu hợp lệ thì ngắt vòng lặp để tối ưu
            }
        }

        // Nếu tìm thấy hộ chiếu nằm ngay bên dưới
        if (validPassportNode != null)
        {
            if (_inkTexture == null)
            {
                GD.PrintErr($"[Hệ thống]: Chưa gán tài nguyên ảnh Ink Texture cho Node con dấu: {Name}!");
                return;
            }

            // Khởi tạo một Node Sprite2D mới động để chứa vết mực in
            Sprite2D inkMark = new Sprite2D();
            inkMark.Texture = _inkTexture;
            
            // Ép cấu hình hiển thị Nearest giúp vết mực in ra sắc nét từng khối ô vuông pixel art
            inkMark.TextureFilter = TextureFilterEnum.Nearest;

            // 1. 🔍 LẤY TỌA ĐỘ LỆCH: Truy cập vào Node Sprite2D con để đọc thông số Position thực tế
            Sprite2D stamperSprite = GetNode<Sprite2D>("Sprite2D");
            Vector2 spriteOffset = stamperSprite != null ? stamperSprite.Position : Vector2.Zero;

            // 2. Thêm vết mực làm con trực tiếp của màn hình chính trước để lấy ma trận sạch
            GetTree().CurrentScene.AddChild(inkMark);

            // 3. 🎯 ĐỒNG BỘ CHUẨN XÁC: 
            // Tọa độ mực = Tọa độ buông chuột + Khoảng lệch Sprite2D con dấu + Lệch tinh chỉnh Inspector
            inkMark.GlobalPosition = _exactDropGlobalPos + spriteOffset + _inkOffset;

            // 4. Ép vết mực chuyển sang làm con của Hộ chiếu để bám dính khi kéo giấy tờ đi
            inkMark.Reparent(validPassportNode, true);
            
            GD.Print($"[Hệ thống]: Đã in vết mực của con dấu {Name} trùng khớp hoàn toàn với vị trí hình ảnh!");
        }
        else
        {
            // Nếu nằm ngoài (rơi ra bàn gỗ trầy xước, vách tường) -> Hủy lệnh, không sinh mực bừa bãi
            GD.Print("[Hệ thống]: Dập hụt ra ngoài! Không in mực lên bàn.");
        }
    }

    /// <summary>
    /// Hàm bổ trợ khẩn cấp đưa con dấu lập tức quay trở lại vị trí mặc định ban đầu trên khay
    /// </summary>
    public void ResetToTray()
    {
        _isDragging = false;
        _isStamping = false;
        Position = _originalLocalPosition;
    }
}
