using AetherVault.Services;

namespace AetherVault.Controls;

internal sealed class CardGridGestureHandler
{
    /// <summary>Minimum list scroll (Y) between touch down and up to treat the gesture as a scroll, not a tap.</summary>
    private const double ScrollDeltaSuppressTap = 8d;

    /// <summary>Squared distance (DIP) finger must move from press point to cancel a tap (thumb slop).</summary>
    private const float TapCancelDistanceSquared = 8f * 8f;

    /// <summary>Axis drift (DIP) that cancels long-press arming so ScrollView can take the pan.</summary>
    private const float LongPressCancelAxisSlop = 5f;

    public event Action<string>? Tapped;
    public event Action<string>? LongPressed;

    // Drag-and-drop events
    public event Action<string, int>? DragStarted;   // (uuid, sourceIndex)
    public event Action<float, float>? DragMoved;    // (canvasX, canvasY)
    public event Action? DragEnded;
    public event Action? DragCancelled;

    // Callbacks wired by GestureSpacerView to dynamically control ScrollView
    // interception via AppoMobi.Maui.Gestures WIllLock.
    internal Action? DisallowScrollIntercept;
    internal Action? AllowScrollIntercept;
    /// <summary>Reset share-touch state at pointer down (see SwipeGestureContainer).</summary>
    internal Action? ResetScrollShareForGestureStart;

    public bool IsDragEnabled { get; set; } = true;

    private enum GestureState { Idle, PressTracking, DragArmed, Dragging }

    private IDispatcherTimer? _longPressTimer;
    private Point _pressPoint;
    private bool _hasMovedBeyondTapThreshold;
    private GestureState _gestureState = GestureState.Idle;
    private string? _armedUuid;
    private int _armedIndex;

    private readonly IDispatcher _dispatcher;
    private readonly Func<float, float, (string? uuid, int index)> _hitTest;
    private readonly Func<double> _getScrollY;

    private double _scrollYAtPress;
    private bool _scrolledSinceDown;

    public CardGridGestureHandler(
        IDispatcher dispatcher,
        Func<float, float, (string? uuid, int index)> hitTest,
        Func<double> getScrollY)
    {
        _dispatcher = dispatcher;
        _hitTest = hitTest;
        _getScrollY = getScrollY;
        // Touch events are delivered by GestureSpacerView via OnGestureEvent.
    }

    // ── Platform-agnostic gesture state machine ───────────────────────────────

    /// <summary>
    /// Called by CardGrid whenever the ScrollView fires a Scrolled event.
    /// Marks that a real scroll occurred so HandleUp can suppress accidental taps
    /// even when ScrollY hasn't updated yet at the moment the finger lifts.
    /// </summary>
    internal void NotifyScrolled()
    {
        if (_gestureState == GestureState.PressTracking)
            _scrolledSinceDown = true;
    }

    internal void HandleDown(float x, float y)
    {
        // Unlocked clears a stale Locked state; Initial lets the parent ScrollView
        // compete for vertical pans (same pattern as SwipeGestureContainer).
        ResetScrollShareForGestureStart?.Invoke();

        _pressPoint = new Point(x, y);
        _hasMovedBeyondTapThreshold = false;
        _scrollYAtPress = _getScrollY();
        _scrolledSinceDown = false;
        _gestureState = GestureState.PressTracking;
        _armedUuid = null;
        _armedIndex = -1;

        _longPressTimer?.Stop();
        _longPressTimer = _dispatcher.CreateTimer();
        _longPressTimer.Interval = TimeSpan.FromMilliseconds(500);
        _longPressTimer.IsRepeating = false;
        _longPressTimer.Tick += (s, args) =>
        {
            if (_gestureState == GestureState.PressTracking)
            {
                var (uuid, index) = _hitTest((float)_pressPoint.X, (float)_pressPoint.Y);
                if (uuid != null)
                {
                    _armedUuid = uuid;
                    _armedIndex = index;
                    _gestureState = GestureState.DragArmed;
                    // Tell the ScrollView not to intercept subsequent moves so the
                    // drag gesture can proceed without the scroll view stealing events.
                    DisallowScrollIntercept?.Invoke();
                    try { HapticFeedback.Perform(HapticFeedbackType.LongPress); }
                    catch (Exception ex) { Logger.LogStuff($"Haptic feedback failed: {ex.Message}", LogLevel.Debug); }
                }
                else
                {
                    _gestureState = GestureState.Idle;
                }
            }
        };
        _longPressTimer.Start();
    }

