using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Objects.BuiltElements;
using Speckle.Core.Logging;
using Unity.Plastic.Antlr3.Runtime.Debug;
using UnityEngine;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Unity
{
    public partial class ConverterUnity : ISpeckleConverter
    {
        #region implemented methods

        public void SetConverterSettings(object settings) => throw new NotImplementedException();

        public string Description => "Default Speckle Kit for Unity";
        public string Name => nameof(ConverterUnity);
        public string Author => "Speckle";
        public string WebsiteOrEmail => "https://speckle.systems";

        public ProgressReport Report { get; }

        public IEnumerable<string> GetServicedApplications() => new string[] {VersionedHostApplications.Unity};

        public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();


        public List<ApplicationPlaceholderObject> ContextObjects { get; set; } =
            new List<ApplicationPlaceholderObject>();

        public void SetContextDocument(object doc) => throw new NotImplementedException();

        public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

        public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) =>
            throw new NotImplementedException();

        public Base ConvertToSpeckle(object @object)
        {
            if (!(@object is GameObject go)) throw new NotSupportedException($"Cannot convert object of type {@object.GetType()} to Speckle");

            Base speckleObject = new Base()
            {
                ["name"] = go.name,
                //["transform"] = TransformToSpeckle(go.Transform), //TODO
                ["tag"] = go.tag,
                ["layer"] = go.layer,
                ["isStatic"] = go.isStatic,
            };

            foreach (Component component in go.GetComponents<Component>())
            {
                try
                {
                    switch (component)
                    {
                        case MeshFilter meshFilter:
                            speckleObject["@displayValue"] = MeshToSpeckle(meshFilter);
                            break;
                        // case Camera camera:
                        //     speckleObject["cameraComponent"] = CameraToSpeckle(camera);
                        //     break;
                    }
                }
                catch(Exception e)
                {
                    Debug.LogError($"Failed to convert {component.GetType()} component\n{e}", component);
                }
            }

            return speckleObject;
        }


        public object ConvertToNative(Base @object)
        {
            switch (@object)
            {
                // case Point o:
                //   return PointToNative(o);
                // case Line o:
                //   return LineToNative(o);
                // case Polyline o:
                //   return PolylineToNative(o);
                // case Curve o:
                //   return CurveToNative(o);
                // case View3D o:
                //   return View3DToNative(o);
                case Mesh o:
                    return MeshToNative(o);
                default:
                    //capture any other object that might have a mesh representation
                    object fallbackObject = DisplayValueToNative(@object);
                    if (fallbackObject != null) return fallbackObject;

                    Debug.LogWarning($"Skipping {@object.speckle_type} {@object.id} - Not supported type");
                    return null;
            }
        }

        public IList<string> DisplayValuePropertyAliases { get; set; } = new[] {"displayValue", "displayMesh"};

        public object DisplayValueToNative(Base @object)
        {
            foreach (string alias in DisplayValuePropertyAliases)
            {
                switch (@object[alias])
                {
                    //capture any other object that might have a mesh representation
                    case IEnumerable<Base> dvCollection:
                        return MeshesToNative(@object, dvCollection.OfType<Mesh>().ToList());
                    case Mesh dvMesh:
                        return MeshesToNative(@object, new[] {dvMesh});
                    case Base dvBase:
                        return ConvertToNative(dvBase);
                }
            }

            return null;
        }

        public List<Base> ConvertToSpeckle(List<object> objects)
        {
            return objects.Select(ConvertToSpeckle).ToList();
        }

        public List<object> ConvertToNative(List<Base> objects)
        {
            return objects.Select(x => ConvertToNative(x)).ToList();
            ;
        }

        public bool CanConvertToSpeckle(object @object)
        {
            switch (@object)
            {
                case GameObject o:
                    return o.GetComponent<MeshFilter>() != null;
                default:
                    return false;
            }
        }

        public bool CanConvertToNative(Base @object)
        {
            switch (@object)
            {
                // case Point _:
                //   return true;
                // case Line _:
                //   return true;
                // case Polyline _:
                //   return true;
                // case Curve _:
                //   return true;
                // case View3D _:
                //   return true;
                // case View2D _:
                //   return false;
                case Mesh _:
                    return true;
                default:
                    foreach (string alias in DisplayValuePropertyAliases)
                    {
                        switch (@object[alias])
                        {
                            //capture any other object that might have a mesh representation
                            case IEnumerable<Base> _:
                                return true;
                            case Mesh _:
                                return true;
                            case Base _:
                                return true;
                        }
                    }
                    break;
            }

            return false;
        }

        #endregion implemented methods
    }
}