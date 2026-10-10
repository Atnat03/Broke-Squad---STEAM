
using System.Collections.Generic;
using Gameplay.IA.Scripts;
using UnityEngine;

namespace Gameplay.IA
{
    public class GuardLineOfSight_View : MonoBehaviour
    {
        [SerializeField] private GuardLineOfSight _lineOfSight;

        [Header("Visual Sight")]
        [SerializeField] private Transform[] _eyesPosition;
        [SerializeField] private Transform _parentSightModel;
        [SerializeField] private Material _sightMeshMaterial;
        [SerializeField] private float _originYSize = 1f;
        [SerializeField] private float _farYSize = 2f;

        private Mesh _mesh;
        private readonly List<Vector3> _vertices = new();
        private readonly List<int> _tri = new();

        private void Start()
        {
            CreateMesh();

            _mesh = new Mesh
            {
                name = "LineOfSight Mesh",
                vertices = _vertices.ToArray(),
                triangles = _tri.ToArray()
            };

            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            GameObject g = new GameObject("LineOfSight MESH");
            g.transform.SetParent(_parentSightModel, false);

            g.AddComponent<MeshFilter>().mesh = _mesh;
            g.AddComponent<MeshRenderer>().material = _sightMeshMaterial;
        }

        private void CreateMesh()
        {
            Transform leftEye = _eyesPosition[0];
            Transform rightEye = _eyesPosition[1];

            Vector3 originLeft = leftEye.position;
            Vector3 originRight = rightEye.position;

            Vector3 left = originLeft + Quaternion.AngleAxis(_lineOfSight.Angle * 0.5f, Vector3.up) * leftEye.forward * _lineOfSight.Radius;
            Vector3 right = originRight + Quaternion.AngleAxis(-_lineOfSight.Angle * 0.5f, Vector3.up) * rightEye.forward * _lineOfSight.Radius;

            Vector3 yo = Vector3.up * (_originYSize * 0.5f);
            Vector3 yf = Vector3.up * (_farYSize * 0.5f);

            //Top
            DrawQuadVision(originLeft + yo, originRight + yo, right + yf, left + yf);
            //Bottom
            DrawQuadVision(originLeft - yo, originRight - yo, right - yf, left - yf);
            //Left
            DrawQuadVision(originLeft + yo, left + yf, left - yf, originLeft - yo);
            //Right
            DrawQuadVision(originRight + yo, right + yf, right - yf, originRight - yo);
            //Front
            DrawQuadVision(left + yf, right + yf, right - yf, left - yf); 
        }


        private void DrawQuadVision(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // Centre de la face
            Vector3 center = (p0 + p1 + p2 + p3) * 0.25f;

            // Centre approximatif du volume de vision
            Vector3 volumeCenter = (
                _eyesPosition[0].position +
                _eyesPosition[1].position +
                GetFarLeft() +
                GetFarRight()
            ) * 0.25f;

            // Normale actuelle de la face
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);

            // Si la normale pointe vers l'intérieur, inverser les sommets
            if (Vector3.Dot(normal, center - volumeCenter) < 0f)
            {
                (p1, p3) = (p3, p1);
            }

            int start = _vertices.Count;

            _vertices.Add(_parentSightModel.InverseTransformPoint(p0));
            _vertices.Add(_parentSightModel.InverseTransformPoint(p1));
            _vertices.Add(_parentSightModel.InverseTransformPoint(p2));
            _vertices.Add(_parentSightModel.InverseTransformPoint(p3));

            _tri.Add(start);
            _tri.Add(start + 1);
            _tri.Add(start + 2);

            _tri.Add(start);
            _tri.Add(start + 2);
            _tri.Add(start + 3);
        }

        private Vector3 GetFarLeft()
        {
            Transform eye = _eyesPosition[0];

            return eye.position + Quaternion.AngleAxis(-_lineOfSight.Angle * 0.5f, Vector3.up) * eye.forward * _lineOfSight.Radius;
        }

        private Vector3 GetFarRight()
        {
            Transform eye = _eyesPosition[1];

            return eye.position + Quaternion.AngleAxis(_lineOfSight.Angle * 0.5f, Vector3.up) * eye.forward * _lineOfSight.Radius;
        }

    }
}
