using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class SprintRegressionVerification
{
    [MenuItem("Ya Casi/Pruebas/Verificar liberación de correr")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Sal de Play antes de verificar correr."); return; }
        Keyboard originalKeyboard = Keyboard.current;
        Gamepad originalGamepad = Gamepad.current;
        Keyboard keyboard = null;
        Gamepad gamepad = null;
        GameObject go = null, button = null;
        var results = new List<string>();
        string report = "LibraryRedesignBackups/SprintRegression.txt";
        try
        {
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            gamepad = InputSystem.AddDevice<Gamepad>(); gamepad.MakeCurrent();
            go = new GameObject("Sprint regression (temporal)", typeof(Rigidbody2D), typeof(Animator));
            var player = go.AddComponent<PlayerMovement>();
            Call(player, "Awake");
            Set(keyboard, new KeyboardState(Key.W, Key.LeftShift)); Call(player, "Update");
            Check(player.IsRunning, "Shift + W activa correr", results);
            Set(keyboard, new KeyboardState(Key.W)); Call(player, "Update");
            Check(!player.IsRunning, "Soltar Shift vuelve a caminar manteniendo W", results);
            Set(keyboard, new KeyboardState(Key.W, Key.RightShift)); Call(player, "Update");
            Check(player.IsRunning, "Shift derecho también activa correr", results);
            Call(player, "OnDisable"); Set(keyboard, new KeyboardState(Key.W)); Call(player, "Update");
            Check(!player.IsRunning, "Soltar Shift durante desactivación no deja carrera pegada", results);
            Set(keyboard, new KeyboardState(Key.W, Key.LeftShift)); Call(player, "Update");
            Call(player, "OnApplicationFocus", false);
            Check(!player.IsRunning, "Perder foco limpia correr", results);
            Set(keyboard, new KeyboardState(Key.W)); Call(player, "OnApplicationFocus", true); Call(player, "Update");
            Check(!player.IsRunning, "Recuperar foco no reactiva una tecla soltada", results);
            Set(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right, rightTrigger = 1 });
            InputSystem.Update(); Call(player, "Update");
            Check(player.IsRunning, "Gatillo mantenido activa correr", results);
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right });
            InputSystem.Update(); Call(player, "Update");
            Check(!player.IsRunning, "Soltar gatillo vuelve a caminar", results);
            button = new GameObject("Sprint touch regression (temporal)");
            var touch = button.AddComponent<TouchGameControls>(); touch.sprint = true;
            var pointer = new PointerEventData(null);
            touch.OnPointerDown(pointer); Call(player, "Update");
            Check(player.IsRunning, "Botón táctil mantenido activa correr", results);
            touch.OnPointerExit(pointer); Call(player, "Update");
            Check(!player.IsRunning && !TouchGameControls.Sprint, "Salir del botón táctil libera correr", results);
            touch.OnPointerDown(pointer); touch.OnPointerUp(pointer); Call(player, "Update");
            Check(!player.IsRunning, "Soltar botón táctil libera correr", results);
            results.Add("RESULTADO: PASS (11 comprobaciones)");
            Debug.Log("SPRINT_REGRESSION_PASS: 11 comprobaciones correctas.");
        }
        catch (Exception error)
        {
            results.Add("FAIL: " + error);
            Debug.LogError("SPRINT_REGRESSION_FAIL: " + error);
        }
        finally
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
            if (button != null) UnityEngine.Object.DestroyImmediate(button);
            TouchGameControls.Direction = Vector2.zero; TouchGameControls.Sprint = false;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (gamepad != null) InputSystem.RemoveDevice(gamepad);
            if (originalKeyboard != null) originalKeyboard.MakeCurrent();
            if (originalGamepad != null) originalGamepad.MakeCurrent();
            Directory.CreateDirectory("LibraryRedesignBackups");
            File.WriteAllText(report, string.Join("\n", results));
        }
    }
    static void Set(Keyboard keyboard, KeyboardState state)
    {
        InputSystem.QueueStateEvent(keyboard, state); InputSystem.Update();
    }
    static void Call(object target, string name, params object[] args) => target.GetType()
        .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    static void Check(bool condition, string caption, List<string> results)
    {
        if (!condition) throw new InvalidOperationException(caption);
        results.Add("PASS: " + caption);
    }
}
