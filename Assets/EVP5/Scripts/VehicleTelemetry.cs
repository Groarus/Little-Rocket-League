﻿//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;

namespace EVP
{

public class VehicleTelemetry : MonoBehaviour
	{
	public VehicleController target;
	public enum DataMode { TireSlipAndForce, GroundMaterial };
	public DataMode dataMode;

	public KeyCode toggleKey = KeyCode.Y;

	public bool show = true;
	public bool gizmos = false;
	public GUIStyle style = new GUIStyle();

	// Default font can be chosen by selecting the VehicleTelemetry.cs script in the Project window.
	// NOTE: for some reason this default setting doesn't work when adding the component
	// in runtime.

	[HideInInspector] public Font defaultFont;


	string m_telemetryText = "";


	void OnEnable ()
		{
		// Default font settings

		if (style.font == null)
			{
				style.font = defaultFont;
			style.normal.textColor = Color.white;
			}

		// Cache data

		if (target == null)
			target = GetComponent<VehicleController>();

		}


	void FixedUpdate ()
		{
		if (target != null && show)
			m_telemetryText = DoTelemetry();
		}


	void Update ()
		{
		if (target != null && gizmos)
			DrawGizmos();

		if (Input.GetKeyDown(toggleKey))
			{
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
				dataMode++;
				if (dataMode > DataMode.GroundMaterial) dataMode = DataMode.TireSlipAndForce;
				}
			else
				{
				show = !show;
				}
			}
		}


	void OnGUI ()
		{
		if (target != null && show)
			{
			GUI.Box(new Rect (8, 8, 545, 180), "Telemetry");
			GUI.Label(new Rect (16, 28, 400, 270), m_telemetryText, style);
			}
		}


	string DoTelemetry ()
		{
		string text = string.Format("V: {0,5:0.0} m/s  {1,5:0.0} km/h {2,5:0.0} mph\n\n", target.speed, target.speed*3.6f, target.speed*2.237f);

		float downForce = 0.0f;

		foreach (WheelData wd in target.wheelData)
			text += GetWheelTelemetry(wd, ref downForce);

		text += string.Format("\n     ΣF:{0,6:0.}  Perceived mass:{1,7:0.0}\n"+
							  "               Rigidbody mass:{2,7:0.0}\n",
							  downForce, -downForce/Physics.gravity.y, target.cachedRigidbody.mass);

		VehicleAudio vehicleAudio = target.GetComponent<VehicleAudio>();
		if (vehicleAudio != null)
			{
			text += string.Format("\nAudio gear/rpm:{0,2:0.} {1,5:0.}", vehicleAudio.simulatedGear, vehicleAudio.simulatedEngineRpm);
			}

		VehicleDamage vehicleDamage = target.GetComponent<VehicleDamage>();
		if (vehicleDamage != null)
			{
			text += string.Format("\nDamage mesh/collider/node:{0,5:0.00} {1,4:0.00} {2,4:0.00}  {3}",
				vehicleDamage.meshDamage, vehicleDamage.colliderDamage, vehicleDamage.nodeDamage, vehicleDamage.isRepairing? "REPAIRING" : "");
			}

		if (target.debugText != "")
			text += "\n\n" + target.debugText;

		return text;
		}