    internal void HandleMove(float x, float y)
    {
        switch (_gestureState)
        {
            case GestureState.PressTracking:
                {
                    float dx = x - (float)_pressPoint.X;
                    float dy = y - (float)_pressPoint.Y;
                    if (dx * dx + dy * dy > TapCancelDistanceSquared)
                        _hasMovedBeyondTapThreshold = true;
                }

                // Cancel long-press if pointer drifts (lets the ScrollView scroll)
                if (Math.Abs(x - _pressPoint.X) > LongPressCancelAxisSlop || Math.Abs(y - _pressPoint.Y) > LongPressCancelAxisSlop)
                {
                    _gestureState = GestureState.Idle;
                    _longPressTimer?.Stop();
                }
                break;

            case GestureState.DragArmed:
                // Transition to dragging once the finger moves from the hold point
                if (Math.Abs(x - _pressPoint.X) > 8 || Math.Abs(y - _pressPoint.Y) > 8)
                {
                    if (!IsDragEnabled) return;

                    var uuid = _armedUuid!;
                    var index = _armedIndex;
                    _gestureState = GestureState.Dragging;
                    MainThread.BeginInvokeOnMainThread(() => DragStarted?.Invoke(uuid, index));
                    MainThread.BeginInvokeOnMainThread(() => DragMoved?.Invoke(x, y));
                }
                break;

            case GestureState.Dragging:
                MainThread.BeginInvokeOnMainThread(() => DragMoved?.Invoke(x, y));
                break;
        }
    }

    internal void HandleUp()
    {
        switch (_gestureState)
        {
            case GestureState.PressTracking:
                // Quick tap: only if finger stayed within slop and the list did not scroll
                // (ScrollView often consumes pans, so we may not see pointer moves during scroll).
                _gestureState = GestureState.Idle;
                _longPressTimer?.Stop();
                bool scrolled = _scrolledSinceDown || Math.Abs(_getScrollY() - _scrollYAtPress) >= ScrollDeltaSuppressTap;
                if (!_hasMovedBeyondTapThreshold && !scrolled)
                {
                    // ScrollY and Scrolled often update one or more UI ticks after finger-up when
                    // the parent ScrollView owns the pan. Re-check after two main-thread posts so
                    // we do not open CardDetail on a scroll that committed just after HandleUp.
                    var tapPoint = _pressPoint;
                    double scrollSnap = _getScrollY();
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (Math.Abs(_getScrollY() - scrollSnap) >= ScrollDeltaSuppressTap) return;
                            var (uuid, _) = _hitTest((float)tapPoint.X, (float)tapPoint.Y);
                            if (uuid != null) Tapped?.Invoke(uuid);
                        });
                    });
                }
                break;

            case GestureState.DragArmed:
                // Long-press without drag → open quantity sheet
                var armedUuid = _armedUuid;
                _gestureState = GestureState.Idle;
                _longPressTimer?.Stop();
                AllowScrollIntercept?.Invoke();
                if (armedUuid != null)
                    MainThread.BeginInvokeOnMainThread(() => LongPressed?.Invoke(armedUuid));
                break;

            case GestureState.Dragging:
                _gestureState = GestureState.Idle;
                AllowScrollIntercept?.Invoke();
                MainThread.BeginInvokeOnMainThread(() => DragEnded?.Invoke());
                break;

            default:
                _gestureState = GestureState.Idle;
                _longPressTimer?.Stop();
                break;
        }
    }

    internal void HandleCancel()
    {
        _longPressTimer?.Stop();

        if (_gestureState == GestureState.Dragging)
        {
            _gestureState = GestureState.Idle;
            AllowScrollIntercept?.Invoke();
            MainThread.BeginInvokeOnMainThread(() => DragCancelled?.Invoke());
        }
        else
        {
            // Also unlock scroll for DragArmed (timer fired while app was backgrounded)
            // and any other interrupted state.
            _gestureState = GestureState.Idle;
            AllowScrollIntercept?.Invoke();
        }
    }
}
