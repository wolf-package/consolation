using System;
using System.Collections.Generic;
using UnityEngine;


namespace Consolation
{
    /// <summary>
    /// A console to display Unity's debug logs in-game.
    ///
    /// Version: 1.4.2
    /// </summary>
    public class ConsoleInGame : MonoBehaviour
    {
        #region Inspector Settings

        [Tooltip("Hotkey to show and hide the console.")] [SerializeField]
        private KeyCode toggleKey = KeyCode.BackQuote;


        [Tooltip("Whether to open as soon as the game starts.")] [SerializeField]
        private bool openOnStart;

        [Tooltip("Whether to open the window by shaking the device (mobile-only).")] [SerializeField]
        private bool shakeToOpen = true;

        [Tooltip("Whether to require touches while shaking to avoid accidental shakes.")] [SerializeField]
        private bool shakeRequiresTouch;

        [Tooltip("Acceleration (squared) above which to open the console.")] [SerializeField]
        private float shakeAcceleration = 3f;

        [Tooltip(
            "Number of seconds that need to pass between visibility toggles. This threshold prevents closing again while shaking to open.")]
        [SerializeField]
        private float toggleThresholdSeconds = .5f;

        [Tooltip("Whether to keep a limited number of logs. Useful if memory usage is a concern.")] [SerializeField]
        private bool restrictLogCount;

        [Tooltip("Number of logs to keep before removing old ones.")] [SerializeField]
        private int maxLogCount = 1000;

        [Tooltip("Whether log messages are collapsed by default or not.")] [SerializeField]
        private bool collapseLogOnStart;

        [Tooltip("Font size to display log entries with.")] [SerializeField]
        private int logFontSize = 12;

        [Tooltip("Amount to scale UI by.")] [SerializeField]
        private float scaleFactor = 1f;

        [SerializeField] private bool isCustomSizeWindowConsole = true;
        [Range(600, 1080)] [SerializeField] private float customWidth = 600;
        [Range(500, 1920)] [SerializeField] private float customHeight = 500;

        [Tooltip("Custom styles to apply to window.")]
        public GUISkin skin;

        #endregion

        static readonly GUIContent clearLabel = new GUIContent("Clear", "Clear contents of console.");
        static readonly GUIContent onlyLastLogLabel = new GUIContent("Only Last Log", "Show only most recent log.");
        static readonly GUIContent collapseLabel = new GUIContent("Collapse", "Hide repeated messages.");
        static GUIStyle dividerStyle;
        private static ConsoleInGame ins;
        const int margin = 20;
        const string windowTitle = "Console";

        static readonly Dictionary<LogType, Color> logTypeColors = new Dictionary<LogType, Color>
        {
            { LogType.Assert, Color.white },
            { LogType.Error, Color.red },
            { LogType.Exception, Color.red },
            { LogType.Log, Color.white },
            { LogType.Warning, Color.yellow },
        };

        bool isCollapsed;
        bool isOnlyLastLogVisible;
        bool isVisible;
        float lastToggleTime;
        readonly List<Log> logs = new List<Log>();
        Vector2 logsScrollPosition;
        readonly ConcurrentQueue<Log> queuedLogs = new ConcurrentQueue<Log>();
        int? selectedLogIndex;
        Vector2 stackTraceScrollPosition;
        readonly Rect titleBarRect = new Rect(0, 0, 10000, 20);
        Rect windowRect = new Rect(margin, margin, 0, 0);


        readonly Dictionary<LogType, bool> logTypeFilters = new Dictionary<LogType, bool>
        {
            { LogType.Assert, true },
            { LogType.Error, true },
            { LogType.Exception, true },
            { LogType.Log, true },
            { LogType.Warning, true },
        };

        #region API

        public static bool ShakeToOpen
        {
            get => ins.shakeToOpen;
            set => ins.shakeToOpen = value;
        }

        public static bool ShakeRequiresTouch
        {
            get => ins.shakeRequiresTouch;
            set => ins.shakeRequiresTouch = value;
        }

        public static float ShakeAcceleration
        {
            get => ins.shakeAcceleration;
            set => ins.shakeAcceleration = value;
        }