	string GetWheelTelemetry (WheelData wd, ref float suspensionForce)
		{
		bool sleeping = !(wd.collider.motorTorque > 0.0f);

		string text = string.Format("{0,-10}{1}{2,5:0.} rpm  ", wd.collider.gameObject.name, sleeping? "×" : ":", wd.angularVelocity * VehicleController.WToRpm);

		if (wd.grounded)
			{
			text += string.Format("C:{0,5:0.00}  ", wd.suspensionCompression);
			// text += string.Format("Vx:{0,6:0.00} Vy:{1,6:0.00}  ", wd.localVelocity.x, wd.localVelocity.y);

			switch (dataMode)
				{
				case DataMode.TireSlipAndForce:
					text += string.Format("F:{0,5:0.}  ", wd.downforce);
					text += string.Format("Sx:{0,6:0.00} Sy:{1,6:0.00} ", wd.tireSlip.x, wd.tireSlip.y);
					text += string.Format("Fx:{0,5:0.} Fy:{1,5:0.}  ", wd.tireForce.x, wd.tireForce.y);
					break;

				case DataMode.GroundMaterial:
					// text += string.Format("Sa:{0,5:0.0}  ", wd.GetSlipAngle() * Mathf.Rad2Deg);
					// text += string.Format("Slip:{0,4:0.0}  ", wd.GetCombinedSlip());
					text += string.Format("F:{0,4:0.0} %  ", wd.downforceRatio);
					text += string.Format("Slip:{0,4:0.0}  ", wd.combinedTireSlip);

					if (wd.groundMaterial != null)
						{
						text += string.Format("Grip:{0,4:0.0} Drag:{1,4:0.0}  [{2}]",
							wd.groundMaterial.grip, wd.groundMaterial.drag,
							wd.groundMaterial.physicMaterial != null? wd.groundMaterial.physicMaterial.name : "no mat");
						}
					break;
				}

			suspensionForce += wd.hit.force;
			}
		else
			{
			text += string.Format("C: 0.--  ");
			}

		return text + "\n";
		}


	void DrawGizmos ()
		{
		CommonTools.DrawCrossMark(target.cachedTransform.TransformPoint(target.cachedRigidbody.centerOfMass), target.cachedTransform, Color.white);

		foreach (WheelData wd in target.wheelData)
			DrawWheelGizmos(wd);

		Vector3 aeroForcePoint = target.cachedTransform.TransformPoint(target.cachedRigidbody.centerOfMass + Vector3.forward * target.aeroAppPointOffset);
		CommonTools.DrawCrossMark(aeroForcePoint, target.cachedTransform, Color.cyan);
		}


	void DrawWheelGizmos (WheelData wd)
		{
		RaycastHit rayHit;
		if (wd.grounded && Physics.Raycast(wd.transform.TransformPoint(wd.collider.center), -wd.transform.up, out rayHit, (wd.collider.suspensionDistance + wd.collider.radius)))
			{
			Debug.DrawLine(rayHit.point, rayHit.point + wd.transform.up * (wd.downforce / 10000.0f), wd.suspensionCompression > 0.99f? Color.magenta : Color.white);

			CommonTools.DrawCrossMark(wd.transform.position, wd.transform, Color.Lerp(Color.green, Color.gray, 0.5f));

			Vector3 forcePoint = rayHit.point + wd.transform.up * target.antiRoll * wd.forceDistance;

			if (wd.wheel.steer)
				{
				if (wd.steerAngle != 0.0f && Mathf.Sign(wd.steerAngle) != Mathf.Sign(wd.tireSlip.x))
					forcePoint += wd.hit.forwardDir * target.steeringOverdrive;
				}

			CommonTools.DrawCrossMark(forcePoint, wd.transform, Color.Lerp(Color.yellow, Color.gray, 0.5f));

			Vector3 tireForce = wd.hit.forwardDir * wd.tireForce.y + wd.hit.sidewaysDir * wd.tireForce.x;
			Debug.DrawLine(forcePoint, forcePoint + CommonTools.Lin2Log(tireForce) * 0.1f, Color.green);

			Vector3 tireSlip = wd.hit.forwardDir * wd.tireSlip.y + wd.hit.sidewaysDir * wd.tireSlip.x;
			Debug.DrawLine(rayHit.point, rayHit.point + CommonTools.Lin2Log(tireSlip) * 0.5f, Color.cyan);

			// Vector3 wheelVelocity = wd.hit.forwardDir * wd.localVelocity.y + wd.hit.sidewaysDir * wd.localVelocity.x;
			// Debug.DrawLine(rayHit.point, rayHit.point + CommonTools.Lin2Log(wheelVelocity) * 0.5f, Color.Lerp(Color.blue, Color.white, 0.5f));

			// Vector3 rigForce = wd.hit.sidewaysDir * wd.localRigForce.x + wd.hit.forwardDir * wd.localRigForce.y;
			// Debug.DrawLine(rayHit.point, rayHit.point + rigForce / 10000.0f, Color.Lerp(Color.red, Color.gray, 0.5f));
			}
		}
	}
}