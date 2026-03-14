using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace TicTacToe
{
    public class AnimationUtils
    {
        /// <summary>
        ///
        /// </summary>
        public static IEnumerator EasePropertyFloatOnObject<PropertyType>(
            object objectWithProperty,
            Func<float, float> easingFunction,
            float duration,
            float minValue = 0,
            float maxValue = 1,
            params string[] fieldsToChange
        )
            where PropertyType : struct
        {
            PropertyInfo propertyInfo =
                Array.Find(
                    objectWithProperty.GetType().GetProperties(),
                    fieldInfo => fieldInfo.PropertyType == typeof(PropertyType)
                ) ?? throw new Exception("Property on object not found.");
            return EasePropertyFloatOnObject(
                objectWithProperty,
                propertyInfo,
                easingFunction,
                duration,
                minValue,
                maxValue,
                fieldsToChange
            );
        }

        public static IEnumerator EasePropertyFloatOnObject(
            object objectWithProperty,
            string propertyName,
            Func<float, float> easingFunction,
            float duration,
            float minValue = 0,
            float maxValue = 1,
            params string[] fieldsToChange
        )
        {
            PropertyInfo propertyInfo =
                objectWithProperty.GetType().GetProperty(propertyName)
                ?? throw new Exception("Property on object not found.");
            return EasePropertyFloatOnObject(
                objectWithProperty,
                propertyInfo,
                easingFunction,
                duration,
                minValue,
                maxValue,
                fieldsToChange
            );
        }

        public static IEnumerator EasePropertyFloatOnObject(
            object objectWithProperty,
            PropertyInfo propertyInfo,
            Func<float, float> easingFunction,
            float duration,
            float minValue = 0,
            float maxValue = 1,
            params string[] fieldsToChange
        )
        {
            object propertyOnObject = propertyInfo.GetValue(objectWithProperty);
            List<FieldInfo> fieldInfos = fieldsToChange
                .ToList()
                .ConvertAll(field =>
                    propertyOnObject.GetType().GetField(field)
                    ?? throw new Exception("Field on property not found.")
                );

            float startingTime = Time.time;
            while (Time.time - duration <= startingTime)
            {
                fieldInfos.ForEach(fieldInfo =>
                {
                    fieldInfo.SetValue(
                        propertyOnObject,
                        CalculateEasePosition(
                            startingTime,
                            easingFunction,
                            duration,
                            minValue,
                            maxValue
                        )
                    );
                    propertyInfo.SetValue(objectWithProperty, propertyOnObject);
                });
                yield return null;
            }
            fieldInfos.ForEach(fieldInfo =>
                fieldInfo.SetValue(
                    propertyOnObject,
                    Mathf.Lerp(minValue, maxValue, easingFunction(1))
                )
            );
            propertyInfo.SetValue(objectWithProperty, propertyOnObject);
        }

        public static float CalculateEasePosition(
            float startingTime,
            Func<float, float> easingFunction,
            float duration,
            float minValue,
            float maxValue
        )
        {
            return Mathf.Lerp(
                minValue,
                maxValue,
                easingFunction((Time.time - startingTime) / duration)
            );
        }
    }
}
