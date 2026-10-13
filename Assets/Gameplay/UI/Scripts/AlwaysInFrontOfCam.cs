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

	public void Start()
	{
		cam = Camera.main;
	}

	private void LateUpdate()
	{
		if (cam != null)
		{
			Vector3 directionToCamera = cam.transform.position - transform.position;
			transform.rotation = Quaternion.LookRotation(-directionToCamera);
			
			if (_scaleModifier)
			{
				float distance = Vector3.Distance(cam.transform.position, transform.position);
				float normalizedDistance = Mathf.Clamp01(distance / _distanceMax);
				transform.localScale = Vector3.one * Mathf.Lerp(_scaleMap.x, _scaleMap.y, normalizedDistance);
			}		
		}
	}

	#endregion
}