        public static float ToggleThresholdSeconds
        {
            get => ins.toggleThresholdSeconds;
            set => ins.toggleThresholdSeconds = value;
        }

        public static bool RestrictLogCount
        {
            get => ins.restrictLogCount;
            set => ins.restrictLogCount = value;
        }

        public static int MaxLogCount
        {
            get => ins.maxLogCount;
            set => ins.maxLogCount = value;
        }

        public static bool CollapseLogOnStart
        {
            get => ins.collapseLogOnStart;
            set => ins.collapseLogOnStart = value;
        }

        public static int LogFontSize
        {
            get => ins.logFontSize;
            set => ins.logFontSize = value;
        }

        public static float ScaleFactor
        {
            get => ins.scaleFactor;
            set => ins.scaleFactor = value;
        }

        public static bool IsCustomSizeWindowConsole
        {
            get => ins.isCustomSizeWindowConsole;
            set => ins.isCustomSizeWindowConsole = value;
        }

        public static float CustomWidth
        {
            get => ins.customWidth;
            set => ins.customWidth = value;
        }

        public static float CustomHeight
        {
            get => ins.customHeight;
            set => ins.customHeight = value;
        }

        public static void Activate(bool isShow)
        {
            ins.isVisible = isShow;
        }

        #endregion


        #region MonoBehaviour Messages

        private void Awake()
        {
            if (ins == null)
            {
                ins = this;
            }
        }

        void OnEnable()
        {
            Application.logMessageReceivedThreaded += HandleLogThreaded;
        }

        void OnDisable()
        {
            Application.logMessageReceivedThreaded -= HandleLogThreaded;
        }

        void OnGUI()
        {
            if (!isVisible)
            {
                return;
            }

            var previousGUISkin = GUI.skin;

            if (skin != null)
            {
                GUI.skin = skin;
            }

            GUI.matrix = Matrix4x4.Scale(Vector3.one * scaleFactor);
            windowRect.width = isCustomSizeWindowConsole
                ? customWidth
                : Screen.width / scaleFactor - margin * 2;
            windowRect.height = isCustomSizeWindowConsole
                ? customHeight
                : Screen.height / scaleFactor - margin * 2;
            windowRect = GUILayout.Window(123456, windowRect, DrawWindow, windowTitle);
            //Debug.Log(windowRect.width + "/" + windowRect.height);
            GUI.skin = previousGUISkin;
        }

        void Start()
        {
            if (collapseLogOnStart)
            {
                isCollapsed = true;
            }

            if (openOnStart)
            {
                isVisible = true;
            }

            if (shakeRequiresTouch)
            {
                EnableMultiTouch();
            }

            // Unity complains if we define this style at the point of declaration.
            dividerStyle = new GUIStyle
            {
                fixedHeight = 1,
                margin = new RectOffset(0, 0, 4, 4),
                normal = { background = Texture2D.whiteTexture },
            };
        }

        void Update()
        {
            UpdateQueuedLogs();

            if (WasToggleKeyPressed())
            {
                isVisible = !isVisible;
            }

            if (shakeToOpen &&
                Time.realtimeSinceStartup - lastToggleTime >= toggleThresholdSeconds &&
                WasShaken() &&
                (!shakeRequiresTouch || WasMultiTouchThresholdExceeded()))
            {
                isVisible = !isVisible;
                lastToggleTime = Time.realtimeSinceStartup;
            }
        }

        #endregion

        void DrawLog(int logIndex, GUIStyle logStyle)
        {
            var log = logs[logIndex];

            GUI.contentColor = logTypeColors[log.Type];

            if (isCollapsed)
            {
                // Draw collapsed log with badge indicating count.
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(log.Message, logStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(log.Count.ToString(), GUI.skin.box);
                }
                GUILayout.EndHorizontal();

                DrawLogSelectButton(logIndex);
            }
            else
            {
                var labelCount = isOnlyLastLogVisible ? 1 : log.Count;

                for (var i = 0; i < labelCount; i += 1)
                {
                    GUILayout.Label(log.Message, logStyle);
                    DrawLogSelectButton(logIndex);
                }
            }

            GUI.contentColor = Color.white;
        }

