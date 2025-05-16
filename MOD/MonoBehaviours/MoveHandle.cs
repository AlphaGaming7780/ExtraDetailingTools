using ExtraLib;
using Game.Prefabs;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ExtraDetailingTools.MonoBehaviours
{
    public class MoveHandle : MonoBehaviour
    {

        GameObject xAxis;
        GameObject yAxis;
        GameObject zAxis;

        GameObject currentAxis;
        Vector3 initialClickPoint;
        Vector3 initialObjectPosition;

        float3 linesLenght;

        Material _material;

        public void Start()
        {
            //gameObject.SetActive(false);
            gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
            enabled = false;

            _material = new Material(Shader.Find("Shader Graphs/OverlayCurve"));

            //Shader[] shaders = Resources.FindObjectsOfTypeAll<Shader>();

            //foreach (Shader shader in shaders)
            //{
            //    EDT.Logger.Info(shader.name);
                
            //}

            //for (int i = 0; i < 32; i++)
            //{
            //    EDT.Logger.Info(LayerMask.LayerToName(i));
            //}
        }

        public void OnDestroy()
        {
            DestroyAxisHandles();
        }

        public void Update()
        {
            if (xAxis == null || yAxis == null || zAxis == null) 
            {
                EDT.Logger.Info("Objects not created");
                return;
            }

            if (currentAxis == null && Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    initialClickPoint = hit.point;
                    initialObjectPosition = gameObject.transform.position;
                    if (hit.collider.gameObject == xAxis)
                        currentAxis = xAxis;
                    else if (hit.collider.gameObject == yAxis)
                        currentAxis = yAxis;
                    else if (hit.collider.gameObject == zAxis)
                        currentAxis = zAxis;
                    else
                        currentAxis = null;
                }
            }

            if (currentAxis != null && Input.GetMouseButtonUp(0))
            {
                currentAxis = null;
            }

            if (currentAxis != null && Input.GetMouseButton(0))
            {
                Plane dragPlane = new Plane(Camera.main.transform.forward, gameObject.transform.position);
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (dragPlane.Raycast(ray, out float distance))
                {
                    Vector3 hitPoint = ray.GetPoint(distance);
                    Vector3 delta = hitPoint - initialClickPoint;

                    delta = Vector3.Project(delta, currentAxis.transform.rotation * Vector3.up);

                    gameObject.transform.position = initialObjectPosition + delta;
                }
            }
        }

        public void Setup(Vector3 position, Quaternion rotation, float3 lineslenght)
        {

            if(enabled) DestroyAxisHandles();

            linesLenght = lineslenght;
            gameObject.transform.position = position;
            //gameObject.transform.rotation = rotation;

            CreateAxisHandles();
        }

        public void DestroyAxisHandles()
        {
            foreach (Transform child in gameObject.transform)
            {
                Destroy(child.gameObject);
            }

            xAxis = null;
            yAxis = null;
            zAxis = null;
            currentAxis = null;
            enabled = false;
        }

        public void CreateAxisHandles()
        {
            xAxis = CreateAxisHandle(Vector3.right, Color.red, linesLenght.x);
            yAxis = CreateAxisHandle(Vector3.up, Color.green, linesLenght.y);
            zAxis = CreateAxisHandle(Vector3.forward, Color.blue, linesLenght.z);
            currentAxis = null;
            enabled = true;
        }

        GameObject CreateAxisHandle(Vector3 direction, Color color, float linelenght = 100f)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.transform.localScale = new Vector3(0.1f, linelenght, 0.1f);
            line.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            MeshRenderer meshRendere = line.GetComponent<MeshRenderer>();

            if(meshRendere.material != null) Destroy(meshRendere.material);

            //OverlayConfigurationPrefab singletonPrefab = EL.m_PrefabSystem.GetSingletonPrefab<OverlayConfigurationPrefab>(GetEntityQuery(ComponentType.ReadOnly<OverlayConfigurationData>()););
            //material = new Material(singletonPrefab.m_CurveMaterial);

            meshRendere.material = new(_material);

            //foreach (string s in meshRendere.material.GetPropertyNames(UnityEngine.MaterialPropertyType.Int)) { EDT.Logger.Info($"{meshRendere.material.name} | Int : {s}"); }
            //foreach (string s in meshRendere.material.GetPropertyNames(UnityEngine.MaterialPropertyType.Float)) { EDT.Logger.Info($"{meshRendere.material.name} | Float : {s}"); }
            //foreach (string s in meshRendere.material.GetPropertyNames(UnityEngine.MaterialPropertyType.Vector)) { EDT.Logger.Info($"{meshRendere.material.name} | Vector : {s}"); }
            //foreach (string s in meshRendere.material.GetPropertyNames(UnityEngine.MaterialPropertyType.Texture)) { EDT.Logger.Info($"{meshRendere.material.name} | Texture : {s}"); }
            //foreach (string s in meshRendere.material.GetPropertyNames(UnityEngine.MaterialPropertyType.Matrix)) { EDT.Logger.Info($"{meshRendere.material.name} | Matrix : {s}"); }
            //foreach (string s in meshRendere.material.GetPropertyNames(UnityEngine.MaterialPropertyType.ConstantBuffer)) { EDT.Logger.Info($"{meshRendere.material.name} | ConstantBuffer : {s}"); }
            //foreach (string s in meshRendere.material.GetPropertyNames(UnityEngine.MaterialPropertyType.ComputeBuffer)) { EDT.Logger.Info($"{meshRendere.material.name} | ComputeBuffer : {s}"); }

            //meshRendere.material = new Material(Shader.Find("HDRP/Unlit"));
            //meshRendere.material = new Material(Shader.Find("BH/Overlay/CurvedOverlayShader"));
            //meshRendere.material.color = color;
            meshRendere.material.SetVector("_EmissionColor", color);
            line.transform.SetParent(gameObject.transform);
            line.transform.localPosition = line.transform.rotation * line.transform.localScale / 2;

            //GameObject arrow = new();
            //MeshFilter meshFilter = arrow.AddComponent<MeshFilter>();
            //meshFilter.mesh = GenerateCone(0.1f, 0.5f, 10);
            //MeshRenderer meshRenderer = arrow.AddComponent<MeshRenderer>();
            //meshRenderer.material.color = color;
            //arrow.layer = LayerMask.NameToLayer("MoveHandle");
            //arrow.transform.SetParent(line.transform);

            return line;
        }

        Mesh GenerateCone(float radius, float height, int numSegments)
        {
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[numSegments + 2];
            int[] triangles = new int[numSegments * 3 * 2];
            vertices[0] = Vector3.zero;
            vertices[vertices.Length - 1] = new Vector3(0, height, 0);

            float angleStep = 360.0f / numSegments;
            for (int i = 0; i < numSegments; i++)
            {
                float angle = Mathf.Deg2Rad * angleStep * i;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 2) % numSegments + 1;

                triangles[numSegments * 3 + i * 3] = vertices.Length - 1;
                triangles[numSegments * 3 + i * 3 + 1] = (i + 2) % numSegments + 1;
                triangles[numSegments * 3 + i * 3 + 2] = i + 1;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

    }
}
