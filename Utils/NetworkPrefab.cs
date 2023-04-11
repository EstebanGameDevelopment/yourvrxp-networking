using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;
#if ENABLE_MIRROR	
using Mirror;
#endif

namespace yourvrexperience.Networking
{
	public class NetworkPrefab : 
#if ENABLE_MIRROR	
	NetworkBehaviour
#else
	MonoBehaviour
#endif	
	, INetworkInitialData
	{
		public const string EventNetworkPrefabHasStarted = "EventNetworkPrefabHasStarted";
		public const string EventNetworkPrefabUpdateNameObject = "EventNetworkPrefabUpdateNameObject";

		public const string Separator = "<np>";

		[SerializeField] protected bool IsInLevel = false;
		[SerializeField] protected string NetworkPrefabName = "";
		[SerializeField] protected string NetworkPathName = "";

		private Rigidbody _networkRigidBody;
		private Collider _networkCollider;
				
		private NetworkObjectID _networkGameID;
		public NetworkObjectID NetworkGameIDView
		{
			get
			{
				if (_networkGameID == null)
				{
					if (this != null)
					{
						_networkGameID = GetComponent<NetworkObjectID>();
					}
				}
				return _networkGameID;
			}
		}
		public string ProviderName
		{
			get { return "NetworkPrefab"; }
		}
		protected Rigidbody NetworkRigidBody
		{
			get {
				if (_networkRigidBody == null)
				{
					_networkRigidBody = this.GetComponent<Rigidbody>();
				}
				return _networkRigidBody;
			}
		}
		protected Collider NetworkCollider
		{
			get {
				if (_networkCollider == null)
				{
					_networkCollider = this.GetComponent<Collider>();
				}
				return _networkCollider;
			}
		}

		public string NameNetworkPrefab
		{ 
			get { return NetworkPrefabName; }
		}
		public string NameNetworkPath
		{ 
			get { return NetworkPathName; } 
		}
		public virtual bool LinkedToCurrentLevel
		{
			get { return false; }
		}

		public virtual string GetInitialData()
		{
			string output = Utilities.SerializeVector3(this.transform.position) + Separator + 
					Utilities.SerializeQuaternion(this.transform.rotation) + Separator + 
					Utilities.SerializeVector3(this.transform.localScale);
			return output;
		}

		public virtual void ApplyInitialData(string data, bool linkedToLevel)
		{
			string[] dataArray = data.Split(Separator, StringSplitOptions.None);
			this.transform.position = Utilities.DeserializeVector3(dataArray[0]);
			this.transform.rotation = Utilities.DeserializeQuaternion(dataArray[1]);
			this.transform.localScale = Utilities.DeserializeVector3(dataArray[2]);
			NetworkGameIDView.LinkedToCurrentLevel = linkedToLevel;	
		}

		protected virtual void Start()
		{
			if (!IsInLevel)
			{
				NetworkController.Instance.DispatchEvent(EventNetworkPrefabHasStarted, this.gameObject, IsInLevel, NetworkPrefabName, NetworkPathName);
#if ENABLE_MIRROR				
				if (NetworkController.Instance.IsServer)
				{
					Invoke("DelayedRequestAuthority", 0.3f);
				}
#endif				
			}
			else
			{
				NetworkController.Instance.DispatchEvent(EventNetworkPrefabHasStarted, this.gameObject, IsInLevel, NetworkPrefabName, NetworkPathName);
			}

			NetworkController.Instance.NetworkEvent += OnNetworkEvent;
		}

		private void DelayedRequestAuthority()
		{
			NetworkGameIDView.RequestAuthority();
		}

		public bool GetIsInLevel()
		{
			return IsInLevel;
		}

		protected virtual void OnDestroy()
		{
			if (NetworkController.Instance != null)	NetworkController.Instance.NetworkEvent -= OnNetworkEvent;
		}

		protected virtual void OnNetworkEvent(string nameEvent, int originNetworkID, int targetNetworkID, object[] parameters)
		{
			if (nameEvent.Equals(NetworkController.EventNetworkControllerClientLevelReady))
			{
				if (NetworkController.Instance.IsServer)
				{
					NetworkController.Instance.DispatchNetworkEvent(EventNetworkPrefabUpdateNameObject, -1, -1, NetworkGameIDView.GetViewID(), this.name, this.gameObject.transform.position, this.gameObject.transform.rotation, this.gameObject.transform.localScale);
				}
			}
			if (nameEvent.Equals(EventNetworkPrefabUpdateNameObject))
			{
				if (!NetworkController.Instance.IsServer)
				{
					int viewID = (int)parameters[0];
					string nameNetworkObject = (string)parameters[1];
					if (NetworkGameIDView.GetViewID() == viewID)
					{
						this.gameObject.name = nameNetworkObject;
						this.gameObject.transform.position = (Vector3)parameters[2];
						this.gameObject.transform.rotation = (Quaternion)parameters[3];
						this.gameObject.transform.localScale = (Vector3)parameters[4];
					}
				}
			}
		}
	}
}