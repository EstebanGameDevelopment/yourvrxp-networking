using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;
#if ENABLE_MIRROR
using Mirror;
#endif
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)
using yourvrexperience.VR;
#endif

namespace yourvrexperience.Networking
{
	public class NetworkDraggable : NetworkPickable, INetworkObject, IPickableObject
	{
		[SerializeField] private float SizeCube = 1;
		[SerializeField] private string CastingTarget = "Floor";
		[SerializeField] private string CastingForbidden = "Forbidden";
		[SerializeField] private Vector3 ShiftFromFloor = new Vector3(0, 0.5f, 0);

		private int _forbiddenMaskLayer = -1;
		private int _floorMaskLayer = -1;
		protected Vector3 _shiftFromFloor;

		protected override void Start()
		{
			base.Start();
			_shiftFromFloor = ShiftFromFloor;
			_floorMaskLayer = LayerMask.GetMask(CastingTarget);
			_forbiddenMaskLayer = LayerMask.GetMask(CastingForbidden);
		}

		private void AdjustPosition(Vector3 position, Vector3 normal)
		{
			Vector3 finalAirPosition = position + normal * (SizeCube/2);
			RaycastHit hitData = new RaycastHit();
			Vector3 positionFloor = RaycastingTools.GetRaycastOriginForward(finalAirPosition, Vector3.down, ref hitData, 1000, _floorMaskLayer);
			if (positionFloor != Vector3.zero)
			{
				this.transform.position = positionFloor + _shiftFromFloor;
			}			
		}

		protected override void MoveToPosition()
		{
			if (NetworkGameIDView.AmOwner())
			{
				bool isForbidden = false;
				RaycastHit hitData = new RaycastHit();
				Vector3 positionForbidden = GetPositionRaycastAgainstSurface(_forbiddenMaskLayer, ref hitData);
				if (positionForbidden != Vector3.zero)
				{
					AdjustPosition(positionForbidden, hitData.normal);
				}
				else
				{
					Vector3 positionFloor = GetPositionRaycastAgainstSurface(_floorMaskLayer, ref hitData);

					if (!isForbidden)
					{
						positionForbidden = RaycastingTools.GetRaycastOriginForward(positionFloor, new Vector3(-1, 0, 0), ref hitData, SizeCube/2, _forbiddenMaskLayer);
						if (positionForbidden != Vector3.zero) isForbidden = true;
					}

					if (!isForbidden)
					{
						positionForbidden = RaycastingTools.GetRaycastOriginForward(positionFloor, new Vector3(1, 0, 0), ref hitData, SizeCube/2, _forbiddenMaskLayer);
						if (positionForbidden != Vector3.zero) isForbidden = true;
					}

					if (!isForbidden)
					{
						positionForbidden = RaycastingTools.GetRaycastOriginForward(positionFloor, new Vector3(0, 0, -1), ref hitData, SizeCube/2, _forbiddenMaskLayer);
						if (positionForbidden != Vector3.zero) isForbidden = true;
					}

					if (!isForbidden)
					{
						positionForbidden = RaycastingTools.GetRaycastOriginForward(positionFloor, new Vector3(0, 0, 1), ref hitData, SizeCube/2, _forbiddenMaskLayer);
						if (positionForbidden != Vector3.zero) isForbidden = true;
					}

					if (!isForbidden)
					{
						this.transform.position = positionFloor + _shiftFromFloor;
					}
					else
					{
						AdjustPosition(positionForbidden + Vector3.up, hitData.normal);
					}
				}
			}
		}
	}
}