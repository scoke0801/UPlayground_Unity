
using UnityEngine;

public class GameObjectManager : BaseManager<GameObjectManager>, IManager
{
    private GameObjectInteractionHandler _interactionHandler;
    private GameObject _player;

    public GameObject Player => _player;

    public void Init()
    {
        _interactionHandler = new GameObjectInteractionHandler();
        
        _player = GameObject.FindWithTag("Player");
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
}
