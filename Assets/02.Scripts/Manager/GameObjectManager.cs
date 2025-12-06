
using Game.FSM;
using UnityEngine;

public class GameObjectManager : BaseManager<GameObjectManager>, IManager
{
    private GameObjectInteractionHandler _interactionHandler;
    private GameObject _player;
    private PlayerBrain _playerBrain;
    
    public GameObject Player => _player;
    public PlayerBrain PlayerBrain => _playerBrain;
    
    public delegate void Interaction();
    public event Interaction OnInteractionOn;
    public event Interaction OnInteractionOut;
    public void OnStartInteraction() => OnInteractionOn?.Invoke();
    public void OnEndInteraction() => OnInteractionOut?.Invoke();

    public void Init()
    {
        _interactionHandler = new GameObjectInteractionHandler();
        
        _player = GameObject.FindWithTag("Player");
        _playerBrain = _player.GetComponent<PlayerBrain>();
    }

    public void Dispose()
    {
        _interactionHandler = null;
    }

    public void OnUpdate()
    {
        _interactionHandler.OnUpdate();
    }

    public void OnFixedUpdate()
    {
    }

    public void OnLateUpdate()
    {
    }
    
    // 플레이어 상태 관련
    public bool IsPlayerInteracting()
    {
        if (PlayerBrain == null)
        {
            return false;
        }

        return PlayerBrain.IsOnInteraction;
    }

    public bool IsInteractionTargetExist()
    {
        return _interactionHandler.IsInteractionTargetExist();
    }
}
