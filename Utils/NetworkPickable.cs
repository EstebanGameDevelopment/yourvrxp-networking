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
#if ENABLE_ULTIMATEXR
using UltimateXR.Manipulation;
#endif

namespace yourvrexperience.Networking
{
	[RequireComponent(typeof(ObjectPickable))]
	public class NetworkPickable : NetworkPrefab, INetworkObject, INetworkPickable
	{
		public const string EventNetworkPickableTakeControl = "EventNetworkPickableTakeControl";
		public const string EventNetworkPickableTakeControlConfirmation = "EventNetworkPickableTakeControlConfirmation";
		public const string EventNetworkPickableReleaseControl = "EventNetworkPickableReleaseControl";
		public const string EventNetworkPickableObjectReleasedControlConfirmed = "EventNetworkPickableObjectReleasedControlConfirmed";

		public const string EventNetworkPickableRefreshTransform = "EventNetworkPickableRefreshTransform";

		protected bool _enabled = true;
		protected bool _requestedOwnership = false;

		protected int _layer;
		private float _restoreServerAuthority = 0;
        		
		protected IObjectPickable _objectPickable;

		public override bool LinkedToCurrentLevel
		{
			get { return true; }
		}
		
		protected override void Start()
		{
			base.Start();

			_layer = this.gameObject.layer;
			_objectPickable = this.GetComponent<IObjectPickable>();
			_objectPickable.PickedEvent += OnPickedEvent;

			if (!IsInLevel)
			{
				bool shouldActivate = true;
				NetworkGameIDView.InitedEvent += OnInitDataEvent;
				shouldActivate = NetworkGameIDView.AmOwner();
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (!IsInLevel)
			{
				NetworkGameIDView.InitedEvent -= OnInitDataEvent;
				_objectPickable.PickedEvent -= OnPickedEvent;
			}
		}

		private void OnPickedEvent(bool isGrabbed)
		{
			ToggleControl();
		}

		public virtual void SetInitData(string initializationData)
		{
			NetworkGameIDView.InitialInstantiationData = initializationData;
		}

		public virtual void OnInitDataEvent(string initializationData)
		{
		}

		public virtual void ActivatePhysics(bool activation, bool force = false)
		{
            if (_enabled || force)
            {
				_objectPickable.ActivatePhysics(activation);
            }
		}

		protected override void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			base.OnNetworkEvent(nameEvent, originNetworkID, targetNetworkID, parameters);

			if (nameEvent.Equals(EventNetworkPickableTakeControl))
			{
				int viewID = (int)parameters[0];
				if (viewID == NetworkGameIDView.GetViewID())
				{
					_enabled = false;
					ActivatePhysics(false, true);
					this.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
				}
			}
			if (nameEvent.Equals(EventNetworkPickableReleaseControl))
			{
				int viewID = (int)parameters[0];
				if (viewID == NetworkGameIDView.GetViewID())
				{
					_enabled = true;
					this.gameObject.layer = _layer;
					ActivatePhysics(true);
					if (NetworkController.Instance.IsServer)
					{
						_restoreServerAuthority = 0.2f;
					}					
					ReportReleaseConfirmation();
				}
			}			
			if (nameEvent.Equals(NetworkObjectID.EventNetworkObjectIDTransferCompletedOwnership))
			{
				int targetNetID = (int)parameters[0];
				if (NetworkGameIDView.GetViewID() == targetNetID)
				{
					if (NetworkGameIDView.AmOwner())
					{
						if (_requestedOwnership)
						{
							_requestedOwnership = false;
							ReportedConfirmationOfOwnerShip();
						}
					}					
				}
			}
			if (nameEvent.Equals(NetworkObjectID.EventNetworkObjectIDOwnershipOfServer))			
			{
				int targetNetID = (int)parameters[0];
				if (NetworkGameIDView.GetViewID() == targetNetID)
				{
					if (NetworkGameIDView.AmOwner())
					{
						NetworkController.Instance.DispatchNetworkEvent(EventNetworkPickableRefreshTransform, -1, -1, NetworkGameIDView.GetViewID(), this.transform.position, this.transform.rotation, this.transform.localScale);
#if ENABLE_MIRROR						
						NetworkController.Instance.DelayNetworkEvent(EventNetworkPickableRefreshTransform, 0.2f, -1, -1, NetworkGameIDView.GetViewID(), this.transform.position, this.transform.rotation, this.transform.localScale);
#endif						
					}					
				}
			}	
			if (nameEvent.Equals(EventNetworkPickableRefreshTransform))
			{
				int targetNetID = (int)parameters[0];
				if (NetworkGameIDView.GetViewID() == targetNetID)
				{
					if (!NetworkGameIDView.AmOwner())
					{
						this.transform.position = (Vector3)parameters[1];
						this.transform.rotation = (Quaternion)parameters[2];
						this.transform.localScale = (Vector3)parameters[3];
					}					
				}
			}
		}

		protected virtual void ReportedConfirmationOfOwnerShip()
		{
			SystemEventController.Instance.DispatchSystemEvent(EventNetworkPickableTakeControlConfirmation, _objectPickable);
		}

		protected virtual void ReportReleaseConfirmation()
		{
			SystemEventController.Instance.DispatchSystemEvent(EventNetworkPickableObjectReleasedControlConfirmed);
		}

		public virtual bool ToggleControl()
		{
			if (_enabled)
			{
				if (!NetworkGameIDView.AmOwner())
				{
					_requestedOwnership = true;
					NetworkGameIDView.RequestAuthority();
				}
				else
				{
					ReportedConfirmationOfOwnerShip();
				} 
				NetworkController.Instance.DispatchNetworkEvent(EventNetworkPickableTakeControl, -1, -1, NetworkGameIDView.GetViewID());
			}
			else
			{
				if (NetworkGameIDView.AmOwner())
				{
					_enabled = true;
					NetworkController.Instance.DispatchNetworkEvent(EventNetworkPickableReleaseControl, -1, -1, NetworkGameIDView.GetViewID());
				}
			}
			return true;
		}

		protected virtual bool RestoreServerAuthority()
		{
			if (NetworkController.Instance.IsServer)
			{
				if (_restoreServerAuthority > 0)
				{
					_restoreServerAuthority -= Time.deltaTime;
					if (_restoreServerAuthority <= 0)
					{
						if (!NetworkGameIDView.AmOwner())
						{
							NetworkGameIDView.RequestAuthority();
						}
						else
						{
							NetworkController.Instance.DispatchNetworkEvent(NetworkObjectID.EventNetworkObjectIDOwnershipOfServer, -1, -1, NetworkGameIDView.GetViewID());
						}
						return true;
					}
				}
			}
			return false;
		}

		protected virtual void Update()
		{
			if (NetworkGameIDView.AmOwner())
			{
				if (!_enabled)
				{
					_objectPickable.IsGrabbed = true;
				}
				else
				{
					_objectPickable.IsGrabbed = false;
				}
			}
			AfterUpdate();
			RestoreServerAuthority();
		}

		public virtual void AfterUpdate(){}
	}
}