        void DrawLogList()
        {
            var logStyle = GUI.skin.label;
            logStyle.fontSize = logFontSize;

            logsScrollPosition = GUILayout.BeginScrollView(logsScrollPosition);

            // Used to determine height of accumulated log labels.
            GUILayout.BeginVertical();
            {
                if (isOnlyLastLogVisible)
                {
                    var lastVisibleLogIndex = GetLastVisibleLogIndex();

                    if (lastVisibleLogIndex.HasValue)
                    {
                        DrawLog(lastVisibleLogIndex.Value, logStyle);
                    }
                }
                else
                {
                    for (var logIndex = 0; logIndex < logs.Count; logIndex++)
                    {
                        if (!IsLogVisible(logIndex))
                        {
                            continue;
                        }

                        DrawLog(logIndex, logStyle);
                    }
                }
            }
            GUILayout.EndVertical();

            var innerScrollRect = GUILayoutUtility.GetLastRect();
            GUILayout.EndScrollView();
            var outerScrollRect = GUILayoutUtility.GetLastRect();

            // If we're scrolled to bottom now, guarantee that it continues to be in next cycle.
            if (Event.current.type == EventType.Repaint && IsScrolledToBottom(innerScrollRect, outerScrollRect))
            {
                ScrollToBottom();
            }
        }

        void DrawLogSelectButton(int logIndex)
        {
            var lastRect = GUILayoutUtility.GetLastRect();

            if (GUI.Button(lastRect, GUIContent.none, GUIStyle.none))
            {
                selectedLogIndex = logIndex;
                stackTraceScrollPosition = Vector2.zero;
            }
        }

        void DrawStackTrace()
        {
            if (!selectedLogIndex.HasValue)
            {
                return;
            }

            GUILayout.Box(GUIContent.none, dividerStyle);

            // GUILayout shrinks the stack trace scroll view every time a new log gets added.
            // We seem to need to set a fixed height to work around this.
            var scrollViewHeight = windowRect.height / 2;

            stackTraceScrollPosition =
                GUILayout.BeginScrollView(stackTraceScrollPosition, GUILayout.Height(scrollViewHeight));
            {
                var selectedLog = logs[selectedLogIndex.Value];
                GUILayout.Label(selectedLog.Message);
                GUILayout.Label(selectedLog.StackTrace);
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Hide Stack Trace", GUILayout.ExpandWidth(false)))
            {
                selectedLogIndex = null;
            }
        }

        void DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(clearLabel))
                {
                    logs.Clear();
                    selectedLogIndex = null;
                }

                foreach (LogType logType in Enum.GetValues(typeof(LogType)))
                {
                    var currentState = logTypeFilters[logType];
                    var label = logType.ToString();
                    logTypeFilters[logType] = GUILayout.Toggle(currentState, label, GUILayout.ExpandWidth(false));
                    GUILayout.Space(20);
                }

                isCollapsed = GUILayout.Toggle(isCollapsed, collapseLabel, GUILayout.ExpandWidth(false));
                isOnlyLastLogVisible =
                    GUILayout.Toggle(isOnlyLastLogVisible, onlyLastLogLabel, GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();
        }

        void DrawWindow(int windowID)
        {
            DrawLogList();
            DrawStackTrace();
            DrawToolbar();

            // Allow the window to be dragged by its title bar.
            GUI.DragWindow(titleBarRect);
        }

        void UpdateQueuedLogs()
        {
            while (queuedLogs.TryDequeue(out var log))
            {
                ProcessLogItem(log);
            }
        }

        int? GetLastVisibleLogIndex()
        {
            for (var logIndex = logs.Count - 1; logIndex >= 0; logIndex--)
            {
                if (IsLogVisible(logIndex))
                {
                    return logIndex;
                }
            }

            return null;
        }

        void HandleLogThreaded(string message, string stackTrace, LogType type)
        {
            // Queue the log into a ConcurrentQueue to be processed later in the Unity main thread,
            // so that we don't get GUI-related errors for logs coming from other threads
            var log = new Log(message, stackTrace, type);
            queuedLogs.Enqueue(log);
        }

