using System;
using UnityEngine;

public class AlwaysInFrontOfCam : MonoBehaviour
{
	#region Variables

	Camera cam;
	[SerializeField] float _distanceMax = 100f;
	[SerializeField] private bool _scaleModifier = true;
	[SerializeField] private Vector2 _scaleMap = new Vector2(0.001f, 0.003f);
	
	#endregion


	#region Fonctions

	private void LateUpdate()
	{
		if (!cam || !cam.isActiveAndEnabled)
			cam = Camera.main;

		if (!cam) return;

		Vector3 directionToCamera = cam.transform.position - transform.position;
		transform.rotation = Quaternion.LookRotation(-directionToCamera);

		if (_scaleModifier)
		{
			float distance = directionToCamera.magnitude;
			float normalizedDistance = Mathf.Clamp01(distance / _distanceMax);
			transform.localScale = Vector3.one * Mathf.Lerp(_scaleMap.x, _scaleMap.y, normalizedDistance);
		}
	}

	#endregion
}
