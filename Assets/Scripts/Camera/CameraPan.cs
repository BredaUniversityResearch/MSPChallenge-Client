using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// CameraPan
/// </summary>
public class CameraPan : MonoBehaviour
{
	private Vector3 prevPos = Vector3.zero;
	private Vector3 panOrigin = Vector3.zero;
	private Vector3 lastPos = Vector3.zero;

	[SerializeField]
	private float cameraZOffset = -500;

	private CameraClamp cameraClamp;
	private Camera cam;
	private Rigidbody body;

	private bool panningWithLMB;
	private bool autoPan;
	private IEnumerator runningAutoPanRoutine;
	private Vector3 autoPanStart;
	private Vector3 autoPanTarget;
	private IEasingFunction autoPanEase = new EaseTunableSlowFastSlow(-0.5f);
	private bool canPanOnCurrentMousePress;

	protected void Start()
	{
		cam = GetComponent<Camera>();
		cameraClamp = GetComponent<CameraClamp>();
		body = GetComponent<Rigidbody>();

		lastPos = transform.position;

		autoPan = false;
	}

	// http://answers.unity3d.com/questions/827834/click-and-drag-camera.html
	protected void Update()
	{
		if (EventSystem.current.IsPointerOverGameObject())
		{
			if (!CameraManager.Instance.canIZoom)
			{
				return;
			}
		}

		if (Input.GetMouseButtonDown(1) || Input.GetAxis("Mouse ScrollWheel") != 0)
		{
			panningWithLMB = false;
			BeginPan();
		}
		else if (Input.GetMouseButtonDown(0) && Main.ControlKeyDown)
		{
			panningWithLMB = true;
			BeginPan();
		}

		if (!panningWithLMB &&  Input.GetMouseButton(1))
		{
			if (canPanOnCurrentMousePress)		
				Pan();		
		}
		else if (panningWithLMB && Input.GetMouseButton(0))
		{
			if (canPanOnCurrentMousePress)
				Pan();
		}
		else
		{
			//Ends the pan
			canPanOnCurrentMousePress = false;
		}

		lastPos = transform.position;
	}

	private void BeginPan()
	{
		autoPan = false;
		prevPos = transform.position;
		panOrigin = cam.ScreenToViewportPoint(Input.mousePosition);
		canPanOnCurrentMousePress = true;
	}

	private void Pan()
	{
		autoPan = false;
		float panSpeed = 2 * cam.orthographicSize;
		Vector3 pos = cam.ScreenToViewportPoint(Input.mousePosition) - panOrigin;
		pos *= panSpeed;
		pos.x *= cam.aspect;

		transform.position = prevPos - pos;
		body.velocity = -(lastPos - transform.position) * 5;
	}

	public void StartAutoPan(Vector3 destination, float speed)
	{
		if (Mathf.Approximately(destination.x, 0) && Mathf.Approximately(destination.y, 0))
		{
			//        return;
		}

		if (runningAutoPanRoutine != null)
		{
			StopCoroutine(runningAutoPanRoutine);
		}
		autoPan = true;
		autoPanStart = transform.position;
		autoPanTarget = destination;
		autoPanTarget.z = cameraZOffset; //There is this weird bug that very rarely sets the camera z to 0 instead of the initial value of -500. I changed this to prevent the issue.
		runningAutoPanRoutine = AutoPanningCoroutine(speed);
		StartCoroutine(AutoPanningCoroutine(speed));
	}

	public void EndAutoPan()
	{
		autoPan = false;
		runningAutoPanRoutine = null;
	}

	private IEnumerator AutoPanningCoroutine(float speed)
	{
		body.velocity = Vector3.zero;
		float time = 0;
		while (autoPan)
		{
			if (time < 1)
			{
				transform.position = Vector3.Lerp(autoPanStart, autoPanTarget, autoPanEase.Evaluate(time));
				time += Time.deltaTime / speed;
			}
			else
			{
				transform.position = autoPanTarget;
				time = 1;
				EndAutoPan();
			}
			cameraClamp.Clamp();

			yield return null;
		}
		yield return null;
	}
}
