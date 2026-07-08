#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

public class RuntimeDebugHotkeys : MonoBehaviour
{
    private Keyboard subscribedKeyboard;
    private bool cedillaPressedFromText;

    private void OnDisable()
    {
        UnsubscribeKeyboard();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        EnsureKeyboardSubscription(keyboard);

        if (keyboard.f8Key.wasPressedThisFrame)
        {
            UIManager.Instance?.DebugAdvanceStoryReport();
        }

        if (WasCedillaPressed(keyboard))
        {
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            gameManager?.DebugJumpToOnePackageBeforeVictory();
        }
    }

    private bool WasCedillaPressed(Keyboard keyboard)
    {
        bool wasPressed = keyboard.semicolonKey.wasPressedThisFrame || cedillaPressedFromText;
        cedillaPressedFromText = false;
        return wasPressed;
    }

    private void EnsureKeyboardSubscription(Keyboard keyboard)
    {
        if (subscribedKeyboard == keyboard)
        {
            return;
        }

        UnsubscribeKeyboard();
        subscribedKeyboard = keyboard;
        subscribedKeyboard.onTextInput += HandleTextInput;
    }

    private void UnsubscribeKeyboard()
    {
        if (subscribedKeyboard == null)
        {
            return;
        }

        subscribedKeyboard.onTextInput -= HandleTextInput;
        subscribedKeyboard = null;
    }

    private void HandleTextInput(char character)
    {
        if (character == 'ç' || character == 'Ç')
        {
            cedillaPressedFromText = true;
        }
    }
}
#endif