        void ProcessLogItem(Log log)
        {
            var lastLog = logs.Count > 0 ? logs[logs.Count - 1] : (Log?)null;
            var isDuplicateOfLastLog = lastLog.HasValue && log.Equals(lastLog.Value);

            if (isDuplicateOfLastLog)
            {
                // Replace previous log with incremented count instead of adding a new one.
                logs[logs.Count - 1] = lastLog.Value.IncrementedCount();
            }
            else
            {
                logs.Add(log);
                TrimExcessLogs();
            }
        }

        bool IsLogVisible(int logIndex)
        {
            var logType = logs[logIndex].Type;
            return logTypeFilters[logType];
        }

        bool IsScrolledToBottom(Rect innerScrollRect, Rect outerScrollRect)
        {
            var innerScrollHeight = innerScrollRect.height;

            // Take into account extra padding added to the scroll container.
            var outerScrollHeight = outerScrollRect.height - GUI.skin.box.padding.vertical;

            // If contents of scroll view haven't exceeded outer container, treat it as scrolled to bottom.
            if (outerScrollHeight > innerScrollHeight)
            {
                return true;
            }

            // Scrolled to bottom (with error margin for float math)
            return Mathf.Approximately(innerScrollHeight, logsScrollPosition.y + outerScrollHeight);
        }

        void ScrollToBottom()
        {
            logsScrollPosition = new Vector2(0, int.MaxValue);
        }

        void TrimExcessLogs()
        {
            if (!restrictLogCount)
            {
                return;
            }

            var amountToRemove = logs.Count - maxLogCount;

            if (amountToRemove <= 0)
            {
                return;
            }

            logs.RemoveRange(0, amountToRemove);
        }

        bool WasMultiTouchThresholdExceeded()
        {
            var touchCount = Input.touchCount;
            return touchCount > 2;
        }

        bool WasShaken()
        {
            var acceleration = Input.acceleration;
            return acceleration.sqrMagnitude > shakeAcceleration;
        }

        bool WasToggleKeyPressed()
        {
            return Input.GetKeyDown(toggleKey);
        }

        static void EnableMultiTouch()
        {
            Input.multiTouchEnabled = true;
        }
    }

    /// <summary>
    /// A basic container for log details.
    /// </summary>
    readonly struct Log
    {
        public readonly int Count;
        public readonly string Message;
        public readonly string StackTrace;
        public readonly LogType Type;

        public Log(string message, string stackTrace, LogType type)
        {
            Count = 1;
            Message = TruncateForGUILabel(message);
            StackTrace = TruncateForGUILabel(stackTrace);
            Type = type;
        }

        Log(string message, string stackTrace, LogType type, int count)
        {
            Count = count;
            Message = message;
            StackTrace = stackTrace;
            Type = type;
        }

        public bool Equals(Log log)
        {
            return Message == log.Message && StackTrace == log.StackTrace && Type == log.Type;
        }

        public Log IncrementedCount()
        {
            return new Log(Message, StackTrace, Type, Count + 1);
        }

        /// <summary>
        /// Returns text shortened to fit in a GUILayout.Label.
        /// </summary>
        static string TruncateForGUILabel(string text)
        {
            // The max string length supported by UnityEngine.GUILayout.Label without triggering this error:
            // "String too long for TextMeshGenerator. Cutting off characters."
            const int maxLabelLength = 16382;

            return string.IsNullOrEmpty(text) || text.Length <= maxLabelLength
                ? text
                : text.Substring(0, maxLabelLength);
        }
    }

    /// <summary>
    /// Alternative to System.Collections.Concurrent.ConcurrentQueue
    /// (It's only available in .NET 4.0 and greater)
    /// </summary>
    /// <remarks>
    /// It's a bit slow (as it uses locks), and only provides a small subset of the interface
    /// Overall, the implementation is intended to be simple & robust
    /// </remarks>
    class ConcurrentQueue<T>
    {
        readonly Queue<T> queue = new Queue<T>();
        readonly object queueLock = new object();

        public void Enqueue(T item)
        {
            lock (queueLock)
            {
                queue.Enqueue(item);
            }
        }

        public bool TryDequeue(out T result)
        {
            lock (queueLock)
            {
                if (queue.Count == 0)
                {
                    result = default(T);
                    return false;
                }

                result = queue.Dequeue();
                return true;
            }
        }
    }
}