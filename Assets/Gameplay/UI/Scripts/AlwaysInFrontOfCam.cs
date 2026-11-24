using System;
using UnityEngine;

public class AlwaysInFrontOfCam : MonoBehaviour
{
	#region Variables
	
	Camera cam;
	[Header("Freeze Rotation")]
	[SerializeField] private bool _freeze_X_rotation = false;
	[SerializeField] private bool _freeze_Y_rotation = false;
	[SerializeField] private bool _freeze_Z_rotation = false;
	private Quaternion _startRotation;
		
	[Header("Scale Modifier with distance")]
	[SerializeField] float _distanceMax = 100f;
	[SerializeField] private bool _scaleModifier = true;
	[SerializeField] private Vector2 _scaleMap = new Vector2(0.001f, 0.003f);
	
	#endregion


	#region Fonctions

	private void Awake()
	{
		_startRotation = transform.rotation;
	}
	
	private void LateUpdate()
	{
		if (!cam || !cam.isActiveAndEnabled)
			cam = Camera.main;

		if (!cam) return;

		Vector3 directionToCamera = cam.transform.position - transform.position;
		Quaternion rot = Quaternion.LookRotation(-directionToCamera);

		if (_freeze_X_rotation)
			rot.x = _startRotation.x;
		if (_freeze_Y_rotation)
			rot.y = _startRotation.y;
		if (_freeze_Z_rotation)
			rot.z = _startRotation.z;
		
		transform.rotation = rot;
		
		if (_scaleModifier)
		{
			float distance = directionToCamera.magnitude;
			float normalizedDistance = Mathf.Clamp01(distance / _distanceMax);
			transform.localScale = Vector3.one * Mathf.Lerp(_scaleMap.x, _scaleMap.y, normalizedDistance);
		}
	}

	#endregion
}
