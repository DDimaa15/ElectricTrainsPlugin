using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ElectricTrainsMods
{
    [BepInPlugin("com.electrictrains.mods", "Electric Trains Mods", "1.5.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> EnableAcceleration;
        public static ConfigEntry<float> ThrottleMultiplier;
        public static ConfigEntry<float> BrakeMultiplier;
        public static ConfigEntry<bool> EnableNoDerail;
        public static ConfigEntry<bool> EnableBlockerPass;
        public static ConfigEntry<bool> EnableInfinitePoints;
        public static ConfigEntry<bool> EnableNoTraffic;
        public static ConfigEntry<bool> EnableFreeCamera;
        public static ConfigEntry<float> CameraZoomMin;
        public static ConfigEntry<float> CameraZoomMax;
        public static ConfigEntry<float> CameraHeightMin;
        public static ConfigEntry<float> CameraHeightMax;
        public static ConfigEntry<bool> EnableUnlockFPS;
        public static ConfigEntry<bool> EnableCruiseSmoothing;
        public static ConfigEntry<float> CruiseMaxChangePerSecond;
        public static ConfigEntry<bool> EnableMapTeleport;
        public static ConfigEntry<bool> EnableTimeControl;
        public static ConfigEntry<KeyCode> TimeForwardKey;
        public static ConfigEntry<KeyCode> TimeBackwardKey;
        public static ConfigEntry<KeyCode> TimeFreezeKey;
        public static ConfigEntry<KeyCode> TimeSpeedUpKey;
        public static ConfigEntry<KeyCode> TimeSlowDownKey;
        public static ConfigEntry<float> TimeStep;
        public static ConfigEntry<bool> EnableWeatherControl;
        public static ConfigEntry<KeyCode> WeatherRainKey;
        public static ConfigEntry<KeyCode> WeatherClearKey;
        public static ConfigEntry<KeyCode> WeatherCycleKey;

        public static MainTrain CurrentTrain;
        private static controls CtrlInstance;
        internal static ManualLogSource Log;

        // Time Control state
        private static bool timeFrozen = false;
        private static float timeSpeedMult = 1f;
        private static readonly float defaultDayNight = 0.0027777778f;
        private static readonly float defaultSunrise = 0.0055555557f;

        // Weather Control state
        // 0 = авто, 1 = ясно, 2 = дождь, 3 = снег
        private static int weatherMode = 0;

        private static FieldInfo f_minimap_enabled = AccessTools.Field(typeof(controls), "minimap_enabled");
        private static FieldInfo f_Minimapcamera = AccessTools.Field(typeof(controls), "Minimapcamera");
        private static FieldInfo f_timeOfDay = AccessTools.Field(typeof(controls), "timeOfDay");
        private static FieldInfo f_dayNightDuration = AccessTools.Field(typeof(controls), "dayNightDuration");
        private static FieldInfo f_sunriseSunsetDuration = AccessTools.Field(typeof(controls), "sunriseSunsetDuration");
        private static FieldInfo f_sediment = AccessTools.Field(typeof(controls), "sediment");
        private static FieldInfo f_season = AccessTools.Field(typeof(controls), "season");
        private static MethodInfo m_setTimeOfTheDay = AccessTools.Method(typeof(controls), "SetTimeOfTheDay");
        private static MethodInfo m_enableSediment = AccessTools.Method(typeof(Sediment), "enableSediment");
        private static MethodInfo m_disableSediment = AccessTools.Method(typeof(Sediment), "disableSediment");

        private void Awake()
        {
            EnableAcceleration = Config.Bind("1. Acceleration", "Enabled", true, "Enable acceleration multiplier");
            ThrottleMultiplier = Config.Bind("1. Acceleration", "Throttle Multiplier", 1f, "Throttle force multiplier");
            BrakeMultiplier = Config.Bind("1. Acceleration", "Brake Multiplier", 1f, "Brake force multiplier");
            EnableNoDerail = Config.Bind("2. No Derail", "Enabled", true, "Disable train derailment");
            EnableBlockerPass = Config.Bind("3. Barrier Pass", "Enabled", true, "Pass through red barriers");
            EnableInfinitePoints = Config.Bind("4. Infinite Points", "Enabled", true, "Set points to 999999999");
            EnableNoTraffic = Config.Bind("5. No Traffic", "Enabled", true, "Disable oncoming trains");
            EnableFreeCamera = Config.Bind("6. Free Camera", "Enabled", true, "Extended camera limits");
            CameraZoomMin = Config.Bind("6. Free Camera", "Zoom Min", 5f, "Minimum zoom");
            CameraZoomMax = Config.Bind("6. Free Camera", "Zoom Max", 179f, "Maximum zoom");
            CameraHeightMin = Config.Bind("6. Free Camera", "Height Min", -500f, "Minimum height");
            CameraHeightMax = Config.Bind("6. Free Camera", "Height Max", 2000f, "Maximum height");
            EnableUnlockFPS = Config.Bind("7. FPS Unlock", "Enabled", true, "Remove frame rate cap");
            EnableCruiseSmoothing = Config.Bind("8. Smooth Cruise Control", "Enabled", true, "Smooth cruise control changes");
            CruiseMaxChangePerSecond = Config.Bind("8. Smooth Cruise Control", "Smoothness", 5f, "Max engine_force_cur change per second");
            EnableMapTeleport = Config.Bind("9. Map Teleport", "Enabled", true, "RMB on minimap to teleport train");
            EnableTimeControl = Config.Bind("10. Time Control", "Enabled", true, "Control time of day");
            TimeForwardKey = Config.Bind("10. Time Control", "Time Forward Key", KeyCode.PageUp, "Advance time of day");
            TimeBackwardKey = Config.Bind("10. Time Control", "Time Backward Key", KeyCode.PageDown, "Rewind time of day");
            TimeFreezeKey = Config.Bind("10. Time Control", "Freeze Toggle Key", KeyCode.F8, "Toggle time freeze");
            TimeSpeedUpKey = Config.Bind("10. Time Control", "Speed Up Key", KeyCode.F9, "Speed up time (x2 each press)");
            TimeSlowDownKey = Config.Bind("10. Time Control", "Slow Down Key", KeyCode.F10, "Slow down time (div2 each press)");
            TimeStep = Config.Bind("10. Time Control", "Time Step", 0.1f, "How much time shifts per keypress (0-4 range)");
            EnableWeatherControl = Config.Bind("11. Weather Control", "Enabled", true, "Control weather");
            WeatherRainKey = Config.Bind("11. Weather Control", "Force Precipitation Key", KeyCode.Keypad1, "Force rain or snow depending on season");
            WeatherClearKey = Config.Bind("11. Weather Control", "Clear Sky Key", KeyCode.Keypad3, "Clear sky");
            WeatherCycleKey = Config.Bind("11. Weather Control", "Cycle Weather Key", KeyCode.Keypad4, "Cycle: auto -> clear -> precipitation");

            TeleportHelper.Log = Logger;
            Log = Logger;

            var harmony = new Harmony("com.electrictrains.mods");
            if (EnableAcceleration.Value) harmony.PatchAll(typeof(Patch_Acceleration));
            if (EnableNoDerail.Value) harmony.PatchAll(typeof(Patch_NoDerail));
            if (EnableBlockerPass.Value) harmony.PatchAll(typeof(Patch_BlockerPass));
            if (EnableInfinitePoints.Value) harmony.PatchAll(typeof(Patch_InfinitePoints));
            if (EnableNoTraffic.Value)
            {
                harmony.PatchAll(typeof(Patch_NoTrafficV1));
                harmony.PatchAll(typeof(Patch_NoTrafficV2));
            }
            if (EnableFreeCamera.Value) harmony.PatchAll(typeof(Patch_CameraLimits));
            if (EnableUnlockFPS.Value) StartCoroutine(UnlockFPS());
            if (EnableCruiseSmoothing.Value) harmony.PatchAll(typeof(Patch_CruiseSmoothing));
            if (EnableMapTeleport.Value) harmony.PatchAll(typeof(Patch_CatchTrain));
            if (EnableWeatherControl.Value) harmony.PatchAll(typeof(Patch_WeatherControl));

            Logger.LogInfo("Electric Trains Mods v1.5.0 loaded!");
        }

        private void Update()
        {
            if (CtrlInstance == null)
                CtrlInstance = FindObjectOfType<controls>();

            // ========== Map Teleport ==========
            if (CurrentTrain != null && EnableMapTeleport.Value && Input.GetMouseButtonDown(1))
            {
                if (CtrlInstance == null) { Logger.LogInfo("[MapTeleport] controls не найден"); return; }

                bool mapOpen = f_minimap_enabled != null && (bool)f_minimap_enabled.GetValue(CtrlInstance);
                Logger.LogInfo($"[MapTeleport] mapOpen={mapOpen}, field={f_minimap_enabled != null}");
                if (!mapOpen) return;

                Camera cam = f_Minimapcamera?.GetValue(CtrlInstance) as Camera;
                Logger.LogInfo($"[MapTeleport] cam={cam}, field={f_Minimapcamera != null}");
                if (cam == null) return;

                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                Vector3 worldPos = ray.origin;
                worldPos.y = 0f;

                var pathanalizer = AccessTools.Field(typeof(MainTrain), "pathanalizer").GetValue(CurrentTrain);
                if (pathanalizer == null) return;

                var points = AccessTools.Field(pathanalizer.GetType(), "points").GetValue(pathanalizer) as Vector3[];
                if (points == null) return;

                int nearest = 0;
                float minDist = float.MaxValue;
                for (int i = 0; i < points.Length; i++)
                {
                    float dx = points[i].x - worldPos.x;
                    float dz = points[i].z - worldPos.z;
                    float d = dx * dx + dz * dz;
                    if (d < minDist) { minDist = d; nearest = i; }
                }

                Logger.LogInfo($"[MapTeleport] nearest point={nearest}, dist={Mathf.Sqrt(minDist)}");
                TeleportHelper.TeleportToPoint(CurrentTrain, nearest);
            }

            // ========== Time Control ==========
            if (EnableTimeControl.Value && CtrlInstance != null)
            {
                if (Input.GetKeyDown(TimeFreezeKey.Value))
                {
                    timeFrozen = !timeFrozen;
                    ApplyTimeSpeed();
                    Logger.LogInfo($"[TimeControl] Freeze={timeFrozen}");
                }
                if (Input.GetKeyDown(TimeSpeedUpKey.Value))
                {
                    timeSpeedMult = Mathf.Min(timeSpeedMult * 2f, 64f);
                    if (!timeFrozen) ApplyTimeSpeed();
                    Logger.LogInfo($"[TimeControl] Speed mult={timeSpeedMult}");
                }
                if (Input.GetKeyDown(TimeSlowDownKey.Value))
                {
                    timeSpeedMult = Mathf.Max(timeSpeedMult * 0.5f, 0.125f);
                    if (!timeFrozen) ApplyTimeSpeed();
                    Logger.LogInfo($"[TimeControl] Speed mult={timeSpeedMult}");
                }
                if (Input.GetKeyDown(TimeForwardKey.Value)) ShiftTime(TimeStep.Value);
                if (Input.GetKeyDown(TimeBackwardKey.Value)) ShiftTime(-TimeStep.Value);
            }

            // ========== Weather Control ==========
            if (EnableWeatherControl.Value && CtrlInstance != null)
            {
                if (Input.GetKeyDown(WeatherRainKey.Value))   SetWeather(2);
                if (Input.GetKeyDown(WeatherClearKey.Value))  SetWeather(1);
                if (Input.GetKeyDown(WeatherCycleKey.Value))  SetWeather(weatherMode == 0 ? 1 : weatherMode == 1 ? 2 : 0);
            }
        }

        private void ShiftTime(float delta)
        {
            if (CtrlInstance == null || f_timeOfDay == null) return;
            float current = (float)f_timeOfDay.GetValue(CtrlInstance);
            float newTime = (current + delta + 4f) % 4f;
            f_timeOfDay.SetValue(CtrlInstance, newTime);
            m_setTimeOfTheDay?.Invoke(CtrlInstance, new object[] { newTime });
            Logger.LogInfo($"[TimeControl] timeOfDay={newTime:F2}");
        }

        private void ApplyTimeSpeed()
        {
            if (f_dayNightDuration == null || f_sunriseSunsetDuration == null) return;
            if (timeFrozen)
            {
                f_dayNightDuration.SetValue(null, 0f);
                f_sunriseSunsetDuration.SetValue(null, 0f);
            }
            else
            {
                f_dayNightDuration.SetValue(null, defaultDayNight * timeSpeedMult);
                f_sunriseSunsetDuration.SetValue(null, defaultSunrise * timeSpeedMult);
            }
        }

        internal static void SetWeather(int mode)
        {
            weatherMode = mode;
            if (CtrlInstance == null || f_sediment == null) return;

            Sediment sed = f_sediment.GetValue(CtrlInstance) as Sediment;
            if (sed == null) return;

            switch (mode)
            {
                case 0: // Авто
                    Log?.LogInfo("[Weather] Auto");
                    break;

                case 1: // Ясно
                    sed.opacityMult = 0f;
                    m_disableSediment?.Invoke(sed, null);
                    Log?.LogInfo("[Weather] Clear sky");
                    break;

                case 2: // Осадки по сезону (зима=снег, иначе=дождь)
                    bool isWinter = f_season != null && (int)f_season.GetValue(CtrlInstance) == 2;
                    sed.isWinter = isWinter;
                    sed.opacityMult = 1f;
                    m_enableSediment?.Invoke(sed, null);
                    Log?.LogInfo($"[Weather] Precipitation (isWinter={isWinter})");
                    break;
            }
        }

        public static int GetWeatherMode() => weatherMode;

        private IEnumerator UnlockFPS()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                Application.targetFrameRate = -1;
                QualitySettings.vSyncCount = 0;
            }
        }
    }

    // ========== 1. Ускорение ==========
    [HarmonyPatch(typeof(MainTrain))]
    internal static class Patch_Acceleration
    {
        private static FieldInfo R_forceField = AccessTools.Field(typeof(MainTrain), "R_force");

        [HarmonyPatch("Force_Calculate_Electric"), HarmonyPostfix]
        static void Electric(MainTrain __instance) => Apply(__instance);
        [HarmonyPatch("Force_Calculate_Diesel"), HarmonyPostfix]
        static void Diesel(MainTrain __instance) => Apply(__instance);

        static void Apply(MainTrain t)
        {
            if (R_forceField == null) return;
            float force = (float)R_forceField.GetValue(t);
            if (force > 0) force *= Plugin.ThrottleMultiplier.Value;
            else force *= Plugin.BrakeMultiplier.Value;
            R_forceField.SetValue(t, force);
        }
    }

    // ========== 2. Отключение схода ==========
    [HarmonyPatch(typeof(MainTrain))]
    internal static class Patch_NoDerail
    {
        private static FieldInfo side_accelField = AccessTools.Field(typeof(MainTrain), "side_accel");

        [HarmonyPatch("FixedUpdate"), HarmonyPostfix]
        static void Postfix(MainTrain __instance)
        {
            if (side_accelField != null) side_accelField.SetValue(__instance, 0f);
        }
    }

    // ========== 3. Барьеры-тупики ==========
    [HarmonyPatch(typeof(Blocker))]
    internal static class Patch_BlockerPass
    {
        [HarmonyPatch("Start"), HarmonyPostfix]
        static void Start(Blocker __instance)
        {
            Collider col = __instance.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    // ========== 4. Бесконечные очки ==========
    [HarmonyPatch(typeof(MenuPlayerPrefs))]
    internal static class Patch_InfinitePoints
    {
        [HarmonyPatch("Awake"), HarmonyPostfix]
        static void Awake(MenuPlayerPrefs __instance)
        {
            var field = AccessTools.Field(typeof(MenuPlayerPrefs), "available_points_value");
            if (field != null) field.SetValue(__instance, 999999999);
        }
    }

    // ========== 5. Отключение трафика ==========
    [HarmonyPatch(typeof(DummyTrainGenerator))]
    internal static class Patch_NoTrafficV1
    {
        [HarmonyPatch("Awake"), HarmonyPrefix]
        static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(DummyTrainGenerator_v2))]
    internal static class Patch_NoTrafficV2
    {
        [HarmonyPatch("Awake"), HarmonyPrefix]
        static bool Prefix() => false;
    }

    // ========== 6. Расширенная камера ==========
    [HarmonyPatch(typeof(CameraControl))]
    internal static class Patch_CameraLimits
    {
        private static FieldInfo zoom = AccessTools.Field(typeof(CameraControl), "zoom");
        private static FieldInfo height = AccessTools.Field(typeof(CameraControl), "height");
        private static FieldInfo minzoom = AccessTools.Field(typeof(CameraControl), "minzoom");
        private static FieldInfo maxzoom = AccessTools.Field(typeof(CameraControl), "maxzoom");
        private static FieldInfo multPos = AccessTools.Field(typeof(CameraControl), "multPos");
        private static FieldInfo defPosStatic = AccessTools.Field(typeof(CameraControl), "defPosStatic");

        [HarmonyPatch("setcamparams_1"), HarmonyPostfix]
        static void SetParams(CameraControl __instance)
        {
            if (minzoom != null) minzoom.SetValue(__instance, Plugin.CameraZoomMin.Value);
            if (maxzoom != null) maxzoom.SetValue(__instance, Plugin.CameraZoomMax.Value);
            if (zoom != null)
            {
                float z = (float)zoom.GetValue(__instance);
                zoom.SetValue(__instance, Mathf.Clamp(z, Plugin.CameraZoomMin.Value, Plugin.CameraZoomMax.Value));
            }
            if (height != null)
            {
                float h = (float)height.GetValue(__instance);
                height.SetValue(__instance, Mathf.Clamp(h, Plugin.CameraHeightMin.Value, Plugin.CameraHeightMax.Value));
            }
        }

        [HarmonyPatch("Camera_1_Update"), HarmonyPostfix]
        static void UpdateCamera(CameraControl __instance)
        {
            if (multPos != null && defPosStatic != null)
                multPos.SetValue(__instance, (float)defPosStatic.GetValue(__instance));
        }
    }

    // ========== 8. Сглаживание круиз-контроля ==========
    [HarmonyPatch(typeof(MainTrain))]
    internal static class Patch_CruiseSmoothing
    {
        private static FieldInfo cruiseEnabled = AccessTools.Field(typeof(MainTrain), "cruise_control_enabled");
        private static FieldInfo engineForceCur = AccessTools.Field(typeof(MainTrain), "engine_force_cur");
        private static float lastEngineForce = 0f;
        private static bool hasLast = false;

        [HarmonyPatch("FixedUpdate"), HarmonyPostfix]
        static void Smooth(MainTrain __instance)
        {
            if (cruiseEnabled == null || engineForceCur == null) return;
            bool isCruising = (bool)cruiseEnabled.GetValue(__instance);
            if (!isCruising) { hasLast = false; return; }

            float current = (float)engineForceCur.GetValue(__instance);
            if (!hasLast) { lastEngineForce = current; hasLast = true; return; }

            float maxDelta = Plugin.CruiseMaxChangePerSecond.Value * Time.fixedDeltaTime;
            float newVal = Mathf.Clamp(current, lastEngineForce - maxDelta, lastEngineForce + maxDelta);
            if (Mathf.Abs(newVal - current) > 0.001f)
                engineForceCur.SetValue(__instance, newVal);
            lastEngineForce = newVal;
        }
    }

    // ========== 11. Погода — удерживаем opacityMult каждый кадр ==========
    // SedimentControl корутина каждую секунду перезаписывает opacityMult,
    // поэтому держим значение в Update самого плагина через корутину
    [HarmonyPatch(typeof(Sediment))]
    internal static class Patch_WeatherControl
    {
        private static FieldInfo f_season = AccessTools.Field(typeof(controls), "season");

        [HarmonyPatch("FixedUpdate"), HarmonyPostfix]
        static void KeepWeather(Sediment __instance)
        {
            if (!Plugin.EnableWeatherControl.Value) return;
            int mode = Plugin.GetWeatherMode();
            if (mode == 0) return;

            switch (mode)
            {
                case 1: // Ясно
                    __instance.opacityMult = 0f;
                    break;
                case 2: // Осадки по сезону
                    var ctrl = UnityEngine.Object.FindObjectOfType<controls>();
                    __instance.isWinter = ctrl != null && f_season != null && (int)f_season.GetValue(ctrl) == 2;
                    __instance.opacityMult = 1f;
                    break;
            }

            // Принудительно включаем sediment если выключен
            if (mode != 1 && !__instance.sedimentEnabled)
            {
                __instance.enableSediment();
            }
            else if (mode == 1 && __instance.sedimentEnabled)
            {
                __instance.disableSediment();
            }
        }
    }

    // ========== Перехват поезда ==========
    [HarmonyPatch(typeof(MainTrain))]
    internal static class Patch_CatchTrain
    {
        [HarmonyPatch("FixedUpdate"), HarmonyPostfix]
        static void CatchTrain(MainTrain __instance)
        {
            Plugin.CurrentTrain = __instance;
        }
    }

    internal static class TeleportHelper
    {
        public static ManualLogSource Log;

        private static FieldInfo f_pathanalizer = AccessTools.Field(typeof(MainTrain), "pathanalizer");
        private static FieldInfo f_cur = AccessTools.Field(typeof(MainTrain), "cur");
        private static FieldInfo f_increment = AccessTools.Field(typeof(MainTrain), "increment");
        private static FieldInfo f_pos = AccessTools.Field(typeof(MainTrain), "pos");
        private static FieldInfo f_points_passed = AccessTools.Field(typeof(MainTrain), "points_passed");
        private static FieldInfo f_points_passed_increment = AccessTools.Field(typeof(MainTrain), "points_passed_increment");
        private static MethodInfo m_setpos = AccessTools.Method(typeof(MainTrain), "setpos");
        private static MethodInfo m_reinit_block = AccessTools.Method(typeof(MainTrain), "reinit_block_points");
        private static MethodInfo m_reinit_passed = AccessTools.Method(typeof(MainTrain), "reinit_passed_points");

        public static void TeleportToPoint(MainTrain train, int targetPoint)
        {
            var pathanalizer = f_pathanalizer.GetValue(train);
            if (pathanalizer == null) return;

            var m_get_prev_point = AccessTools.Method(pathanalizer.GetType(), "get_prev_point");
            var f_points = AccessTools.Field(pathanalizer.GetType(), "points");
            Vector3[] points = f_points.GetValue(pathanalizer) as Vector3[];
            int[] points_passed = f_points_passed.GetValue(train) as int[];
            int[] points_passed_inc = f_points_passed_increment.GetValue(train) as int[];
            int increment = (int)f_increment.GetValue(train);

            int num2 = targetPoint;
            for (int i = points_passed.Length - 1; i >= 0; i--)
            {
                num2 = (int)m_get_prev_point.Invoke(pathanalizer, new object[] { num2, increment });
                points_passed[i] = num2;
                points_passed_inc[i] = increment;
            }
            f_points_passed.SetValue(train, points_passed);
            f_points_passed_increment.SetValue(train, points_passed_inc);

            f_cur.SetValue(train, targetPoint);
            if (points != null)
                f_pos.SetValue(train, points[targetPoint]);

            m_reinit_passed?.Invoke(train, null);
            m_reinit_block?.Invoke(train, null);
            m_setpos?.Invoke(train, null);
        }
    }
}
