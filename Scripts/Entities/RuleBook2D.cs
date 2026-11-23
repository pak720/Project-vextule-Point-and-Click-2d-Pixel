namespace Game.Entities;

using Godot;
using System;

public partial class RuleBook2D : Area2D
{
    private const string BaseAssetPath = "res://Assets/Sprites/Documents/RuleBook/";
    private static readonly Vector2 ClosedHitboxSize = new Vector2(164.0f, 210.0f);
    private static readonly Vector2 OpenHitboxSize = new Vector2(328.0f, 212.0f);

    [ExportGroup("Tài nguyên Hình ảnh (Sprites)")]
    [Export] private Texture2D _coverTexture;
    [Export] private Texture2D[] _spreadTextures;
    [Export] private Texture2D[] _flipTextures;

    [ExportGroup("Vị trí & Thu phóng")]
    [Export] private Vector2 _slotPosition = new Vector2(307.0f, 886.0f);
    [Export] private float _deskBoundaryX = 460.0f;
    [Export] private Vector2 _dockedScale = new Vector2(0.42f, 0.42f);
    [Export] private Vector2 _deskScale = new Vector2(2.0f, 2.0f);
    [Export] private Vector2 _deskMinBounds = new Vector2(465.0f, 120.0f);
    [Export] private Vector2 _deskMaxBounds = new Vector2(1390.0f, 940.0f);

    [ExportGroup("Cấu hình Tương tác & Animation")]
    [Export] private float _dragThreshold = 8.0f;
    [Export] private float _flipFrameDuration = 0.045f;
    [Export] private float _openCloseHalfDuration = 0.11f;
    [Export] private float _returnToSlotDuration = 0.35f;

    private Node2D _bookPivot;
    private Sprite2D _bookSprite;
    private CollisionShape2D _collisionShape;
    private RectangleShape2D _hitboxShape;

    private bool _isPointerDown = false;
    private bool _isDragging = false;
    private bool _isOnDesk = false;
    private bool _isOpen = false;
    private bool _isAnimating = false;
    private bool _isHovered = false;
    private int _currentSpreadIndex = 0;

    private Vector2 _pressGlobalMousePos = Vector2.Zero;
    private Vector2 _dragOffset = Vector2.Zero;

    private Tween _moveTween;
    private Tween _scaleTween;
    private Tween _animTween;
    private Tween _hoverTween;

    public bool IsOpen => _isOpen;
    public bool IsOnDesk => _isOnDesk;
    public int CurrentSpreadIndex => _currentSpreadIndex;

    public override void _Ready()
    {
        _bookPivot = GetNodeOrNull<Node2D>("BookPivot");
        _bookSprite = GetNodeOrNull<Sprite2D>("BookPivot/BookSprite") ?? GetNodeOrNull<Sprite2D>("BookSprite");
        _collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        LoadFallbackTextures();

        if (_bookSprite != null)
        {
            _bookSprite.TextureFilter = TextureFilterEnum.Nearest;
            _bookSprite.Texture = _coverTexture;
        }

        if (_collisionShape != null)
        {
            _hitboxShape = new RectangleShape2D { Size = ClosedHitboxSize };
            _collisionShape.Shape = _hitboxShape;
        }

        // Đặt quyển sổ nằm gọn trong hộc quầy NPC khi bắt đầu
        GlobalPosition = _slotPosition;
        Scale = _dockedScale;
        _isOnDesk = false;
        _isOpen = false;
        _currentSpreadIndex = 0;

        InputEvent += OnBookInputEvent;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    private void LoadFallbackTextures()
    {
        _coverTexture ??= LoadTextureSafe(BaseAssetPath + "cover.png");

        if (_spreadTextures == null || _spreadTextures.Length == 0 || _spreadTextures[0] == null)
        {
            _spreadTextures = new Texture2D[6];
            for (int i = 0; i < 6; i++)
            {
                _spreadTextures[i] = LoadTextureSafe($"{BaseAssetPath}page_{i + 1:00}.png");
            }
        }

        if (_flipTextures == null || _flipTextures.Length == 0 || _flipTextures[0] == null)
        {
            _flipTextures = new Texture2D[6];
            for (int i = 0; i < 6; i++)
            {
                _flipTextures[i] = LoadTextureSafe($"{BaseAssetPath}flip_{i + 1:00}.png");
            }
        }
    }

    private static Texture2D LoadTextureSafe(string resPath)
    {
        if (ResourceLoader.Exists(resPath))
        {
            var tex = GD.Load<Texture2D>(resPath);
            if (tex != null) return tex;
        }

        string globalPath = ProjectSettings.GlobalizePath(resPath);
        if (System.IO.File.Exists(globalPath))
        {
            var img = Image.LoadFromFile(globalPath);
            if (img != null && !img.IsEmpty())
            {
                return ImageTexture.CreateFromImage(img);
            }
        }

        GD.PrintErr($"[RuleBook2D]: Không thể tải texture tại '{resPath}'.");
        return null;
    }

    private void OnMouseEntered()
    {
        _isHovered = true;
        if (!_isDragging && !_isOnDesk && _bookPivot != null)
        {
            _hoverTween?.Kill();
            _hoverTween = CreateTween();
            _hoverTween.TweenProperty(_bookPivot, "position:y", -10.0f, 0.12f)
                       .SetTrans(Tween.TransitionType.Quad)
                       .SetEase(Tween.EaseType.Out);
        }
    }

    private void OnMouseExited()
    {
        _isHovered = false;
        if (_bookPivot != null)
        {
            _hoverTween?.Kill();
            _hoverTween = CreateTween();
            _hoverTween.TweenProperty(_bookPivot, "position:y", 0.0f, 0.12f)
                       .SetTrans(Tween.TransitionType.Quad)
                       .SetEase(Tween.EaseType.Out);
        }
    }

    private void OnBookInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is not InputEventMouseButton mouseEvent)
            return;

