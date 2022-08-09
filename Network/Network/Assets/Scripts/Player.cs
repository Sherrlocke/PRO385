using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.UI;
using TMPro;

public class Player : NetworkBehaviour
{
    [SerializeField] private Ball prefabBall;
    [SerializeField] private RBBall prefabRBBall;
    [Networked] private TickTimer delay { get; set; }

    private NetworkCharacterControllerPrototype characterControllerPrototype;
    private Vector3 forward;

    private void Awake()
    {
        characterControllerPrototype = GetComponent<NetworkCharacterControllerPrototype>();
        forward = transform.forward;
    }

    [Networked(OnChanged = nameof(OnBallSpawned))]
    public NetworkBool spawned { get; set; }

    public static void OnBallSpawned(Changed<Player> changed)
    {
        changed.Behaviour.material.color = Color.white;
    }

    private Material _material;
    Material material
    {
        get
        {
            if (_material == null)
                _material = GetComponentInChildren<MeshRenderer>().material;
            return _material;
        }
    }

    public override void Render()
    {
        material.color = Color.Lerp(material.color, Color.blue, Time.deltaTime);
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            characterControllerPrototype.Move(5 * data.direction * Runner.DeltaTime);

            if (data.direction.sqrMagnitude > 0) forward = data.direction;
        }

        if (delay.ExpiredOrNotRunning(Runner))
        {
            // Transform ball
            if ((data.buttons & NetworkInputData.MOUSEBUTTON1) != 0)
            {
                delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                Runner.Spawn(prefabBall,
                transform.position + forward, Quaternion.LookRotation(forward),
                Object.InputAuthority, (runner, o) =>
                {
                    // Initialize the Ball before synchronizing it
                    o.GetComponent<Ball>().Init();
                });
                spawned = !spawned;
            }
            // Rigid body ball
            else if ((data.buttons & NetworkInputData.MOUSEBUTTON2) != 0)
            {
                delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                Runner.Spawn(prefabRBBall,
                  transform.position + forward,
                  Quaternion.LookRotation(forward),
                  Object.InputAuthority,
                  (runner, o) =>
                  {
                      o.GetComponent<RBBall>().Init(10 * forward);
                  });
                spawned = !spawned;
            }
        }
    }

    private void Update()
    {
        if (Object.HasInputAuthority && Input.GetKeyDown(KeyCode.R))
        {
            RPC_SendMessage("ggez!");
        }
    }

    private TMP_Text _messages;

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_SendMessage(string message, RpcInfo info = default)
    {
        if (_messages == null)
            _messages = FindObjectOfType<TMP_Text>();
        if (info.IsInvokeLocal)
            message = $"You said: {message}\n";
        else
            message = $"Some other player said: {message}\n";
        _messages.text += message;
    }
}
