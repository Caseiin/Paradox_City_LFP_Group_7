using System;
using UnityEngine;
using GameDevExtensionMethods;
using Unity.VisualScripting;
/// <summary>
/// Apples that need to be caught by player
/// Registers itself Checked
/// Calculate a vague fall distance and project communication to player
/// Make a unmuffled noise and effect when reaches ground
/// Notify Isaac Newton(Listener)
/// </summary>


public class Apple : MonoBehaviour
{
    [SerializeField] AppleRegistry registry;
    [SerializeField] AppleTrajectoryProjector projector;
    public LayerMask groundMask;
    bool isGrounded;
    bool isAirborne;


    public static event Action<Transform> OnAppleAirborne;
    public static event Action OnAppleGrounded;

    Rigidbody _rb;

    void Awake()
    {
        _rb = this.GetOrAddComponent<Rigidbody>();
        _rb.useGravity = true;
    }

    void OnEnable() => registry.Register(this);
    void OnDisable() => registry.Deregister(this);
    
    void Start()=> StartFalling();


    // void OnCollisionEnter(Collision collision)
    // {
    //     if (isGrounded) return; // guard against double-fire
    //     if (((1 << collision.gameObject.layer) & groundMask) == 0) return;

    //     isGrounded = true;
    //     OnAppleGrounded?.Invoke();
    //     // play unmuffled noise / effect here, or let a listener do it



    //     // Return to pool
    //     AppleDeployManager.Instance.ReturnToPool(this);
    // }


    public void StartFalling(){
        _rb.useGravity = true;
        isAirborne = true;
        OnAppleAirborne?.Invoke(transform);
    }

    

    void FixedUpdate(){
        if (isAirborne){
            projector.Project(transform, _rb, groundMask);
        }
    }


}
