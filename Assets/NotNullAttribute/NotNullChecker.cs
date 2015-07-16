﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace RedBlueTools
{
	public class NotNullChecker
	{
		
		public static List<NotNullViolation> FindErroringFields (GameObject sourceObject, string assetPath)
		{
			List<NotNullViolation> erroringFields = new List<NotNullViolation> ();
			MonoBehaviour[] monobehaviours = sourceObject.GetComponents<MonoBehaviour> ();
			for (int i = 0; i < monobehaviours.Length; i++) {
				try {
					if (MonoBehaviourHasErrors (monobehaviours [i])) {
						List<NotNullViolation> violationsOnMonoBehaviour = FindErroringFields (monobehaviours [i]);
						erroringFields.AddRange (violationsOnMonoBehaviour);
					}
				} catch (System.ArgumentNullException) {
					// TODO: Handle missing monobehaviours
				}
			}

			return erroringFields;
		}

		static List<NotNullViolation> FindErroringFields (MonoBehaviour sourceMB)
		{
			if (sourceMB == null) {
				throw new System.ArgumentNullException ("MonoBehaviour is null. It likely references" +
					" a script that's been deleted.");
			}

			List<NotNullViolation> erroringFields = new List<NotNullViolation> ();
		
			// Add null NotNull fields
			List<FieldInfo> notNullFields = 
				ReflectionUtilities.GetFieldsWithAttributeFromType<NotNullAttribute> (sourceMB.GetType ());
			foreach (FieldInfo notNullField in notNullFields) {
				object fieldObject = notNullField.GetValue (sourceMB);
				if (fieldObject == null || fieldObject.Equals (null)) {
					erroringFields.Add (new NotNullViolation (notNullField, sourceMB, false));
				}
			}
		
			// Flag notNullAttributes that are allowed to be null as prefabs
			foreach (NotNullViolation errorField in erroringFields) {
				FieldInfo fieldInfo = errorField.FieldInfo;
				foreach (Attribute attribute in Attribute.GetCustomAttributes (fieldInfo)) {
					if (attribute.GetType () == typeof(NotNullAttribute)) {
						NotNullAttribute notNullAttribute = (NotNullAttribute)attribute;
						errorField.AllowNullAsPrefab = notNullAttribute.IgnorePrefab;
					}
				}
			}
		
			return erroringFields;
		}
	
		static bool MonoBehaviourHasErrors (MonoBehaviour mb)
		{
			return FindErroringFields (mb).Count > 0;
		}
	}

}