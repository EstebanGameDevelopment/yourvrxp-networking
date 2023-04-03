
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
    public class CustomButton : Button
    {
		public Action<CustomButton> ClickedButton;

		public override void OnPointerClick(PointerEventData eventData)
		{
			ClickedButton?.Invoke(this);
		}
    }
}