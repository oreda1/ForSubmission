using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    private Controller inputActions;

 
    public float LeftStickX { get; private set; }    
    public float RightStickX { get; private set; }
    public float LeftStickY { get; private set; }
    public float RightStickY { get; private set; }  



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        inputActions = new Controller();
        inputActions.Enable();
    }

    private void Update()
    {
   
        LeftStickX=Input.GetAxis("LeftStickHorizontal");
        LeftStickY= Input.GetAxis("LeftStickVertical");

        RightStickX = Input.GetAxis("RightStickHorizontal");
        RightStickY = Input.GetAxis("RightStickVertical");  



    }

    public bool IsJumpPressed()
    {
        return inputActions.Player.Move.WasPressedThisFrame();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            inputActions.Disable();
        }
    }
}