        if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            // Khóa không kéo trùng với tài liệu khác
            if (Document2D.CurrentDraggedDocument != null && Document2D.CurrentDraggedDocument != this)
                return;

            _moveTween?.Kill();
            _hoverTween?.Kill();
            if (_bookPivot != null) _bookPivot.Position = Vector2.Zero;

            _isPointerDown = true;
            _isDragging = false;
            Document2D.CurrentDraggedDocument = this;
            _pressGlobalMousePos = GetGlobalMousePosition();
            _dragOffset = GlobalPosition - _pressGlobalMousePos;

            // Đưa quyển sổ lên trên cùng trong DocumentDeskArea
            GetParent()?.MoveChild(this, GetParent().GetChildCount() - 1);
            GetViewport().SetInputAsHandled();
        }
        else if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
        {
            // Chuột phải lên sổ đang mở trên bàn để đóng nhanh
            if (_isOnDesk && _isOpen && !_isAnimating && !_isDragging)
            {
                CloseBook(animate: true);
                GetViewport().SetInputAsHandled();
            }
        }
        else if (mouseEvent.Pressed && _isOnDesk && _isOpen && !_isAnimating && !_isDragging)
        {
            if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
            {
                NextPage();
                GetViewport().SetInputAsHandled();
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
            {
                if (_currentSpreadIndex > 0)
                {
                    PreviousPage();
                }
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        // Bắt nhả chuột trái ở _Input để không bao giờ bị kẹt kéo dù chuột trượt ra khỏi collider
        if (_isPointerDown && @event is InputEventMouseButton mouseEvent &&
            mouseEvent.ButtonIndex == MouseButton.Left && !mouseEvent.Pressed)
        {
            EndPointerInteraction();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (!_isPointerDown)
            return;

        Vector2 mousePos = GetGlobalMousePosition();

        if (!_isDragging && mousePos.DistanceTo(_pressGlobalMousePos) >= _dragThreshold)
        {
            _isDragging = true;
        }

        if (_isDragging)
        {
            GlobalPosition = mousePos + _dragOffset;

            bool shouldBeOnDesk = GlobalPosition.X >= _deskBoundaryX;
            if (shouldBeOnDesk != _isOnDesk)
            {
                SetOnDeskState(shouldBeOnDesk, animate: true);
            }
        }
    }

    private void EndPointerInteraction()
    {
        bool wasDragging = _isDragging;
        _isPointerDown = false;
        _isDragging = false;

        if (Document2D.CurrentDraggedDocument == this)
        {
            Document2D.CurrentDraggedDocument = null;
        }

        if (wasDragging)
        {
            if (!_isOnDesk)
            {
                ReturnToSlot();
            }
            else
            {
                ClampBookInsideDesk(animate: true);
            }
            return;
        }

        // Đây là thao tác Click (không phải kéo)
        if (_isOnDesk)
        {
            if (!_isAnimating)
            {
                HandleDeskClick(ToLocal(GetGlobalMousePosition()));
            }
        }
        else
        {
            // Phản hồi rung nhẹ khi click lúc còn trong hộc NPC (nhắc người chơi kéo sang bàn)
            PlayDockedNudgeFeedback();
        }
    }

    private void SetOnDeskState(bool onDesk, bool animate)
    {
        _isOnDesk = onDesk;
        Vector2 targetScale = onDesk ? _deskScale : _dockedScale;

        // Nếu kéo từ bàn về khu vực NPC mà sổ đang mở thì tự động đóng sổ lại
        if (!onDesk && _isOpen)
        {
            CloseBook(animate: true);
        }

        // Bù trừ offset khi co giãn để điểm nắm chuột không bị trôi lệch tâm
        if (_isDragging && Scale.X > 0.001f && Scale.Y > 0.001f)
        {
            Vector2 scaleRatio = new Vector2(targetScale.X / Scale.X, targetScale.Y / Scale.Y);
            _dragOffset *= scaleRatio;
            GlobalPosition = GetGlobalMousePosition() + _dragOffset;
        }

        _scaleTween?.Kill();
        if (animate)
        {
            _scaleTween = CreateTween();
            _scaleTween.TweenProperty(this, "scale", targetScale, 0.18f)
                       .SetTrans(Tween.TransitionType.Back)
                       .SetEase(Tween.EaseType.Out);
        }
        else
        {
            Scale = targetScale;
        }
    }

    private void ReturnToSlot()
    {
        if (_isOpen)
        {
            CloseBook(animate: true);
        }

        _scaleTween?.Kill();
        _scaleTween = CreateTween();
        _scaleTween.TweenProperty(this, "scale", _dockedScale, 0.2f)
                   .SetTrans(Tween.TransitionType.Quad)
                   .SetEase(Tween.EaseType.Out);

        float distance = GlobalPosition.DistanceTo(_slotPosition);
        float duration = Mathf.Clamp(distance / 1100.0f, 0.18f, _returnToSlotDuration);

        _moveTween?.Kill();
        _moveTween = CreateTween();
        _moveTween.TweenProperty(this, "global_position", _slotPosition, duration)
                  .SetTrans(Tween.TransitionType.Back)
                  .SetEase(Tween.EaseType.Out);
    }

    private void ClampBookInsideDesk(bool animate)
    {
        Vector2 activeSize = (_isOpen ? OpenHitboxSize : ClosedHitboxSize) * _deskScale;
        float halfW = activeSize.X * 0.5f;
        float halfH = activeSize.Y * 0.5f;

        float minX = _deskMinBounds.X + halfW;
        float maxX = Mathf.Max(minX, _deskMaxBounds.X - halfW);
        float minY = _deskMinBounds.Y + halfH;
        float maxY = Mathf.Max(minY, _deskMaxBounds.Y - halfH);

        Vector2 clampedPos = new Vector2(
            Mathf.Clamp(GlobalPosition.X, minX, maxX),
            Mathf.Clamp(GlobalPosition.Y, minY, maxY)
        );

        if (GlobalPosition.DistanceSquaredTo(clampedPos) < 1.0f)
            return;

        _moveTween?.Kill();
        if (animate)
        {
            _moveTween = CreateTween();
            _moveTween.TweenProperty(this, "global_position", clampedPos, 0.16f)
                      .SetTrans(Tween.TransitionType.Quad)
                      .SetEase(Tween.EaseType.Out);
        }
        else
        {
            GlobalPosition = clampedPos;
        }
    }

    private void HandleDeskClick(Vector2 localPos)
    {
        if (!_isOpen)
        {
            OpenBook(animate: true);
            return;
        }

        // 1. Click dải ruy-băng bookmark ở góc trên bên phải (sprite 360x240, tâm 0,0)
        if (localPos.X >= 126.0f && localPos.X <= 156.0f && localPos.Y >= -112.0f && localPos.Y <= -72.0f)
        {
            if (_currentSpreadIndex == 0)
            {
                CloseBook(animate: true);
            }
            else
            {
                FlipToPage(0);
            }
            return;
        }

        // 2. Mục lục tương tác (4 thanh [01]..[04] trong CONTENTS ở trang đầu tiên)
        if (_currentSpreadIndex == 0 && localPos.X >= 18.0f && localPos.X <= 150.0f)
        {
            if (localPos.Y >= -70.0f && localPos.Y <= -46.0f) { FlipToPage(1); return; }
            if (localPos.Y >= -41.0f && localPos.Y <= -17.0f) { FlipToPage(2); return; }
            if (localPos.Y >= -12.0f && localPos.Y <= 12.0f)  { FlipToPage(3); return; }
            if (localPos.Y >= 17.0f  && localPos.Y <= 41.0f)  { FlipToPage(4); return; }
        }

        // 3. Click nửa phải → lật trang kế; click nửa trái → lật lùi (ở trang 1 thì đóng sổ)
        if (localPos.X >= 0.0f)
        {
            NextPage();
        }
        else
        {
            if (_currentSpreadIndex > 0)
            {
                PreviousPage();
            }
            else
            {
                CloseBook(animate: true);
            }
        }
    }

    public void OpenBook(bool animate = true)
    {
        if (_isOpen || _bookSprite == null || _spreadTextures == null || _spreadTextures.Length == 0)
            return;

        _animTween?.Kill();
        _isOpen = true;
        UpdateHitboxSize(OpenHitboxSize);

        _currentSpreadIndex = Mathf.Clamp(_currentSpreadIndex, 0, _spreadTextures.Length - 1);

        if (!animate || _bookPivot == null)
        {
            _isAnimating = false;
            if (_bookPivot != null) _bookPivot.Scale = Vector2.One;
            _bookSprite.Texture = _spreadTextures[_currentSpreadIndex];
            if (_isOnDesk) ClampBookInsideDesk(animate: true);
            return;
        }

        _isAnimating = true;
        _bookPivot.Scale = Vector2.One;

        // Giữ sổ mở trọn vẹn bên trong phạm vi bàn làm việc
        if (_isOnDesk && !_isDragging)
        {
            ClampBookInsideDesk(animate: true);
        }

        _animTween = CreateTween();
        _animTween.SetParallel(false);

        // Bước 1: Gập bìa theo trục ngang tạo cảm giác 3D mở sổ
        _animTween.TweenProperty(_bookPivot, "scale", new Vector2(0.08f, 1.04f), _openCloseHalfDuration)
                  .SetTrans(Tween.TransitionType.Quad)
                  .SetEase(Tween.EaseType.In);

        // Bước 2: Chuyển sang chuỗi frame trang mở bung ra
        _animTween.TweenCallback(Callable.From(() =>
        {
            SetFlipFrameSafe(2); // flip_03
            _bookPivot.Scale = new Vector2(0.55f, 1.03f);
        }));

        _animTween.TweenProperty(_bookPivot, "scale", new Vector2(0.82f, 1.02f), _flipFrameDuration)
                  .SetTrans(Tween.TransitionType.Quad)
                  .SetEase(Tween.EaseType.Out);
        _animTween.TweenCallback(Callable.From(() => SetFlipFrameSafe(3))); // flip_04

        _animTween.TweenProperty(_bookPivot, "scale", new Vector2(0.96f, 1.01f), _flipFrameDuration)
                  .SetTrans(Tween.TransitionType.Quad)
                  .SetEase(Tween.EaseType.Out);
        _animTween.TweenCallback(Callable.From(() => SetFlipFrameSafe(4))); // flip_05

        _animTween.TweenProperty(_bookPivot, "scale", Vector2.One, _flipFrameDuration * 1.2f)
                  .SetTrans(Tween.TransitionType.Back)
                  .SetEase(Tween.EaseType.Out);

        _animTween.TweenCallback(Callable.From(() =>
        {
            _bookSprite.Texture = _spreadTextures[_currentSpreadIndex];
            _bookPivot.Scale = Vector2.One;
            _isAnimating = false;
        }));
    }

    public void CloseBook(bool animate = true)
    {
        if (!_isOpen || _bookSprite == null)
            return;

        _animTween?.Kill();
        _isOpen = false;
        UpdateHitboxSize(ClosedHitboxSize);

        if (!animate || _bookPivot == null)
        {
            _isAnimating = false;
            if (_bookPivot != null) _bookPivot.Scale = Vector2.One;
            _bookSprite.Texture = _coverTexture;
            return;
        }

        _isAnimating = true;
        _bookPivot.Scale = Vector2.One;

        _animTween = CreateTween();
        _animTween.SetParallel(false);

        // Bước 1: Chạy ngược frame gập trang về gáy
        _animTween.TweenCallback(Callable.From(() => SetFlipFrameSafe(4))); // flip_05
        _animTween.TweenProperty(_bookPivot, "scale", new Vector2(0.88f, 1.02f), _flipFrameDuration);

        _animTween.TweenCallback(Callable.From(() => SetFlipFrameSafe(3))); // flip_04
        _animTween.TweenProperty(_bookPivot, "scale", new Vector2(0.62f, 1.03f), _flipFrameDuration);

        _animTween.TweenCallback(Callable.From(() => SetFlipFrameSafe(2))); // flip_03
        _animTween.TweenProperty(_bookPivot, "scale", new Vector2(0.10f, 1.04f), _openCloseHalfDuration * 0.85f)
                  .SetTrans(Tween.TransitionType.Quad)
                  .SetEase(Tween.EaseType.In);

        // Bước 2: Lật sang bìa đóng lại
        _animTween.TweenCallback(Callable.From(() =>
        {
            _bookSprite.Texture = _coverTexture;
            _bookPivot.Scale = new Vector2(0.10f, 1.03f);
        }));

        _animTween.TweenProperty(_bookPivot, "scale", Vector2.One, _openCloseHalfDuration * 1.15f)
                  .SetTrans(Tween.TransitionType.Back)
                  .SetEase(Tween.EaseType.Out);

        _animTween.TweenCallback(Callable.From(() =>
        {
            _bookPivot.Scale = Vector2.One;
            _isAnimating = false;
        }));
    }

    public void NextPage()
    {
        if (!_isOpen || _isAnimating || _spreadTextures == null)
            return;

        if (_currentSpreadIndex < _spreadTextures.Length - 1)
        {
            FlipToPage(_currentSpreadIndex + 1);
        }
        else
        {
            PlayEdgeFeedback(1.0f);
        }
    }

    public void PreviousPage()
    {
        if (!_isOpen || _isAnimating || _spreadTextures == null)
            return;

        if (_currentSpreadIndex > 0)
        {
            FlipToPage(_currentSpreadIndex - 1);
        }
        else
        {
            CloseBook(animate: true);
        }
    }

    public void FlipToPage(int targetIndex)
    {
        if (!_isOpen || _isAnimating || _bookSprite == null || _spreadTextures == null)
            return;

        targetIndex = Mathf.Clamp(targetIndex, 0, _spreadTextures.Length - 1);
        if (targetIndex == _currentSpreadIndex)
            return;

        bool forward = targetIndex > _currentSpreadIndex;
        int[] frameOrder = forward ? new[] { 0, 1, 2, 3, 4 } : new[] { 4, 3, 2, 1, 0 };

        _animTween?.Kill();
        _isAnimating = true;

        _animTween = CreateTween();
        _animTween.SetParallel(false);

        for (int i = 0; i < frameOrder.Length; i++)
        {
            int frameIdx = frameOrder[i];
            float liftY = (i == 2) ? 1.03f : (i == 1 || i == 3 ? 1.015f : 1.0f);

            _animTween.TweenCallback(Callable.From(() => SetFlipFrameSafe(frameIdx)));
            if (_bookPivot != null)
            {
                _animTween.TweenProperty(_bookPivot, "scale:y", liftY, _flipFrameDuration);
            }
            else
            {
                _animTween.TweenInterval(_flipFrameDuration);
            }
        }

        _animTween.TweenCallback(Callable.From(() =>
        {
            _currentSpreadIndex = targetIndex;
            _bookSprite.Texture = _spreadTextures[_currentSpreadIndex];
            if (_bookPivot != null) _bookPivot.Scale = Vector2.One;
            _isAnimating = false;
        }));
    }

    private void SetFlipFrameSafe(int index)
    {
        if (_bookSprite != null && _flipTextures != null && index >= 0 && index < _flipTextures.Length && _flipTextures[index] != null)
        {
            _bookSprite.Texture = _flipTextures[index];
        }
    }

    private void UpdateHitboxSize(Vector2 size)
    {
        if (_hitboxShape != null)
        {
            _hitboxShape.Size = size;
        }
    }

    private void PlayDockedNudgeFeedback()
    {
        if (_bookPivot == null) return;
        _hoverTween?.Kill();
        _hoverTween = CreateTween();
        _hoverTween.TweenProperty(_bookPivot, "position", new Vector2(12.0f, -12.0f), 0.08f)
                   .SetTrans(Tween.TransitionType.Quad)
                   .SetEase(Tween.EaseType.Out);
        _hoverTween.TweenProperty(_bookPivot, "position", Vector2.Zero, 0.12f)
                   .SetTrans(Tween.TransitionType.Bounce)
                   .SetEase(Tween.EaseType.Out);
    }

    private void PlayEdgeFeedback(float direction)
    {
        if (_bookPivot == null) return;
        _hoverTween?.Kill();
        _hoverTween = CreateTween();
        _hoverTween.TweenProperty(_bookPivot, "position:x", direction * 5.0f, 0.05f);
        _hoverTween.TweenProperty(_bookPivot, "position:x", 0.0f, 0.08f);
    }
}
