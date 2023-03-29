using AutoAlignHexes.Utils;
using UnityEditor;
using UnityEngine;

namespace AutoAlignHexes
{
    public class AutoAlignHexes : EditorWindow
    {
        private float _radius = 5.0f;
        private int _selectedRadiusType = 0;
        private readonly string[] _radiusTypes = { "Outer", "Inner" };
        private int _selectedOrientation = 0;
        private readonly string[] _orientations = { "Flat-Top", "Pointy-Top" };
        private float _expandBy = 0.0f;
        private float _contractBy = 0.0f;

        private HexRadius _radiusType;
        private HexOrientation _orientation;
        
        [MenuItem("Tools/Align Hexes")]
        private static void Init()
        {
            var window = (AutoAlignHexes)GetWindow(typeof(AutoAlignHexes));
            window.titleContent.text = "Hex Creator";
            window.maxSize = new Vector2(300, 165);
            window.minSize = window.maxSize;
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.BeginVertical();

            DrawAlignBlock();
            
            GUILayout.Space(10);

            DrawExpandBlock();
            
            GUILayout.Space(10);

            DrawContractBlock();
            
            GUILayout.EndVertical();
        }

        private void DrawAlignBlock()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Radius", GUILayout.Width(100));
            _radius = EditorGUILayout.FloatField(_radius, GUILayout.Width(50));
            _selectedRadiusType = EditorGUILayout.Popup(_selectedRadiusType, _radiusTypes); 
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Orientation", GUILayout.Width(100));
            _selectedOrientation = EditorGUILayout.Popup(_selectedOrientation, _orientations); 
            GUILayout.EndHorizontal();
            
            _radiusType = _selectedRadiusType == 0 ? HexRadius.Outer : HexRadius.Inner;
            _orientation = _selectedOrientation == 0 ? HexOrientation.FlatTop : HexOrientation.PointyTop;

            if(GUILayout.Button("Align Hexes"))
            {
                MoveHexes(0.0f);
            }
        }

        private void DrawExpandBlock()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Expand By", GUILayout.Width(100));
            EditorGUI.BeginChangeCheck();
            _expandBy = EditorGUILayout.FloatField(_expandBy);
            if (EditorGUI.EndChangeCheck())
            {
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();
            
            if(GUILayout.Button("Expand"))
            {
                MoveHexes(_expandBy);
                _expandBy = 0;
            }
        }

        private void DrawContractBlock()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Contract By", GUILayout.Width(100));
            EditorGUI.BeginChangeCheck();
            _contractBy = EditorGUILayout.FloatField(_contractBy);
            if (EditorGUI.EndChangeCheck())
            {
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();
            
            if(GUILayout.Button("Contract"))
            {
                MoveHexes(-_contractBy);
                _contractBy = 0;
            }
        }

        private void MoveHexes(float additionalMove)
        {
            var selectedGameObject = Selection.activeGameObject;
            if (selectedGameObject == null) return;

            var radius = ConvertInnerRadiusToOuterByType(_radiusType, _radius);

            var childrenTransforms = selectedGameObject.transform.GetAllChildrenTransform();
            foreach (var childTransform in childrenTransforms)
            {
                var childPosition = childTransform.localPosition;
                var alignedPosition = _orientation switch
                {
                    HexOrientation.FlatTop => GetAlignedPositionFlatTop(childPosition, radius, radius + additionalMove),
                    HexOrientation.PointyTop => GetAlignedPositionPointyTop(childPosition, radius, radius + additionalMove),
                    _ => GetAlignedPositionFlatTop(childPosition, radius, radius + additionalMove)
                };
                childTransform.localPosition = new Vector3(alignedPosition.x, 0.0f, alignedPosition.y);
            }
            
            _radius = ConvertOuterRadiusToInnerByType(_radiusType, radius) + additionalMove;
        }

        private static float GetOuterRadiusByInner(float radius)
        {
            return radius * 2 / Mathf.Sqrt(3);
        }
        
        private static float GetInnerRadiusByOuter(float radius)
        {
            return Mathf.Sqrt(3) * radius / 2;
        }

        private static float ConvertInnerRadiusToOuterByType(HexRadius radiusType, float radius)
        {
            return radiusType switch
            {
                HexRadius.Outer => radius,
                HexRadius.Inner => GetOuterRadiusByInner(radius),
                _ => radius
            };
        }
        
        private static float ConvertOuterRadiusToInnerByType(HexRadius radiusType, float radius)
        {
            return radiusType switch
            {
                HexRadius.Outer => radius,
                HexRadius.Inner => GetInnerRadiusByOuter(radius),
                _ => radius
            };
        }
        
        private static Vector2 GetAlignedPositionFlatTop(Vector3 position, float currentRadius, float expectedRadius)
        {
            var q = 2.0f / 3 * position.x / currentRadius;
            var r = (-1.0f / 3 * position.x + Mathf.Sqrt(3) / 3 * position.z) / currentRadius;
            var s = -q - r;
            
            var roundedX = Mathf.RoundToInt(q);
            var roundedY = Mathf.RoundToInt(r);
            var roundedZ = Mathf.RoundToInt(s);
            
            var xDiff = Mathf.Abs(roundedX - q);
            var yDiff = Mathf.Abs(roundedY - r);
            var zDiff = Mathf.Abs(roundedZ - s);
            
            if (xDiff > yDiff && xDiff > zDiff)
            {
                roundedX = -roundedY - roundedZ;
            }
            else if (yDiff > zDiff)
            {
                roundedY = -roundedX - roundedZ;
            }
            
            var x = expectedRadius * (3.0f / 2 * roundedX);
            var y = expectedRadius * (Mathf.Sqrt(3) / 2 * roundedX + Mathf.Sqrt(3) * roundedY);

            return new Vector2(x, y);
        }
        
        private static Vector2 GetAlignedPositionPointyTop(Vector3 position, float currentRadius, float expectedRadius)
        {
            var q = (Mathf.Sqrt(3)/3 * position.x - 1.0f/3 * position.z) / currentRadius;
            var r = 2.0f/3 * position.z / currentRadius;
            var s = -q - r;
            
            var roundedX = Mathf.RoundToInt(q);
            var roundedY = Mathf.RoundToInt(r);
            var roundedZ = Mathf.RoundToInt(s);
            
            var xDiff = Mathf.Abs(roundedX - q);
            var yDiff = Mathf.Abs(roundedY - r);
            var zDiff = Mathf.Abs(roundedZ - s);
            
            if (xDiff > yDiff && xDiff > zDiff)
            {
                roundedX = -roundedY - roundedZ;
            }
            else if (yDiff > zDiff)
            {
                roundedY = -roundedX - roundedZ;
            }

            var x = expectedRadius * (Mathf.Sqrt(3) * roundedX + Mathf.Sqrt(3) / 2 * roundedY);
            var y = expectedRadius * (3.0f / 2 * roundedY);

            return new Vector2(x, y);
        }
    }
}
