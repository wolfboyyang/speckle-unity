#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Objects.Converter.Unity;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity.Converter
{
    /// <summary>
    /// <see cref="Component"/> for recursive conversion of Speckle Objects to Native, and Native Objects to Speckle
    /// </summary>
    [AddComponentMenu("Speckle/Conversion" + nameof(ConverterComponent))]
    [ExecuteAlways, DisallowMultipleComponent]
    public class ConverterComponent : MonoBehaviour
    {
        public ISpeckleConverter ConverterInstance { get; set; } = default!;

        void Awake()
        {
            ConverterInstance = new ConverterUnity();
        }
        
#region ToNative

        /// <summary>
        /// Given <paramref name="baseObject"/>,
        /// will recursively convert any objects in the tree
        /// </summary>
        /// <param name="baseObject">The Speckle object to convert + its children</param>
        /// <param name="parent">Optional parent transform for the created root <see cref="GameObject"/>s</param>
        /// <returns> A list of all created <see cref="GameObject"/>s</returns>
        public virtual List<GameObject> RecursivelyConvertToNative(Base baseObject, Transform? parent)
            => RecursivelyConvertToNative(baseObject, parent, o => ConverterInstance.CanConvertToNative(o));
        
        /// <inheritdoc cref="RecursivelyConvertToNative(Base, Transform)"/>
        /// <param name="predicate">A function to determine if an object should be converted</param>
        public virtual List<GameObject> RecursivelyConvertToNative(Base baseObject, Transform? parent, Func<Base, bool> predicate)
        {
            LoadMaterialOverrides();

            var createdGameObjects = new List<GameObject>();
            RecurseTreeToNative(baseObject, parent, predicate, createdGameObjects);
            //TODO track event
            
            return createdGameObjects;

        }
        
        
        public virtual void RecurseTreeToNative(Base baseObject, Transform? parent, Func<Base, bool> predicate, IList<GameObject> outCreatedObjects)
        {
            object? converted = null;
            if(predicate(baseObject))
                converted = ConverterInstance.ConvertToNative(baseObject);

            // Handle new GameObjects
            Transform? nextParent = parent;
            if (converted is GameObject go)
            {
                outCreatedObjects.Add(go);
                
                nextParent = go.transform;
                go.name = baseObject["name"] as string ?? $"{baseObject.speckle_type} - {baseObject.id}";
                go.transform.SetParent(parent);
                if (baseObject["tag"] is string t) go.tag = t;
                if (baseObject["layer"] is int layer) go.layer = layer;
                if (baseObject["isStatic"] is bool isStatic) go.isStatic = isStatic;
            }

            ConvertChildren(baseObject, nextParent, predicate, outCreatedObjects);
        }

        protected virtual void ConvertChildren(Base baseObject, Transform? parent, Func<Base, bool> predicate, IList<GameObject> outCreatedObjects)
        {
            // Find child objects to convert,
            IEnumerable<string> potentialChildren = baseObject.GetDynamicMembers();
            if (!ConverterInstance.CanConvertToNative(baseObject))
            {
                potentialChildren = potentialChildren.Concat(baseObject.GetInstanceMembersNames());
            }

            foreach (string? c in potentialChildren)
            {
                object? value = baseObject[c];
                
                //Ignore everything but objects inheriting Base
                if(value == null) continue;
                if(value.GetType().IsSimpleType()) continue;
                
                if(value is Base o)
                {
                    RecurseTreeToNative(o, parent, predicate, outCreatedObjects);
                }
                else if (value is IDictionary dictionary)
                {
                    foreach (object obj in dictionary.Keys)
                    {
                        if(obj is Base b) RecurseTreeToNative(b, parent, predicate, outCreatedObjects);
                    }
                }
                else if (value is IList collection)
                {
                    foreach (object obj in collection)
                    {
                        if(obj is Base b) RecurseTreeToNative(b, parent, predicate, outCreatedObjects);
                    }
                }
                else
                {
                    Debug.Log(value.GetType());
                }
            }

        }

        protected virtual void LoadMaterialOverrides()
        {
            //using the ApplicationPlaceholderObject to pass materials
            //available in Assets/Materials to the converters
            var materials = Resources.LoadAll("", typeof(Material)).Cast<Material>().ToArray();
            if (materials.Length == 0) Debug.Log("To automatically assign materials to recieved meshes, materials have to be in the \'Assets/Resources\' folder!");
            var placeholderObjects = materials.Select(x => new ApplicationPlaceholderObject { NativeObject = x }).ToList();
            ConverterInstance.SetContextObjects(placeholderObjects);
        }
#endregion

#region ToSpeckle

        /// <summary>
        /// Given a collection of <paramref name="rootObjects"/>,
        /// will recursively convert any <see cref="GameObject"/>s in the tree
        /// where a given <paramref name="predicate"/> function holds true.
        /// </summary>
        /// <example>
        /// Convert all objects in a scene that have a given tag
        /// <code>
        ///     Base b = RecursivelyConvertToSpeckle(SceneManager.GetActiveScene().GetRootGameObjects(), o => o.CompareTag("myTag"));
        /// </code>
        /// Convert a selection of objects that share a common rootObject(s)
        /// <code>
        ///     GameObject parent = ...
        ///     ISet selection = ...
        ///     Base b = RecursivelyConvertToSpeckle(parent, o => selection.contains(o));
        /// </code>
        /// </example>
        /// <param name="rootObjects">Root objects of a tree</param>
        /// <param name="predicate">A function to determine if an object should be converted</param>
        /// <returns>A simple <see cref="Base"/> wrapping converted objects</returns>
        public virtual Base RecursivelyConvertToSpeckle(IEnumerable<GameObject> rootObjects, Func<GameObject, bool> predicate)
        {
            List<Base> convertedRootObjects = new List<Base>();
            foreach (GameObject rootObject in rootObjects)
            {
                RecurseTreeToSpeckle(rootObject, predicate, convertedRootObjects);
            }
            
            return new Base()
            {
                ["objects"] = convertedRootObjects,
            };
        }
        
        public virtual Base RecursivelyConvertToSpeckle(GameObject rootObject, Func<GameObject, bool> predicate)
        {
            return RecursivelyConvertToSpeckle(new[] {rootObject}, predicate);
        }
        
        public virtual void RecurseTreeToSpeckle(GameObject rootObject, Func<GameObject, bool> predicate, List<Base> outConverted)
        {
            // Convert children first
            var convertedChildren = new List<Base>(rootObject.transform.childCount);
            foreach(Transform child in rootObject.transform)
            {
                RecurseTreeToSpeckle(child.gameObject, predicate, convertedChildren);
            }
            
            if (ConverterInstance.CanConvertToSpeckle(rootObject) && predicate(rootObject))
            {
                // Convert and output 
                Base converted = ConverterInstance.ConvertToSpeckle(rootObject);
                converted["objects"] = convertedChildren;
                outConverted.Add(converted);
            }
            else
            {
                // Skip this object, and output any children
                outConverted.AddRange(convertedChildren);
            }

        }

        #endregion

    }
}
