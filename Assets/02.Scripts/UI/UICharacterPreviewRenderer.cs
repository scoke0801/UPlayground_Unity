using AYellowpaper.SerializedCollections;
using KinematicCharacterController;
using UnityEngine;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

/// <summary>
/// 인벤토리 UI용 캐릭터 프리뷰 렌더러
/// </summary>
public class UICharacterPreviewRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _previewCamera;
    [SerializeField] private Transform _previewCharacterRoot;
    [SerializeField] private RenderTexture _renderTexture;
    
    [Header("Settings")]
    [SerializeField] private Vector3 _cameraOffset = new Vector3(0, 1.5f, 2.5f);
    [SerializeField] private float _rotationSpeed = 100f;

    [SerializeField] private SerializedDictionary<CharacterActorType, GameObject> _actorPrefabDict;
    
    private GameObject _currentPreviewCharacter;
    private float _currentRotation = 0f;

    private void Awake()
    {
        // 카메라 초기 설정
        _previewCamera.enabled = false;
        _previewCamera.cullingMask = 1 << LayerMask.NameToLayer("CharacterPreview");
        _previewCamera.clearFlags = CameraClearFlags.SolidColor;
        _previewCamera.backgroundColor = new Color(0, 0, 0, 0);
    }

    /// <summary>
    /// 프리뷰 활성화
    /// </summary>
    public void ShowPreview()
    {
        if (_currentPreviewCharacter != null)
            Destroy(_currentPreviewCharacter);

        if(GameObjectManager.Instance == null)
        {
            return;
        }

        PlayerActor player = GameObjectManager.Instance.Player;
        if (player == null)
        {
            return;
        }

        if (_actorPrefabDict.TryGetValue(player.CharacterType, out GameObject targetPrefab) == false)
        {
            return;
        }
        
        _currentPreviewCharacter = Instantiate(targetPrefab, _previewCharacterRoot);
        if (_currentPreviewCharacter == null)
        {
            return;
        }
        _currentPreviewCharacter.transform.localPosition = Vector3.zero;
        _currentPreviewCharacter.transform.localRotation = Quaternion.identity;
        
        SetLayerRecursively(_currentPreviewCharacter, "CharacterPreview");
        
        // KCC와 플레이어 스크립트 비활성화
        //DisablePlayerComponents(_currentPreviewCharacter);
        
        _previewCamera.enabled = true;
    }

    /// <summary>
    /// 프리뷰 비활성화
    /// </summary>
    public void HidePreview()
    {
        _previewCamera.enabled = false;
        
        if (_currentPreviewCharacter != null)
        {
            Destroy(_currentPreviewCharacter);
            _currentPreviewCharacter = null;
        }
    }

    /// <summary>
    /// 캐릭터 회전 (마우스 드래그용)
    /// </summary>
    public void RotateCharacter(float deltaX)
    {
        if (_currentPreviewCharacter == null) return;
        
        _currentRotation += deltaX * _rotationSpeed * Time.deltaTime;
        _currentPreviewCharacter.transform.localRotation = Quaternion.Euler(0, _currentRotation, 0);
    }

    /// <summary>
    /// RenderTexture 반환
    /// </summary>
    public RenderTexture GetRenderTexture() => _renderTexture;

    private void SetLayerRecursively(GameObject obj, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        obj.layer = layer;
        
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layerName);
        }
    }

    private void DisablePlayerComponents(GameObject character)
    {
        // KCC 비활성화
        var kcc = character.GetComponent<KinematicCharacterMotor>();
        if (kcc != null) kcc.enabled = false;
        
        // 플레이어 컨트롤러 비활성화 (이름은 프로젝트에 맞게 수정)
        var controller = character.GetComponent<MonoBehaviour>();
        if (controller != null) controller.enabled = false;
        
        // Rigidbody 비활성화
        var rb = character.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        
        // Collider 비활성화
        var colliders = character.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
            col.enabled = false;
    }
}